using UnityEngine;

namespace OneMoreFloor
{
    public static class Loc
    {
        public static TextCatalog Catalog { get; private set; }
        public static GameLanguage Language { get; private set; }
        public static bool HasChosenLanguage { get; private set; }
        const string Preference="OneMoreFloor.Language";
        public static void Initialize()
        {
            var source=Resources.Load<TextAsset>("Localization/Strings");
            if(source==null) throw new System.InvalidOperationException("Missing localization: Resources/Localization/Strings.txt");
            Catalog=new TextCatalog(source.text);
            GameLanguage saved;
            HasChosenLanguage=GameLanguages.TryParse(PlayerPrefs.GetString(Preference,""),out saved);
            Language=HasChosenLanguage ? saved : GameLanguage.English;
        }
        public static void Choose(GameLanguage language)
        {
            // Validate before saving so an invalid preference cannot bypass first-run selection.
            string code=GameLanguages.Code(language);
            Language=language; HasChosenLanguage=true;
            PlayerPrefs.SetString(Preference,code); PlayerPrefs.Save();
        }
        public static string T(string key,params object[] args) { return Catalog.Get(Language,key,args); }
        public static string LevelTitle(int number) { return T("level."+number); }
    }
}
