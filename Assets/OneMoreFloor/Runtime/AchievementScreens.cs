using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Text=TMPro.TMP_Text;
namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        Achievements achievements;
        RectTransform achievementAlert,achievementNotice;
        CanvasGroup achievementNoticeOpacity;
        Text achievementNoticeTitle,achievementNoticeReward;
        readonly Queue<int> achievementNotices=new Queue<int>();
        int activeAchievement=-1;
        float achievementNoticeAge;
        const float NoticeIn=.24f,NoticeHold=3.2f,NoticeOut=.20f;

        float UtilityHeight {get{return Mathf.Max(.058f,44f/Mathf.Max(1,Screen.safeArea.height));}}
        Button CircularControl(string name,UIIcon icon,float centerX,float centerY,Action action)
        {
            float pixels=Mathf.Min(Screen.safeArea.width,Screen.safeArea.height*600f/1060f);
            float width=Mathf.Max(.133f,44f/Mathf.Max(1,pixels)),height=UtilityHeight;
            var button=ui.SymbolButton(name,overlay,icon,centerX-width*.5f,centerY-height*.5f,centerX+width*.5f,centerY+height*.5f,action,UITheme.Surface);
            var fit=button.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;
            return button;
        }
        Button MenuUtility(string name,UIIcon icon,float bottom,float top,Action action)
        {
            float upperCenter=Mathf.Min(.96f,.994f-UtilityHeight*.5f);
            float center=name=="Menu settings" ? upperCenter : Mathf.Min((bottom+top)*.5f,upperCenter-UtilityHeight-.012f);
            return CircularControl(name,icon,.8885f,center,action);
        }
        string AchievementTitle(int id)
        {
            var definition=Achievements.Definition(id);
            return definition.IsRooftop ? Loc.T("achievement.roof."+definition.FinaleLevel) : Loc.T("achievement.deliver",definition.Target);
        }
        void ShowAchievements(int category=0,int page=0)
        {
            if(RequireLanguage())return;
            ClearOverlay(true);wheelDisk=null;
            category=Math.Max(0,Math.Min(1,category));
            ScreenHeader(Loc.T("achievement.title"),Loc.T("achievement.summary",achievements.EarnedCount,Achievements.Count),ShowMenu);
            Wallet(overlay);
            const int pageSize=4;
            int count=category==0 ? Achievements.DeliveryCount : Achievements.Count-Achievements.DeliveryCount;
            int pages=(count+pageSize-1)/pageSize;
            page=Math.Max(0,Math.Min(pages-1,page));int currentCategory=category,currentPage=page;
            for(int tab=0;tab<2;tab++)
            {
                int chosen=tab;float left=tab==0 ? .055f : .513f;
                ui.Button("Achievement category "+tab,overlay,Loc.T(tab==0 ? "achievement.deliveries" : "achievement.rooftops"),left,.744f,left+.432f,.797f,
                    ()=>ShowAchievements(chosen),tab==category ? UITheme.Green : UITheme.Surface,19,false);
            }
            for(int row=0;row<pageSize;row++)
            {
                int index=page*pageSize+row;if(index>=count)break;
                int id=category==0 ? index : index+Achievements.DeliveryCount;
                var definition=Achievements.Definition(id);bool earned=achievements.Earned(id),claimed=achievements.Claimed(id);
                float top=.723f-row*.139f;
                var card=ui.Card("Achievement card "+id,overlay,.055f,top-.125f,.945f,top).rectTransform;
                var emblem=ui.Surface("Achievement emblem",card,.025f,.19f,.18f,.83f,UITheme.Soft,UITheme.SmallRadius,false,false).rectTransform;
                if(definition.IsRooftop)
                    ui.Picture("Achievement guest",emblem,"person_"+RooftopFinales.ForLevel(definition.FinaleLevel).Appearance,.12f,.03f,.88f,.97f);
                else ui.Icon("Delivery achievement",emblem,UIIcon.Person,.17f,.15f,.83f,.85f,UITheme.Green);
                ui.Text("Achievement name",card,AchievementTitle(id),.215f,.43f,.715f,.96f,20,UITheme.Ink,TextAnchor.MiddleLeft,earned && !claimed);
                string detail=claimed ? Loc.T("achievement.claimed") : definition.IsRooftop ? Loc.T("achievement.level",definition.FinaleLevel) : Loc.T("achievement.progress",achievements.Progress(id),definition.Target);
                ui.Text("Achievement progress",card,detail,.215f,.21f,.715f,.43f,13,earned && !claimed ? UITheme.Green : UITheme.Muted,TextAnchor.MiddleLeft);
                ui.Progress("Achievement progress bar",card,.215f,.11f,.715f,.15f,(float)achievements.Progress(id)/definition.Target,earned ? UITheme.Gold : UITheme.Green);
                string claimText=claimed ? Loc.T("achievement.received") : earned ? Loc.T("achievement.claim",definition.Reward) : Loc.T("achievement.reward",definition.Reward);
                var claim=ui.Button("Claim achievement "+id,card,claimText,.755f,.20f,.975f,.82f,()=>ClaimAchievement(id,currentCategory,currentPage),earned && !claimed ? UITheme.Green : UITheme.Soft,16,false);
                claim.interactable=earned && !claimed;
            }
            var previous=CircularControl("Previous achievements",UIIcon.ArrowLeft,.12f,.133f,()=>ShowAchievements(currentCategory,currentPage-1));previous.interactable=page>0;
            ui.Text("Achievement page",overlay,(page+1)+" / "+pages,.30f,.104f,.70f,.162f,17,UITheme.Muted);
            var next=CircularControl("Next achievements",UIIcon.ArrowRight,.88f,.133f,()=>ShowAchievements(currentCategory,currentPage+1));next.interactable=page<pages-1;
            ui.Button("Achievements menu",overlay,Loc.T("common.menu"),.055f,.025f,.945f,.084f,ShowMenu,UITheme.Soft,21);
        }
        void ClaimAchievement(int id,int category,int page)
        {
            var wallet=wheelTestMode ? CosmeticStore.Load() : cosmetics;
            int reward=achievements.Claim(id,wallet);if(reward==0)return;
            CosmeticStore.SaveAccount(wallet,achievements);
            if(wheelTestMode)cosmetics.AddCoins(reward);
            UpdateAchievementAlert();ShowAchievements(category,page);
        }
        void UpdateAchievementAlert()
        {if(achievementAlert!=null)achievementAlert.gameObject.SetActive(achievements.PendingCount>0);}
        void ResetAchievementNotices()
        {
            achievementNotices.Clear();activeAchievement=-1;achievementNoticeAge=0;
            if(achievementNotice!=null)achievementNotice.gameObject.SetActive(false);
            if(achievementNoticeOpacity!=null)achievementNoticeOpacity.alpha=0;
        }
        void QueueAchievementNotices(int mask)
        {
            for(int id=0;id<Achievements.Count;id++)if((mask&(1<<id))!=0)achievementNotices.Enqueue(id);
            UpdateAchievementAlert();
        }
        void BuildAchievementNotice()
        {
            var layer=ui.Rect("Achievement notification layer",safe,0,0,1,1);layer.gameObject.AddComponent<RectMask2D>();
            achievementNoticeOpacity=layer.gameObject.AddComponent<CanvasGroup>();achievementNoticeOpacity.blocksRaycasts=false;achievementNoticeOpacity.interactable=false;
            achievementNotice=ui.Surface("Achievement notification",layer,.036f,.835f,.964f,.917f,UITheme.Surface,UITheme.ButtonRadius,true).rectTransform;
            ui.Icon("Achievement notification flag",achievementNotice,UIIcon.Flag,.025f,.24f,.105f,.77f,UITheme.Green);
            ui.Text("Achievement earned",achievementNotice,Loc.T("achievement.earned"),.135f,.65f,.78f,.93f,12,UITheme.Green,TextAnchor.MiddleLeft,true);
            achievementNoticeTitle=ui.Text("Achievement notification title",achievementNotice,"",.135f,.09f,.78f,.65f,18,UITheme.Ink,TextAnchor.MiddleLeft);
            var reward=ui.Surface("Achievement reward",achievementNotice,.81f,.17f,.975f,.82f,UITheme.Soft,UITheme.BadgeRadius,false,false).rectTransform;
            ui.Icon("Achievement reward coin",reward,UIIcon.Coin,.06f,.34f,.34f,.77f,UITheme.Gold);
            achievementNoticeReward=ui.Text("Achievement reward amount",reward,"",.38f,.13f,.96f,.88f,20,UITheme.Ink);
            achievementNotice.gameObject.SetActive(false);achievementNoticeOpacity.alpha=0;
            if(activeAchievement>=0)SetAchievementNoticeText();
        }
        void SetAchievementNoticeText()
        {
            achievementNoticeTitle.text=AchievementTitle(activeAchievement);
            achievementNoticeReward.text=Achievements.Definition(activeAchievement).Reward.ToString();
        }
        void TickAchievementNotice(float dt)
        {
            if(achievementNotice==null)return;
            if(game==null){ResetAchievementNotices();return;}
            if(game.State==PlayState.Paused){achievementNotice.gameObject.SetActive(false);return;}
            if(activeAchievement<0)
            {
                if(achievementNotices.Count==0)return;
                activeAchievement=achievementNotices.Dequeue();achievementNoticeAge=0;SetAchievementNoticeText();
            }
            achievementNotice.gameObject.SetActive(true);achievementNoticeAge+=dt;
            float enter=Mathf.SmoothStep(0,1,Mathf.Clamp01(achievementNoticeAge/NoticeIn));
            float exit=Mathf.SmoothStep(0,1,Mathf.Clamp01((achievementNoticeAge-NoticeHold)/NoticeOut));
            float visible=enter*(1-exit),bottom=Mathf.Lerp(1.012f,.835f,visible);
            achievementNoticeOpacity.alpha=visible;GameUI.Place(achievementNotice,.036f,bottom,.964f,bottom+.082f);
            if(achievementNoticeAge>=NoticeHold+NoticeOut){activeAchievement=-1;achievementNotice.gameObject.SetActive(false);}
        }
    }
}
