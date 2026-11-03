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
        void SaveCosmetics() { if(!wheelTestMode) CosmeticStore.Save(cosmetics); }
        string ItemName(int id) { return Loc.T("cosmetic."+(id%Cosmetics.PerKind)); }
        string Category(int kind) { return Loc.T("collection.kind."+kind); }
        string RewardName(int id) { return Category(id/Cosmetics.PerKind)+": "+ItemName(id)+" · "+Loc.T("rarity."+(int)Cosmetics.Rarity(id%Cosmetics.PerKind)); }
        Text Wallet(Transform parent,bool golden=false)
        { return ui.Text("Wallet",parent,Loc.T("wheel.coins",cosmetics.Coins),.06f,.88f,.94f,.925f,23,golden ? GameUI.Gold : GameUI.Teal,TextAnchor.MiddleCenter,true); }
        void ShowWheel(SpinReward result=null,int stoppedSector=0)
        {
            if(RequireLanguage()) return;
            ClearOverlay(true); wheelDisk=null;
            ui.Panel("Velvet backdrop",overlay,-.2f,-.1f,1.2f,1.1f,GameUI.Hex("192D36"),false);
            ui.Text("Title",overlay,Loc.T("wheel.title"),.04f,.925f,.96f,.99f,36,GameUI.Gold,TextAnchor.MiddleCenter,true);
            wheelWallet=Wallet(overlay,true);
            ui.Text("Wheel help",overlay,Loc.T("wheel.tryLuck"),.06f,.837f,.94f,.875f,25,GameUI.Gold,TextAnchor.MiddleCenter,true);
            // A square viewport keeps the wheel circular at all portrait safe-area sizes.
            var slot=ui.Rect("Wheel slot",overlay,.14f,.415f,.86f,.78f);
            var viewport=ui.Rect("Wheel viewport",slot,0,0,1,1);
            var aspect=viewport.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio=1;
            ui.Picture("Wheel shadow",viewport,"circle",-.025f,-.04f,1.025f,1.01f,false).color=new Color(0,0,0,.4f);
            wheelDisk=ui.Rect("Rotating wheel",viewport,0,0,1,1);
            var graphic=AddWheelGraphic<FortuneWheelGraphic>(wheelDisk); graphic.raycastTarget=false;
            for(int sector=0;sector<FortuneWheelGraphic.Sectors;sector++)
            {
                float angle=sector*Mathf.PI*2/FortuneWheelGraphic.Sectors;
                float x=.5f+Mathf.Sin(angle)*.335f,y=.5f+Mathf.Cos(angle)*.335f;
                RewardIcon(wheelDisk,FortuneWheelGraphic.KindAt(sector),x-.075f,y-.075f,x+.075f,y+.075f);
            }
            wheelDisk.localEulerAngles=new Vector3(0,0,FortuneWheelLayout.AngleForSector(stoppedSector));
            // The pointer belongs to the stationary viewport, never to the rotating disk.
            var pointer=ui.Rect("Fixed downward pointer",viewport,.445f,.905f,.555f,1.04f);
            AddWheelGraphic<WheelPointerGraphic>(pointer);
            RewardIcon(overlay,-1,.19f,.36f,.255f,.403f);
            ui.Text("Coin legend",overlay,Loc.T("wheel.coinLabel"),.26f,.36f,.46f,.403f,18,GameUI.Cream);
            RewardIcon(overlay,0,.52f,.36f,.585f,.403f);
            ui.Text("Mystery legend",overlay,Loc.T("wheel.decoration"),.59f,.36f,.89f,.403f,18,GameUI.Cream);
            var prizeCard=ui.Panel("Reveal card",overlay,.065f,.288f,.935f,.352f,GameUI.Hex("304550")).rectTransform;
            if(result==null)
                ui.Text("Mystery",prizeCard,Loc.T("wheel.mystery"),.03f,.06f,.97f,.94f,20,GameUI.Cream);
            else
            {
                if(result.IsCoins) RewardIcon(prizeCard,-1,.015f,.08f,.15f,.92f);
                else
                {
                    int id=result.CosmeticId,kind=id/Cosmetics.PerKind,index=id%Cosmetics.PerKind;
                    var preview=ui.Panel("Revealed style",prizeCard,.015f,.08f,.15f,.92f,GameUI.Hex("BDD4D0"),false).rectTransform;
                    CosmeticStyle.Preview(ui,preview,(CosmeticKind)kind,index);
                }
                ui.Text("Revealed reward",prizeCard,result.IsCoins ? Loc.T("wheel.coinReward",result.CoinAmount) : Loc.T("wheel.reward",RewardName(result.CosmeticId)),.17f,.06f,.98f,.94f,22,GameUI.Gold,TextAnchor.MiddleCenter,true);
            }
            freeSpin=ui.Button("Free spin",overlay,"",.07f,.20f,.93f,.28f,BeginDailyAdSpin,GameUI.Hex("C7953C"),23);
            freeCountdown=freeSpin.GetComponentInChildren<Text>(); freeCountdown.color=GameUI.Ink;
            paidSpin=ui.Button("Paid spin",overlay,Loc.T("wheel.paid",Cosmetics.SpinCost),.07f,.105f,.93f,.185f,()=>BeginSpin(false),GameUI.Hex("75572F"),23);
            ui.Button("Back",overlay,Loc.T("common.menu"),.07f,.025f,.47f,.09f,ShowMenu,GameUI.Ink,21);
            ui.Button("Collection",overlay,Loc.T("collection.title"),.50f,.025f,.93f,.09f,()=>ShowCollection(),GameUI.Ink,21);
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
        void ShowCollection(int kind=0,int rarity=0)
        {
            if(RequireLanguage()) return;
            ClearOverlay(true); wheelDisk=null;
            ui.Text("Collection title",overlay,Loc.T("collection.title"),.04f,.925f,.96f,.99f,36,GameUI.Ink,TextAnchor.MiddleCenter,true);
            Wallet(overlay);
            for(int k=0;k<3;k++)
            {
                int tab=k; float x=.035f+k*.32f;
                ui.Button("Category",overlay,Category(k),x,.79f,x+.29f,.865f,()=>ShowCollection(tab,rarity),k==kind ? GameUI.Coral : GameUI.Teal,20);
            }
            for(int r=0;r<4;r++)
            {
                int filter=r; float x=.035f+r*.24f;
                ui.Button("Rarity filter",overlay,Loc.T("rarity."+r),x,.727f,x+.225f,.782f,
                    ()=>ShowCollection(kind,filter),r==rarity ? CosmeticStyle.RarityColor(r==0 ? 0 : r==1 ? 10 : r==2 ? 15 : 20) : GameUI.Ink,15);
            }
            int start=rarity==0 ? 0 : 10+(rarity-1)*5, count=rarity==0 ? 10 : 5;
            for(int slot=0;slot<count;slot++)
            {
                int index=start+slot; float x=.045f+(slot%2)*.48f,y=.714f-(slot/2)*.117f;
                bool owned=cosmetics.Owns((CosmeticKind)kind,index),selected=cosmetics.Selected((CosmeticKind)kind)==index;
                var card=ui.Panel("Cosmetic card",overlay,x,y-.108f,x+.43f,y,selected ? GameUI.Gold : GameUI.Cream).rectTransform;
                ui.Panel("Rarity accent",card,0,.08f,.012f,.92f,CosmeticStyle.RarityColor(index),false);
                var preview=ui.Panel("Preview",card,.025f,.09f,.33f,.92f,GameUI.Hex("BDD4D0"),false).rectTransform;
                CosmeticStyle.Preview(ui,preview,(CosmeticKind)kind,index);
                ui.Text("Item name",card,ItemName(index),.35f,.65f,.98f,.98f,17,GameUI.Ink,TextAnchor.MiddleCenter,true);
                ui.Text("Rarity",card,Loc.T("rarity."+rarity),.35f,.43f,.98f,.67f,13,CosmeticStyle.RarityColor(index));
                string status=selected ? "collection.selected" : owned ? "collection.select" : "collection.locked";
                var button=ui.Button("Select cosmetic",card,Loc.T(status),.35f,.055f,.97f,.43f,()=>
                {
                    if(!cosmetics.Select((CosmeticKind)kind,index)) return;
                    SaveCosmetics(); RebuildCosmeticUI(); ShowCollection(kind,rarity);
                },selected ? GameUI.Teal : GameUI.Ink,15);
                button.interactable=owned && !selected;
            }
            ui.Text("Cosmetic note",overlay,Loc.T("collection.detail"),.05f,.072f,.95f,.115f,17,GameUI.Ink);
            ui.Button("Menu",overlay,Loc.T("common.menu"),.05f,.012f,.48f,.07f,ShowMenu,GameUI.Ink,21);
            ui.Button("Wheel",overlay,Loc.T("wheel.title"),.52f,.012f,.95f,.07f,()=>ShowWheel(),GameUI.Coral,21);
        }
        void RebuildCosmeticUI()
        {
            building.Reset(); uiRoot.SetActive(false); Destroy(uiRoot);
            lastScreen=Vector2Int.zero; ui=new GameUI(); BuildUI();
            buildingRoot.gameObject.SetActive(false);
        }
    }
}
