using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        Cosmetics cosmetics;
        bool wheelTestMode;
#if UNITY_EDITOR
        public static string WheelTestPreference { get { return "OneMoreFloor.WheelTest."+Application.dataPath; } }
#endif
        void InitializeWheelTesting()
        {
#if UNITY_EDITOR
            wheelTestMode=UnityEditor.EditorPrefs.GetBool(WheelTestPreference,false);
#endif
        }
        SpinReward RollWheel(bool free,bool rewardEarned=false)
        {
#if UNITY_EDITOR
            if(wheelTestMode) return cosmetics.SpinPreview(UtcNow,cosmeticRandom);
#endif
            return free ? cosmetics.SpinAfterAd(rewardEarned,UtcNow,cosmeticRandom) : cosmetics.Spin(false,UtcNow,cosmeticRandom);
        }
        readonly System.Random cosmeticRandom=new System.Random();
        RectTransform wheelDisk;
        Text freeCountdown,wheelWallet;
        Button freeSpin,paidSpin;
        bool spinning;
        float countdownTick;
        static long UtcNow { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
        void SaveCosmetics()
        {CosmeticStore.SaveAccount(wheelTestMode ? CosmeticStore.Load() : cosmetics,achievements);}
        string ItemName(int id) { return Loc.T("cosmetic."+(id%Cosmetics.PerKind)); }
        string Category(int kind) { return Loc.T("collection.kind."+kind); }
        string RewardName(int id) { return Category(id/Cosmetics.PerKind)+": "+ItemName(id)+" · "+Loc.T("rarity."+(int)Cosmetics.Rarity(id%Cosmetics.PerKind)); }
        Text Wallet(Transform parent,bool golden=false)
        {
            var chip=ui.Surface("Wallet capsule",parent,.30f,.817f,.70f,.866f,golden ? new Color(.70f,.84f,.72f,.085f) : UITheme.Soft,UITheme.SmallRadius,false,false,true).rectTransform;
            ui.Icon("Wallet coin",chip,UIIcon.Coin,.055f,.20f,.18f,.80f,golden ? UITheme.Gold : UITheme.Green);
            return ui.Text("Wallet",chip,Loc.T("wheel.coins",cosmetics.Coins),.23f,.08f,.95f,.92f,18,golden ? UITheme.OnDark : UITheme.Ink,TextAnchor.MiddleCenter);
        }
        void ShowWheel(SpinReward result=null,int stoppedSector=0)
        {
            if(RequireLanguage()) return;
            ClearOverlay(true); wheelDisk=null;
            ui.Gradient("Velvet backdrop",overlay,-.2f,-.1f,1.2f,1.1f,UITheme.Deep,GameUI.Hex("284F40"));
            ScreenHeader(Loc.T("wheel.title"),Loc.T("wheel.tryLuck"),ShowMenu,true);
            wheelWallet=Wallet(overlay,true);
            ui.Picture("Wheel halo",overlay,"circle",.09f,.400f,.91f,.790f,false).color=new Color(.52f,.76f,.58f,.10f);
            // The viewport remains square: camera ratios never stretch the disk.
            var slot=ui.Rect("Wheel slot",overlay,.105f,.395f,.895f,.790f);
            var viewport=ui.Rect("Wheel viewport",slot,0,0,1,1);
            var aspect=viewport.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
            ui.Picture("Wheel shadow",viewport,"circle",-.012f,-.034f,1.012f,.990f,false).color=new Color(0,0,0,.25f);
            wheelDisk=ui.Rect("Rotating wheel",viewport,0,0,1,1);
            AddWheelGraphic<FortuneWheelGraphic>(wheelDisk);
            for(int sector=0;sector<FortuneWheelGraphic.Sectors;sector++)
            {
                float angle=sector*Mathf.PI*2/FortuneWheelGraphic.Sectors;
                float x=.5f+Mathf.Sin(angle)*.335f,y=.5f+Mathf.Cos(angle)*.335f;
                RewardIcon(wheelDisk,FortuneWheelGraphic.KindAt(sector),x-.075f,y-.075f,x+.075f,y+.075f);
            }
            wheelDisk.localEulerAngles=new Vector3(0,0,FortuneWheelLayout.AngleForSector(stoppedSector));
            AddWheelGraphic<WheelPointerGraphic>(ui.Rect("Fixed downward pointer",viewport,.445f,.905f,.555f,1.04f));
            RewardIcon(overlay,-1,.18f,.350f,.235f,.389f);
            ui.Text("Coin legend",overlay,Loc.T("wheel.coinLabel"),.245f,.350f,.46f,.389f,15,UITheme.OnDark,TextAnchor.MiddleLeft);
            RewardIcon(overlay,0,.525f,.350f,.580f,.389f);
            ui.Text("Mystery legend",overlay,Loc.T("wheel.decoration"),.595f,.350f,.89f,.389f,15,UITheme.OnDark,TextAnchor.MiddleLeft);
            var prizeCard=ui.Surface("Reveal card",overlay,.055f,.266f,.945f,.343f,new Color(.72f,.82f,.67f,.065f),UITheme.SmallRadius,false,false,true).rectTransform;
            if(result!=null)prizeCard.gameObject.AddComponent<UIPanelEntrance>();
            if(result==null)
            {
                ui.Icon("Mystery sparkle",prizeCard,UIIcon.Star,.035f,.24f,.115f,.76f,UITheme.Gold);
                ui.Text("Mystery",prizeCard,Loc.T("wheel.mystery"),.15f,.10f,.97f,.90f,18,UITheme.OnDark,TextAnchor.MiddleLeft);
            }
            else if(result.IsCoins)
            {
                RewardIcon(prizeCard,-1,.025f,.13f,.17f,.87f);
                ui.Text("Revealed reward",prizeCard,Loc.T("wheel.coinReward",result.CoinAmount),.20f,.12f,.96f,.88f,27,UITheme.Gold,TextAnchor.MiddleLeft,true);
            }
            else
            {
                int id=result.CosmeticId,kind=id/Cosmetics.PerKind,index=id%Cosmetics.PerKind;
                var preview=ui.Panel("Revealed style",prizeCard,.025f,.12f,.18f,.88f,UITheme.Soft).rectTransform;
                CosmeticStyle.Preview(ui,preview,(CosmeticKind)kind,index);
                ui.Text("Reward item name",prizeCard,ItemName(id),.22f,.41f,.96f,.91f,22,UITheme.OnDark,TextAnchor.MiddleLeft);
                ui.Text("Reward rarity",prizeCard,Category(kind)+" · "+Loc.T("rarity."+(int)Cosmetics.Rarity(index)),.22f,.10f,.96f,.44f,14,UITheme.Gold,TextAnchor.MiddleLeft);
            }
            paidSpin=ui.ActionButton("Paid spin",overlay,Loc.T("wheel.paid",Cosmetics.SpinCost),UIIcon.Coin,.055f,.171f,.945f,.246f,()=>BeginSpin(false),UITheme.DarkAction,24,UIButtonStyle.DarkPrimary);
            freeSpin=ui.ActionButton("Free spin",overlay,"",UIIcon.Video,.055f,.091f,.945f,.154f,BeginDailyAdSpin,UITheme.Deep,19,UIButtonStyle.DarkSecondary);
            freeCountdown=freeSpin.GetComponentInChildren<Text>();freeCountdown.color=UITheme.Gold;
            GameUI.Place(freeCountdown.rectTransform,.16f,.38f,.94f,.91f);
            ui.Text("Daily ad schedule",freeSpin.transform,Loc.T("wheel.adDaily"),.16f,.08f,.94f,.36f,12,new Color(UITheme.Gold.r,UITheme.Gold.g,UITheme.Gold.b,.72f));
            ui.ActionButton("Collection",overlay,Loc.T("collection.title"),UIIcon.Collection,.055f,.018f,.945f,.070f,()=>ShowCollection(),UITheme.Deep,18,UIButtonStyle.DarkSecondary);
            RefreshWheel();
        }
        void RefreshWheel()
        {
            if(freeCountdown==null) return;
            long left=cosmetics.FreeIn(UtcNow);
            freeCountdown.text=wheelTestMode ? Loc.T("ads.testSpin") : cosmetics.Complete ? Loc.T("wheel.complete") : left==0 ? Loc.T(ads.Ready ? "wheel.adSpin" : "ads.unavailable") : Loc.T("wheel.wait",string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:00}:{1:00}:{2:00}",left/3600,left/60%60,left%60));
            freeSpin.interactable=!spinning && !adPending && (wheelTestMode || (!cosmetics.Complete && left==0 && ads.Ready));
            paidSpin.interactable=!spinning && !adPending && !wheelTestMode && !cosmetics.Complete && cosmetics.Coins>=Cosmetics.SpinCost;
        }
        void UpdateWheel()
        {
            if(wheelDisk==null || !wheelDisk.gameObject.activeInHierarchy) return;
            countdownTick+=Time.unscaledDeltaTime;
            if(countdownTick>=1) { countdownTick=0; RefreshWheel(); }
        }
        void BeginDailyAdSpin()
        {
            if(wheelTestMode) { BeginSpin(true,true); return; }
            if(adPending || spinning || cosmetics.Complete || cosmetics.FreeIn(UtcNow)>0) return;
            SetAdPending(true);
            bool started=ads.Show(RewardedPlacement.DailyWheel,earned=>
            {
                if(this==null) return;
                SetAdPending(false);
                if(earned) BeginSpin(true,true);
                else ShowAdNotice("ads.incomplete");
            });
            if(!started) { SetAdPending(false); ShowAdNotice("ads.unavailable"); }
        }
        void BeginSpin(bool free,bool rewardEarned=false)
        {
            if(spinning || adPending) return;
            int previousBalance=cosmetics.Coins;
            float startRotation=wheelDisk!=null ? wheelDisk.localEulerAngles.z : 0;
            var reward=RollWheel(free,rewardEarned);
            if(reward==null) { RefreshWheel(); return; }
            // Persist before animation: backgrounding or quitting never loses the awarded item.
            SaveCosmetics();
            ShowWheel();
            wheelDisk.localEulerAngles=new Vector3(0,0,startRotation);
            // Award has already been saved, but revealing the wallet would spoil its amount.
            wheelWallet.text=Loc.T("wheel.coins",previousBalance);
            spinning=true;
            foreach(var button in overlay.GetComponentsInChildren<Button>()) button.interactable=false;
            // Only the generic category is represented on the disk; the exact prize is
            // neither labelled nor tinted until the animation has finished.
            int sector=FortuneWheelLayout.SectorFor(reward,cosmeticRandom);
            StartCoroutine(AnimateSpin(reward,sector,startRotation));
        }
        IEnumerator AnimateSpin(SpinReward reward,int sector,float startRotation)
        {
            var disk=wheelDisk;
            float t=0;
            while(t<3.4f && disk!=null)
            {
                t+=Time.unscaledDeltaTime;
                float progress=Mathf.Clamp01(t/3.4f);
                disk.localEulerAngles=new Vector3(0,0,FortuneWheelLayout.RotationAt(startRotation,sector,progress));
                yield return null;
            }
            // Resolve the visible stopping slot from the final rotation used by the animation.
            int landed=FortuneWheelLayout.SectorUnderPointer(FortuneWheelLayout.RotationAt(startRotation,sector,1));
            spinning=false;
            if(disk!=null && disk.gameObject.activeInHierarchy) ShowWheel(reward,landed);
        }
        T AddWheelGraphic<T>(RectTransform rect) where T : Graphic
        {
            // Explicit renderer setup also works with uGUI versions where Graphic does
            // not declare CanvasRenderer as a required component.
            if(rect.GetComponent<CanvasRenderer>()==null) rect.gameObject.AddComponent<CanvasRenderer>();
            var graphic=rect.gameObject.AddComponent<T>(); graphic.raycastTarget=false;
            graphic.canvasRenderer.SetColor(Color.white);
            graphic.SetAllDirty(); return graphic;
        }
        void RewardIcon(Transform parent,int kind,float x0,float y0,float x1,float y1)
        {
            var icon=AddWheelGraphic<WheelRewardIcon>(ui.Rect("Prize emblem",parent,x0,y0,x1,y1));
            icon.Kind=kind; icon.raycastTarget=false;
        }
        void ShowCollection(int kind=0,int rarity=0,int page=0)
        {
            if(RequireLanguage()) return;
            ClearOverlay(true);wheelDisk=null;
            int ownedCount=0;for(int k=0;k<3;k++)for(int i=0;i<Cosmetics.PerKind;i++)if(cosmetics.Owns((CosmeticKind)k,i))ownedCount++;
            ScreenHeader(Loc.T("collection.title"),Loc.T("ui.collectionCount",ownedCount,Cosmetics.Total),ShowMenu);
            var categories=ui.Surface("Category tabs",overlay,.055f,.809f,.945f,.864f,UITheme.Soft,UITheme.SmallRadius,false,false).rectTransform;
            for(int k=0;k<3;k++)
            {
                int tab=k;float x=.008f+k*.331f;
                ui.Button("Category",categories,Category(k),x,.08f,x+.322f,.92f,()=>ShowCollection(tab,rarity),k==kind ? UITheme.Surface : UITheme.Soft,19,false);
            }
            for(int r=0;r<4;r++)
            {
                int filter=r;float x=.055f+r*.226f;
                ui.Button("Rarity filter",overlay,Loc.T("rarity."+r),x,.750f,x+.213f,.796f,
                    ()=>ShowCollection(kind,filter),r==rarity ? CosmeticStyle.RarityColor(r==0 ? 0 : r==1 ? 10 : r==2 ? 15 : 20) : UITheme.Surface,14,false);
            }
            const int pageSize=6;
            int start=rarity==0 ? 0 : 10+(rarity-1)*5,count=rarity==0 ? 10 : 5,pages=(count+pageSize-1)/pageSize;
            page=Mathf.Clamp(page,0,pages-1);int currentPage=page;
            for(int slot=0;slot<Mathf.Min(pageSize,count-page*pageSize);slot++)
            {
                int index=start+page*pageSize+slot;float x=.055f+(slot%2)*.460f,y=.723f-(slot/2)*.183f;
                bool owned=cosmetics.Owns((CosmeticKind)kind,index),selected=cosmetics.Selected((CosmeticKind)kind)==index;
                var card=(RectTransform)ui.Button("Cosmetic card",overlay,"",x,y-.171f,x+.430f,y,
                    ()=>CollectionDetail(kind,index,rarity,currentPage),selected ? UITheme.Soft : UITheme.Surface,
                    UITheme.ButtonText,true,UIButtonStyle.Card).transform;
                var preview=ui.Panel("Preview",card,.045f,.560f,.955f,.950f,UITheme.Soft).rectTransform;
                CosmeticStyle.Preview(ui,preview,(CosmeticKind)kind,index);
                ui.Text("Item name",card,ItemName(index),.055f,.390f,.945f,.550f,18,UITheme.Ink,TextAnchor.MiddleLeft);
                ui.Text("Rarity",card,Loc.T("rarity."+rarity),.055f,.280f,.945f,.390f,13,CosmeticStyle.RarityInk(index),TextAnchor.MiddleLeft,true);
                string status=selected ? "collection.selected" : owned ? "collection.select" : "collection.locked";
                var equip=ui.Button("Select cosmetic",card,Loc.T(status),.055f,.025f,.945f,.260f,()=>
                {
                    if(!cosmetics.Select((CosmeticKind)kind,index))return;
                    SaveCosmetics();RebuildCosmeticUI();ShowCollection(kind,rarity,currentPage);
                },selected ? UITheme.Green : UITheme.Soft,14,false);
                equip.interactable=owned && !selected;
                if(selected)
                {
                    var badge=ui.Picture("Equipped badge",preview,"circle",.83f,.64f,.98f,.96f);badge.color=UITheme.Green;
                    ui.Icon("Equipped tick",badge.transform,UIIcon.Check,.20f,.20f,.80f,.80f,UITheme.ContrastText(UITheme.Green));
                }
            }
            var previous=ui.SymbolButton("Previous cosmetics",overlay,UIIcon.ArrowLeft,.055f,.110f,.155f,.165f,()=>ShowCollection(kind,rarity,currentPage-1),UITheme.Surface);previous.interactable=page>0;
            ui.Text("Collection page",overlay,(page+1)+" / "+pages,.25f,.110f,.75f,.165f,16,UITheme.Muted);
            var next=ui.SymbolButton("Next cosmetics",overlay,UIIcon.ArrowRight,.845f,.110f,.945f,.165f,()=>ShowCollection(kind,rarity,currentPage+1),UITheme.Surface);next.interactable=page<pages-1;
            ui.Text("Cosmetic note",overlay,Loc.T("collection.detail"),.075f,.075f,.925f,.106f,13,UITheme.Muted);
            ui.ActionButton("Wheel",overlay,Loc.T("wheel.title"),UIIcon.Wheel,.055f,.018f,.945f,.069f,()=>ShowWheel(),UITheme.Green,19);
        }
        void RebuildCosmeticUI()
        {
            building.Reset(); uiRoot.SetActive(false); Destroy(uiRoot);
            lastScreen=Vector2Int.zero; ui=CreateUI(); BuildUI();
            buildingRoot.gameObject.SetActive(false);
        }
    }
}
