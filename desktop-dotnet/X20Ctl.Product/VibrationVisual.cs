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
        var islands = Flow.Islands(model);
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

        // owner, 13 Oct 2026: the areas he marked on every model (each grip, and the bumpers on the X20 family), cleaned so
        // the outer side follows the controller's real edge. Fractions of the 1536 x 1024 photo, so they scale with it.
        public static List<Island> Islands(string model)
        {
            var list = new List<Island>();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "vibration-outlines.json")));
                if (!doc.RootElement.GetProperty("models").TryGetProperty(model, out var shapes)) return list;
                foreach (var shape in shapes.EnumerateArray())
                {
                    var pts = shape.GetProperty("points").EnumerateArray().Select(q => new Point(q[0].GetDouble() * 1536, q[1].GetDouble() * 1024)).ToList();
                    if (pts.Count < 8) continue;
                    pts.Add(pts[0]); // every outline is a closed loop
                    var a = pts.ToArray(); var along = new double[a.Length];
                    for (int k = 1; k < a.Length; k++) along[k] = along[k - 1] + (a[k] - a[k - 1]).Length;
                    var line = new StreamGeometry();
                    using (var ctx = line.Open()) { ctx.BeginFigure(a[0], false, true); ctx.PolyLineTo(a.Skip(1).ToList(), true, true); }
                    line.Freeze();
                    list.Add(new Island(a, along, along[^1], shape.GetProperty("side").GetString() == "left", shape.GetProperty("part").GetString() == "grip", line));
                }
            }
            catch (Exception e) when (e is System.IO.IOException or System.Text.Json.JsonException or KeyNotFoundException or InvalidOperationException) { }
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
            // owner's reference, 13 Oct 2026: two coincident layers on each outline. A thin permanent cyan rail, and over it a
            // short, thicker pulse that carries the controller's RGB colours (blue, cyan, magenta, orange), feathered at both
            // ends, travelling round the loop. Strength sets how fast and how often the pulse runs and how bright it is.
            // The bumper outlines echo their grip about 80 ms later. Today one strength drives both sides; each side keeps its
            // own clock so separate motor values can drive them apart.
            double left = s, right = s;
            var rail = Color.FromRgb(70, 200, 255);
            foreach (var part in Parts)
            {
                double m = part.Left ? left : right; if (m <= 0) continue;
                double railCore = part.Grip ? 4.5 : 3.5;
                byte railAlpha = (byte)((.45 + .25 * m) * 255);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(railAlpha * .22), rail.R, rail.G, rail.B)), railCore + 6) { LineJoin = PenLineJoin.Round }, part.Line);
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(railAlpha, rail.R, rail.G, rail.B)), railCore) { LineJoin = PenLineJoin.Round }, part.Line);
                double q = .4, period = 2.6 - 1.8 * m, travel = .7 + .25 * m;
                if (moving)
                {
                    double t = time - (part.Grip ? 0 : .08) + (part.Left ? 0 : .37);
                    double cycle = t / period + .04 * Math.Sin(t * .9 + (part.Left ? 0 : 2)), phase = cycle - Math.Floor(cycle);
                    q = phase / travel;
                    if (m > .6) Pulse(dc, part, ((phase - .5 + 1) % 1) / travel, m, .45 * (m - .6) / .4, time);
                }
                Pulse(dc, part, q, m, 1, moving ? time : 0);
            }
        }
        private static Color Rgb(double t)
        {
            t -= Math.Floor(t);
            Color[] stops = { Color.FromRgb(60, 120, 255), Color.FromRgb(80, 225, 255), Color.FromRgb(255, 80, 210), Color.FromRgb(255, 155, 60), Color.FromRgb(60, 120, 255) };
            double x = t * 4; int i = Math.Min((int)x, 3); return Mix(stops[i], stops[i + 1], x - i);
        }
        /// <summary>One pulse at progress q round the loop (outside 0..1 it is resting): a short bright strip of RGB light with
        /// feathered ends, a soft bloom under it, easing in as it starts and out as it ends.</summary>
        private static void Pulse(DrawingContext dc, Island part, double q, double m, double weight, double time)
        {
            if (q is < 0 or > 1 || weight <= 0) return;
            double envelope = q < .12 ? Math.Sin(q / .12 * Math.PI / 2) : q > .88 ? Math.Sin((1 - q) / .12 * Math.PI / 2) : 1;
            double span = part.Length * (part.Grip ? .2 : .42), head = part.Length * q;
            double core = part.Grip ? 9 : 7, bloom = part.Grip ? 18 : 13;
            for (int i = 0; i + 1 < part.Points.Length; i++)
            {
                double behind = head - part.Along[i]; if (behind < 0) behind += part.Length; // a loop: the tail wraps past the start
                if (behind > span) continue;
                double x = 1 - behind / span, feather = Math.Pow(Math.Sin(x * Math.PI), .8);
                double a = envelope * feather * weight * (.6 + .4 * m);
                var c = Rgb(part.Along[i] / part.Length * .9 + time * .05);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(80 * a, 0, 255), c.R, c.G, c.B)), bloom) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, part.Points[i], part.Points[i + 1]);
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
