using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIShapeImage : Image
    {
        public float Radius=UITheme.CardRadius,StrokeWidth=UITheme.Stroke,ShadowOpacity,ShadowBlur=UITheme.ShadowBlur,ShadowOffset=UITheme.ShadowOffset;
        public Color StrokeColor=UITheme.Border;
        public bool Dark,Disabled;
        float focus;
        public void SetState(bool disabled,float focusAmount)
        {
            if(Disabled==disabled && Mathf.Abs(focus-focusAmount)<.001f)return;
            Disabled=disabled;focus=focusAmount;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;if(r.width<=0 || r.height<=0)return;
            if(ShadowOpacity>0)
                for(int i=6;i>=1;i--)
                {
                    float spread=ShadowBlur*i/6;
                    Fill(vh,new Rect(r.xMin-spread,r.yMin-spread-ShadowOffset,r.width+2*spread,r.height+2*spread),Radius+spread,new Color(.125f,.239f,.212f,ShadowOpacity*(7-i)/21));
                }
            Color face=Disabled ? Color.Lerp(color,Dark ? UITheme.Deep : UITheme.Background,Dark ? .26f : .32f) : color;
            Color edge=Color.Lerp(StrokeColor,UITheme.Focus,focus);
            float stroke=StrokeWidth+focus*.65f;
            if(stroke>0 && edge.a>0)
            {
                Fill(vh,r,Radius,edge);
                float inset=Mathf.Min(stroke,Mathf.Min(r.width,r.height)*.5f);
                Fill(vh,new Rect(r.xMin+inset,r.yMin+inset,r.width-2*inset,r.height-2*inset),Mathf.Max(0,Radius-inset),face);
            }
            else Fill(vh,r,Radius,face);
        }
        static void Fill(VertexHelper vh,Rect r,float radius,Color tint)
        {
            if(r.width<=0 || r.height<=0)return;
            radius=Mathf.Clamp(radius,0,Mathf.Min(r.width,r.height)*.5f);int start=vh.currentVertCount;
            vh.AddVert(r.center,tint,Vector2.zero);const int steps=8;
            for(int corner=0;corner<4;corner++)
            {
                float x=corner<2 ? r.xMax-radius : r.xMin+radius,y=corner==0 || corner==3 ? r.yMin+radius : r.yMax-radius;
                for(int step=0;step<=steps;step++)
                {
                    float angle=((corner-1)*90+step*90f/steps)*Mathf.Deg2Rad;
                    vh.AddVert(new Vector2(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius),tint,Vector2.zero);
                }
            }
            const int count=4*(steps+1);for(int i=0;i<count;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%count);
        }
    }
}
