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
        RectTransform[] languageChoices;
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
            UIWindowMotion.Show(languageLayer.gameObject);
            var scrim=ui.Panel("Language background",languageLayer,-.2f,-.1f,1.2f,1.1f,required ? UITheme.Background : UITheme.Scrim,false);
            scrim.raycastTarget=true;
            var card=ui.ModalSurface("Language card",languageLayer,.055f,.06f,.945f,.94f).rectTransform;
            card.gameObject.AddComponent<UIPanelEntrance>();
            languageEyebrow=ui.Text("Language eyebrow",card,Loc.T("language.eyebrow"),.06f,.91f,.94f,.97f,13,UITheme.Green,TextAnchor.MiddleCenter,true);
            languageHeading=ui.Text("Language title",card,Loc.T("language.title"),.07f,.80f,.93f,.905f,34,UITheme.Ink,TextAnchor.MiddleCenter,true);
            languageDescription=ui.Text("Language description",card,Loc.T("language.detail"),.07f,.72f,.93f,.80f,18,UITheme.Muted);
            languageCards=new Image[GameLanguages.Count];languageChoices=new RectTransform[GameLanguages.Count];
            for(int i=0;i<GameLanguages.Count;i++)
            {
                var language=(GameLanguage)i; float top=.695f-i*.074f;
                var button=ui.Button("Language "+GameLanguages.Code(language),card,GameLanguages.NativeName(language),.09f,top-.063f,.91f,top,
                    ()=>{ selectedLanguage=language; UpdateLanguageCards(); },UITheme.Surface,22);
                languageCards[i]=button.GetComponent<Image>();
                var label=button.GetComponentInChildren<Text>();label.alignment=TMPro.TextAlignmentOptions.Left;
                GameUI.Place(label.rectTransform,.07f,.08f,.82f,.92f);
                languageChoices[i]=ui.Rect("Chosen language",button.transform,.86f,.25f,.94f,.75f);
                ui.Icon("Chosen language tick",languageChoices[i],UIIcon.Check,0,0,1,1,UITheme.Green);
            }
            languageConfirm=ui.Button("Confirm language",card,Loc.T("language.confirm"),.09f,.105f,.91f,.175f,()=>ApplyLanguage(required),UITheme.Terra,26).GetComponentInChildren<Text>();
            languageCancel=null;
            if(!required) languageCancel=ui.Button("Cancel language",card,Loc.T("language.cancel"),.09f,.025f,.91f,.09f,()=>UIWindowMotion.Hide(languageLayer.gameObject),UITheme.Secondary,UITheme.SecondaryText).GetComponentInChildren<Text>();
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
                languageCards[i].color=chosen ? UITheme.Soft : UITheme.Surface;
                languageChoices[i].gameObject.SetActive(chosen);
                languageCards[i].GetComponentInChildren<Text>().color=UITheme.Ink;
            }
        }
        void ApplyLanguage(bool firstChoice)
        {
            Loc.Choose(selectedLanguage);
            // Rebuild visual elements, preserving the paused model, riders, route and elapsed time.
            building.Reset(); uiRoot.SetActive(false); Destroy(uiRoot);
            lastScreen=Vector2Int.zero;
            ui=CreateUI(); BuildUI();
            if(game==null) ShowMenu();
            else
            {
                buildingRoot.gameObject.SetActive(true);
                Render(.25f); // Settle visual fades only; the paused game is never ticked here.
                RenderPauseScreen();
            }
            if(!firstChoice) { UpdateSoundLabel(); UIWindowMotion.Show(settingsLayer.gameObject); }
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
