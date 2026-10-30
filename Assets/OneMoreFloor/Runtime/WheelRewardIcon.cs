using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // Neutral emblems conceal the exact cosmetic and coin amount until the reveal.
    public sealed class WheelRewardIcon : Graphic
    {
        public int Kind=-1; // coin, sky, hallway, lift
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
            WheelMesh.Quad(vh,center,unit,-.88f,-.82f,.88f,.82f,dark);
            WheelMesh.Quad(vh,center,unit,-.78f,-.72f,.78f,.72f,light);
            WheelMesh.Quad(vh,center,unit,-.64f,-.58f,.64f,.58f,GameUI.Hex("B78038"));
            if(Kind==0)
            {
                WheelMesh.Disc(vh,center+new Vector2(.29f,.28f)*unit,.18f*unit,light);
                WheelMesh.Triangle(vh,center+new Vector2(-.6f,-.49f)*unit,center+new Vector2(-.2f,.24f)*unit,center+new Vector2(.28f,-.49f)*unit,gold);
                WheelMesh.Triangle(vh,center+new Vector2(-.12f,-.49f)*unit,center+new Vector2(.33f,.04f)*unit,center+new Vector2(.61f,-.49f)*unit,light);
            }
            else if(Kind==1)
            {
                WheelMesh.Quad(vh,center,unit,-.12f,-.50f,.48f,.46f,light);
                WheelMesh.Quad(vh,center,unit,-.03f,-.50f,.39f,.37f,dark);
                WheelMesh.Quad(vh,center,unit,-.54f,-.16f,-.26f,.38f,gold);
                for(int i=0;i<3;i++) WheelMesh.Quad(vh,center,unit,-.66f,-.58f+i*.07f,.66f,-.55f+i*.07f,gold);
            }
            else
            {
                WheelMesh.Quad(vh,center,unit,-.46f,-.50f,-.035f,.27f,light);
                WheelMesh.Quad(vh,center,unit,.035f,-.50f,.46f,.27f,light);
                WheelMesh.Triangle(vh,center+new Vector2(-.27f,.37f)*unit,center+new Vector2(-.14f,.56f)*unit,center+new Vector2(-.01f,.37f)*unit,light);
                WheelMesh.Triangle(vh,center+new Vector2(.01f,.56f)*unit,center+new Vector2(.14f,.37f)*unit,center+new Vector2(.27f,.56f)*unit,gold);
            }
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
