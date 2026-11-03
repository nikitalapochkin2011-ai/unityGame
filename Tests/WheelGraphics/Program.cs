using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OneMoreFloor;
using UnityEngine;
using UnityEngine.UI;

int passed=0;
void Check(bool value,string label) { if(!value) throw new Exception(label); passed++; Console.WriteLine("PASS mesh: "+label); }
foreach(var graphic in new Graphic[]{new FortuneWheelGraphic(),new WheelRewardIcon(),new WheelPointerGraphic(),new UISurfaceGraphic()})
{
    Check(graphic.GetType().GetCustomAttributes(typeof(RequireComponent),true).Cast<RequireComponent>().Any(a=>a.Type==typeof(CanvasRenderer)),graphic.GetType().Name+" explicitly requires renderer");
    foreach(var size in new[]{(400f,400f),(72f,64f),(28f,38f)})
    {
        var mesh=graphic.Generate(size.Item1,size.Item2);
        Check(mesh.Triangles.Count>0 && mesh.Vertices.Count<65000,"nonempty bounded mesh "+graphic.GetType().Name+" "+size);
        bool valid=true, visible=false;
        foreach(var (a,b,c) in mesh.Triangles)
        {
            valid &= a>=0 && b>=0 && c>=0 && a<mesh.Vertices.Count && b<mesh.Vertices.Count && c<mesh.Vertices.Count;
            var va=mesh.Vertices[a]; var vb=mesh.Vertices[b]; var vc=mesh.Vertices[c];
            float area=(vb.position.x-va.position.x)*(vc.position.y-va.position.y)-(vb.position.y-va.position.y)*(vc.position.x-va.position.x);
            visible |= Math.Abs(area)>.01 && va.color.a>0 && vb.color.a>0 && vc.color.a>0;
            foreach(var v in new[]{va,vb,vc}) valid &= float.IsFinite(v.position.x) && float.IsFinite(v.position.y) && Math.Abs(v.position.x)<=size.Item1*.51 && Math.Abs(v.position.y)<=size.Item2*.51;
        }
        Check(valid && visible,"finite visible triangles within rectangle "+graphic.GetType().Name+" "+size);
    }
}
var icon=new WheelRewardIcon();
int coinCount=icon.Generate(80,80).Triangles.Count;
icon.Kind=0; int questionCount=icon.Generate(80,80).Triangles.Count;
Check(coinCount!=questionCount && icon.Dirtied>0,"coin and question meshes differ; changing symbol requests redraw");
var pointer=new WheelPointerGraphic().Generate(50,60);
var tip=pointer.Vertices.OrderBy(v=>v.position.y).First().position;
Check(Math.Abs(tip.x)<.001 && tip.y<0,"pointer tip is centered and points downward");

foreach(UIIcon kind in Enum.GetValues(typeof(UIIcon)))
{
    var symbol=new UIIconGraphic{Icon=kind};
    foreach(var size in new[]{(18f,18f),(32f,48f),(60f,60f)})
    {
        var mesh=symbol.Generate(size.Item1,size.Item2);
        bool valid=mesh.Vertices.Count>0 && mesh.Vertices.Count<65000 && mesh.Triangles.Count>0;
        foreach(var v in mesh.Vertices)valid &= float.IsFinite(v.position.x) && float.IsFinite(v.position.y) && Math.Abs(v.position.x)<=size.Item1*.5f && Math.Abs(v.position.y)<=size.Item2*.5f;
        foreach(var (a,b,c) in mesh.Triangles)valid &= a>=0 && b>=0 && c>=0 && a<mesh.Vertices.Count && b<mesh.Vertices.Count && c<mesh.Vertices.Count;
        Check(valid,"bounded vector icon mesh "+kind+" "+size);
    }
}
Check(new UISurfaceGraphic().Generate(0,100).Vertices.Count==0 && new UIIconGraphic().Generate(40,0).Vertices.Count==0,"zero-size layout does not produce invalid UI triangles");

