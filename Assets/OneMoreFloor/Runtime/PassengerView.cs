using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // One persistent visual per passenger: the same person walks from queue to cabin.
    public sealed class PassengerView
    {
        public readonly RectTransform Root;
        public bool WasRiding { get; private set; }
        public bool Departing { get; private set; }
        public bool Finished { get { return Departing && departureTime >= .55f; } }
        readonly CanvasGroup opacity;
        readonly Image portrait, badge, patience;
        readonly RectTransform patienceTrack;
        readonly Text destination;
        readonly int appearance;
        Vector2 position, target, size;
        float age, departureTime;
        bool initialized;
        public PassengerView(GameUI ui, Transform parent, int appearance)
        {
            this.appearance = appearance;
            Root = ui.Rect("Passenger " + appearance,parent,0,0,0,0);
            opacity = Root.gameObject.AddComponent<CanvasGroup>(); opacity.blocksRaycasts = false; opacity.interactable = false;
            portrait = ui.Picture("Person",Root,"person_"+(appearance%8),0,.06f,1,.8f);
            // Destination always belongs to the person and stays above their head.
            badge = ui.Panel("Destination bubble",Root,.15f,.79f,.85f,1,GameUI.Cream);
            var tip = ui.Picture("Bubble tip",Root,"triangle",.43f,.75f,.6f,.82f);
            tip.color = GameUI.Cream; tip.rectTransform.localEulerAngles = new Vector3(0,0,-90);
            destination = ui.Text("Destination",badge.transform,"",0,0,1,1,23,GameUI.Ink,TextAnchor.MiddleCenter,true);
            patienceTrack = ui.Panel("Patience track",Root,.09f,.015f,.91f,.055f,GameUI.Ink).rectTransform;
            patience = ui.Panel("Patience",patienceTrack,0,0,1,1,GameUI.Teal,false);
        }
        public void Set(Passenger passenger, Vector2 center, Vector2 dimensions, bool riding, float patienceRatio)
        {
            target = center; size = dimensions;
            if (riding && WasRiding && initialized) position.y = center.y;
            WasRiding = riding;
            if (!initialized) { position = target + new Vector2(-.025f,0); initialized = true; }
            destination.text = (passenger.Destination+1).ToString();
            badge.color = riding ? GameUI.Gold : GameUI.Cream;
            patienceTrack.gameObject.SetActive(!riding);
            patience.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(patienceRatio),1);
            patience.color = Color.Lerp(GameUI.Coral,GameUI.Teal,Mathf.Clamp01(patienceRatio * 1.6f));
            // An impatient passenger shows a warm warning badge as well as a shortening bar.
            if (!riding && patienceRatio < .25f) badge.color = GameUI.Hex("F5BA91");
        }
        public void Leave(bool delivered)
        {
            if (Departing) return;
            Departing = true; departureTime = 0;
            target = position + new Vector2(delivered ? .12f : -.1f,.035f);
            destination.text = delivered ? "✓" : "!";
            badge.color = delivered ? GameUI.Gold : GameUI.Coral;
            patienceTrack.gameObject.SetActive(false);
        }
        public void Tick(float dt)
        {
            age += dt;
            float distance = Vector2.Distance(position,target);
            position = Vector2.Lerp(position,target,1-Mathf.Exp(-dt*15));
            if (Departing) { departureTime += dt; opacity.alpha = Mathf.Clamp01(1-departureTime/.55f); }
            else opacity.alpha = Mathf.Clamp01(age/.22f);
            float bob = distance > .004f ? Mathf.Sin(age*23)*.004f : Mathf.Sin(age*2.8f+appearance)*.001f;
            GameUI.Place(Root,position.x-size.x*.5f,position.y,position.x+size.x*.5f,position.y+size.y);
            portrait.rectTransform.localEulerAngles = new Vector3(0,0,distance>.004f ? Mathf.Sin(age*23)*3 : 0);
            portrait.rectTransform.anchoredPosition = new Vector2(0,bob * ((RectTransform)Root.parent).rect.height);
        }
        public void Destroy() { Root.gameObject.SetActive(false); Object.Destroy(Root.gameObject); }
    }
}
