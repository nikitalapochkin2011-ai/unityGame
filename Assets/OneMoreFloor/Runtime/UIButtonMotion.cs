using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // Uses unscaled time so paused screens and ad dialogs keep responsive controls.
    public sealed class UIButtonMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler
    {
        Button button;bool pressed,hovered;float scale=1,velocity;
        void Awake(){button=GetComponent<Button>();}
        public void OnPointerDown(PointerEventData e){if(button!=null && button.IsInteractable())pressed=true;}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnPointerEnter(PointerEventData e){hovered=e.pointerId<0;}
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;}
        void Update()
        {
            bool active=button!=null && button.IsInteractable();
            float target=active && pressed ? .970f : active && hovered ? 1.008f : 1;
            if(Mathf.Abs(scale-target)<.0001f && Mathf.Abs(velocity)<.0001f)return;
            scale=Mathf.SmoothDamp(scale,target,ref velocity,.055f,10,Mathf.Min(Time.unscaledDeltaTime,.05f));
            if(Mathf.Abs(scale-target)<.0001f){scale=target;velocity=0;}
            transform.localScale=Vector3.one*scale;
        }
        void OnDisable(){pressed=hovered=false;velocity=0;scale=1;transform.localScale=Vector3.one;}
    }
}
