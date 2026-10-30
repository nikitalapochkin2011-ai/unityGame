using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WheelPointerGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect=rectTransform.rect; var center=rect.center;
            float unit=Mathf.Min(rect.width,rect.height)*.5f;
            WheelMesh.Triangle(vh,center+new Vector2(-.98f,.8f)*unit,center+new Vector2(.98f,.8f)*unit,center+new Vector2(0,-.98f)*unit,GameUI.Hex("704113"));
            WheelMesh.Triangle(vh,center+new Vector2(-.78f,.67f)*unit,center+new Vector2(.78f,.67f)*unit,center+new Vector2(0,-.78f)*unit,GameUI.Hex("FFE6A0"));
        }
    }
}
