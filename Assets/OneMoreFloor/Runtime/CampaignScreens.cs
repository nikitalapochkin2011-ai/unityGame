using System;
using UnityEngine;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        CampaignProgress progress;
        int currentLevel=1, levelPage;
        static int seedSerial;
        RectTransform coachRoot, coachPointer, coachCard;
        Text coachTitle, coachDetail, pointerLabel;

        // A new seed for each attempt, also when Restart is pressed within the same millisecond.
        static int NewSeed() { unchecked { return Guid.NewGuid().GetHashCode() ^ ++seedSerial; } }

        void ShowLevels()
        {
            levelPage=Campaign.PageForLevel(progress.Unlocked); RenderLevelPage();
        }
        void RenderLevelPage()
        {
            levelPage=Math.Max(0,Math.Min(Campaign.PageCount-1,levelPage));
            game=null; building.Reset(); buildingRoot.gameObject.SetActive(false);
            coachRoot.gameObject.SetActive(false); settingsLayer.gameObject.SetActive(false); ClearOverlay(true);
            int first=levelPage*Campaign.LevelsPerPage,last=Math.Min(Campaign.LevelCount,first+Campaign.LevelsPerPage);
            ScreenHeader(Loc.T("ui.allLevels"),Loc.T("campaign.progress",progress.Completed,Campaign.LevelCount),ShowMenu);
            ui.Progress("Overall chapter progress",overlay,.055f,.854f,.945f,.861f,progress.Completed/(float)Campaign.LevelCount);
            ui.Pill("Chapter range",overlay,Loc.T("campaign.range",first+1,last),.055f,.790f,.945f,.838f,GameUI.Soft,GameUI.Ink,20);
            for(int i=first;i<last;i++)
            {
                int number=i+1; var level=Settings.Levels[i]; bool unlocked=progress.IsUnlocked(number);
                float top=.770f-(i-first)*.119f;
                var button=ui.Button("Level "+number,overlay,"",.055f,top-.108f,.945f,top,
                    ()=>ChooseLevel(number),unlocked ? GameUI.Cream : GameUI.Soft);
                button.interactable=unlocked;
                var numberTile=ui.Panel("Number tile",button.transform,.035f,.18f,.165f,.82f,unlocked ? GameUI.Soft : GameUI.Border).rectTransform;
                ui.Text("Level number",numberTile,number.ToString(),.04f,.05f,.96f,.95f,28,unlocked ? GameUI.Teal : GameUI.Muted,TextAnchor.MiddleCenter,true);
                if(number==progress.Unlocked)ui.Panel("Current level marker",button.transform,.012f,.22f,.021f,.78f,GameUI.Teal);
                ui.Icon("Level status",button.transform,!unlocked ? UIIcon.Lock : number<=progress.Completed ? UIIcon.Check : UIIcon.ArrowRight,.90f,.32f,.97f,.68f,unlocked ? GameUI.Teal : GameUI.Muted);
                ui.Text("Level title",button.transform,LevelTitle(number),.19f,.47f,.88f,.9f,25,GameUI.Ink,TextAnchor.MiddleLeft,true);
                string detail=unlocked ? Loc.T("campaign.detail",level.ActiveFloors,level.DeliveryGoal) : Loc.T("campaign.locked",i);
                if(number<=progress.Completed) detail+=Loc.T("campaign.best",PlayerPrefs.GetInt("OneMoreFloor.LevelBest."+number,0));
                ui.Text("Level detail",button.transform,detail,.19f,.09f,.94f,.47f,18,GameUI.Muted,TextAnchor.MiddleLeft);

            }
            var previous=ui.SymbolButton("Previous levels",overlay,UIIcon.ArrowLeft,.055f,.105f,.155f,.162f,()=>{levelPage--; RenderLevelPage();},GameUI.Cream);
            previous.interactable=levelPage>0;
            ui.Text("Level page",overlay,(levelPage+1)+" / "+Campaign.PageCount,.3f,.107f,.7f,.163f,22,GameUI.Ink,TextAnchor.MiddleCenter,true);
            var next=ui.SymbolButton("Next levels",overlay,UIIcon.ArrowRight,.845f,.105f,.945f,.162f,()=>{levelPage++; RenderLevelPage();},GameUI.Cream);
            next.interactable=levelPage<Campaign.PageCount-1;
            ui.Button("Back to menu",overlay,Loc.T("common.menu"),.055f,.025f,.945f,.084f,ShowMenu,GameUI.Soft,21);
        }
        void ChooseLevel(int number)
        {
            if(!progress.IsUnlocked(number)) return;
            currentLevel=number; var level=Settings.Levels[number-1]; ClearOverlay(true);
            var card=Modal(Loc.T("level.number",number,Campaign.LevelCount),LevelTitle(number));
            ui.Text("Level goal",card,Loc.T("level.goal",level.DeliveryGoal),.08f,.52f,.92f,.67f,31,GameUI.Teal,TextAnchor.MiddleCenter,true);
            string detail=Loc.T("level.floors",level.ActiveFloors,Settings.Rules.Capacity)+"\n";
            detail+=(level.Guided ? Loc.T("level.guided") : Loc.T("traffic."+level.Traffic))+"\n";
            detail+=Loc.T("level.rules",Settings.Rules.LossLimit);
            ui.Text("Level briefing",card,detail,.07f,.29f,.93f,.53f,21,GameUI.Muted);
            ui.Button("Begin level",card,level.Guided ? Loc.T("level.startTutorial") : Loc.T("level.start"),.09f,.15f,.91f,.27f,NewGame,GameUI.Coral);
            ui.Button("Back to levels",card,Loc.T("common.levels"),.09f,.025f,.91f,.125f,ShowLevels,GameUI.Teal,23);
        }
        void ShowBoxIntroduction()
        {
            // No session exists yet: the explanation always precedes the first possible box spawn.
            game=null; buildingRoot.gameObject.SetActive(false); coachRoot.gameObject.SetActive(false);
            ClearOverlay(true);
            var card=Modal(Loc.T("box.eyebrow"),Loc.T("box.title"));
            var person=new PassengerView(ui,card,2);
            person.Set(new Passenger(2,80,true,2),new Vector2(.5f,.41f),new Vector2(.25f,.29f),false,1);
            person.Tick(1);
            ui.Text("Box explanation",card,Loc.T("box.detail"),.055f,.19f,.945f,.405f,21,GameUI.Muted);
            ui.Button("Box understood",card,Loc.T("box.start"),.09f,.045f,.91f,.16f,()=>
            {
                PlayerPrefs.SetInt("OneMoreFloor.BoxExplained",1); PlayerPrefs.Save(); NewGame();
            },GameUI.Coral,26);
        }
        void WinLevel()
        {
            SaveBest(); progress.Complete(currentLevel);
            PlayerPrefs.SetInt("OneMoreFloor.Campaign.Completed",progress.Completed);
            string key="OneMoreFloor.LevelBest."+currentLevel;
            PlayerPrefs.SetInt(key,Math.Max(game.Score,PlayerPrefs.GetInt(key,0))); PlayerPrefs.Save();
            coachRoot.gameObject.SetActive(false); ClearOverlay(); Play(deliveryTone);
            bool last=currentLevel==Campaign.LevelCount;
            var card=Modal(Loc.T("win.eyebrow",currentLevel),last ? Loc.T("win.last",Campaign.LevelCount) : Loc.T("win.title"));
            ui.Picture("Medal halo",card,"circle",.375f,.49f,.625f,.66f).color=GameUI.Soft;
            ui.Icon("Win medal",card,UIIcon.Star,.425f,.525f,.575f,.625f,GameUI.Gold);
            ui.Text("Win score",card,Loc.T("win.score",game.Score),.08f,.385f,.92f,.51f,41,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Win detail",card,Loc.T("win.delivered",game.Delivered)+"\n"+(last ? Loc.T("win.all") : Loc.T("win.unlocked",currentLevel+1,LevelTitle(currentLevel+1))),.07f,.29f,.93f,.39f,20,GameUI.Muted);
            ui.ActionButton("Advance",card,last ? Loc.T("common.levels") : Loc.T("win.next"),UIIcon.ArrowRight,.09f,.165f,.91f,.28f,
                ()=>{ if(last) ShowLevels(); else ChooseLevel(currentLevel+1); },GameUI.Coral,25);
            ui.Button("Replay",card,Loc.T("common.replay"),.09f,.035f,.49f,.13f,NewGame,GameUI.Soft,20);
            ui.Button("Levels",card,Loc.T("common.levels"),.52f,.035f,.91f,.13f,ShowLevels,GameUI.Soft,20);
        }
        void BuildCoach()
        {
            coachRoot=ui.Rect("Live tutorial",safe,0,0,1,1);
            // This layer is above the house, below all modals, and never captures a tap.
            coachRoot.SetSiblingIndex(overlay.GetSiblingIndex());
            coachCard=ui.Panel("Coach instructions",coachRoot,.04f,.012f,.96f,.135f,GameUI.Cream).rectTransform;
            coachTitle=ui.Text("Coach step",coachCard,"",.04f,.58f,.96f,.95f,24,GameUI.Teal,TextAnchor.MiddleCenter,true);
            coachDetail=ui.Text("Coach detail",coachCard,"",.04f,.05f,.96f,.58f,20,GameUI.Ink);
            coachPointer=ui.Rect("Arrow to actual floor",coachRoot,.08f,.2f,.57f,.25f);
            ui.Text("Arrow",coachPointer,"←",0,0,.21f,1,40,GameUI.Coral,TextAnchor.MiddleCenter,true);
            var label=ui.Panel("Arrow caption",coachPointer,.22f,.06f,1,.94f,GameUI.Gold);
            pointerLabel=ui.Text("Tap caption",label.transform,"",.025f,.05f,.975f,.95f,19,GameUI.Ink,TextAnchor.MiddleCenter,true);
            coachRoot.gameObject.SetActive(false);
        }
        void RenderCoach()
        {
            bool showing=game!=null && game.Level.Guided && game.Delivered<2 && game.State==PlayState.Playing;
            coachRoot.gameObject.SetActive(showing);
            if(!showing) return;
            if(game.Delivered==1)
            {
                coachPointer.gameObject.SetActive(false);
                coachTitle.text=Loc.T("coach.firstScore",game.Rules.DeliveryPoints);
                coachDetail.text=Loc.T("coach.afterFirst",game.Level.DeliveryGoal);
                return;
            }
            int floor=-1; bool riding=game.Lift.Riders.Count>0;
            if(riding) floor=game.Lift.Riders[0].Destination;
            else for(int f=0;f<game.Level.ActiveFloors;f++) if(game.Queues.Floors[f].Count>0) { floor=f; break; }
            coachPointer.gameObject.SetActive(floor>=0);
            coachTitle.text=Loc.T(riding ? "coach.deliver" : "coach.pickup");
            coachDetail.text=floor<0 ? Loc.T("coach.wait") :
                riding ? Loc.T("coach.deliverDetail",floor+1) :
                Loc.T("coach.pickupDetail",floor+1);
            if(floor<0) return;
            float y=buildingRoot.anchorMin.y+(buildingRoot.anchorMax.y-buildingRoot.anchorMin.y)*(floor+.56f)/Settings.Rules.Floors;
            GameUI.Place(coachPointer,.079f,y-.024f,.57f,y+.024f);
            pointerLabel.text=Loc.T("coach.tap",floor+1);
            if(game.Lift.Moving && game.Lift.TargetFloor==floor)
            {
                pointerLabel.text=Loc.T("coach.moving",floor+1);
                coachDetail.text=Loc.T("coach.stop");
            }
        }
    }
}