// Curves, shadow bounds and redraw behavior of the production premium surfaces.
foreach(var size in new[]{(500f,80f),(48f,48f),(12f,24f)})
foreach(float radius in new[]{0f,18f,1000f})
{
    var surface=new UIShapeImage{Radius=radius,ShadowOpacity=UITheme.CardShadow,ShadowBlur=7,ShadowOffset=2};
    var mesh=surface.Generate(size.Item1,size.Item2);
    Check(mesh.Vertices.Count<500 && mesh.Triangles.Count>0 && mesh.Vertices.All(v=>
        float.IsFinite(v.position.x) && float.IsFinite(v.position.y) &&
        Math.Abs(v.position.x)<=size.Item1/2+7.01f && v.position.y>=-size.Item2/2-9.01f && v.position.y<=size.Item2/2+5.01f),
        "rounded face and soft shadow stay in finite expanded bounds "+size+" radius "+radius);
    Check(mesh.Triangles.All(t=>t.a>=0 && t.b>=0 && t.c>=0 && t.a<mesh.Vertices.Count && t.b<mesh.Vertices.Count && t.c<mesh.Vertices.Count),"surface triangle indices valid");
}
var stateful=new UIShapeImage{color=UITheme.Terra};
var original=stateful.Generate(200,60).Vertices.Last().color;
stateful.SetState(true,0);var disabled=stateful.Generate(200,60).Vertices.Last().color;
Check(stateful.Disabled && Math.Abs(disabled.r-original.r)>.01,"disabled state changes the real face mesh");
int dirty=stateful.Dirtied;stateful.SetState(true,0);
Check(stateful.Dirtied==dirty,"idle surface does not continuously rebuild geometry");
stateful.SetState(false,1);var focusedMesh=stateful.Generate(200,60);
Check(!stateful.Disabled && stateful.Dirtied==dirty+1 && focusedMesh.Vertices.First().color.a>0,"keyboard focus redraws outline without changing content");
Check(new UIShapeImage().Generate(0,80).Vertices.Count==0 && new UIShapeImage().Generate(200,0).Vertices.Count==0,"collapsed premium surfaces have empty meshes");

if(args.Length>0)
{
    // A diagnostic drawing of the real generated triangles, not a Unity screenshot.
    var svg=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 500 550'><rect width='500' height='550' fill='#192d36'/><text x='250' y='36' fill='#eabc59' font-family='sans-serif' font-size='24' text-anchor='middle'>Try your luck</text>");
    string N(float n)=>n.ToString("0.###",CultureInfo.InvariantCulture);
    void Draw(Graphic g,float width,float height,float x,float y,float rotation=0)
    {
        svg.Append($"<g transform='translate({N(x)} {N(y)}) rotate({N(-rotation)}) scale(1 -1)'>");
        var mesh=g.Generate(width,height);
        foreach(var (a,b,c) in mesh.Triangles)
        {
            var va=mesh.Vertices[a]; var vb=mesh.Vertices[b]; var vc=mesh.Vertices[c]; var color=va.color;
            svg.Append($"<path d='M{N(va.position.x)} {N(va.position.y)} L{N(vb.position.x)} {N(vb.position.y)} L{N(vc.position.x)} {N(vc.position.y)} Z' fill='#{(int)Math.Round(color.r*255):X2}{(int)Math.Round(color.g*255):X2}{(int)Math.Round(color.b*255):X2}'/>");
        }
        svg.Append("</g>");
    }
    float angle=args.Length>1 ? float.Parse(args[1],CultureInfo.InvariantCulture) : 0;
    Draw(new FortuneWheelGraphic(),400,400,250,285,angle);
    for(int i=0;i<12;i++)
    {
        double a=(i*30-angle)*Math.PI/180;
        Draw(new WheelRewardIcon{Kind=FortuneWheelLayout.KindAt(i)},60,60,250+(float)Math.Sin(a)*134,285-(float)Math.Cos(a)*134,angle);
    }
    Draw(new WheelPointerGraphic(),44,54,250,96);
    svg.Append("<text x='250' y='525' fill='#fff1be' font-family='sans-serif' font-size='14' text-anchor='middle'>Mesh diagnostic — not a Unity screenshot</text></svg>");
    File.WriteAllText(args[0],svg.ToString());
}
Console.WriteLine(passed+" mesh checks passed; Unity rendering remains unverified.");
