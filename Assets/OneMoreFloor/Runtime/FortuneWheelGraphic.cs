using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FortuneWheelGraphic : Graphic
    {
        public const int Sectors=FortuneWheelLayout.Sectors;
        public static int KindAt(int sector) { return FortuneWheelLayout.KindAt(sector); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect=rectTransform.rect;
            var center=rect.center; float radius=Mathf.Min(rect.width,rect.height)*.5f;
            WheelMesh.Disc(vh,center,radius,GameUI.Hex("633C1C"));
            WheelMesh.Ring(vh,center,.92f*radius,.99f*radius,GameUI.Hex("EABC59"));
            WheelMesh.Ring(vh,center,.94f*radius,.96f*radius,GameUI.Hex("FFF0B1"));
            for(int i=0;i<Sectors;i++)
            {
                Color color=GameUI.Hex(i%2==0 ? "E3B45F" : "79532E");
                for(int j=0;j<12;j++)
                {
                    float a=(i-.5f+j/12f)*Mathf.PI*2/Sectors,b=(i-.5f+(j+1)/12f)*Mathf.PI*2/Sectors;
                    WheelMesh.Triangle(vh,center,center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius*.91f,center+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*radius*.91f,color);
                }
                float angle=(i-.5f)*Mathf.PI*2/Sectors;
                Vector2 edge=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));
                Vector2 side=new Vector2(edge.y,-edge.x)*radius*.005f;
                WheelMesh.Triangle(vh,center,center+edge*radius*.92f-side,center+edge*radius*.92f+side,GameUI.Hex("FFE6A0"));
                float mid=i*Mathf.PI*2/Sectors;
                WheelMesh.Disc(vh,center+new Vector2(Mathf.Sin(mid),Mathf.Cos(mid))*radius*.95f,radius*.018f,GameUI.Hex("FFF4CC"));
            }
            WheelMesh.Disc(vh,center,radius*.23f,GameUI.Hex("704113"));
            WheelMesh.Disc(vh,center,radius*.20f,GameUI.Hex("EABC59"));
            WheelMesh.Ring(vh,center,radius*.15f,radius*.18f,GameUI.Hex("FFF0B1"));
            WheelMesh.Star(vh,center,radius*.12f,GameUI.Hex("FFF0B1"));
        }
    }
}
