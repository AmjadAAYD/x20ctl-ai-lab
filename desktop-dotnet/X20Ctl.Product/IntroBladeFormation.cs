using System.Windows;
using System.Windows.Media;
namespace X20Ctl.Product;

/// <summary>The two controller contours shear directly into the two original X blades.
/// The source never takes a circular intermediate shape; the original mark is the landing geometry.</summary>
internal sealed class IntroBladeFormation : FrameworkElement
{
    private Point[][] source=[], target=[];
    private Geometry[] blades=[];
    private double time=-1;
    public void Configure(Point[][] from,Point[][] to,PathGeometry mark)
    {
        source=from;target=to;
        blades=mark.Figures.Select(f=>{var g=new PathGeometry();g.Figures.Add(f.Clone());g.Freeze();return (Geometry)g;}).ToArray();
    }
    public void SetTime(double t){time=t;InvalidateVisual();}
    private static double Ease(double x){x=Math.Clamp(x,0,1);return 1-Math.Pow(1-x,3);}
    protected override void OnRender(DrawingContext dc)
    {
        if(time<0||time>1.65||source.Length!=2)return;
        double progress=Math.Clamp(time/1.15,0,1),fade=1-Ease((time-1.22)/.4);
        for(int f=0;f<2;f++)
        {
            var direction=f==0?new Vector(-1,1):new Vector(1,-1);
            var colour=f==0?Color.FromRgb(81,190,255):Color.FromRgb(255,163,84);
            var a=source[f];var b=target[f];
            Point At(int i)
            {
                double delay=.12*i/(a.Length-1),u=Ease((progress-delay)/(.88));
                var start=new Point(390+(a[i].X-390)*1.1,320+(a[i].Y-320)*1.1);
                // A directional shear follows each blade, never an orbit or radial explosion.
                var bow=direction*(34*Math.Sin(Math.PI*u));
                return start+(b[i]-start)*u+bow;
            }
            var ribbon=new StreamGeometry();using(var g=ribbon.Open()){g.BeginFigure(At(0),false,progress>=.999);g.PolyLineTo(Enumerable.Range(1,a.Length-1).Select(At).ToArray(),true,true);}ribbon.Freeze();
            double ribbonAlpha=(1-Ease((progress-.60)/.4))*fade;
            dc.PushOpacity(ribbonAlpha);
            dc.DrawGeometry(null,new Pen(new SolidColorBrush(Color.FromArgb(25,colour.R,colour.G,colour.B)),22){LineJoin=PenLineJoin.Round},ribbon);
            // The contour thickens into a lamina before its chisel edge seats in the original mark.
            dc.DrawGeometry(null,new Pen(new SolidColorBrush(colour),6.5){LineJoin=PenLineJoin.Round,StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},ribbon);
            dc.DrawGeometry(null,new Pen(Brushes.White,1.7){LineJoin=PenLineJoin.Round},ribbon);dc.Pop();
            double seat=Ease((progress-.30-f*.08)/.62);
            if(seat<=0)continue;
            dc.PushTransform(new TranslateTransform(direction.X*90*(1-seat),direction.Y*90*(1-seat)));
            dc.PushOpacity(seat*fade);
            var fill=new LinearGradientBrush(new GradientStopCollection {new(colour,0),new(Colors.White,.45),new(Color.FromRgb(220,230,244),1)},f==0?45:225);
            dc.DrawGeometry(fill,null,blades[f]);
            dc.DrawGeometry(null,new Pen(new SolidColorBrush(colour),2.2*(1-seat)+.5),blades[f]);
            // A bright machining edge traverses the blade's own silhouette as it seats.
            dc.PushClip(blades[f]);
            var band=new LinearGradientBrush(new GradientStopCollection{new(Colors.Transparent,0),new(Color.FromArgb(220,255,255,255),.5),new(Colors.Transparent,1)},0);
            dc.PushTransform(new RotateTransform(-43,390,320));
            dc.DrawRectangle(band,null,new Rect(30+seat*650,0,34,680));dc.Pop();dc.Pop();
            dc.Pop();dc.Pop();
        }
    }
}
