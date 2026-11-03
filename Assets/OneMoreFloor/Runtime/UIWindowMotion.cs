using UnityEngine;
namespace OneMoreFloor
{
    // Pure presentation. Input is released immediately; gameplay callbacks are never
    // postponed until a closing animation finishes. Reopening cancels a pending close.
    public sealed class UIWindowMotion : MonoBehaviour
    {
        CanvasGroup group;UIPanelEntrance panel;float age,alpha,startScale;bool closing,opening;
        public static bool IsClosing(GameObject window){var motion=window.GetComponent<UIWindowMotion>();return motion!=null && motion.closing;}
        public static void Show(GameObject window,bool replay=true)
        {
            var motion=window.GetComponent<UIWindowMotion>();
            bool animate=replay && (!window.activeSelf || (motion!=null && motion.closing));
            if(motion==null && animate)motion=window.AddComponent<UIWindowMotion>();
            if(motion!=null)motion.Cancel(replay);
            window.SetActive(true);
            if(animate)motion.Open();
        }
        public static void Hide(GameObject window)
        {
            if(!window.activeSelf)return;
            var motion=window.GetComponent<UIWindowMotion>();if(motion==null)motion=window.AddComponent<UIWindowMotion>();
            motion.Begin();
        }
        void Open()
        {
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            group.alpha=0;age=0;opening=true;
        }
        void Begin()
        {
            if(closing)return;opening=false;group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            panel=GetComponentInChildren<UIPanelEntrance>();if(panel!=null)panel.Suspend();
            startScale=panel!=null ? panel.transform.localScale.x : 1;
            age=0;alpha=group.alpha;group.blocksRaycasts=false;group.interactable=false;closing=true;
        }
        void Cancel(bool replay)
        {
            bool wasClosing=closing;closing=opening=false;
            if(group!=null){group.alpha=1;group.blocksRaycasts=true;group.interactable=true;}
            if(panel!=null){if(wasClosing && replay)panel.Replay();else panel.Finish();}
        }
        void Update()
        {
            if(opening)
            {
                age+=Time.unscaledDeltaTime;float progress=Mathf.Clamp01(age/UITheme.ModalIn);
                group.alpha=1-(1-progress)*(1-progress)*(1-progress);if(progress>=1)opening=false;return;
            }
            if(!closing)return;age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/UITheme.ModalOut),ease=t*t*(3-2*t);
            group.alpha=alpha*(1-ease);if(panel!=null)panel.transform.localScale=Vector3.one*Mathf.Lerp(startScale,UITheme.ModalScale,ease);
            if(t<1)return;closing=false;gameObject.SetActive(false);group.alpha=1;group.blocksRaycasts=true;group.interactable=true;
        }
    }
}
