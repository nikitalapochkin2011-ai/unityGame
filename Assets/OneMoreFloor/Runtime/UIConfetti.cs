using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIConfetti : MaskableGraphic
    {
        const int Count=64;const float Lifetime=2.65f;
        float age;
        static readonly Color[] Palette={UITheme.Gold,UITheme.Terra,UITheme.Soft,GameUI.Hex("86A58C"),GameUI.Hex("E5CBA1")};
        protected override void OnEnable(){base.OnEnable();age=0;raycastTarget=false;SetVerticesDirty();}
        void Update()
        {
            if(!Application.isFocused)return;
            age+=Time.unscaledDeltaTime;SetVerticesDirty();
            if(age>=Lifetime)Destroy(gameObject);
        }
        static float Fraction(int i,int salt)
        {
            unchecked
            {
                uint n=(uint)i*0x9E3779B9u+(uint)salt*0x85EBCA6Bu+0x4A83u;
                n^=n>>16;n*=0x7FEB352Du;n^=n>>15;n*=0x846CA68Bu;n^=n>>16;
                return (n&0xFFFFFF)/16777215f;
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;if(r.width<=0 || r.height<=0)return;
            for(int i=0;i<Count;i++)
            {
                float t=age-Fraction(i,1)*.19f;if(t<0 || t>2.35f)continue;
                float side=i%2==0 ? -1 : 1;
                float x=side*r.width*(.475f-(.035f+Fraction(i,2)*.11f)*t);
                x+=Mathf.Sin(t*5+i)*r.width*.008f;
                float y=r.height*(-.035f+(.29f+Fraction(i,3)*.16f)*t-.18f*t*t);
                if(x<r.xMin-12 || x>r.xMax+12 || y<r.yMin || y>r.yMax)continue;
                var tint=Palette[i%Palette.Length];tint.a*=Mathf.Clamp01(t/.055f)*Mathf.Clamp01((2.35f-t)/.6f)*.88f;
                float angle=(i*31+t*(i%2==0 ? 165 : -145))*Mathf.Deg2Rad;
                float w=(2.5f+Fraction(i,4)*2.5f)*(.25f+.75f*Mathf.Abs(Mathf.Cos(t*5+i))),h=2+Fraction(i,5)*2;
                float cos=Mathf.Cos(angle),sin=Mathf.Sin(angle);int start=vh.currentVertCount;
                for(int c=0;c<4;c++)
                {
                    float dx=c==0 || c==3 ? -w : w,dy=c<2 ? -h : h;
                    vh.AddVert(new Vector2(x+dx*cos-dy*sin,y+dx*sin+dy*cos),tint,Vector2.zero);
                }
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
