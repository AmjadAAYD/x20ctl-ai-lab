using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using X20Ctl.Zone;
namespace X20Ctl.Product;

/// <summary>Owned-silhouette visual preview driven by a local draft, never motor telemetry.
/// Since 10 Oct 2026 (owner: "just keep it as a fast heartbeat") each motor's outline beats, lub-dub, brightening and
/// swelling a touch on each thump, faster as the strength rises (72 to 180 bpm). Both grips beat together; only the
/// X20 Pro and the Dune have trigger motors, so only they beat on the triggers too. The body stays still.
/// Reduced motion shows the same picture held still.</summary>
public sealed class VibrationVisual : Grid
{
    private readonly bool animations;
    private readonly Flow behind, over;
    private int strength;
    private TimeSpan started, last;
    private bool rendering;
    private readonly TranslateTransform shake = new();
    public int Strength => strength;
    public bool IsRunning { get; private set; }
    public bool ReducedMotion => !animations;
    public VibrationVisualProfile Profile => VibrationVisualProfile.For(strength);
    public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(nameof(Phase), typeof(double), typeof(VibrationVisual), new FrameworkPropertyMetadata(0d));
    public double Phase { get => (double)GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }
    /// <summary>Preview motor levels 0..1 (heavy left, light right), for the page's meters.</summary>
    public event Action<double, double>? Levels;

    public VibrationVisual(Geometry outline, bool animate, string model = "x20")
    {
        animations = animate; IsHitTestVisible = false; Width = 1536; Height = 1024;
        var tracks = Flow.Load(model);
        var rim = Flow.LoadRim(model);
        var islands = Flow.Islands(rim);
        behind = new Flow(tracks, false) { Rim = rim }; over = new Flow(tracks, true) { Rim = rim, Parts = islands };
        Children.Add(behind); Children.Add(new PhotoController(model)); Children.Add(over);
        RenderTransform = shake;
        Loaded += (_, _) => UpdateClock(); Unloaded += (_, _) => Stop(); IsVisibleChanged += (_, _) => UpdateClock();
    }
    public void SetStrength(int percent) { if (percent == strength) return; strength = percent; UpdateClock(); }

    private void Stop()
    {
        if (rendering) { CompositionTarget.Rendering -= Tick; rendering = false; }
        IsRunning = false; Phase = 0; shake.X = shake.Y = 0;
        behind.Draw(strength, 0, false); over.Draw(strength, 0, false); Levels?.Invoke(0, 0);
    }
    private void UpdateClock()
    {
        Stop();
        if (strength == 0 || !IsVisible) return;
        if (!animations) { double s = strength / 100.0; Levels?.Invoke(s, s); return; }
        IsRunning = true; started = TimeSpan.Zero; rendering = true; CompositionTarget.Rendering += Tick;
    }
    private void Tick(object? sender, EventArgs e)
    {
        var now = ((RenderingEventArgs)e).RenderingTime; if (now == last) return; last = now;
        if (started == TimeSpan.Zero) started = now;
        double t = (now - started).TotalSeconds, s = strength / 100.0;
        Phase = t % 1;
        behind.Draw(strength, t, true); over.Draw(strength, t, true);
        // the meters breathe with the preview: the heavy motor rolls slowly, the light one flutters
        // a motor holds a level: the meters are fixed at the strength, and the picture shakes, harder as it rises
        Levels?.Invoke(s, s);

    }

    /// <summary>A contour resampled at an even step, with outward normals, so light can travel along it smoothly.</summary>
    private sealed class Track
    {
        public required Point[] Points; public required Vector[] Normals; public required Geometry Shape; public required Point Center;
        public required int Direction; public required bool Grip;
    }

    /// <summary>The light itself. The back layer is the warmth behind each grip; the front layer is the living outline.</summary>
    private sealed class Flow : FrameworkElement
    {
        private readonly List<Track> tracks; private readonly bool front;
        private int strength; private double time; private bool moving;
        private static readonly Color Deep = Color.FromRgb(56, 132, 255), Ice = Color.FromRgb(170, 214, 255), White = Color.FromRgb(240, 247, 255);
        public Track? Rim { get; init; }
        public List<Island> Parts { get; init; } = [];

