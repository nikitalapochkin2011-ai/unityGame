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
            ui.Text("Campaign label",overlay,Loc.T("campaign.label",Campaign.LevelCount),.08f,.92f,.92f,.96f,17,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Campaign title",overlay,Loc.T("campaign.range",first+1,last),.06f,.825f,.94f,.92f,45,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Campaign progress",overlay,Loc.T("campaign.progress",progress.Completed,Campaign.LevelCount),.08f,.76f,.92f,.825f,20,GameUI.Teal);
            for(int i=first;i<last;i++)
            {
                int number=i+1; var level=Settings.Levels[i]; bool unlocked=progress.IsUnlocked(number);
                float top=.735f-(i-first)*.112f;
                var button=ui.Button("Level "+number,overlay,"",.07f,top-.102f,.93f,top,
                    ()=>ChooseLevel(number),unlocked ? GameUI.Cream : GameUI.Hex("A4BBB3"));
                button.interactable=unlocked;
                ui.Text("Level number",button.transform,number.ToString(),.02f,.1f,.15f,.9f,36,unlocked ? GameUI.Coral : GameUI.Hex("6B8682"),TextAnchor.MiddleCenter,true);
                ui.Text("Level title",button.transform,LevelTitle(number),.19f,.47f,.88f,.9f,25,GameUI.Ink,TextAnchor.MiddleLeft,true);
                string detail=unlocked ? Loc.T("campaign.detail",level.ActiveFloors,level.DeliveryGoal) : Loc.T("campaign.locked",i);
                if(number<=progress.Completed) detail+=Loc.T("campaign.best",PlayerPrefs.GetInt("OneMoreFloor.LevelBest."+number,0));
                ui.Text("Level detail",button.transform,detail,.19f,.09f,.94f,.47f,18,GameUI.Hex("4C7478"),TextAnchor.MiddleLeft);
                if(number<=progress.Completed)
                    ui.Text("Completed",button.transform,"✓",.88f,.49f,.98f,.92f,27,GameUI.Teal,TextAnchor.MiddleCenter,true);
            }
            var previous=ui.Button("Previous levels",overlay,"←",.07f,.107f,.27f,.163f,()=>{levelPage--; RenderLevelPage();},GameUI.Teal,28);
            previous.interactable=levelPage>0;
            ui.Text("Level page",overlay,(levelPage+1)+" / "+Campaign.PageCount,.3f,.107f,.7f,.163f,22,GameUI.Ink,TextAnchor.MiddleCenter,true);
            var next=ui.Button("Next levels",overlay,"→",.73f,.107f,.93f,.163f,()=>{levelPage++; RenderLevelPage();},GameUI.Teal,28);
            next.interactable=levelPage<Campaign.PageCount-1;
            ui.Button("Back to menu",overlay,Loc.T("common.menu"),.12f,.025f,.88f,.088f,ShowMenu,GameUI.Teal,25);
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
            ui.Text("Level briefing",card,detail,.07f,.29f,.93f,.53f,21,GameUI.Hex("627D7C"));
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
            ui.Text("Box explanation",card,Loc.T("box.detail"),.055f,.19f,.945f,.405f,21,GameUI.Hex("627D7C"));
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
            ui.Text("Win score",card,Loc.T("win.score",game.Score),.08f,.5f,.92f,.68f,48,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Win detail",card,Loc.T("win.delivered",game.Delivered)+"\n"+(last ? Loc.T("win.all") : Loc.T("win.unlocked",currentLevel+1,LevelTitle(currentLevel+1))),.07f,.36f,.93f,.51f,24,GameUI.Hex("627D7C"));
            ui.Button("Advance",card,last ? Loc.T("common.levels") : Loc.T("win.next"),.09f,.205f,.91f,.325f,
                ()=>{ if(last) ShowLevels(); else ChooseLevel(currentLevel+1); },GameUI.Coral,26);
            ui.Button("Replay",card,Loc.T("common.replay"),.09f,.06f,.49f,.16f,NewGame,GameUI.Teal,23);
            ui.Button("Levels",card,Loc.T("common.levels"),.52f,.06f,.91f,.16f,ShowLevels,GameUI.Teal,23);
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
