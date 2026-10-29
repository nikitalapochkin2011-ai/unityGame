using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    public sealed class GameApp : MonoBehaviour
    {
        public GameSettings Settings;
        GameSession game;
        GameUI ui;
        BuildingView building;
        RectTransform safe, overlay, buildingRoot;
        CanvasGroup screenFade;
        Text score, multiplier, occupancy, route, feedback, record, soundLabel, streak, lossesLabel;
        Image loadCard;
        Image[] lossMarks, streakMarks;
        int best, sessionStartBest;
        bool sound;
        float feedbackTime, screenAge;
        Rect lastSafe;
        Vector2Int lastScreen;
        AudioSource audioSource;
        AudioClip boardTone, deliveryTone, lossTone, tapTone;
        readonly List<AudioClip> clips = new List<AudioClip>();

        void Start()
        {
            Application.targetFrameRate=60; Screen.orientation=ScreenOrientation.Portrait;
            if(Settings==null) Settings=ScriptableObject.CreateInstance<GameSettings>();
            Settings.Rules.Validate();
            ui=new GameUI();
            best=PlayerPrefs.GetInt("OneMoreFloor.Best",0);
            sound=PlayerPrefs.GetInt("OneMoreFloor.Sound",1)!=0;
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false;
            boardTone=Tone("Boarding",new []{440f,554f},.09f,.08f);
            deliveryTone=Tone("Delivered",new []{523f,659f,784f},.085f,.09f);
            lossTone=Tone("Lost",new []{294f,247f},.14f,.07f);
            tapTone=Tone("Tap",new []{400f},.035f,.035f);
            BuildUI(); ShowMenu();
        }
        AudioClip Tone(string name,float[] notes,float noteLength,float volume)
        {
            const int rate=44100; int noteSamples=(int)(rate*noteLength);
            var samples=new float[noteSamples*notes.Length];
            for(int n=0;n<notes.Length;n++)
                for(int i=0;i<noteSamples;i++)
                {
                    float t=(float)i/rate;
                    float envelope=Mathf.Min(1,t/.008f)*Mathf.Pow(1-(float)i/noteSamples,2);
                    samples[n*noteSamples+i]=Mathf.Sin(2*Mathf.PI*notes[n]*t)*envelope*volume;
                }
            var clip=AudioClip.Create(name,samples.Length,1,rate,false); clip.SetData(samples,0); clips.Add(clip); return clip;
        }
        void Play(AudioClip clip) { if(sound && audioSource!=null) audioSource.PlayOneShot(clip); }
        void BuildUI()
        {
            var root=new GameObject("One More Floor UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(600,1060); scaler.matchWidthOrHeight=1;
            ui.Panel("Evening sky",root.transform,0,0,1,1,GameUI.Hex("BDD4D0"),false);
            ui.Picture("Distant neighborhood",root.transform,"skyline",0,0,1,.62f,false);
            ui.Panel("Street",root.transform,0,0,1,.055f,GameUI.Hex("648A89"),false);
            safe=ui.Rect("Portrait safe area",root.transform,0,0,1,1);
            ui.Text("Brand",safe,"ЕЩЁ ОДИН ЭТАЖ",.04f,.95f,.79f,.993f,28,GameUI.Ink,TextAnchor.MiddleLeft,true);
            ui.Text("Subtitle",safe,"МАЛЕНЬКИЙ ДОМ. БОЛЬШИЕ ПЛАНЫ.",.043f,.923f,.8f,.949f,14,GameUI.Hex("4C7478"),TextAnchor.MiddleLeft);
            ui.Button("Pause",safe,"II",.85f,.936f,.96f,.991f,Pause,GameUI.Ink,28);
            var scoreCard=ui.Panel("Score card",safe,.04f,.851f,.96f,.918f,GameUI.Cream);
            ui.Text("Score caption",scoreCard.transform,"ОЧКИ",.04f,.48f,.23f,.94f,13,GameUI.Muted,TextAnchor.MiddleLeft,true);
            score=ui.Text("Score",scoreCard.transform,"0",.04f,.01f,.4f,.66f,36,GameUI.Ink,TextAnchor.MiddleLeft,true);
            multiplier=ui.Text("Multiplier",scoreCard.transform,"×1",.77f,.08f,.96f,.92f,34,GameUI.Teal,TextAnchor.MiddleCenter,true);
            streak=ui.Text("Streak caption",scoreCard.transform,"ДО ×2",.42f,.55f,.75f,.94f,13,GameUI.Muted);
            streakMarks=new Image[5];
            for(int i=0;i<5;i++) streakMarks[i]=ui.Panel("Streak dot",scoreCard.transform,.435f+i*.06f,.22f,.475f+i*.06f,.4f,GameUI.Hex("D9D9C9"));
            var load=ui.Panel("Occupancy card",safe,.04f,.789f,.49f,.842f,GameUI.Hex("DCE6D8")); loadCard=load;
            occupancy=ui.Text("Occupancy",load.transform,"В лифте  0 / 4",.04f,.1f,.96f,.9f,22,GameUI.Ink,TextAnchor.MiddleCenter,true);
            var losses=ui.Panel("Lost card",safe,.51f,.789f,.96f,.842f,GameUI.Hex("DCE6D8"));
            lossesLabel=ui.Text("Lost caption",losses.transform,"УШЛИ",.06f,.15f,.46f,.85f,16,GameUI.Hex("607D7D"),TextAnchor.MiddleLeft,true);
            lossMarks=new Image[Settings.Rules.LossLimit];
            for(int i=0;i<lossMarks.Length;i++)
            {
                float x=.49f+i*.43f/lossMarks.Length;
                lossMarks[i]=ui.Picture("Loss mark",losses.transform,"circle",x,.31f,x+.3f/lossMarks.Length,.69f);
            }
            buildingRoot=ui.Rect("Illustrated apartment building",safe,.04f,.14f,.96f,.735f);
            building=new BuildingView(ui,buildingRoot,Settings.Rules,SelectFloor);
            var routeCard=ui.Panel("Route",safe,.04f,.067f,.96f,.117f,GameUI.Ink);
            route=ui.Text("Route",routeCard.transform,"Выберите этаж",.03f,.08f,.97f,.92f,22,GameUI.Cream,TextAnchor.MiddleCenter,true);
            feedback=ui.Text("Feedback",safe,"",.04f,.025f,.96f,.065f,20,GameUI.Ink);
            overlay=ui.Rect("Screen overlay",safe,0,0,1,1);
            screenFade=overlay.gameObject.AddComponent<CanvasGroup>();
            ApplySafeArea();
        }
        void ClearOverlay(bool menu=false)
        {
            foreach(Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.gameObject.SetActive(true); screenAge=0; screenFade.alpha=0;
            var scrim=ui.Panel("Modal background",overlay,-.2f,-.1f,1.2f,1.1f,
                menu ? GameUI.Hex("BDD4D0") : new Color(.08f,.18f,.23f,.67f),false);
            scrim.raycastTarget=true;
        }
        RectTransform Modal(string eyebrow,string title)
        {
            var card=ui.Panel("Screen card",overlay,.055f,.16f,.945f,.85f,GameUI.Cream).rectTransform;
            ui.Text("Eyebrow",card,eyebrow,.08f,.86f,.92f,.94f,16,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Heading",card,title,.06f,.67f,.94f,.87f,43,GameUI.Ink,TextAnchor.MiddleCenter,true);
            return card;
        }
        void ShowMenu()
        {
            game=null; building.Reset(); buildingRoot.gameObject.SetActive(false); ClearOverlay(true);
            ui.Picture("Menu skyline",overlay,"skyline",-.13f,0,1.13f,.73f,false);
            ui.Text("House label",overlay,"ДОМ № 6  /  ВАША НОВАЯ СМЕНА",.08f,.917f,.92f,.95f,16,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Game title",overlay,"Ещё один\nэтаж",.07f,.735f,.93f,.916f,68,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Tagline",overlay,"Кому-то домой. Кому-то в гости.\nА вам — ещё один рекорд.",.1f,.65f,.9f,.742f,23,GameUI.Hex("476B70"));
            var hero=ui.Panel("Lobby illustration",overlay,.12f,.333f,.88f,.635f,GameUI.Hex("E8D3B5")).rectTransform;
            ui.Panel("Lobby floor",hero,.02f,.015f,.98f,.09f,GameUI.Hex("C19B7A"),false);
            ui.Picture("Lobby window",hero,"window",.045f,.38f,.31f,.89f);
            ui.Picture("Lobby plant",hero,"plant",.045f,.085f,.23f,.39f);
            ui.Picture("Lobby elevator",hero,"cabin",.52f,.06f,.94f,.97f);
            MenuPerson(hero,0,.24f,.09f,.47f,.77f,"4");
            MenuPerson(hero,1,.40f,.08f,.63f,.74f,"2");
            MenuPerson(hero,4,.68f,.13f,.87f,.74f,"6");
            ui.Text("Lobby plaque",hero,"ДОБРО ПОЖАЛОВАТЬ",.04f,.9f,.48f,.98f,12,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Button("Play",overlay,"Начать смену  →",.12f,.237f,.88f,.313f,NewGame,GameUI.Coral,31);
            record=ui.Text("Record",overlay,"ЛИЧНЫЙ РЕКОРД   "+best,.12f,.17f,.88f,.224f,23,GameUI.Ink,TextAnchor.MiddleCenter,true);
            var soundButton=ui.Button("Sound",overlay,"",.27f,.092f,.73f,.146f,ToggleSound,GameUI.Teal,21);
            soundLabel=soundButton.GetComponentInChildren<Text>(); UpdateSoundLabel();
            ui.Text("Menu instructions",overlay,"Нажимайте этажи. Перевозите людей.\nНе дайте троим уйти, не дождавшись лифта.",.08f,.012f,.92f,.077f,18,GameUI.Hex("476B70"));
        }
        void MenuPerson(Transform parent,int index,float x0,float y0,float x1,float y1,string destination)
        {
            ui.Picture("Neighbor",parent,"person_"+index,x0,y0,x1,y1);
            var bubble=ui.Panel("Neighbor destination",parent,x0+.035f,y1-.005f,x1-.035f,y1+.115f,GameUI.Cream);
            ui.Text("Destination",bubble.transform,destination,0,0,1,1,24,GameUI.Ink,TextAnchor.MiddleCenter,true);
        }
        void ToggleSound()
        {
            sound=!sound; PlayerPrefs.SetInt("OneMoreFloor.Sound",sound ? 1 : 0); PlayerPrefs.Save(); UpdateSoundLabel(); Play(tapTone);
        }
        void UpdateSoundLabel() { if(soundLabel!=null) soundLabel.text=sound ? "Звук включён" : "Звук выключен"; }
        void SelectFloor(int floor)
        {
            if(game==null || game.State!=PlayState.Playing) return;
            game.SelectFloor(floor); Play(tapTone);
        }
        void NewGame()
        {
            building.Reset(); buildingRoot.gameObject.SetActive(true);
            game=new GameSession(Settings.Rules,Environment.TickCount); sessionStartBest=best;
            game.Feedback+=OnFeedback; feedback.text="Нажмите номер этажа — лифт отправится к нему";
            feedbackTime=5; overlay.gameObject.SetActive(false); Render(0);
            if(PlayerPrefs.GetInt("OneMoreFloor.Tutorial",0)==0) ShowTutorial();
        }
        void ShowTutorial()
        {
            game.Pause(); ClearOverlay();
            var card=Modal("ПЕРВАЯ СМЕНА","Здесь все\nсвои");
            ui.Text("Tutorial",card,
                "1. Нажмите на этаж или коридор.\nЛифт заберёт людей по очереди.\n\n2. Цифра над головой — куда ехать.\nПолоска под ногами — терпение.\n\n3. Можно заранее выбрать следующую\nостановку. Не дайте троим уйти!",
                .07f,.24f,.93f,.67f,23,GameUI.Hex("627D7C"));
            ui.Button("Understood",card,"Понятно, поехали!",.09f,.065f,.91f,.195f,()=>
            {
                PlayerPrefs.SetInt("OneMoreFloor.Tutorial",1); PlayerPrefs.Save();
                game.Resume(); overlay.gameObject.SetActive(false);
            },GameUI.Coral,26);
        }
        void OnFeedback(string message)
        {
            feedback.text=message; feedbackTime=2.2f;
            if(message.StartsWith("Доставлено")) Play(deliveryTone);
            else if(message.StartsWith("Пассажир")) Play(lossTone);
            else if(message.StartsWith("Вошли")) Play(boardTone);
        }
        void Pause()
        {
            if(game==null || game.State!=PlayState.Playing) return;
            game.Pause(); SaveBest(); ClearOverlay();
            var card=Modal("ЛИФТ ПОДОЖДЁТ","Небольшой\nперерыв");
            ui.Text("Pause detail",card,"Пассажиры никуда не торопятся,\nпока игра на паузе.",.08f,.51f,.92f,.65f,23,GameUI.Hex("627D7C"));
            ui.Button("Continue",card,"Продолжить",.09f,.345f,.91f,.47f,()=>{ game.Resume(); overlay.gameObject.SetActive(false); },GameUI.Coral);
            ui.Button("Restart",card,"Начать заново",.09f,.19f,.91f,.305f,NewGame);
            ui.Button("Menu",card,"В главное меню",.09f,.055f,.91f,.155f,ShowMenu,GameUI.Hex("708E8C"),23);
        }
        void SaveBest()
        {
            if(game!=null && game.Score>best) { best=game.Score; PlayerPrefs.SetInt("OneMoreFloor.Best",best); PlayerPrefs.Save(); }
        }
        void EndGame()
        {
            bool newRecord=game.Score>sessionStartBest; SaveBest(); ClearOverlay();
            var card=Modal(newRecord ? "НОВЫЙ ЛИЧНЫЙ РЕКОРД!" : "СПАСИБО ЗА СМЕНУ","На сегодня\nвсё!");
            ui.Text("Final score",card,game.Score.ToString(),.1f,.49f,.9f,.68f,68,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Results",card,"Доставлено: "+game.Delivered+"    •    Рекорд: "+best,.06f,.4f,.94f,.49f,22,GameUI.Hex("627D7C"));
            ui.Button("Retry",card,"Ещё одну смену  →",.09f,.22f,.91f,.355f,NewGame,GameUI.Coral);
            ui.Button("Menu",card,"В главное меню",.09f,.07f,.91f,.18f,ShowMenu);
        }
        void Update()
        {
            ApplySafeArea();
            if(overlay!=null && overlay.gameObject.activeSelf)
            { screenAge+=Time.unscaledDeltaTime; screenFade.alpha=Mathf.Clamp01(screenAge/.18f); }
            if(game==null) return;
            var before=game.State;
            game.Tick(Time.unscaledDeltaTime);
            if(before==PlayState.Playing)
            {
                float dt=Mathf.Min(Time.unscaledDeltaTime,.25f);
                feedbackTime-=dt; if(feedbackTime<=0) feedback.text="Число над головой — нужный пассажиру этаж";
                Render(dt);
            }
            if(before!=PlayState.GameOver && game.State==PlayState.GameOver) EndGame();
        }
        void Render(float dt)
        {
            score.text=game.Score.ToString(); multiplier.text="×"+game.Multiplier;
            streak.text=game.Multiplier==3 ? "МАКСИМУМ" : "ДО ×"+(game.Multiplier+1);
            for(int i=0;i<streakMarks.Length;i++) streakMarks[i].color=game.Multiplier==3 || i<game.Streak%5 ? GameUI.Gold : GameUI.Hex("D9D9C9");
            for(int i=0;i<lossMarks.Length;i++) lossMarks[i].color=i<game.Lost ? GameUI.Coral : GameUI.Hex("ADC2B8");
            occupancy.text="В лифте  "+game.Lift.Riders.Count+" / "+game.Rules.Capacity;
            loadCard.color=game.Lift.Riders.Count==game.Rules.Capacity ? GameUI.Gold : GameUI.Hex("DCE6D8");
            lossesLabel.text="УШЛИ "+game.Lost+"/"+game.Rules.LossLimit;
            route.text=game.Lift.Moving ? "Едем на "+(game.Lift.TargetFloor+1)+" этаж" : "Остановка: "+(game.Lift.CurrentFloor+1)+" этаж";
            if(game.Lift.NextFloor.HasValue) route.text+="   •   Затем: "+(game.Lift.NextFloor.Value+1);
            building.Render(game,dt);
        }
        void ApplySafeArea()
        {
            if(safe==null || Screen.width==0 || Screen.height==0) return;
            var area=Screen.safeArea;
            if(area==lastSafe && lastScreen==new Vector2Int(Screen.width,Screen.height)) return;
            lastSafe=area; lastScreen=new Vector2Int(Screen.width,Screen.height);
            // Keep a portrait game readable even in a wide Editor Game window/tablet.
            float width=Mathf.Min(area.width,area.height*600f/1060f);
            float left=area.x+(area.width-width)*.5f;
            safe.anchorMin=new Vector2(left/Screen.width,area.yMin/Screen.height);
            safe.anchorMax=new Vector2((left+width)/Screen.width,area.yMax/Screen.height);
        }
        void OnApplicationPause(bool paused) { if(paused) { SaveBest(); Pause(); } }
        void OnApplicationFocus(bool focused) { if(!focused) { SaveBest(); Pause(); } }
        void OnApplicationQuit() { SaveBest(); }
        void OnDestroy() { foreach(var clip in clips) if(clip!=null) Destroy(clip); }
    }
}
