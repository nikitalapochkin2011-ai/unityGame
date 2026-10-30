using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // Neutral emblems conceal the exact cosmetic and coin amount until the reveal.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WheelRewardIcon : Graphic
    {
        int kind=-1;
        public int Kind { get { return kind; } set { if(kind==value) return; kind=value; SetVerticesDirty(); } } // coin or mystery
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center=rectTransform.rect.center;
            float unit=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.5f;
            Color dark=GameUI.Hex("704113"), gold=GameUI.Hex("EAB64F"), light=GameUI.Hex("FFF1BE");
            if(Kind<0)
            {
                WheelMesh.Disc(vh,center+new Vector2(0,-.09f)*unit,.86f*unit,dark);
                WheelMesh.Disc(vh,center,.86f*unit,gold);
                WheelMesh.Ring(vh,center,.70f*unit,.82f*unit,light);
                WheelMesh.Disc(vh,center,.61f*unit,GameUI.Hex("C88A2D"));
                WheelMesh.Disc(vh,center+new Vector2(-.025f,.035f)*unit,.57f*unit,gold);
                WheelMesh.Star(vh,center,.39f*unit,light);
                return;
            }
            // A raised question mark on a gold medallion, identical for every decoration.
            WheelMesh.Disc(vh,center+new Vector2(0,-.055f)*unit,.87f*unit,dark);
            WheelMesh.Disc(vh,center,.87f*unit,gold);
            WheelMesh.Disc(vh,center,.73f*unit,GameUI.Hex("304550"));
            WheelMesh.Ring(vh,center,.77f*unit,.82f*unit,light);
            // Rounded hook and stem; a mesh symbol does not depend on TMP/font loading.
            for(int i=0;i<32;i++)
            {
                float angle=(155-i*240f/31)*Mathf.PI/180;
                var point=center+new Vector2(Mathf.Cos(angle)*.30f,.24f+Mathf.Sin(angle)*.30f)*unit;
                WheelMesh.Disc(vh,point,.095f*unit,light);
            }
            WheelMesh.Quad(vh,center,unit,-.07f,-.20f,.12f,.06f,light);
            WheelMesh.Disc(vh,center+new Vector2(.025f,-.42f)*unit,.105f*unit,light);
        }
    }
    internal static class WheelMesh
    {
        public static void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color color)
        {
            int start=vh.currentVertCount;
            vh.AddVert(a,color,Vector2.zero); vh.AddVert(b,color,Vector2.zero); vh.AddVert(c,color,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2);
        }
        public static void Quad(VertexHelper vh,Vector2 center,float unit,float x0,float y0,float x1,float y1,Color color)
        {
            Vector2 a=center+new Vector2(x0,y0)*unit,b=center+new Vector2(x0,y1)*unit,c=center+new Vector2(x1,y1)*unit,d=center+new Vector2(x1,y0)*unit;
            Triangle(vh,a,b,c,color); Triangle(vh,a,c,d,color);
        }
        public static void Disc(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI*2/96,b=(i+1)*Mathf.PI*2/96;
                Triangle(vh,center,center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius,center+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*radius,color);
            }
        }
        public static void Ring(VertexHelper vh,Vector2 center,float inner,float outer,Color color)
        {
            for(int i=0;i<128;i++)
            {
                float a=i*Mathf.PI*2/128,b=(i+1)*Mathf.PI*2/128;
                var va=new Vector2(Mathf.Sin(a),Mathf.Cos(a)); var vb=new Vector2(Mathf.Sin(b),Mathf.Cos(b));
                Triangle(vh,center+va*inner,center+va*outer,center+vb*outer,color);
                Triangle(vh,center+va*inner,center+vb*outer,center+vb*inner,color);
            }
        }
        public static void Star(VertexHelper vh,Vector2 center,float radius,Color color)
        {
            for(int i=0;i<10;i++)
            {
                float a=i*Mathf.PI/5,b=(i+1)*Mathf.PI/5;
                Triangle(vh,center,center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius*(i%2==0 ? 1 : .46f),center+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*radius*(i%2==0 ? .46f : 1),color);
            }
        }
    }
}
