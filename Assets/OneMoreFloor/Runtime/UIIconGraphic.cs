using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    public enum UIIcon { Lift,Play,ArrowLeft,ArrowRight,Settings,Wheel,Collection,Person,Star,Check,Lock,Heart,Video,Home,Pause,Help,Flag,Close,Coin }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIIconGraphic : MaskableGraphic
    {
        public UIIcon Icon;
        VertexHelper mesh; Vector2 center; float unit;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh=vh; var r=rectTransform.rect; center=r.center; unit=Mathf.Min(r.width,r.height)*.39f;
            if(unit<=0) return;
            switch(Icon)
            {
                case UIIcon.Lift:
                    Path(true,-.76f,-.88f,.76f,-.88f,.76f,.88f,-.76f,.88f);Line(0,-.83f,0,.55f);Line(-.52f,.57f,.52f,.57f);Line(-.47f,-.36f,-.47f,.18f);Line(.47f,-.36f,.47f,.18f);break;
                case UIIcon.Play: Fill(-.42f,-.75f,.73f,0,-.42f,.75f);break;
                case UIIcon.ArrowLeft: Path(false,.69f,0,-.61f,0,-.12f,.51f);Path(false,-.61f,0,-.12f,-.51f);break;
                case UIIcon.ArrowRight:Path(false,-.69f,0,.61f,0,.12f,.51f);Path(false,.61f,0,.12f,-.51f);break;
                case UIIcon.Check:Path(false,-.73f,0,-.20f,-.48f,.73f,.58f);break;
                case UIIcon.Close:Line(-.57f,-.57f,.57f,.57f);Line(-.57f,.57f,.57f,-.57f);break;
                case UIIcon.Pause:Line(-.34f,-.66f,-.34f,.66f,.23f);Line(.34f,-.66f,.34f,.66f,.23f);break;
                case UIIcon.Person:Circle(0,.46f,.31f);Path(false,-.67f,-.79f,-.64f,-.37f,-.35f,-.16f,.35f,-.16f,.64f,-.37f,.67f,-.79f);break;
                case UIIcon.Heart:Fill(0,-.83f,-.87f,.10f,-.85f,.52f,-.55f,.80f,-.23f,.75f,0,.50f,.23f,.75f,.55f,.80f,.85f,.52f,.87f,.10f);break;
                case UIIcon.Star:
                    var points=new float[20];for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad,v=i%2==0 ? .89f : .42f;points[i*2]=Mathf.Cos(a)*v;points[i*2+1]=Mathf.Sin(a)*v;}Fill(points);break;
                case UIIcon.Lock:
                    Path(true,-.66f,-.80f,.66f,-.80f,.66f,.15f,-.66f,.15f);Path(false,-.43f,.15f,-.43f,.57f,-.24f,.80f,.24f,.80f,.43f,.57f,.43f,.15f);Dot(0,-.25f,.10f);break;
                case UIIcon.Collection:
                    Path(true,-.83f,.12f,-.13f,.12f,-.13f,.82f,-.83f,.82f);Path(true,.13f,.12f,.83f,.12f,.83f,.82f,.13f,.82f);Path(true,-.83f,-.83f,-.13f,-.83f,-.13f,-.13f,-.83f,-.13f);Path(true,.13f,-.83f,.83f,-.83f,.83f,-.13f,.13f,-.13f);break;
                case UIIcon.Wheel:
                    Circle(0,0,.84f);Circle(0,0,.19f);for(int i=0;i<8;i++){float t=i*Mathf.PI/4;Line(Mathf.Cos(t)*.30f,Mathf.Sin(t)*.30f,Mathf.Cos(t)*.69f,Mathf.Sin(t)*.69f,.09f);}break;
                case UIIcon.Settings:
                    Circle(0,0,.53f);Circle(0,0,.18f);for(int i=0;i<8;i++){float t=i*Mathf.PI/4;Line(Mathf.Cos(t)*.54f,Mathf.Sin(t)*.54f,Mathf.Cos(t)*.85f,Mathf.Sin(t)*.85f,.21f);}break;
                case UIIcon.Video:Path(true,-.86f,-.62f,.29f,-.62f,.29f,.62f,-.86f,.62f);Path(true,.30f,.28f,.85f,.59f,.85f,-.59f,.30f,-.28f);break;
                case UIIcon.Home:Path(false,-.9f,.04f,0,.84f,.9f,.04f);Path(false,-.64f,.19f,-.64f,-.8f,-.17f,-.8f,-.17f,-.22f,.23f,-.22f,.23f,-.8f,.65f,-.8f,.65f,.19f);break;
                case UIIcon.Help:Path(false,-.42f,.53f,-.22f,.78f,.23f,.78f,.46f,.56f,.43f,.23f,0,-.1f,0,-.30f);Dot(0,-.74f,.11f);break;
                case UIIcon.Flag:Line(-.64f,-.86f,-.64f,.85f);Path(true,-.64f,.76f,.64f,.76f,.39f,.32f,.64f,-.03f,-.64f,-.03f);break;
                default:Circle(0,0,.80f);Circle(0,0,.56f);Line(0,-.30f,0,.30f,.19f);break;
            }
        }
        Vector2 P(float x,float y){return center+new Vector2(x,y)*unit;}
        void Dot(float x,float y,float radius){Disc(P(x,y),radius*unit);}
        void Circle(float x,float y,float radius)
        {
            const int segments=36;int start=mesh.currentVertCount;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,c=Mathf.Cos(a),s=Mathf.Sin(a);
                mesh.AddVert(P(x+c*(radius+.05f),y+s*(radius+.05f)),color,Vector2.zero);
                mesh.AddVert(P(x+c*(radius-.05f),y+s*(radius-.05f)),color,Vector2.zero);
            }
            for(int i=0;i<segments;i++)
            {
                int next=(i+1)%segments;
                mesh.AddTriangle(start+i*2,start+i*2+1,start+next*2);
                mesh.AddTriangle(start+next*2,start+i*2+1,start+next*2+1);
            }
        }
        void Path(bool closed,params float[] points)
        {
            for(int i=0;i<points.Length-2;i+=2)Line(points[i],points[i+1],points[i+2],points[i+3]);
            if(closed)Line(points[points.Length-2],points[points.Length-1],points[0],points[1]);
        }
        void Line(float x0,float y0,float x1,float y1,float width=.13f)
        {
            var a=P(x0,y0);var b=P(x1,y1);var d=b-a;float length=Mathf.Sqrt(d.x*d.x+d.y*d.y);
            if(length<.001f)return;
            var n=new Vector2(-d.y/length,d.x/length)*(width*unit*.5f);int i=mesh.currentVertCount;
            mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);mesh.AddVert(a-n,color,Vector2.zero);
            mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);Disc(a,width*unit*.5f);Disc(b,width*unit*.5f);
        }
        void Disc(Vector2 point,float radius)
        {
            int i=mesh.currentVertCount;mesh.AddVert(point,color,Vector2.zero);
            for(int n=0;n<12;n++){float a=n*Mathf.PI*2/12;mesh.AddVert(point+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);}
            for(int n=0;n<12;n++)mesh.AddTriangle(i,i+1+n,i+1+(n+1)%12);
        }
        void Fill(params float[] points)
        {
            // A fan from the shape centroid also handles the concave heart/star silhouettes.
            Vector2 middle=Vector2.zero;for(int n=0;n<points.Length;n+=2)middle+=P(points[n],points[n+1]);middle*=2f/points.Length;
            int i=mesh.currentVertCount;mesh.AddVert(middle,color,Vector2.zero);
            for(int n=0;n<points.Length;n+=2)mesh.AddVert(P(points[n],points[n+1]),color,Vector2.zero);
            int count=points.Length/2;for(int n=0;n<count;n++)mesh.AddTriangle(i,i+1+n,i+1+(n+1)%count);
        }
    }
}
