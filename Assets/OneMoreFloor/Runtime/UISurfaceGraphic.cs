using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UISurfaceGraphic : MaskableGraphic
    {
        public Color Top=Color.white, Bottom=Color.white;
        public float Radius=22;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect;
            if(r.width<=0 || r.height<=0) return;
            float radius=Mathf.Clamp(Radius,0,Mathf.Min(r.width,r.height)*.5f);
            vh.AddVert(r.center,Color.Lerp(Bottom,Top,.5f)*color,Vector2.zero);
            const int steps=8; int count=0;
            for(int corner=0;corner<4;corner++)
            {
                float x=corner<2 ? r.xMax-radius : r.xMin+radius;
                float y=corner==0 || corner==3 ? r.yMin+radius : r.yMax-radius;
                for(int step=0;step<=steps;step++)
                {
                    float angle=((corner-1)*90+step*90f/steps)*Mathf.Deg2Rad;
                    var point=new Vector2(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius);
                    vh.AddVert(point,Color.Lerp(Bottom,Top,(point.y-r.yMin)/r.height)*color,Vector2.zero); count++;
                }
            }
            for(int i=0;i<count;i++) vh.AddTriangle(0,1+i,1+(i+1)%count);
        }
    }
}
