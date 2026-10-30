using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        GameObject uiRoot;
        RectTransform languageLayer;
        GameLanguage selectedLanguage;
        Image[] languageCards;
        Text languageEyebrow, languageHeading, languageDescription, languageConfirm, languageCancel;

        void BuildLanguageLayer()
        {
            languageLayer=ui.Rect("Language selection",safe,0,0,1,1);
            languageLayer.gameObject.SetActive(false);
        }
        void ShowLanguagePicker(bool required)
        {
            // A required choice covers all menu actions and precedes any tutorial/session.
            if(game!=null && game.State==PlayState.Playing) Pause();
            selectedLanguage=Loc.Language;
            foreach(Transform child in languageLayer) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            languageLayer.gameObject.SetActive(true);
            var scrim=ui.Panel("Language background",languageLayer,-.2f,-.1f,1.2f,1.1f,GameUI.Hex("BDD4D0"),false);
            scrim.raycastTarget=true;
            var card=ui.Panel("Language card",languageLayer,.055f,.06f,.945f,.94f,GameUI.Cream).rectTransform;
            languageEyebrow=ui.Text("Language eyebrow",card,Loc.T("language.eyebrow"),.06f,.91f,.94f,.97f,17,GameUI.Teal,TextAnchor.MiddleCenter,true);
            languageHeading=ui.Text("Language title",card,Loc.T("language.title"),.06f,.79f,.94f,.915f,38,GameUI.Ink,TextAnchor.MiddleCenter,true);
            languageDescription=ui.Text("Language description",card,Loc.T("language.detail"),.07f,.69f,.93f,.80f,22,GameUI.Hex("627D7C"));
            languageCards=new Image[GameLanguages.Count];
            for(int i=0;i<GameLanguages.Count;i++)
            {
                var language=(GameLanguage)i; float top=.675f-i*.071f;
                var button=ui.Button("Language "+GameLanguages.Code(language),card,GameLanguages.NativeName(language),.09f,top-.061f,.91f,top,
                    ()=>{ selectedLanguage=language; UpdateLanguageCards(); },GameUI.Teal,25);
                languageCards[i]=button.GetComponent<Image>();
            }
            languageConfirm=ui.Button("Confirm language",card,Loc.T("language.confirm"),.09f,.105f,.91f,.175f,()=>ApplyLanguage(required),GameUI.Coral,26).GetComponentInChildren<Text>();
            languageCancel=null;
            if(!required) languageCancel=ui.Button("Cancel language",card,Loc.T("language.cancel"),.09f,.025f,.91f,.09f,()=>languageLayer.gameObject.SetActive(false),GameUI.Teal,23).GetComponentInChildren<Text>();
            UpdateLanguageCards();
        }
        void UpdateLanguageCards()
        {
            languageEyebrow.text=Loc.Catalog.Get(selectedLanguage,"language.eyebrow");
            languageHeading.text=Loc.Catalog.Get(selectedLanguage,"language.title");
            languageDescription.text=Loc.Catalog.Get(selectedLanguage,"language.detail");
            languageConfirm.text=Loc.Catalog.Get(selectedLanguage,"language.confirm");
            if(languageCancel!=null) languageCancel.text=Loc.Catalog.Get(selectedLanguage,"language.cancel");
            for(int i=0;i<languageCards.Length;i++)
            {
                bool chosen=i==(int)selectedLanguage;
                languageCards[i].color=chosen ? GameUI.Gold : GameUI.Teal;
                languageCards[i].GetComponentInChildren<Text>().color=chosen ? GameUI.Ink : GameUI.Cream;
            }
        }
        void ApplyLanguage(bool firstChoice)
        {
            Loc.Choose(selectedLanguage);
            // Rebuild visual elements, preserving the paused model, riders, route and elapsed time.
            building.Reset(); uiRoot.SetActive(false); Destroy(uiRoot);
            lastScreen=Vector2Int.zero;
            ui=new GameUI(); BuildUI();
            if(game==null) ShowMenu();
            else
            {
                buildingRoot.gameObject.SetActive(true);
                Render(.25f); // Settle visual fades only; the paused game is never ticked here.
                RenderPauseScreen();
            }
            if(!firstChoice) { UpdateSoundLabel(); settingsLayer.gameObject.SetActive(true); }
        }
        bool RequireLanguage()
        {
            if(Loc.HasChosenLanguage) return false;
            ShowLanguagePicker(true); return true;
        }
        string LevelTitle(int number)
        {
            // A developer's custom level name remains theirs; built-in names use the catalogue.
            return Settings.Levels[number-1].Title==Campaign.CreateLevels()[number-1].Title
                ? Loc.LevelTitle(number) : Settings.Levels[number-1].Title;
        }
    }
}
