using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        RewardedAdsService ads;
        bool adPending;
        Button continueAdButton;
        Text continueAdLabel;
        void InitializeAds()
        {
            ads=gameObject.AddComponent<RewardedAdsService>();
            ads.Configure(Settings.Ads);
        }
        void SetAdPending(bool value)
        {
            adPending=value;
            if(overlay!=null)
                foreach(var button in overlay.GetComponentsInChildren<Button>()) button.interactable=!value;
            if(!value) { RefreshWheel(); RefreshContinueAd(); }
        }
        void ShowAdNotice(string key)
        {
            bool wheel=wheelDisk!=null;
            if(wheel) ShowWheel();
            else if(game!=null && game.State==PlayState.GameOver) EndGame();
            if(overlay!=null)
            {
                var notice=ui.Card("Ad notice",overlay,.08f,wheel ? .35f : .88f,.92f,wheel ? .407f : .94f,GameUI.Deep).rectTransform;
                ui.Text("Ad result",notice,Loc.T(key),.05f,.08f,.95f,.92f,16,GameUI.Cream);
                StartCoroutine(HideAdNotice(notice.gameObject));
            }
        }
        System.Collections.IEnumerator HideAdNotice(GameObject notice)
        {
            float age=0;while(age<2.8f && notice!=null){age+=Time.unscaledDeltaTime;yield return null;}
            if(notice!=null)Destroy(notice);
        }
        void RefreshContinueAd()
        {
            if(continueAdButton==null) return;
            continueAdButton.interactable=!adPending && game!=null && game.CanContinueAfterAd && ads.Ready;
            continueAdLabel.text=Loc.T(ads.Ready ? "ads.continue" : "ads.unavailable");
        }
        void ContinueWithAd()
        {
            var session=game;
            if(adPending || session==null || !session.CanContinueAfterAd) return;
            SetAdPending(true);
            bool started=ads.Show(RewardedPlacement.ContinueRun,earned=>
            {
                if(this==null) return;
                SetAdPending(false);
                if(game!=session) return;
                if(earned && session.ContinueAfterAd())
                {
                    if(!Application.isFocused) { session.Pause(); RenderPauseScreen(); return; }
                    overlay.gameObject.SetActive(false);
                    feedback.color=GameUI.Teal;feedback.text=Loc.T("ads.restored"); feedbackTime=2.5f;
                    Render(0);
                }
                else ShowAdNotice("ads.incomplete");
            });
            if(!started) { SetAdPending(false); ShowAdNotice("ads.unavailable"); }
        }
    }
}
