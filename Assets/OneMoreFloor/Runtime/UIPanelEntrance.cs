using UnityEngine;
namespace OneMoreFloor
{
    public sealed class UIPanelEntrance : MonoBehaviour
    {
        CanvasGroup group;float age;bool suspended;
        void OnEnable(){Replay();}
        public void Replay()
        {
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            age=0;suspended=false;group.alpha=0;transform.localScale=Vector3.one*UITheme.ModalScale;
        }
        public void Suspend(){suspended=true;}
        public void Finish(){age=UITheme.ModalIn;suspended=false;if(group!=null)group.alpha=1;transform.localScale=Vector3.one;}
        void Update()
        {
            if(suspended || age>=UITheme.ModalIn)return;
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/UITheme.ModalIn),ease=1-(1-t)*(1-t)*(1-t);
            group.alpha=ease;transform.localScale=Vector3.one*Mathf.Lerp(UITheme.ModalScale,1,ease);
        }
    }
}
