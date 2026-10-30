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
        readonly System.Random cosmeticRandom=new System.Random();
        RectTransform wheelDisk;
        Text freeCountdown;
        Button freeSpin,paidSpin;
        bool spinning;
        float countdownTick;
        static long UtcNow { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
        void SaveCosmetics() { CosmeticStore.Save(cosmetics); }
        string ItemName(int id) { return Loc.T("cosmetic."+(id%10)); }
        string Category(int kind) { return Loc.T("collection.kind."+kind); }
        string RewardName(int id) { return Category(id/10)+": "+ItemName(id); }
        void Wallet(Transform parent)
        { ui.Text("Wallet",parent,Loc.T("wheel.coins",cosmetics.Coins),.06f,.88f,.94f,.925f,23,GameUI.Teal,TextAnchor.MiddleCenter,true); }
        void ShowWheel(string result="")
        {
            if(RequireLanguage()) return;
            ClearOverlay(true); wheelDisk=null;
            ui.Text("Title",overlay,Loc.T("wheel.title"),.04f,.925f,.96f,.99f,36,GameUI.Ink,TextAnchor.MiddleCenter,true);
            Wallet(overlay);
            ui.Text("Wheel help",overlay,Loc.T("wheel.detail"),.06f,.79f,.94f,.88f,19,GameUI.Ink);
            // A square viewport keeps the wheel circular at all portrait safe-area sizes.
            var slot=ui.Rect("Wheel slot",overlay,.14f,.415f,.86f,.78f);
            var viewport=ui.Rect("Wheel viewport",slot,0,0,1,1);
            var aspect=viewport.gameObject.AddComponent<AspectRatioFitter>(); aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio=1;
            wheelDisk=ui.Rect("Rotating wheel",viewport,0,0,1,1);
            var graphic=wheelDisk.gameObject.AddComponent<FortuneWheelGraphic>(); graphic.raycastTarget=false;
            for(int id=0;id<30;id++)
            {
                float angle=id*Mathf.PI*2/30;
                float x=.5f+Mathf.Sin(angle)*.40f,y=.5f+Mathf.Cos(angle)*.40f;
                var t=ui.Text("Sector",wheelDisk,cosmetics.Owns((CosmeticKind)(id/10),id%10) ? "✓" : (id%10+1).ToString(),x-.035f,y-.025f,x+.035f,y+.025f,13,GameUI.Ink);
                t.rectTransform.localEulerAngles=new Vector3(0,0,-id*12);
            }
            ui.Picture("Pointer",viewport,"triangle",.455f,.96f,.545f,1.06f).rectTransform.localEulerAngles=new Vector3(0,0,180);
            ui.Text("Wheel legend",overlay,Loc.T("wheel.legend",Category(0),Category(1),Category(2)),.04f,.355f,.96f,.413f,17,GameUI.Ink);
            ui.Text("Reward",overlay,result,.04f,.29f,.96f,.355f,22,GameUI.Teal,TextAnchor.MiddleCenter,true);
            freeSpin=ui.Button("Free spin",overlay,"",.07f,.20f,.93f,.28f,()=>BeginSpin(true),GameUI.Coral,23);
            freeCountdown=freeSpin.GetComponentInChildren<Text>();
            paidSpin=ui.Button("Paid spin",overlay,Loc.T("wheel.paid",Cosmetics.SpinCost),.07f,.105f,.93f,.185f,()=>BeginSpin(false),GameUI.Teal,23);
            ui.Button("Back",overlay,Loc.T("common.menu"),.07f,.025f,.47f,.09f,ShowMenu,GameUI.Ink,21);
            ui.Button("Collection",overlay,Loc.T("collection.title"),.50f,.025f,.93f,.09f,()=>ShowCollection(),GameUI.Ink,21);
            RefreshWheel();
        }
        void RefreshWheel()
        {
            if(freeCountdown==null) return;
            long left=cosmetics.FreeIn(UtcNow);
            freeCountdown.text=cosmetics.Complete ? Loc.T("wheel.complete") : left==0 ? Loc.T("wheel.free") : Loc.T("wheel.wait",string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:00}:{1:00}:{2:00}",left/3600,left/60%60,left%60));
            freeSpin.interactable=!spinning && !cosmetics.Complete && left==0;
            paidSpin.interactable=!spinning && !cosmetics.Complete && cosmetics.Coins>=Cosmetics.SpinCost;
        }
        void UpdateWheel()
        {
            if(wheelDisk==null || !wheelDisk.gameObject.activeInHierarchy) return;
            countdownTick+=Time.unscaledDeltaTime;
            if(countdownTick>=1) { countdownTick=0; RefreshWheel(); }
        }
        void BeginSpin(bool free)
        {
            if(spinning) return;
            int reward=cosmetics.Spin(free,UtcNow,cosmeticRandom);
            if(reward<0) { RefreshWheel(); return; }
            // Persist before animation: backgrounding or quitting never loses the awarded item.
            SaveCosmetics(); spinning=true;
            foreach(var button in overlay.GetComponentsInChildren<Button>()) button.interactable=false;
            StartCoroutine(AnimateSpin(reward));
        }
        IEnumerator AnimateSpin(int reward)
        {
            var disk=wheelDisk;
            float t=0, end=360*5+reward*12;
            while(t<2.8f && disk!=null)
            {
                t+=Time.unscaledDeltaTime;
                float progress=Mathf.Clamp01(t/2.8f);
                disk.localEulerAngles=new Vector3(0,0,end*(1-Mathf.Pow(1-progress,3)));
                yield return null;
            }
            spinning=false;
            if(disk!=null && disk.gameObject.activeInHierarchy) ShowWheel(Loc.T("wheel.reward",RewardName(reward)));
        }
        void ShowCollection(int kind=0)
        {
            if(RequireLanguage()) return;
            ClearOverlay(true); wheelDisk=null;
            ui.Text("Collection title",overlay,Loc.T("collection.title"),.04f,.925f,.96f,.99f,36,GameUI.Ink,TextAnchor.MiddleCenter,true);
            Wallet(overlay);
            for(int k=0;k<3;k++)
            {
                int tab=k; float x=.035f+k*.32f;
                ui.Button("Category",overlay,Category(k),x,.79f,x+.29f,.865f,()=>ShowCollection(tab),k==kind ? GameUI.Coral : GameUI.Teal,20);
            }
            for(int i=0;i<10;i++)
            {
                int index=i; float x=.045f+(i%2)*.48f,y=.765f-(i/2)*.131f;
                bool owned=cosmetics.Owns((CosmeticKind)kind,i),selected=cosmetics.Selected((CosmeticKind)kind)==i;
                var card=ui.Panel("Cosmetic card",overlay,x,y-.12f,x+.43f,y,selected ? GameUI.Gold : GameUI.Cream).rectTransform;
                var preview=ui.Panel("Preview",card,.02f,.14f,.32f,.91f,GameUI.Hex("BDD4D0"),false).rectTransform;
                if(kind==0) { ui.Picture("Original neighborhood",preview,"skyline",0,0,1,1,false); CosmeticStyle.Background(ui,preview,i); }
                else { ui.Picture("Material preview",preview,kind==1 ? "hallway" : "cabin",0,0,1,1,false).color=CosmeticStyle.Tint(i); CosmeticStyle.Detail(ui,preview,i,kind==2); }
                ui.Text("Item name",card,ItemName(i),.34f,.58f,.98f,.98f,19,GameUI.Ink,TextAnchor.MiddleCenter,true);
                string status=selected ? "collection.selected" : owned ? "collection.select" : "collection.locked";
                var button=ui.Button("Select cosmetic",card,Loc.T(status),.35f,.06f,.97f,.54f,()=>
                {
                    if(!cosmetics.Select((CosmeticKind)kind,index)) return;
                    SaveCosmetics(); RebuildCosmeticUI(); ShowCollection(kind);
                },selected ? GameUI.Teal : GameUI.Ink,17);
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
