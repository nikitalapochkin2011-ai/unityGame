using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    public sealed class FortuneWheelGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect=rectTransform.rect;
            var center=rect.center; float radius=Mathf.Min(rect.width,rect.height)*.5f;
            Color[] colors={GameUI.Gold,GameUI.Hex("A4D9AC"),GameUI.Hex("86C9ED")};
            for(int i=0;i<30;i++)
            {
                var color=Color.Lerp(colors[i/10],Color.white,i%2*.2f);
                for(int j=0;j<4;j++)
                {
                    float a=(i-.5f+j/4f)*Mathf.PI*2/30,b=(i-.5f+(j+1)/4f)*Mathf.PI*2/30;
                    int v=vh.currentVertCount;
                    vh.AddVert(center,color,Vector2.zero);
                    vh.AddVert(center+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius,color,Vector2.zero);
                    vh.AddVert(center+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*radius,color,Vector2.zero);
                    vh.AddTriangle(v,v+1,v+2);
                }
            }
        }
    }
}