        /// <summary>One of the four separate pieces that light up: a grip pad's loop or a bumper cap, resampled for the
        /// travelling pulse, with its base line built once.</summary>
        public sealed record Island(Point[] Points, double[] Along, double Length, bool Left, bool Grip, Geometry Line);

        // owner, 13 Oct 2026: four islands, never one outline. Traced from his blue lines on the X20 Pro, as fractions of the
        // controller's own outline box, so they scale with it and carry over to the other models. The right side mirrors the left.
        private static readonly Point[] GripLoop = { new(.0775, .4233), new(.1224, .464), new(.1769, .5148), new(.2381, .5718), new(.2286, .6003), new(.2109, .6572), new(.1837, .759), new(.1496, .8403), new(.1224, .9014), new(.0952, .9217), new(.0612, .9217), new(.0271, .881), new(.0108, .82), new(.0135, .7386), new(.0244, .6369), new(.0407, .5352), new(.0612, .464) };
        private static readonly Point[] ShoulderCap = { new(.1633, .0631), new(.1973, .0265), new(.2449, .0062), new(.2858, .0021), new(.3062, .0224) };

        public static List<Island> Islands(Track? rim)
        {
            var list = new List<Island>(); if (rim == null) return list;
            var box = rim.Shape.Bounds;
            foreach (bool left in new[] { true, false })
                foreach (var (shape, closed) in new[] { (GripLoop, true), (ShoulderCap, false) })
                {
                    var pts = Smooth(shape.Select(q => new Point(box.X + (left ? q.X : 1 - q.X) * box.Width, box.Y + q.Y * box.Height)).ToArray(), closed);
                    var along = new double[pts.Length];
                    for (int i = 1; i < pts.Length; i++) along[i] = along[i - 1] + (pts[i] - pts[i - 1]).Length;
                    var line = new StreamGeometry();
                    using (var ctx = line.Open()) { ctx.BeginFigure(pts[0], false, false); ctx.PolyLineTo(pts.Skip(1).ToList(), true, true); }
                    line.Freeze();
                    list.Add(new Island(pts, along, along[^1], left, shape == GripLoop, line));
                }
            return list;
        }
        /// <summary>Catmull-Rom through the traced points, so the contour reads as fitted, not hand drawn.</summary>
        private static Point[] Smooth(Point[] p, bool closed)
        {
            var o = new List<Point>(); int m = p.Length, steps = 12;
            for (int i = 0; i < (closed ? m : m - 1); i++)
            {
                Point p0 = closed || i > 0 ? p[(i - 1 + m) % m] : p[0], p1 = p[i], p2 = p[(i + 1) % m], p3 = closed || i + 2 < m ? p[(i + 2) % m] : p[m - 1];
                for (int k = 0; k < steps; k++)
                {
                    double t = (double)k / steps, t2 = t * t, t3 = t2 * t;
                    o.Add(new Point(.5 * (2 * p1.X + (-p0.X + p2.X) * t + (2 * p0.X - 5 * p1.X + 4 * p2.X - p3.X) * t2 + (-p0.X + 3 * p1.X - 3 * p2.X + p3.X) * t3),
                                    .5 * (2 * p1.Y + (-p0.Y + p2.Y) * t + (2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * t2 + (-p0.Y + 3 * p1.Y - 3 * p2.Y + p3.Y) * t3)));
                }
            }
            o.Add(closed ? p[0] : p[m - 1]); return o.ToArray();
        }
        /// <summary>The controller's whole outline (the lighting silhouette), resampled so light can run round it.</summary>
        public static Track? LoadRim(string model)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "lighting.json")));
                string d = doc.RootElement.GetProperty("models").GetProperty(model).GetProperty("front").GetString()!;
                return Build(Geometry.Parse(d), 1, false);
            }
            catch (Exception e) when (e is System.IO.IOException or System.Text.Json.JsonException or KeyNotFoundException or FormatException or InvalidOperationException) { return null; }
        }
        private static Color Hue(double t)
        {
            t -= Math.Floor(t);
            Color[] stops = { Color.FromRgb(70, 225, 255), Color.FromRgb(70, 120, 255), Color.FromRgb(255, 90, 215), Color.FromRgb(255, 160, 60), Color.FromRgb(70, 225, 255) };
            double x = t * 4; int i = (int)x; return Mix(stops[i], stops[Math.Min(i + 1, 4)], x - i);
        }
        public Flow(List<Track> tracks, bool front) { this.tracks = tracks; this.front = front; Width = 1536; Height = 1024; IsHitTestVisible = false; }
        public void Draw(int s, double t, bool animate) { strength = s; time = t; moving = animate; InvalidateVisual(); }

        /// <summary>The motors live in the grips (and the triggers, on models that have trigger motors): the model's
        /// haptics contours (src/assets/controllers/&lt;model&gt;/haptics-geometry.json) are where the light runs.</summary>
        public static List<Track> Load(string model)
        {
            var list = new List<Track>();
            try
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "haptics", model, "haptics-geometry.json");
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
                var c = doc.RootElement.GetProperty("contours");
                bool triggers = model is "x20_pro" or "dune"; // owner, 10 Oct 2026: every other model has the same two grip rotors
                foreach (var (key, direction, grip) in new[] { ("left_grip", 1, true), ("right_grip", -1, true), ("left_trigger", 1, false), ("right_trigger", -1, false) }.Where(k => k.Item3 || triggers))
                    if (c.TryGetProperty(key, out var data) && data.GetString() is { Length: > 0 } text && Build(Geometry.Parse(text), direction, grip) is { } track) list.Add(track);
            }
            catch (Exception e) when (e is System.IO.IOException or System.Text.Json.JsonException or KeyNotFoundException or FormatException or InvalidOperationException) { }
            return list;
        }
        private static Track? Build(Geometry shape, int direction, bool grip)
        {
            shape.Freeze();
            var flat = shape.GetFlattenedPathGeometry(.5, ToleranceType.Absolute);
            var raw = new List<Point>();
            foreach (var figure in flat.Figures)
            {
                if (raw.Count > 0) break; // one closed contour per motor
                raw.Add(figure.StartPoint);
                foreach (var segment in figure.Segments)
                    if (segment is PolyLineSegment poly) raw.AddRange(poly.Points); else if (segment is LineSegment line) raw.Add(line.Point);
            }
            if (raw.Count < 4) return null;
            raw.Add(raw[0]);
            // even 3 px steps
            const double step = 3;
            var points = new List<Point> { raw[0] }; double carry = 0;
            for (int i = 1; i < raw.Count; i++)
            {
                Vector d = raw[i] - raw[i - 1]; double len = d.Length; if (len < 1e-6) continue; d /= len;
                double at = step - carry;
                while (at <= len) { points.Add(raw[i - 1] + d * at); at += step; }
                carry = len - (at - step);
            }
            if (points.Count < 8) return null;
            var b = shape.Bounds; var center = new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
            var normals = new Vector[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                Vector t = points[(i + 1) % points.Count] - points[(i - 1 + points.Count) % points.Count];
                var n = new Vector(t.Y, -t.X); if (n.Length > 0) n.Normalize();
                if (Vector.Multiply(n, points[i] - center) < 0) n = -n;
                normals[i] = n;
            }
            return new Track { Points = points.ToArray(), Normals = normals, Shape = shape, Center = center, Direction = direction, Grip = grip };
        }

        /// <summary>A heartbeat, lub then dub: 0..1 over one beat period.</summary>
        public static double Beat(double t, double s)
        {
            double period = 60 / (72 + 108 * s); // 72 bpm at the lightest, 180 bpm at 100%
            double p = t % period / period;
            static double Thump(double x) => x < 0 ? 0 : (1 - Math.Exp(-x / .018)) * Math.Exp(-x / .09);
            return Math.Clamp((Thump(p) + .62 * Thump(p - .30)) / .78, 0, 1);
        }
        protected override void OnRender(DrawingContext dc)
        {
            if (strength == 0 || tracks.Count == 0) return;
            // owner direction 10 Oct 2026: no comets or ripples, just a fast heartbeat on each motor's outline
            double s = strength / 100.0, beat = .5;
            if (!front || Parts.Count == 0) return;
            // A faint base line on each island, and a thin pulse of energy travelling along it. Strength sets how often the
            // pulse comes, how fast it runs and how bright it is; above 60 % a second, weaker pulse follows, and near 100 %
            // the lines (never the controller) tremble by under a pixel. The bumper caps echo their grip ~80 ms later.
            // Today one strength drives both motors; each side keeps its own clock so a split strength can drive them apart.
            double left = s, right = s;
            foreach (var part in Parts)
            {
                double m = part.Left ? left : right; if (m <= 0) continue;
                double weight = part.Grip ? 1 : .6;
                Color deep = part.Left ? Color.FromRgb(40, 95, 210) : Color.FromRgb(175, 92, 24), mid = part.Left ? Color.FromRgb(80, 205, 255) : Color.FromRgb(255, 150, 52), hot = part.Left ? Color.FromRgb(215, 246, 255) : Color.FromRgb(255, 228, 186);
                bool tremble = moving && m > .75;
                if (tremble) { double k = (m - .75) / .25 * .6; dc.PushTransform(new TranslateTransform(k * Math.Sin(time * 91 + (part.Left ? 0 : 1.7)), k * .7 * Math.Sin(time * 77 + 0.6))); }
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)((.15 + .2 * m) * weight * 255), mid.R, mid.G, mid.B)), 1.3) { LineJoin = PenLineJoin.Round }, part.Line);
                if (moving)
                {
                    double period = 2.6 - 1.9 * m, travel = .6 + .3 * m;
                    double t = time - (part.Grip ? 0 : .08) + (part.Left ? 0 : .37);
                    double cycle = t / period + .04 * Math.Sin(t * .9 + (part.Left ? 0 : 2)), phase = cycle - Math.Floor(cycle);
                    Pulse(dc, part, phase / travel, m, weight, deep, mid, hot);
                    if (m > .6) Pulse(dc, part, (phase - .45 + 1) % 1 / travel, m, weight * .45 * (m - .6) / .4, deep, mid, hot);
                }
                else Pulse(dc, part, .45, m, weight, deep, mid, hot);
                if (tremble) dc.Pop();
            }
        }
        /// <summary>One travelling pulse at progress q (0..1 along the island; outside that range it is resting): a short bright
        /// head with a fading tail, easing in at the start and out at the end.</summary>
        private static void Pulse(DrawingContext dc, Island part, double q, double m, double weight, Color deep, Color mid, Color hot)
        {
            if (q is < 0 or > 1 || weight <= 0) return;
            double envelope = q < .15 ? Math.Sin(q / .15 * Math.PI / 2) : q > .85 ? Math.Sin((1 - q) / .15 * Math.PI / 2) : 1;
            double head = part.Length * q, tail = part.Length * (part.Grip ? .14 + .08 * m : .35);
            double core = 1.6 + 1.2 * m;
            for (int i = 0; i + 1 < part.Points.Length; i++)
            {
                double d = part.Along[i]; if (d > head || d < head - tail) continue;
                double w = Math.Pow((d - (head - tail)) / tail, 1.5), a = envelope * w * weight * (.55 + .45 * m);
                var c = w > .7 ? Mix(mid, hot, (w - .7) / .3) : Mix(deep, mid, w / .7);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(60 * a, 0, 255), c.R, c.G, c.B)), core + 4 + 3 * m) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, part.Points[i], part.Points[i + 1]);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(255 * a, 0, 255), c.R, c.G, c.B)), core) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, part.Points[i], part.Points[i + 1]);
            }
        }        /// <summary>A polyline through <paramref name="count"/> points starting at <paramref name="start"/>, wrapping round the contour.</summary>
        private static StreamGeometry Poly(Point[] points, int start, int count, bool closed)
        {
            int n = points.Length; var g = new StreamGeometry();
            using (var ctx = g.Open())
            {
                ctx.BeginFigure(points[((start % n) + n) % n], false, closed);
                var rest = new List<Point>(count);
                for (int i = 1; i < count; i++) rest.Add(points[(((start + i) % n) + n) % n]);
                ctx.PolyLineTo(rest, true, true);
            }
            g.Freeze(); return g;
        }
        private static Color Mix(Color a, Color b, double k) => Color.FromRgb((byte)(a.R + (b.R - a.R) * k), (byte)(a.G + (b.G - a.G) * k), (byte)(a.B + (b.B - a.B) * k));
    }
}
