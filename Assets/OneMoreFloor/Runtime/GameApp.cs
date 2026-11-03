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
        Graphic[] lossMarks, streakMarks;
        int best, tutorialPage;
        const int TutorialVersion = 2;
        bool tutorialStartsGame;
        bool sound;
        float feedbackTime, screenAge;
        Rect lastSafe;
        Vector2Int lastScreen;
        AudioSource audioSource;
        AudioClip boardTone, deliveryTone, lossTone, tapTone, arrivalTone, celebrationTone;
        bool awaitingRoofFinale,victorySaved;
        readonly List<AudioClip> clips = new List<AudioClip>();

        void Start()
        {
            Application.targetFrameRate=60; Screen.orientation=ScreenOrientation.Portrait;
            if(Settings==null) Settings=ScriptableObject.CreateInstance<GameSettings>();
            Settings.Prepare();
            progress=new CampaignProgress(PlayerPrefs.GetInt("OneMoreFloor.Campaign.Completed",0));
            cosmetics=CosmeticStore.Load(); InitializeWheelTesting();
            Loc.Initialize(); InitializeAds(); ui=CreateUI();
            best=PlayerPrefs.GetInt("OneMoreFloor.Best",0);
            sound=PlayerPrefs.GetInt("OneMoreFloor.Sound",1)!=0;
            audioSource=gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake=false;
            boardTone=Tone("Boarding",new []{440f,554f},.09f,.08f);
            deliveryTone=Tone("Delivered",new []{523f,659f,784f},.085f,.09f);
            lossTone=Tone("Lost",new []{294f,247f},.14f,.07f);
            tapTone=Tone("Tap",new []{400f},.035f,.035f);
            arrivalTone=Cue("Soft lift bell",CozySound.Bell());
            celebrationTone=Cue("Airy celebration",CozySound.Celebration());
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
        AudioClip Cue(string name,float[] samples)
        {
            var clip=AudioClip.Create(name,samples.Length,1,44100,false);clip.SetData(samples,0);clips.Add(clip);return clip;
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
            ui.Panel("Evening sky",root.transform,0,0,1,1,GameUI.Paper,false);
            ui.Picture("Distant neighborhood",root.transform,"skyline",0,0,1,.62f,false);
            CosmeticStyle.Background(ui,root.transform,cosmetics.Selected(CosmeticKind.Background));
            ui.Gradient("Game atmosphere",root.transform,0,0,1,1,new Color(.98f,.96f,.92f,.94f),new Color(.95f,.96f,.90f,.72f));
            ui.Gradient("Quiet sky",root.transform,0,.66f,1,1,GameUI.Paper,new Color(.96f,.94f,.90f,0));
            safe=ui.Rect("Portrait safe area",root.transform,0,0,1,1);
            var header=ui.Surface("Game header",safe,.036f,.920f,.964f,.988f,UITheme.Surface).rectTransform;
            ui.Icon("Lift mark",header,UIIcon.Lift,.025f,.21f,.095f,.80f,UITheme.Green);
            ui.Text("Brand",header,Loc.T("app.title"),.12f,.51f,.73f,.92f,19,UITheme.Ink,TextAnchor.MiddleLeft,true);
            difficultyLabel=ui.Text("Difficulty",header,"",.12f,.09f,.73f,.52f,13,UITheme.Muted,TextAnchor.MiddleLeft);
            ui.SymbolButton("Pause",safe,UIIcon.Pause,.735f,.926f,.835f,.982f,Pause,UITheme.Soft);
            ui.SymbolButton("Settings",safe,UIIcon.Settings,.855f,.926f,.955f,.982f,ShowSettings,UITheme.Soft);
            var scoreCard=ui.Surface("Score card",safe,.036f,.839f,.637f,.912f,UITheme.Surface).rectTransform;
            ui.Text("Score caption",scoreCard,Loc.T("hud.score"),.065f,.69f,.46f,.92f,12,UITheme.Muted,TextAnchor.MiddleLeft,true);
            score=ui.Text("Score",scoreCard,"0",.065f,.11f,.46f,.73f,44,UITheme.Ink,TextAnchor.MiddleLeft,true);
            var multiplierBadge=ui.Surface("Multiplier badge",scoreCard,.77f,.46f,.94f,.86f,UITheme.Soft,UITheme.BadgeRadius,false,false).rectTransform;
            multiplier=ui.Text("Multiplier",multiplierBadge,"×1",.05f,.02f,.95f,.98f,23,UITheme.Green,TextAnchor.MiddleCenter,true);
            streak=ui.Text("Streak caption",scoreCard,Loc.T("hud.streak",2),.47f,.45f,.73f,.94f,12,UITheme.Muted);
            streakMarks=new Graphic[5];
            for(int i=0;i<5;i++) streakMarks[i]=ui.Panel("Streak dot",scoreCard,.49f+i*.09f,.20f,.55f+i*.09f,.26f,UITheme.Border);
            var load=ui.Surface("Occupancy card",safe,.663f,.839f,.964f,.912f,UITheme.Surface);loadCard=load;
            ui.Icon("Capacity symbol",load.transform,UIIcon.Person,.39f,.52f,.61f,.87f,UITheme.Green);
            occupancy=ui.Text("Occupancy",load.transform,Loc.T("hud.occupancy",0,Settings.Rules.Capacity),.05f,.11f,.95f,.48f,19,UITheme.Ink,TextAnchor.MiddleCenter,true);
            var losses=ui.Surface("Lost card",safe,.036f,.792f,.964f,.826f,UITheme.Surface,UITheme.SmallRadius,false,false).rectTransform;
            lossesLabel=ui.Text("Lost caption",losses,Loc.T("hud.losses",0,Settings.Rules.LossLimit),.035f,.08f,.65f,.92f,15,UITheme.Muted,TextAnchor.MiddleLeft);
            lossMarks=new Graphic[Settings.Rules.LossLimit];
            for(int i=0;i<lossMarks.Length;i++)
            {
                float x=.765f+i*.205f/lossMarks.Length;
                lossMarks[i]=ui.Icon("Remaining patience life",losses,UIIcon.Heart,x,.19f,x+.16f/lossMarks.Length,.81f,UITheme.Terra);
            }
            ui.Card("Building tray",safe,.017f,.145f,.983f,.790f);
            buildingRoot=ui.Rect("Illustrated apartment building",safe,.026f,.160f,.974f,.685f);
            buildingRoot.gameObject.AddComponent<UIArtwork>();
            building=new BuildingView(ui,buildingRoot,Settings.Rules,SelectFloor,cosmetics.Selected(CosmeticKind.Floor),cosmetics.Selected(CosmeticKind.Lift));
            var routeCard=ui.Surface("Route",safe,.036f,.078f,.964f,.130f,UITheme.Deep,UITheme.ButtonRadius,false,false,true).rectTransform;
            ui.Icon("Route icon",routeCard,UIIcon.Lift,.025f,.22f,.085f,.78f,UITheme.Gold);
            route=ui.Text("Route",routeCard,Loc.T("route.choose"),.12f,.08f,.96f,.92f,18,UITheme.Surface,TextAnchor.MiddleLeft);
            ui.Panel("Footer clarity",safe,.02f,0,.98f,.071f,UITheme.Background,false);
            feedback=ui.Text("Feedback",safe,"",.06f,.026f,.94f,.064f,16,UITheme.Ink);
            overlay=ui.Rect("Screen overlay",safe,0,0,1,1);
            screenFade=overlay.gameObject.AddComponent<CanvasGroup>();
            BuildCoach(); BuildSettings(); BuildLanguageLayer(); ApplySafeArea();
        }
        void ClearOverlay(bool menu=false)
        {
            UIWindowMotion.Show(overlay.gameObject,false);
            foreach(Transform child in overlay) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            overlay.gameObject.SetActive(true); screenAge=0; screenFade.alpha=0;
            var scrim=ui.Panel("Modal background",overlay,-.2f,-.1f,1.2f,1.1f,
                menu ? UITheme.Background : UITheme.Scrim,false);
            scrim.raycastTarget=true;
            if(menu) ui.Gradient("Paper wash",overlay,-.2f,-.1f,1.2f,1.1f,UITheme.Surface,GameUI.Hex("E5ECDD"));
        }
        RectTransform Modal(string eyebrow,string title,Transform parent=null)
        {
            var card=ui.ModalSurface("Screen card",parent ?? overlay,.055f,.16f,.945f,.85f).rectTransform;
            ui.Panel("Modal accent",card,.43f,.965f,.57f,.974f,UITheme.Gold);
            ui.Panel("Modal divider",card,.09f,.666f,.91f,.668f,UITheme.Border,false);
            card.gameObject.AddComponent<UIPanelEntrance>();
            ui.Text("Eyebrow",card,eyebrow,.08f,.875f,.92f,.94f,13,UITheme.Green,TextAnchor.MiddleCenter,true);
            ui.Text("Heading",card,title,.065f,.71f,.935f,.87f,UITheme.Heading,UITheme.Ink,TextAnchor.MiddleCenter,true);
            return card;
        }
        void ShowMenu()
        {
            game=null; coachRoot.gameObject.SetActive(false); building.Reset(); buildingRoot.gameObject.SetActive(false); settingsLayer.gameObject.SetActive(false); ClearOverlay(true);
            ui.Picture("Menu skyline",overlay,"skyline",0,0,1,.65f,false).color=new Color(1,1,1,.32f);
            CosmeticStyle.Background(ui,overlay,cosmetics.Selected(CosmeticKind.Background));
            ui.Gradient("Menu lower wash",overlay,0,0,1,.37f,UITheme.Background,UITheme.Background);
            ui.Gradient("Legible menu sky",overlay,0,.69f,1,1,UITheme.Surface,UITheme.Surface);
            var logo=ui.Panel("App mark",overlay,.055f,.923f,.137f,.970f,UITheme.Soft).rectTransform;
            ui.Icon("App mark lift",logo,UIIcon.Lift,.20f,.15f,.80f,.85f,UITheme.Green);
            ui.Text("House label",overlay,Loc.T("menu.house"),.16f,.928f,.82f,.964f,13,UITheme.Muted,TextAnchor.MiddleLeft,true);
            ui.SymbolButton("Menu settings",overlay,UIIcon.Settings,.845f,.919f,.945f,.976f,ShowSettings,UITheme.Surface);
            ui.Text("Game title",overlay,Loc.T("app.menuTitle"),.065f,.771f,.94f,.914f,65,UITheme.Ink,TextAnchor.MiddleLeft,true);
            ui.Text("Tagline",overlay,Loc.T("menu.tagline"),.07f,.708f,.93f,.771f,19,UITheme.Muted,TextAnchor.MiddleLeft);
            var frame=ui.Card("Lobby frame",overlay,.055f,.368f,.945f,.687f).rectTransform;
            var hero=ui.Panel("Lobby illustration",frame,.025f,.045f,.975f,.965f,GameUI.Hex("E8D3B5")).rectTransform;
            hero.gameObject.AddComponent<UIArtwork>();
            ui.Panel("Lobby floor",hero,.015f,.015f,.985f,.09f,GameUI.Hex("C19B7A"),false);
            ui.Picture("Lobby window",hero,"window",.04f,.40f,.30f,.86f);
            ui.Picture("Lobby plant",hero,"plant",.035f,.08f,.22f,.39f);
            int lobbyLift=cosmetics.Selected(CosmeticKind.Lift), lobbyFloor=cosmetics.Selected(CosmeticKind.Floor);
            if(lobbyFloor>=10) ui.Picture("Themed lobby",hero,CosmeticStyle.Asset(CosmeticKind.Floor,lobbyFloor),0,0,1,1,false);
            ui.Picture("Lobby elevator",hero,lobbyLift>=10 ? "lobby_lift_"+lobbyLift : "lobby_cabin",.55f,.055f,.965f,.985f).color=CosmeticStyle.Tint(lobbyLift);
            MenuPerson(hero,8,.23f,.08f,.45f,.76f,"4");
            MenuPerson(hero,12,.395f,.07f,.615f,.74f,"2");
            MenuPerson(hero,4,.69f,.11f,.875f,.72f,"6");
            ui.Pill("Lobby welcome",hero,Loc.T("menu.welcome"),.035f,.875f,.505f,.985f,GameUI.Cream,GameUI.Teal,11);
            record=ui.Text("Record",overlay,Loc.T("menu.completed",progress.Completed,Campaign.LevelCount),.065f,.335f,.53f,.367f,12,UITheme.Ink,TextAnchor.MiddleLeft,true);
            ui.Progress("Campaign progress",overlay,.565f,.349f,.935f,.356f,progress.Completed/(float)Campaign.LevelCount);
            var play=ui.ActionButton("Play",overlay,Loc.T("ui.play"),UIIcon.Play,.055f,.241f,.945f,.317f,PlayNextLevel,UITheme.Terra,26);
            GameUI.Place(play.GetComponentInChildren<Text>().rectTransform,.17f,.12f,.58f,.88f);
            ui.Panel("Level separator",play.transform,.63f,.30f,.632f,.70f,new Color(1,1,1,.25f),false);
            ui.Text("Next level",play.transform,Loc.T("ui.level",progress.Unlocked),.665f,.12f,.95f,.88f,14,new Color(1,1,1,.90f));
            ui.ActionButton("All levels",overlay,Loc.T("ui.allLevels"),UIIcon.Flag,.055f,.174f,.487f,.226f,StartShift,UITheme.Surface,18);
            ui.ActionButton("Learn",overlay,Loc.T("ui.howToPlay"),UIIcon.Help,.513f,.174f,.945f,.226f,()=>ShowTutorial(false),UITheme.Surface,18);
            var wheel=ui.Button("Wheel",overlay,"",.055f,.055f,.487f,.150f,()=>ShowWheel(),UITheme.Surface,UITheme.ButtonText,true,UIButtonStyle.Card);
            ui.Icon("Wheel link icon",wheel.transform,UIIcon.Wheel,.055f,.25f,.245f,.77f,UITheme.Terra);
            ui.Text("Wheel link title",wheel.transform,Loc.T("wheel.title"),.29f,.47f,.96f,.88f,19,UITheme.Ink,TextAnchor.MiddleLeft);
            ui.Text("Wheel balance",wheel.transform,Loc.T("wheel.coins",cosmetics.Coins),.29f,.12f,.96f,.48f,14,UITheme.Muted,TextAnchor.MiddleLeft);
            var collection=ui.Button("Collection",overlay,"",.513f,.055f,.945f,.150f,()=>ShowCollection(),UITheme.Surface,UITheme.ButtonText,true,UIButtonStyle.Card);
            ui.Icon("Collection link icon",collection.transform,UIIcon.Collection,.055f,.25f,.245f,.77f,UITheme.Green);
            ui.Text("Collection link title",collection.transform,Loc.T("collection.title"),.29f,.47f,.96f,.88f,19,UITheme.Ink,TextAnchor.MiddleLeft);
            int count=0;for(int k=0;k<3;k++)for(int i=0;i<Cosmetics.PerKind;i++)if(cosmetics.Owns((CosmeticKind)k,i))count++;
            ui.Text("Collection count",collection.transform,Loc.T("ui.collectionCount",count,Cosmetics.Total),.29f,.12f,.96f,.48f,14,UITheme.Muted,TextAnchor.MiddleLeft);
            ui.Text("Best score",overlay,Loc.T("menu.best",best),.07f,.015f,.93f,.043f,13,UITheme.Muted);
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
            if(soundTrack!=null) soundTrack.color=sound ? UITheme.Green : UITheme.Border;
            if(soundThumb!=null) GameUI.Place(soundThumb,sound ? .55f : .05f,.1f,sound ? .95f : .45f,.9f);
        }
        void BuildSettings()
        {
            settingsLayer=ui.Rect("Settings overlay",safe,0,0,1,1);
            var scrim=ui.Panel("Settings scrim",settingsLayer,-.2f,-.1f,1.2f,1.1f,UITheme.Scrim,false);
            scrim.raycastTarget=true;
            var card=Modal(Loc.T("settings.eyebrow"),Loc.T("settings.title"),settingsLayer);
            ui.Icon("Settings gear",card,UIIcon.Settings,.43f,.58f,.57f,.66f,UITheme.Green);
            ui.Text("Settings description",card,Loc.T("settings.detail"),.08f,.49f,.92f,.60f,22,UITheme.Muted);
            var toggle=ui.Button("Sound switch",card,"",.09f,.34f,.91f,.46f,ToggleSound,GameUI.Hex("E0E9DB"),25);
            soundLabel=toggle.GetComponentInChildren<Text>(); soundLabel.color=UITheme.Ink; soundLabel.alignment=TextAlignmentOptions.Left;
            GameUI.Place(soundLabel.rectTransform,.06f,.1f,.69f,.9f);
            soundTrack=ui.Panel("Switch track",toggle.transform,.73f,.29f,.94f,.71f,UITheme.Green);
            soundThumb=ui.Picture("Switch thumb",soundTrack.transform,"circle",.55f,.1f,.95f,.9f).rectTransform;
            ui.Button("Language settings",card,Loc.T("settings.language",GameLanguages.NativeName(Loc.Language)),.09f,.205f,.91f,.315f,()=>ShowLanguagePicker(false),UITheme.Secondary,UITheme.SecondaryText);
            UpdateSoundLabel();
            ui.Button("Settings close",card,Loc.T("common.done"),.09f,.075f,.91f,.19f,()=>UIWindowMotion.Hide(settingsLayer.gameObject),UITheme.Terra);
            settingsLayer.gameObject.SetActive(false);
        }
        void ShowSettings()
        {
            if(game!=null && game.Finished) return;
            // Opening settings during play always leads back to an explicit pause screen.
            // Closing them after the app was backgrounded must never silently resume the shift.
            Pause(); UpdateSoundLabel(); UIWindowMotion.Show(settingsLayer.gameObject);
        }
        void StartShift() { if(!RequireLanguage()) ShowLevels(); }
        void MarkTutorial()
        {
            PlayerPrefs.SetInt("OneMoreFloor.TutorialVersion",TutorialVersion); PlayerPrefs.Save();
        }
        void SelectFloor(int floor)
        {
            if(game==null || game.State!=PlayState.Playing) return;
            game.SelectFloor(floor);
        }
        void NewGame()
        {
            if(RequireLanguage()) return;
            var level=Settings.Levels[currentLevel-1];
            if(level.BoxChance>0 && PlayerPrefs.GetInt("OneMoreFloor.BoxExplained",0)==0)
            { ShowBoxIntroduction(); return; }
            building.Reset(); buildingRoot.gameObject.SetActive(true); settingsLayer.gameObject.SetActive(false);
            awaitingRoofFinale=victorySaved=false;
            game=new GameSession(Settings.Rules,NewSeed(),level,currentLevel);
            game.Arrived+=floor=>Play(arrivalTone);
            game.Feedback+=OnFeedback; feedback.color=UITheme.Ink;feedback.text=Loc.T("feedback.goal",level.DeliveryGoal);
            feedbackTime=5; DismissOverlay(); Render(0);
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
                var badge=ui.Panel("Tutorial destination",card,.455f,.625f,.545f,.67f,UITheme.Surface);
                ui.Text("Destination",badge.transform,"3",0,0,1,1,25,UITheme.Green,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.0");
                ui.Panel("Tutorial patience",card,.4f,.445f,.6f,.456f,UITheme.Green,false);
            }
            else if(tutorialPage==1)
            {
                ui.Picture("Tutorial lift",card,"cabin",.2f,.43f,.8f,.665f);
                detail=Loc.T("tutorial.detail.1",Settings.Rules.Capacity);
            }
            else if(tutorialPage==2)
            {
                int points=Settings.Rules.DeliveryPoints;
                ui.Text("Scoring example",card,points+" → "+(points*2)+" → "+(points*3),.12f,.46f,.88f,.65f,48,UITheme.Green,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.2",points,Settings.Rules.LossLimit);
            }
            else
            {
                ui.Text("Difficulty example",card,"3  →  4  →  5  →  6",.12f,.46f,.88f,.65f,45,UITheme.Green,TextAnchor.MiddleCenter,true);
                detail=Loc.T("tutorial.detail.3");
            }
            ui.Text("Tutorial explanation",card,detail,.06f,.22f,.94f,.43f,23,UITheme.Muted);
            for(int i=0;i<4;i++) ui.Picture("Tutorial page",card,"circle",.39f+i*.06f,.177f,.42f+i*.06f,.195f).color=i==tutorialPage ? UITheme.Terra : GameUI.Hex("C1CFC3");
            if(tutorialPage>0) ui.Button("Previous",card,Loc.T("common.back"),.07f,.04f,.32f,.145f,()=>{tutorialPage--; RenderTutorial();},UITheme.Green,21);
            else ui.Button("Exit tutorial",card,Loc.T("common.menu"),.07f,.04f,.32f,.145f,ShowMenu,UITheme.Secondary,UITheme.SecondaryText);
            ui.Button("Next",card,tutorialPage<3 ? Loc.T("common.next") : (tutorialStartsGame ? Loc.T("common.levels") : Loc.T("common.gotIt")),.35f,.04f,.93f,.145f,()=>
            {
                if(tutorialPage<3) { tutorialPage++; RenderTutorial(); }
                else { MarkTutorial(); if(tutorialStartsGame) ShowLevels(); else ShowMenu(); }
            },UITheme.Terra,24);
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
                default: key=game!=null && message.Count==game.RoofFloor+1 ? "feedback.roof" : "feedback.floor"; break;
            }
            feedback.color=message.Kind==FeedbackKind.Lost ? UITheme.Terra : message.Kind==FeedbackKind.Delivered ? UITheme.Green : UITheme.Ink;
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
            ui.Text("Pause detail",card,Loc.T("pause.detail"),.08f,.51f,.92f,.65f,23,UITheme.Muted);
            ui.Button("Continue",card,Loc.T("common.continue"),.09f,.365f,.91f,.48f,()=>{ game.Resume(); DismissOverlay(); Render(0); },UITheme.Terra);
            ui.Button("Pause settings",card,Loc.T("settings.title"),.09f,.23f,.91f,.33f,ShowSettings,UITheme.Secondary,UITheme.SecondaryText);
            ui.Button("Restart",card,Loc.T("common.restart"),.09f,.085f,.49f,.19f,NewGame,UITheme.Secondary,UITheme.SecondaryText,true,UIButtonStyle.Danger);
            ui.Button("Menu",card,Loc.T("common.menu"),.52f,.085f,.91f,.19f,ShowMenu,UITheme.Secondary,UITheme.SecondaryText);
        }
        void SaveBest()
        {
            if(game!=null && game.Score>best) { best=game.Score; PlayerPrefs.SetInt("OneMoreFloor.Best",best); PlayerPrefs.Save(); }
        }
        void EndGame()
        {
            SaveBest(); coachRoot.gameObject.SetActive(false); ClearOverlay();
            var card=Modal(Loc.T("level.number",currentLevel,Campaign.LevelCount),Loc.T("loss.title"));
            ui.Text("Final score",card,Loc.T("win.score",game.Score),.1f,.5f,.9f,.67f,45,UITheme.Green,TextAnchor.MiddleCenter,true);
            ui.Text("Results",card,Loc.T("loss.detail",game.Delivered,game.Level.DeliveryGoal,game.Lost),.06f,.40f,.94f,.51f,22,UITheme.Muted);
            ui.Text("Ad continue detail",card,Loc.T("ads.continueDetail",Math.Max(0,game.Lost-1)),.06f,.295f,.94f,.405f,19,UITheme.Ink);
            continueAdButton=ui.ActionButton("Continue after rewarded ad",card,Loc.T("ads.continue"),UIIcon.Video,.09f,.195f,.91f,.29f,ContinueWithAd,UITheme.Terra,22);
            continueAdLabel=continueAdButton.GetComponentInChildren<Text>();
            ui.Button("Retry",card,Loc.T("loss.retry"),.09f,.085f,.49f,.18f,NewGame,UITheme.Secondary,UITheme.SecondaryText,true,UIButtonStyle.Danger);
            ui.Button("Levels",card,Loc.T("common.levels"),.52f,.085f,.91f,.18f,ShowLevels,UITheme.Secondary,UITheme.SecondaryText);
            RefreshContinueAd();
        }
        void Update()
        {
            ApplySafeArea(); UpdateWheel(); RefreshContinueAd();
            if(overlay!=null && overlay.gameObject.activeSelf && !UIWindowMotion.IsClosing(overlay.gameObject))
            { screenAge+=Time.unscaledDeltaTime; screenFade.alpha=Mathf.SmoothStep(0,1,Mathf.Clamp01(screenAge/UITheme.ModalIn)); }
            if(game==null)return;
            if(awaitingRoofFinale && game.State==PlayState.Won)
            {
                building.Render(game,Mathf.Min(Time.unscaledDeltaTime,.1f));
                if(building.RoofFinaleComplete){awaitingRoofFinale=false;WinLevel();}
                return;
            }
            if(game.Finished && building.AnimationsPending)building.Render(game,Mathf.Min(Time.unscaledDeltaTime,.1f));
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
                feedbackTime-=dt; if(feedbackTime<=0) { feedback.text=Loc.T("feedback.hint");feedback.color=UITheme.Muted; }
                Render(dt);
            }
            if(before==PlayState.Playing && game.State==PlayState.GameOver) EndGame();
            else if(before==PlayState.Playing && game.State==PlayState.Won) PresentVictory();
        }
        void Render(float dt)
        {
            score.text=game.Score.ToString(); multiplier.text="×"+game.Multiplier;
            difficultyLabel.text=Loc.T("hud.progress",currentLevel,Campaign.LevelCount,Math.Min(game.Delivered,game.Level.DeliveryGoal),game.Level.DeliveryGoal);
            streak.text=game.Multiplier==3 ? Loc.T("hud.max") : Loc.T("hud.streak",game.Multiplier+1);
            for(int i=0;i<streakMarks.Length;i++) streakMarks[i].color=game.Multiplier==3 || i<game.Streak%5 ? UITheme.Gold : UITheme.Border;
            for(int i=0;i<lossMarks.Length;i++) lossMarks[i].color=i<game.Lost ? UITheme.Border : UITheme.Terra;
            occupancy.text=Loc.T("hud.occupancy",game.Lift.OccupiedSpaces,game.Rules.Capacity);
            loadCard.color=game.Lift.OccupiedSpaces==game.Rules.Capacity ? UITheme.Gold : UITheme.Soft;
            lossesLabel.text=Loc.T("hud.losses",game.Lost,game.Rules.LossLimit);
            route.text=game.Lift.Moving ? (game.Lift.TargetFloor==game.RoofFloor ? Loc.T("route.roofMoving") : Loc.T("route.moving",game.Lift.TargetFloor+1)) :
                (game.Lift.CurrentFloor==game.RoofFloor ? Loc.T("route.roofStopped") : Loc.T("route.stopped",game.Lift.CurrentFloor+1));
            if(game.Lift.NextFloor.HasValue)route.text+=game.Lift.NextFloor.Value==game.RoofFloor ? Loc.T("route.roofNext") : Loc.T("route.next",game.Lift.NextFloor.Value+1);
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
