using System.Windows;
using System.Windows.Media;
namespace X20Ctl.Product;

/// <summary>How the white outline is drawn, shared with the sound (tools/build_intro_sound.py, OUTLINE): one smooth
/// stroke from 1.45 s to 2.45 s, slow to start, quick through the middle, easing as it closes. The plucks are placed where
/// the line passes each eighth of the controller, so the music follows the drawing rather than the drawing a beat (owner
/// 17 Oct 2026: the line drawn on a beat felt forced).</summary>
internal static class IntroBeats
{
    public const double OutlineFrom=1.45, OutlineFor=1.0;
    /// <summary>How much of the outline is drawn at time t, 0 to 1.</summary>
    public static double Drawn(double t){double x=Math.Clamp((t-OutlineFrom)/OutlineFor,0,1);return x*x*(3-2*x);}
}

/// <summary>Fog at the lock (owner 17 Oct 2026, in place of sakura petals): as the blades lock, like a sword going home
/// into its sheath, a breath of cold mist is pushed out of the seam to both sides, swells, drifts and thins away before
/// the X flies to the header. Soft radial puffs on closed-form paths, so nothing moves between frames. Most of it lies
/// behind the mark; a faint veil passes in front.</summary>
internal sealed class IntroFog : FrameworkElement
{
    private const int Count=40;
    private readonly bool front;
    private double time=-1;
    private readonly Puff[] puffs;
    private static readonly Point Heart=new(390,320);
    private static readonly Vector Axis=new Vector(-294,260)/Math.Sqrt(294*294+260*260); // the blades' "/" line
    private static readonly double Angle=Math.Atan2(Axis.Y,Axis.X)*180/Math.PI;
    private readonly record struct Puff(Point From,Vector Velocity,double Start,double End,double Stretch,double Peak,double Delay,double Tint);

    public IntroFog(bool front)
    {
        this.front=front;var rng=new Random(20261017);var list=new List<Puff>();
        for(int i=0;i<Count;i++)
        {
            double along=(rng.NextDouble()*2-1)*170*Math.Sqrt(rng.NextDouble());
            var normal=new Vector(Axis.Y,-Axis.X)*(i%2==0?1:-1);
            var velocity=normal*(260+rng.NextDouble()*320)+Axis*((rng.NextDouble()*2-1)*120)+new Vector((rng.NextDouble()*2-1)*90,10+rng.NextDouble()*30);
            if((i%3==0)==front)list.Add(new(Heart+Axis*along,velocity,18+rng.NextDouble()*22,110+rng.NextDouble()*170,2.0+rng.NextDouble()*1.4,
                (front?.04:.085)+rng.NextDouble()*.05,Math.Abs(along)/170*.09,rng.NextDouble()));
        }
        puffs=list.ToArray();
    }

    /// <summary>Seconds since the lock; below zero or once it has thinned away, nothing is drawn.</summary>
    public void SetTime(double t){if(t==time)return;time=t;InvalidateVisual();}

    protected override void OnRender(DrawingContext dc)
    {
        if(time<0||time>2.0)return;
        foreach(var p in puffs)
        {
            double t=time-p.Delay;if(t<=0)continue;
            const double drag=1.7;double carried=(1-Math.Exp(-drag*t))/drag;
            var at=p.From+p.Velocity*carried+new Vector(0,6*t);
            double r=p.Start+(p.End-p.Start)*(1-Math.Exp(-1.5*t));
            // it rises quickly as it is pushed out, then thins slowly; gone by 1.95 s
            double alpha=p.Peak*Math.Clamp(t/.18,0,1)*Math.Pow(1-Math.Clamp((time-.35)/1.6,0,1),1.6);
            if(alpha<=.002)continue;
            byte g=(byte)(214+12*p.Tint);var colour=Color.FromRgb((byte)(g-10),(byte)(g-4),g);
            var brush=new RadialGradientBrush(new GradientStopCollection{new(Color.FromArgb((byte)(255*alpha),colour.R,colour.G,colour.B),0),
                new(Color.FromArgb((byte)(150*alpha),colour.R,colour.G,colour.B),.4),new(Color.FromArgb((byte)(45*alpha),colour.R,colour.G,colour.B),.72),new(Color.FromArgb(0,colour.R,colour.G,colour.B),1)});
            // each wisp leaves along the blades' line, as the seam it came from, and settles flat as it spreads
            double settle=1-Math.Exp(-1.8*t);
            dc.PushTransform(new RotateTransform(Angle*(1-settle)+(Angle>90?180:0)*settle,at.X,at.Y));
            dc.DrawEllipse(brush,null,at,r*p.Stretch,r*(1-.35*settle));
            dc.Pop();
        }
    }
}
