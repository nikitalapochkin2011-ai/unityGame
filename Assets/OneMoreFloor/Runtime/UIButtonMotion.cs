using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace OneMoreFloor
{
    public sealed class UIButtonMotion : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public bool PreserveArtwork;
        public float DisabledOpacity=.62f;
        Button button;UIShapeImage shape;CanvasGroup opacity;
        bool pressed,hovered,focused,lastActive=true;float scale=1,from=1,target=1,age=1,velocity,focusAmount;
        void Awake(){button=GetComponent<Button>();shape=GetComponent<UIShapeImage>();}
        public void Configure(bool artwork,float disabledOpacity)
        { PreserveArtwork=artwork;DisabledOpacity=disabledOpacity;if(!artwork){opacity=GetComponent<CanvasGroup>();if(opacity==null)opacity=gameObject.AddComponent<CanvasGroup>();} }
        public void OnPointerDown(PointerEventData e){if(button!=null && button.IsInteractable()){pressed=true;focused=false;}}
        public void OnPointerUp(PointerEventData e){pressed=false;}
        public void OnPointerEnter(PointerEventData e){hovered=e.pointerId<0;}
        public void OnPointerExit(PointerEventData e){hovered=false;pressed=false;}
        public void OnSelect(BaseEventData e){focused=true;}
        public void OnDeselect(BaseEventData e){focused=false;pressed=false;}
        void Update()
        {
            bool active=button!=null && button.IsInteractable();float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(PreserveArtwork)
            {
                float goal=active && pressed ? .970f : active && hovered ? 1.008f : 1;
                if(Mathf.Abs(scale-goal)<.0001f && Mathf.Abs(velocity)<.0001f)return;
                scale=Mathf.SmoothDamp(scale,goal,ref velocity,.055f,10,dt);
                if(Mathf.Abs(scale-goal)<.0001f){scale=goal;velocity=0;}transform.localScale=Vector3.one*scale;return;
            }
            if(active!=lastActive){lastActive=active;if(opacity!=null)opacity.alpha=active ? 1 : DisabledOpacity;}
            float next=active && pressed ? UITheme.PressScale : 1;
            if(next!=target){from=scale;target=next;age=0;}
            if(age<1)
            {
                age=Mathf.Min(1,age+dt/(target<1 ? UITheme.PressDuration : UITheme.ReleaseDuration));
                float ease=age*age*(3-2*age);scale=Mathf.Lerp(from,target,ease);transform.localScale=Vector3.one*scale;
            }
            focusAmount=Mathf.MoveTowards(focusAmount,active && focused ? 1 : 0,dt/UITheme.ColorDuration);
            if(shape!=null)shape.SetState(!active,focusAmount);
        }
        void OnDisable(){pressed=hovered=focused=false;velocity=0;scale=from=target=1;age=1;focusAmount=0;transform.localScale=Vector3.one;}
    }
}
