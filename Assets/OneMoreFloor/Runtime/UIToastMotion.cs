using UnityEngine;
namespace OneMoreFloor
{
    // Keeps the old 2.8-second notice lifetime while softening both ends.
    public sealed class UIToastMotion : MonoBehaviour
    {
        CanvasGroup group;float age;
        void OnEnable()
        {
            group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;group.interactable=false;group.alpha=0;age=0;
        }
        void Update()
        {
            age+=Time.unscaledDeltaTime;
            group.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.2f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((2.8f-age)/.24f));
            if(age>=2.8f)Destroy(gameObject);
        }
    }
}
