using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp : MonoBehaviour
    {
        public GameSettings Settings;
        GameSession game;
        GameUI ui;
        BuildingView building;
        RectTransform safe, overlay, buildingRoot, settingsLayer;
        CanvasGroup screenFade;
        Text score, multiplier, occupancy, route, feedback, record, soundLabel, streak, lossesLabel, difficultyLabel;
        Image loadCard, soundTrack;
        RectTransform soundThumb;
        Image[] lossMarks, streakMarks;
        int best, tutorialPage;
        const int TutorialVersion = 2;
        bool tutorialStartsGame;
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
            Settings.Prepare();
            progress=new CampaignProgress(PlayerPrefs.GetInt("OneMoreFloor.Campaign.Completed",0));
            cosmetics=CosmeticStore.Load(); InitializeWheelTesting();
            Loc.Initialize(); ui=new GameUI();
            best=PlayerPrefs.GetInt("OneMoreFloor.Best",0);
            sound=PlayerPrefs.GetInt("OneMoreFloor.Sound",1)!=0;
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false;
            boardTone=Tone("Boarding",new []{440f,554f},.09f,.08f);
            deliveryTone=Tone("Delivered",new []{523f,659f,784f},.085f,.09f);
            lossTone=Tone("Lost",new []{294f,247f},.14f,.07f);
            tapTone=Tone("Tap",new []{400f},.035f,.035f);
            BuildUI(); ShowMenu(); if(!Loc.HasChosenLanguage) ShowLanguagePicker(true);
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
            root.transform.SetParent(transform,false); uiRoot=root;
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.pixelPerfect=true;
            canvas.additionalShaderChannels|=AdditionalCanvasShaderChannels.TexCoord1;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(600,1060); scaler.matchWidthOrHeight=1;
            ui.Panel("Evening sky",root.transform,0,0,1,1,GameUI.Hex("BDD4D0"),false);
            ui.Picture("Distant neighborhood",root.transform,"skyline",0,0,1,.62f,false);
            ui.Panel("Street",root.transform,0,0,1,.055f,GameUI.Hex("648A89"),false);
            CosmeticStyle.Background(ui,root.transform,cosmetics.Selected(CosmeticKind.Background));
            safe=ui.Rect("Portrait safe area",root.transform,0,0,1,1);
            ui.Text("Brand",safe,Loc.T("app.title"),.04f,.95f,.68f,.993f,26,GameUI.Ink,TextAnchor.MiddleLeft,true);
            difficultyLabel=ui.Text("Difficulty",safe,"",.043f,.923f,.7f,.949f,14,GameUI.Hex("4C7478"),TextAnchor.MiddleLeft);
            ui.Button("Pause",safe,"II",.71f,.936f,.82f,.991f,Pause,GameUI.Ink,28);
            ui.IconButton("Settings",safe,"gear",.85f,.936f,.96f,.991f,ShowSettings,GameUI.Ink);
            var scoreCard=ui.Panel("Score card",safe,.04f,.851f,.96f,.918f,GameUI.Cream);
            ui.Text("Score caption",scoreCard.transform,Loc.T("hud.score"),.04f,.48f,.23f,.94f,13,GameUI.Muted,TextAnchor.MiddleLeft,true);
            score=ui.Text("Score",scoreCard.transform,"0",.04f,.01f,.4f,.66f,36,GameUI.Ink,TextAnchor.MiddleLeft,true);
            multiplier=ui.Text("Multiplier",scoreCard.transform,"×1",.77f,.08f,.96f,.92f,34,GameUI.Teal,TextAnchor.MiddleCenter,true);
            streak=ui.Text("Streak caption",scoreCard.transform,Loc.T("hud.streak",2),.42f,.55f,.75f,.94f,13,GameUI.Muted);
            streakMarks=new Image[5];
            for(int i=0;i<5;i++) streakMarks[i]=ui.Panel("Streak dot",scoreCard.transform,.435f+i*.06f,.22f,.475f+i*.06f,.4f,GameUI.Hex("D9D9C9"));
            var load=ui.Panel("Occupancy card",safe,.04f,.789f,.49f,.842f,GameUI.Hex("DCE6D8")); loadCard=load;
            occupancy=ui.Text("Occupancy",load.transform,Loc.T("hud.occupancy",0,Settings.Rules.Capacity),.04f,.1f,.96f,.9f,22,GameUI.Ink,TextAnchor.MiddleCenter,true);
            var losses=ui.Panel("Lost card",safe,.51f,.789f,.96f,.842f,GameUI.Hex("DCE6D8"));
            lossesLabel=ui.Text("Lost caption",losses.transform,Loc.T("hud.losses",0,Settings.Rules.LossLimit),.06f,.15f,.46f,.85f,16,GameUI.Hex("607D7D"),TextAnchor.MiddleLeft,true);
            lossMarks=new Image[Settings.Rules.LossLimit];
            for(int i=0;i<lossMarks.Length;i++)
            {
                float x=.49f+i*.43f/lossMarks.Length;
                lossMarks[i]=ui.Picture("Loss mark",losses.transform,"circle",x,.31f,x+.3f/lossMarks.Length,.69f);
            }
            buildingRoot=ui.Rect("Illustrated apartment building",safe,.005f,.14f,.995f,.735f);
            building=new BuildingView(ui,buildingRoot,Settings.Rules,SelectFloor,cosmetics.Selected(CosmeticKind.Floor),cosmetics.Selected(CosmeticKind.Lift));
            var routeCard=ui.Panel("Route",safe,.04f,.067f,.96f,.117f,GameUI.Ink);
            route=ui.Text("Route",routeCard.transform,Loc.T("route.choose"),.03f,.08f,.97f,.92f,22,GameUI.Cream,TextAnchor.MiddleCenter,true);
            feedback=ui.Text("Feedback",safe,"",.04f,.025f,.96f,.065f,20,GameUI.Ink);
            overlay=ui.Rect("Screen overlay",safe,0,0,1,1);
            screenFade=overlay.gameObject.AddComponent<CanvasGroup>();
            BuildCoach(); BuildSettings(); BuildLanguageLayer(); ApplySafeArea();
        }
        void ClearOverlay(bool menu=false)
        {
            foreach(Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.gameObject.SetActive(true); screenAge=0; screenFade.alpha=0;
            var scrim=ui.Panel("Modal background",overlay,-.2f,-.1f,1.2f,1.1f,
                menu ? GameUI.Hex("BDD4D0") : new Color(.08f,.18f,.23f,.67f),false);
            scrim.raycastTarget=true;
        }
        RectTransform Modal(string eyebrow,string title,Transform parent=null)
        {
            var card=ui.Panel("Screen card",parent ?? overlay,.055f,.16f,.945f,.85f,GameUI.Cream).rectTransform;
            ui.Text("Eyebrow",card,eyebrow,.08f,.86f,.92f,.94f,16,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Heading",card,title,.06f,.67f,.94f,.87f,43,GameUI.Ink,TextAnchor.MiddleCenter,true);
            return card;
        }
        void ShowMenu()
        {
            game=null; coachRoot.gameObject.SetActive(false); building.Reset(); buildingRoot.gameObject.SetActive(false); settingsLayer.gameObject.SetActive(false); ClearOverlay(true);
            ui.Picture("Menu skyline",overlay,"skyline",-.13f,0,1.13f,.73f,false);
            CosmeticStyle.Background(ui,overlay,cosmetics.Selected(CosmeticKind.Background));
            ui.Text("House label",overlay,Loc.T("menu.house"),.08f,.925f,.79f,.96f,16,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.IconButton("Menu settings",overlay,"gear",.84f,.935f,.95f,.99f,ShowSettings,GameUI.Ink);
            ui.Text("Game title",overlay,Loc.T("app.menuTitle"),.07f,.735f,.93f,.916f,68,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Tagline",overlay,Loc.T("menu.tagline"),.1f,.65f,.9f,.742f,23,GameUI.Hex("476B70"));
            var hero=ui.Panel("Lobby illustration",overlay,.12f,.333f,.88f,.635f,(cosmetics.Selected(CosmeticKind.Floor)==0 ? GameUI.Hex("E8D3B5") : CosmeticStyle.Tint(cosmetics.Selected(CosmeticKind.Floor)))).rectTransform;
            ui.Panel("Lobby floor",hero,.02f,.015f,.98f,.09f,GameUI.Hex("C19B7A"),false);
            ui.Picture("Lobby window",hero,"window",.045f,.38f,.31f,.89f);
            ui.Picture("Lobby plant",hero,"plant",.045f,.085f,.23f,.39f);
            ui.Picture("Lobby elevator",hero,"lobby_cabin",.52f,.06f,.94f,.97f).color=CosmeticStyle.Tint(cosmetics.Selected(CosmeticKind.Lift));
            MenuPerson(hero,0,.24f,.09f,.47f,.77f,"4");
            MenuPerson(hero,1,.40f,.08f,.63f,.74f,"2");
            MenuPerson(hero,4,.68f,.13f,.87f,.74f,"6");
            ui.Text("Lobby plaque",hero,Loc.T("menu.welcome"),.04f,.9f,.48f,.98f,12,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Button("Play",overlay,Loc.T("menu.play"),.12f,.237f,.88f,.313f,StartShift,GameUI.Coral,31);
            ui.Button("Learn",overlay,Loc.T("menu.learn"),.12f,.162f,.88f,.218f,()=>ShowTutorial(false),GameUI.Teal,23);
            record=ui.Text("Record",overlay,Loc.T("menu.completed",progress.Completed,Campaign.LevelCount),.12f,.101f,.88f,.15f,23,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Text("Best score",overlay,Loc.T("menu.best",best),.12f,.075f,.88f,.104f,18,GameUI.Teal);
            ui.Button("Wheel",overlay,Loc.T("wheel.title"),.04f,.012f,.49f,.069f,()=>ShowWheel(),GameUI.Coral,19);
            ui.Button("Collection",overlay,Loc.T("collection.title"),.51f,.012f,.96f,.069f,()=>ShowCollection(),GameUI.Teal,19);
        }
        void MenuPerson(Transform parent,int index,float x0,float y0,float x1,float y1,string destination)
        {
            ui.Picture("Neighbor",parent,"person_"+index,x0,y0,x1,y1);
            var bubble=ui.Panel("Neighbor destination",parent,x0+.035f,y1-.005f,x1-.035f,y1+.115f,GameUI.Cream);
            ui.Text("Destination",bubble.transform,destination,0,0,1,1,24,GameUI.Ink,TextAnchor.MiddleCenter,true);
        }
        void ToggleSound()
        {
            sound=!sound; PlayerPrefs.SetInt("OneMoreFloor.Sound",sound ? 1 : 0); PlayerPrefs.Save();
            if(!sound) audioSource.Stop();
            UpdateSoundLabel(); Play(tapTone);
        }
        void UpdateSoundLabel()
        {
            if(soundLabel!=null) soundLabel.text=Loc.T(sound ? "settings.soundOn" : "settings.soundOff");
            if(soundTrack!=null) soundTrack.color=sound ? GameUI.Teal : GameUI.Hex("91A39C");
            if(soundThumb!=null) GameUI.Place(soundThumb,sound ? .55f : .05f,.1f,sound ? .95f : .45f,.9f);
        }
        void BuildSettings()
        {
            settingsLayer=ui.Rect("Settings overlay",safe,0,0,1,1);
            var scrim=ui.Panel("Settings scrim",settingsLayer,-.2f,-.1f,1.2f,1.1f,new Color(.08f,.18f,.23f,.76f),false);
            scrim.raycastTarget=true;
            var card=Modal(Loc.T("settings.eyebrow"),Loc.T("settings.title"),settingsLayer);
            ui.Picture("Settings gear",card,"gear",.43f,.59f,.57f,.70f).color=GameUI.Teal;
            ui.Text("Settings description",card,Loc.T("settings.detail"),.08f,.49f,.92f,.60f,22,GameUI.Hex("627D7C"));
            var toggle=ui.Button("Sound switch",card,"",.09f,.34f,.91f,.46f,ToggleSound,GameUI.Hex("E0E9DB"),25);
            soundLabel=toggle.GetComponentInChildren<Text>(); soundLabel.color=GameUI.Ink; soundLabel.alignment=TextAlignmentOptions.Left;
            GameUI.Place(soundLabel.rectTransform,.06f,.1f,.69f,.9f);
            soundTrack=ui.Panel("Switch track",toggle.transform,.73f,.29f,.94f,.71f,GameUI.Teal);
            soundThumb=ui.Picture("Switch thumb",soundTrack.transform,"circle",.55f,.1f,.95f,.9f).rectTransform;
            ui.Button("Language settings",card,Loc.T("settings.language",GameLanguages.NativeName(Loc.Language)),.09f,.205f,.91f,.315f,()=>ShowLanguagePicker(false),GameUI.Teal,23);
            UpdateSoundLabel();
            ui.Button("Settings close",card,Loc.T("common.done"),.09f,.075f,.91f,.19f,()=>settingsLayer.gameObject.SetActive(false),GameUI.Coral);
            settingsLayer.gameObject.SetActive(false);
        }
        void ShowSettings()
        {
            if(game!=null && game.Finished) return;
            // Opening settings during play always leads back to an explicit pause screen.
            // Closing them after the app was backgrounded must never silently resume the shift.
            Pause(); UpdateSoundLabel(); settingsLayer.gameObject.SetActive(true);
        }
        void StartShift() { if(!RequireLanguage()) ShowLevels(); }
        void MarkTutorial()
        {
            PlayerPrefs.SetInt("OneMoreFloor.TutorialVersion",TutorialVersion); PlayerPrefs.Save();
        }
        void SelectFloor(int floor)
        {
            if(game==null || game.State!=PlayState.Playing) return;
            game.SelectFloor(floor); Play(tapTone);
        }
        void NewGame()
        {
            if(RequireLanguage()) return;
            var level=Settings.Levels[currentLevel-1];
            if(level.BoxChance>0 && PlayerPrefs.GetInt("OneMoreFloor.BoxExplained",0)==0)
            { ShowBoxIntroduction(); return; }
            building.Reset(); buildingRoot.gameObject.SetActive(true); settingsLayer.gameObject.SetActive(false);
            game=new GameSession(Settings.Rules,NewSeed(),level,currentLevel);
            game.Feedback+=OnFeedback; feedback.text=Loc.T("feedback.goal",level.DeliveryGoal);
            feedbackTime=5; overlay.gameObject.SetActive(false); Render(0);
        }
        void ShowTutorial(bool startAfter)
        {
            if(RequireLanguage()) return;
            tutorialStartsGame=startAfter; tutorialPage=0; RenderTutorial();
        }
        void RenderTutorial()
        {
            ClearOverlay(true);

            var card=Modal(Loc.T("tutorial.step",tutorialPage+1),Loc.T("tutorial.title."+tutorialPage));
            string detail;
            if(tutorialPage==0)
            {
                ui.Picture("Tutorial neighbor",card,"person_0",.38f,.46f,.62f,.625f);
                var badge=ui.Panel("Tutorial destination",card,.455f,.625f,.545f,.67f,GameUI.Cream);
                ui.Text("Destination",badge.transform,"3",0,0,1,1,25,GameUI.Teal,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.0");
                ui.Panel("Tutorial patience",card,.4f,.445f,.6f,.456f,GameUI.Teal,false);
            }
            else if(tutorialPage==1)
            {
                ui.Picture("Tutorial lift",card,"cabin",.2f,.43f,.8f,.665f);
                detail=Loc.T("tutorial.detail.1",Settings.Rules.Capacity);
            }
            else if(tutorialPage==2)
            {
                int points=Settings.Rules.DeliveryPoints;
                ui.Text("Scoring example",card,points+" → "+(points*2)+" → "+(points*3),.12f,.46f,.88f,.65f,48,GameUI.Teal,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.2",points,Settings.Rules.LossLimit);
            }
            else
            {
                ui.Text("Difficulty example",card,"3  →  4  →  5  →  6",.12f,.46f,.88f,.65f,45,GameUI.Teal,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.3");
            }
            ui.Text("Tutorial explanation",card,detail,.06f,.22f,.94f,.43f,23,GameUI.Hex("627D7C"));
            for(int i=0;i<4;i++) ui.Picture("Tutorial page",card,"circle",.39f+i*.06f,.177f,.42f+i*.06f,.195f).color=i==tutorialPage ? GameUI.Coral : GameUI.Hex("C1CFC3");
            if(tutorialPage>0) ui.Button("Previous",card,Loc.T("common.back"),.07f,.04f,.32f,.145f,()=>{tutorialPage--; RenderTutorial();},GameUI.Hex("708E8C"),21);
            else ui.Button("Exit tutorial",card,Loc.T("common.menu"),.07f,.04f,.32f,.145f,ShowMenu,GameUI.Hex("708E8C"),21);
            ui.Button("Next",card,tutorialPage<3 ? Loc.T("common.next") : (tutorialStartsGame ? Loc.T("common.levels") : Loc.T("common.gotIt")),.35f,.04f,.93f,.145f,()=>
            {
                if(tutorialPage<3) { tutorialPage++; RenderTutorial(); }
                else { MarkTutorial(); if(tutorialStartsGame) ShowLevels(); else ShowMenu(); }
            },GameUI.Coral,24);
        }
        void OnFeedback(GameFeedback message)
        {
            string key;
            switch(message.Kind)
            {
                case FeedbackKind.Delivered: key="feedback.delivered"; Play(deliveryTone); break;
                case FeedbackKind.Boarded: key="feedback.boarded"; Play(boardTone); break;
                case FeedbackKind.Lost: key="feedback.lost"; Play(lossTone); break;
                case FeedbackKind.Won: key="feedback.won"; break;
                case FeedbackKind.BoxBlocked: key="feedback.box"; break;
                default: key="feedback.floor"; break;
            }
            feedback.text=Loc.T(key,message.Count,message.Multiplier);
            feedbackTime=2.2f;
        }
        void Pause()
        {
            if(game==null || game.State!=PlayState.Playing) return;
            game.Pause(); SaveBest(); RenderPauseScreen();
        }
        void RenderPauseScreen()
        {
            coachRoot.gameObject.SetActive(false); ClearOverlay();
            var card=Modal(Loc.T("pause.eyebrow"),Loc.T("pause.title"));
            ui.Text("Pause detail",card,Loc.T("pause.detail"),.08f,.51f,.92f,.65f,23,GameUI.Hex("627D7C"));
            ui.Button("Continue",card,Loc.T("common.continue"),.09f,.365f,.91f,.48f,()=>{ game.Resume(); overlay.gameObject.SetActive(false); Render(0); },GameUI.Coral);
            ui.Button("Pause settings",card,Loc.T("settings.title"),.09f,.23f,.91f,.33f,ShowSettings,GameUI.Teal,24);
            ui.Button("Restart",card,Loc.T("common.restart"),.09f,.085f,.49f,.19f,NewGame,GameUI.Hex("708E8C"),21);
            ui.Button("Menu",card,Loc.T("common.menu"),.52f,.085f,.91f,.19f,ShowMenu,GameUI.Hex("708E8C"),21);
        }
        void SaveBest()
        {
            if(game!=null && game.Score>best) { best=game.Score; PlayerPrefs.SetInt("OneMoreFloor.Best",best); PlayerPrefs.Save(); }
        }
        void EndGame()
        {
            SaveBest(); coachRoot.gameObject.SetActive(false); ClearOverlay();
            var card=Modal(Loc.T("level.number",currentLevel,Campaign.LevelCount),Loc.T("loss.title"));
            ui.Text("Final score",card,Loc.T("win.score",game.Score),.1f,.5f,.9f,.67f,45,GameUI.Teal,TextAnchor.MiddleCenter,true);
            ui.Text("Results",card,Loc.T("loss.detail",game.Delivered,game.Level.DeliveryGoal,game.Rules.LossLimit),.06f,.38f,.94f,.51f,23,GameUI.Hex("627D7C"));
            ui.Button("Retry",card,Loc.T("loss.retry"),.09f,.22f,.91f,.345f,NewGame,GameUI.Coral);
            ui.Button("Levels",card,Loc.T("common.levels"),.09f,.07f,.91f,.18f,ShowLevels);
        }
        void Update()
        {
            ApplySafeArea(); UpdateWheel();
            if(overlay!=null && overlay.gameObject.activeSelf)
            { screenAge+=Time.unscaledDeltaTime; screenFade.alpha=Mathf.Clamp01(screenAge/.18f); }
            if(game==null) return;
            var before=game.State;
            int deliveredBefore=game.Delivered;
            game.Tick(Time.unscaledDeltaTime);
            // The winning stop emits Won instead of Delivered. Count the model delta so
            // its passengers also roll for coins, exactly once, including multi-frame ticks.
            int coinsEarned=cosmetics.OnDelivered(game.Delivered-deliveredBefore,cosmeticRandom);
            if(coinsEarned>0)
            {
                SaveCosmetics(); feedback.text+=" · "+Loc.T("wheel.drop",coinsEarned); feedbackTime=2.2f;
            }
            if(before==PlayState.Playing)
            {
                float dt=Mathf.Min(Time.unscaledDeltaTime,.25f);
                feedbackTime-=dt; if(feedbackTime<=0) feedback.text=Loc.T("feedback.hint");
                Render(dt);
            }
            if(before==PlayState.Playing && game.State==PlayState.GameOver) EndGame();
            else if(before==PlayState.Playing && game.State==PlayState.Won) WinLevel();
        }
        void Render(float dt)
        {
            score.text=game.Score.ToString(); multiplier.text="×"+game.Multiplier;
            difficultyLabel.text=Loc.T("hud.progress",currentLevel,Campaign.LevelCount,Math.Min(game.Delivered,game.Level.DeliveryGoal),game.Level.DeliveryGoal);
            streak.text=game.Multiplier==3 ? Loc.T("hud.max") : Loc.T("hud.streak",game.Multiplier+1);
            for(int i=0;i<streakMarks.Length;i++) streakMarks[i].color=game.Multiplier==3 || i<game.Streak%5 ? GameUI.Gold : GameUI.Hex("D9D9C9");
            for(int i=0;i<lossMarks.Length;i++) lossMarks[i].color=i<game.Lost ? GameUI.Coral : GameUI.Hex("ADC2B8");
            occupancy.text=Loc.T("hud.occupancy",game.Lift.OccupiedSpaces,game.Rules.Capacity);
            loadCard.color=game.Lift.OccupiedSpaces==game.Rules.Capacity ? GameUI.Gold : GameUI.Hex("DCE6D8");
            lossesLabel.text=Loc.T("hud.losses",game.Lost,game.Rules.LossLimit);
            route.text=game.Lift.Moving ? Loc.T("route.moving",game.Lift.TargetFloor+1) : Loc.T("route.stopped",game.Lift.CurrentFloor+1);
            if(game.Lift.NextFloor.HasValue) route.text+=Loc.T("route.next",game.Lift.NextFloor.Value+1);
            building.Render(game,dt); RenderCoach();
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
