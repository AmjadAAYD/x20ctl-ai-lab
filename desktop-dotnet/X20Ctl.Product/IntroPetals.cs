using System.Windows;
using System.Windows.Media;
namespace X20Ctl.Product;

/// <summary>The beats the picture and the sound share (owner request 17 Oct 2026: the outline should move with the music).
/// tools/build_intro_sound.py plucks a note at each of these times; keep the two in step.</summary>
internal static class IntroBeats
{
    /// <summary>The white outline is drawn in eight surges, one per pluck, 1.45 s to 2.325 s; the ninth beat (2.45 s) is
    /// the line closing on itself.</summary>
    public static readonly double[] Outline=Enumerable.Range(0,9).Select(k=>1.45+k*.125).ToArray();
    private const double Surge=.11;
    /// <summary>How much of the outline is drawn at time t, 0 to 1: each pluck pushes the line one eighth further, quickly
    /// and then easing, so it moves on the beat and rests between.</summary>
    public static double Drawn(double t)
    {
        if(t<Outline[0])return 0;
        for(int k=0;k<8;k++)if(t<Outline[k+1]){double x=Math.Clamp((t-Outline[k])/Surge,0,1);return (k+1-Math.Pow(1-x,3))/8;}
        return 1;
    }
}

/// <summary>Sakura petals (owner request 17 Oct 2026): when the two blades lock, like a sword going home into its sheath,
/// a breath of petals is thrown off the seam and drifts down, turning, gone before the X flies to the header. Drawn in
/// one visual from closed-form paths, so it costs nothing between frames. Two layers: most petals behind the mark, a few
/// in front of it.</summary>
internal sealed class IntroPetals : FrameworkElement
{
    private const int Count=72;
    private readonly bool front;
    private double time=-1;
    private readonly Petal[] petals;
    private static readonly Geometry Shape=MakeShape();
    private static readonly Point Heart=new(390,320);
    private static readonly Vector Axis=new Vector(-294,260)/Math.Sqrt(294*294+260*260); // the blades' "/" line
    private readonly record struct Petal(Point From,Vector Velocity,double Fall,double Sway,double SwayRate,double Phase,double Angle,double Spin,double FlipRate,double Size,Color Tip,Color Base,double Delay);

    public IntroPetals(bool front)
    {
        this.front=front;var rng=new Random(20261017);var list=new List<Petal>();
        Color[] tips={Color.FromRgb(255,222,230),Color.FromRgb(255,236,242),Color.FromRgb(255,206,218)};
        for(int i=0;i<Count;i++)
        {
            double along=(rng.NextDouble()*2-1)*150*Math.Pow(rng.NextDouble(),.6);
            var normal=new Vector(Axis.Y,-Axis.X)*(rng.Next(2)==0?1:-1);
            double speed=320+rng.NextDouble()*420;
            var velocity=normal*speed+Axis*((rng.NextDouble()*2-1)*120)+new Vector(0,-120-rng.NextDouble()*160);
            var tip=tips[rng.Next(tips.Length)];
            if((i%4==0)==front)list.Add(new(Heart+Axis*along,velocity,55+rng.NextDouble()*45,9+rng.NextDouble()*18,1.6+rng.NextDouble()*2.2,rng.NextDouble()*Math.PI*2,
                rng.NextDouble()*360,(rng.NextDouble()*2-1)*260,2.5+rng.NextDouble()*4,(front?18:12)+rng.NextDouble()*9,tip,Color.FromRgb(236,140,168),Math.Abs(along)/150*.07));
        }
        petals=list.ToArray();
    }

    /// <summary>A sakura petal one unit long, its stem at the origin and its notched tip down +Y.</summary>
    private static Geometry MakeShape()
    {
        var g=Geometry.Parse("M0,0 C0.5,0.12 0.62,0.55 0.3,1 L0,0.84 L-0.3,1 C-0.62,0.55 -0.5,0.12 0,0 Z");g.Freeze();return g;
    }

    /// <summary>Seconds since the lock; below zero or past the drift, nothing is drawn.</summary>
    public void SetTime(double t){if(t==time)return;time=t;InvalidateVisual();}

    protected override void OnRender(DrawingContext dc)
    {
        if(time<0||time>2.1)return;
        foreach(var p in petals)
        {
            double t=time-p.Delay;if(t<=0)continue;
            const double drag=1.9;double carried=(1-Math.Exp(-drag*t))/drag;
            var at=p.From+p.Velocity*carried+new Vector(0,p.Fall*(t-carried))+new Vector(p.Sway*Math.Sin(p.SwayRate*t+p.Phase)*(1-Math.Exp(-2*t)),0);
            double alpha=Math.Clamp(t/.06,0,1)*(1-Math.Clamp((time-1.45)/.5,0,1));
            if(alpha<=0)continue;
            double flip=Math.Cos(p.FlipRate*t+p.Phase);flip=Math.Sign(flip)*Math.Max(.18,Math.Abs(flip));
            var transform=new Matrix();transform.Scale(p.Size*flip,p.Size);transform.Rotate(p.Angle+p.Spin*carried*1.8+40*t);transform.Translate(at.X,at.Y);
            var fill=new LinearGradientBrush(p.Base,p.Tip,new Point(.5,0),new Point(.5,1));
            dc.PushOpacity(alpha*(front?.95:.85));dc.PushTransform(new MatrixTransform(transform));
            dc.DrawGeometry(fill,null,Shape);
            dc.Pop();dc.Pop();
        }
    }
}
