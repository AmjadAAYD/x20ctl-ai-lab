using System.Windows;
using System.Windows.Media;

namespace X20Ctl.Product;

/// <summary>The generic gamepad outline used where no model photo belongs: the empty player cards and the identity scan.
/// Coordinates are from the owner's reference glyph (14 Oct 2026), moved to the origin.</summary>
public static class GamepadGlyph
{
    private const string Data =
        "M245,140 L250,130 Q252,128 258,128 L345,128 Q352,128 355,134 L360,140 L440,142 C480,144 505,175 515,215 " +
        "C525,260 530,320 520,360 C512,392 480,408 455,402 C430,396 415,375 400,345 C390,325 375,318 355,318 L235,318 " +
        "C215,318 200,325 190,345 C175,375 160,396 135,402 C110,408 85,392 80,360 C72,320 78,260 90,215 C102,175 125,144 165,142 Z";

    /// <summary>The outline moved to the origin, frozen.</summary>
    public static readonly Geometry Outline;
    /// <summary>Distance round the outline, for dash animations.</summary>
    public static readonly double Length;
    public static readonly Size Size;

    static GamepadGlyph()
    {
        var raw = Geometry.Parse(Data);
        double length = 0;
        foreach (var figure in raw.GetFlattenedPathGeometry().Figures)
        {
            var from = figure.StartPoint;
            foreach (var segment in figure.Segments)
                if (segment is PolyLineSegment poly) foreach (var p in poly.Points) { length += (p - from).Length; from = p; }
            length += (figure.StartPoint - from).Length;
        }
        var bounds = raw.Bounds;
        var g = Geometry.Parse(Data); g.Transform = new TranslateTransform(-bounds.X, -bounds.Y); g.Freeze();
        Outline = g; Length = length; Size = bounds.Size;
    }
}
