using UnityEngine;
namespace OneMoreFloor
{
    public sealed class UIPanelEntrance : MonoBehaviour
    {
        CanvasGroup group;float age;
        void OnEnable()
        {
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            age=0;group.alpha=0;transform.localScale=Vector3.one*.975f;
        }
        void Update()
        {
            if(age>=.22f)return;
            age+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(age/.22f),ease=1-(1-t)*(1-t)*(1-t);
            group.alpha=ease;transform.localScale=Vector3.one*Mathf.Lerp(.975f,1,ease);

        }
    }
}
