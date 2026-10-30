using System;
using UnityEngine;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        CampaignProgress progress;
        int currentLevel=1;
        static int seedSerial;
        RectTransform coachRoot, coachPointer, coachCard;
        Text coachTitle, coachDetail, pointerLabel;

        // A new seed for each attempt, also when Restart is pressed within the same millisecond.
        static int NewSeed() { unchecked { return Guid.NewGuid().GetHashCode() ^ ++seedSerial; } }

        void ShowLevels()
        {
            game=null; building.Reset(); buildingRoot.gameObject.SetActive(false);
            coachRoot.gameObject.SetActive(false); settingsLayer.gameObject.SetActive(false); ClearOverlay(true);
            ui.Text("Campaign label",overlay,"ДОМ № 6  /  ПЯТЬ РАБОЧИХ ДНЕЙ",.08f,.92f,.92f,.96f,17,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Campaign title",overlay,"Ваши истории",.06f,.825f,.94f,.92f,45,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Campaign progress",overlay,"Пройдено "+progress.Completed+" из 5. Можно переиграть любой открытый уровень.",.08f,.76f,.92f,.825f,20,GameUI.Teal);
            for(int i=0;i<Campaign.LevelCount;i++)
            {
                int number=i+1; var level=Settings.Levels[i]; bool unlocked=progress.IsUnlocked(number);
                float top=.735f-i*.12f;
                var button=ui.Button("Level "+number,overlay,"",.07f,top-.105f,.93f,top,
                    ()=>ChooseLevel(number),unlocked ? GameUI.Cream : GameUI.Hex("A4BBB3"));
                button.interactable=unlocked;
                ui.Text("Level number",button.transform,number.ToString(),.02f,.1f,.15f,.9f,40,unlocked ? GameUI.Coral : GameUI.Hex("6B8682"),TextAnchor.MiddleCenter,true);
                ui.Text("Level title",button.transform,level.Title,.19f,.45f,.92f,.9f,25,GameUI.Ink,TextAnchor.MiddleLeft,true);
                string detail=unlocked ? "Этажи 1–"+level.ActiveFloors+"  •  "+level.DeliveryGoal+" доставок" : "Пройдите уровень "+i+", чтобы открыть";
                if(number<=progress.Completed) detail+="  •  Рекорд "+PlayerPrefs.GetInt("OneMoreFloor.LevelBest."+number,0);
                ui.Text("Level detail",button.transform,detail,.19f,.09f,.94f,.47f,18,GameUI.Hex("4C7478"),TextAnchor.MiddleLeft);
                if(number<=progress.Completed)
                    ui.Text("Completed",button.transform,"✓",.87f,.47f,.98f,.92f,27,GameUI.Teal,TextAnchor.MiddleCenter,true);
            }
            ui.Button("Back to menu",overlay,"В главное меню",.12f,.055f,.88f,.122f,ShowMenu,GameUI.Teal,25);
        }
        void ChooseLevel(int number)
        {
            if(!progress.IsUnlocked(number)) return;
            currentLevel=number; var level=Settings.Levels[number-1]; ClearOverlay(true);
            var card=Modal("УРОВЕНЬ "+number+" / 5",level.Title);
            ui.Text("Level goal",card,"Доставьте "+level.DeliveryGoal+" человек",.08f,.52f,.92f,.67f,31,GameUI.Teal,TextAnchor.MiddleCenter,true);
            string detail="Доступны этажи 1–"+level.ActiveFloors+". В лифте 4 места.\nТри ушедших пассажира — повтор уровня.\n";
            detail+=level.Guided ? "Начнём с одного соседа и подсказок.\nПервая доставка без спешки и таймера." :
                level.BoxChance>0 ? "Встречаются соседи с коробкой: им нужны 2 места.\nКаждая попытка — новые люди и маршруты." :
                "Маршруты и внешность соседей случайны.\nТемп чуть бодрее, чем на первом уровне.";
            ui.Text("Level briefing",card,detail,.07f,.29f,.93f,.53f,23,GameUI.Hex("627D7C"));
            ui.Button("Begin level",card,level.Guided ? "Начать обучение →" : "Поехали →",.09f,.15f,.91f,.27f,NewGame,GameUI.Coral);
            ui.Button("Back to levels",card,"К уровням",.09f,.025f,.91f,.125f,ShowLevels,GameUI.Teal,23);
        }
        void ShowBoxIntroduction()
        {
            // No session exists yet: the explanation always precedes the first possible box spawn.
            game=null; buildingRoot.gameObject.SetActive(false); coachRoot.gameObject.SetActive(false);
            ClearOverlay(true);
            var card=Modal("НОВЫЙ СОСЕД","Осторожно,\nновоселье!");
            var person=new PassengerView(ui,card,2);
            person.Set(new Passenger(2,80,true,2),new Vector2(.5f,.41f),new Vector2(.25f,.29f),false,1);
            person.Tick(1);
            ui.Text("Box explanation",card,"Обычный пассажир, но с большой коробкой.\nЗанимает 2 из 4 мест. Нужный этаж — над головой.\nОчки и терпение такие же, как у остальных.\nСвободно одно место? Он подождёт, не нарушая очередь.",.055f,.20f,.945f,.40f,22,GameUI.Hex("627D7C"));
            ui.Button("Box understood",card,"Понятно, поехали! →",.09f,.045f,.91f,.16f,()=>
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
            var card=Modal("УРОВЕНЬ "+currentLevel+" ПРОЙДЕН!",last ? "Пять историй\nпозади!" : "Отличная\nработа!");
            ui.Text("Win score",card,game.Score+" очков",.08f,.5f,.92f,.68f,48,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Win detail",card,"Доставлено: "+game.Delivered+" человек\n"+(last ? "Все уровни открыты. Попробуете улучшить результат?" : "Открыт уровень "+(currentLevel+1)+" — "+Settings.Levels[currentLevel].Title),.07f,.36f,.93f,.51f,24,GameUI.Hex("627D7C"));
            ui.Button("Advance",card,last ? "К уровням →" : "Следующий уровень →",.09f,.205f,.91f,.325f,
                ()=>{ if(last) ShowLevels(); else ChooseLevel(currentLevel+1); },GameUI.Coral,26);
            ui.Button("Replay",card,"Повторить",.09f,.06f,.49f,.16f,NewGame,GameUI.Teal,23);
            ui.Button("Levels",card,"Уровни",.52f,.06f,.91f,.16f,ShowLevels,GameUI.Teal,23);
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
                coachTitle.text="Первая доставка! +"+game.Rules.DeliveryPoints+" очков";
                coachDetail.text="Теперь перевозите остальных. Цель: "+game.Level.DeliveryGoal+" доставок.\nПолоска — терпение. Трое ушедших — повтор уровня.";
                return;
            }
            int floor=-1; bool riding=game.Lift.Riders.Count>0;
            if(riding) floor=game.Lift.Riders[0].Destination;
            else for(int f=0;f<game.Level.ActiveFloors;f++) if(game.Queues.Floors[f].Count>0) { floor=f; break; }
            coachPointer.gameObject.SetActive(floor>=0);
            coachTitle.text=riding ? "2 / 2 • Доставьте соседа" : "1 / 2 • Заберите соседа";
            coachDetail.text=floor<0 ? "Сейчас появится первый пассажир. Спешить не нужно." :
                riding ? "Цифра над головой — нужный этаж.\nНажмите "+(floor+1)+". На остановке сосед выйдет сам." :
                "Нажмите "+(floor+1)+" слева или на его коридор.\nПосле остановки пассажир сам войдёт в лифт.";
            if(floor<0) return;
            float y=buildingRoot.anchorMin.y+(buildingRoot.anchorMax.y-buildingRoot.anchorMin.y)*(floor+.56f)/Settings.Rules.Floors;
            GameUI.Place(coachPointer,.079f,y-.024f,.57f,y+.024f);
            pointerLabel.text="Нажмите этаж "+(floor+1);
            if(game.Lift.Moving && game.Lift.TargetFloor==floor)
            {
                pointerLabel.text="Едем на этаж "+(floor+1);
                coachDetail.text="Дождитесь полной остановки лифта.\nПосадка и высадка происходят автоматически.";
            }
        }
    }
}
