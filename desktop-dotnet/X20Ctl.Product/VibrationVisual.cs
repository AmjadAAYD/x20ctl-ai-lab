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
        behind = new Flow(tracks, false); over = new Flow(tracks, true);
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
        double amp = .5 + 2.6 * s;
        shake.X = amp * Math.Sin(t * 211) * Math.Cos(t * 97); shake.Y = amp * .7 * Math.Sin(t * 173 + 1);
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
            if (!front)
            {
                foreach (var track in tracks.Where(k => k.Grip))
                {
                    double r = 230 + 30 * beat; byte a = (byte)Math.Clamp((.08 + .26 * beat) * (.4 + .6 * s) * 255, 0, 255);
                    dc.DrawEllipse(new RadialGradientBrush(Color.FromArgb(a, Deep.R, Deep.G, Deep.B), Color.FromArgb(0, Deep.R, Deep.G, Deep.B)), null, new Point(track.Center.X, track.Center.Y + 40), r, r * .82);
                }
                return;
            }
            // owner, 13 Oct 2026, with the maker's own motor-haptics picture as the reference: real cylindrical motors in the
            // grips and triggers, a blue field of vibration arcs round each, and a stepped level bar. Everything holds at the
            // draft strength: a weight spins at a steady speed and the motor itself trembles, nothing swells or beats.
            double rate = 3 + 13 * s, angle = moving ? time * rate * Math.Tau : .6, level = .5 + .5 * s;
            double ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var face = new Typeface("Segoe UI Semibold");
            FormattedText Label(string text, double size, Color color) => new(text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, face, size, new SolidColorBrush(color), ppd);
            var blue = Color.FromRgb(70, 160, 255); var ice = Color.FromRgb(190, 225, 255);
            var steel = new LinearGradientBrush(new GradientStopCollection {
                new(Color.FromRgb(22, 48, 104), 0), new(Color.FromRgb(160, 198, 248), .32), new(Color.FromRgb(74, 116, 200), .66), new(Color.FromRgb(16, 34, 78), 1) }, 0);
            // owner, 13 Oct 2026, from his own marked-up screenshot: no motor object. The grip pads themselves vibrate: ripples
            // radiate from the middle of each pad out to its edge at a steady pace (faster with strength), the pad glows from
            // inside and a rim of light follows its edge. Nothing swells or beats.
            double freq = .5 + 1.5 * s;
            foreach (var track in tracks)
            {
                var c = track.Center;
                // a little larger than the traced contour: the pads reach further down and out than the outline does
                var pad = track.Shape.Clone(); pad.Transform = new ScaleTransform(1.12, 1.12, c.X, c.Y);
                var tb = pad.Bounds;
                dc.PushClip(pad);
                dc.DrawRectangle(new RadialGradientBrush(Color.FromArgb((byte)(120 * level), 120, 200, 255), Color.FromArgb((byte)(26 * level), 40, 110, 255)) { Center = new Point(.5, .5), RadiusX = .6, RadiusY = .6 },
                    null, new Rect(tb.X - 4, tb.Y - 4, tb.Width + 8, tb.Height + 8));
                for (int k = 0; k < 4; k++)
                {
                    double p = moving ? (time * freq + k / 4.0) % 1 : (.15 + k * .25), sc = .2 + .95 * p;
                    var ring = Mix(Color.FromRgb(225, 244, 255), Color.FromRgb(70, 150, 255), p);
                    dc.PushTransform(new ScaleTransform(sc * 1.12, sc * 1.12, c.X, c.Y));
                    dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(235 * Math.Pow(1 - p, 1.1) * level, 0, 255), ring.R, ring.G, ring.B)), (5.5 - 3.6 * p) / sc), track.Shape);
                    dc.Pop();
                }
                dc.Pop();
                // the rim of light along the pad's edge
                dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(120 + 110 * s), 150, 215, 255)), 2.6) { LineJoin = PenLineJoin.Round }, pad);
            }
            // the level bar: five steps, 0 to 100 %, filled to the draft strength and held there
            var motors = tracks.Where(t => t.Grip).DefaultIfEmpty(tracks[0]).ToList();
            double midX = motors.Average(t => t.Center.X), baseY = tracks.Max(t => t.Shape.Bounds.Bottom) + 30;
            const double seg = 124, bar = 22; double x0 = midX - seg * 2.5;
            for (int n = 0; n < 5; n++)
            {
                double x = x0 + n * seg;
                var cell = new StreamGeometry();
                using (var ctx = cell.Open())
                {
                    ctx.BeginFigure(new Point(x + 12, baseY), true, true);
                    ctx.LineTo(new Point(x + seg - 6, baseY), true, false); ctx.LineTo(new Point(x + seg - 18, baseY + bar), true, false); ctx.LineTo(new Point(x, baseY + bar), true, false);
                }
                cell.Freeze();
                dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(46, 190, 215, 255)), null, cell);
                double f = Math.Clamp(s * 5 - n, 0, 1);
                if (f > 0)
                {
                    dc.PushClip(new RectangleGeometry(new Rect(x, baseY - 2, seg * f, bar + 4)));
                    dc.DrawGeometry(new LinearGradientBrush(Color.FromRgb(120, 215, 255), Color.FromRgb(58, 120, 255), 0), null, cell);
                    dc.Pop();
                }
            }
            for (int n = 0; n <= 5; n++)
            {
                var t = Label(n * 20 + "%", 28, Color.FromArgb(200, 200, 220, 250));
                dc.DrawText(t, new Point(x0 + n * seg - t.Width / 2 - 6, baseY + bar + 8));
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
