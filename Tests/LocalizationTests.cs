using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OneMoreFloor;

static class LocalizationTests
{
    static int passed;
    static void Check(bool condition,string name)
    { if(!condition) throw new Exception(name); passed++; Console.WriteLine("PASS localization: "+name); }
    static void Reject(Action action,string name)
    {
        bool rejected=false; try { action(); } catch(FormatException) { rejected=true; }
        Check(rejected,name);
    }
    public static void Run()
    {
        UnityEngine.PlayerPrefs.ClearForTests();
        UnityEngine.PlayerPrefs.SetString("OneMoreFloor.Campaign.Completed","5");
        UnityEngine.PlayerPrefs.SetString("OneMoreFloor.Sound","0");
        Loc.Initialize();
        Check(Loc.Language==GameLanguage.English && !Loc.HasChosenLanguage && UnityEngine.PlayerPrefs.Saves==0,"actual Loc wrapper starts in unconfirmed English without saving implicitly");
        Loc.Choose(GameLanguage.English);
        Check(Loc.HasChosenLanguage && UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Language","")=="en" && UnityEngine.PlayerPrefs.Saves==1,"explicit English confirmation saves a valid choice");
        for(int i=0;i<GameLanguages.Count;i++)
        {
            Loc.Choose((GameLanguage)i); Loc.Initialize();
            Check(Loc.Language==(GameLanguage)i && Loc.HasChosenLanguage && Loc.T("app.title").Length>0,"actual wrapper reloads confirmed language: "+GameLanguages.Code((GameLanguage)i));
        }
        Check(UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Campaign.Completed","")=="5" && UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Sound","")=="0","language choices preserve unrelated progress and sound settings");
        UnityEngine.PlayerPrefs.SetString("OneMoreFloor.Language","broken"); Loc.Initialize();
        Check(Loc.Language==GameLanguage.English && !Loc.HasChosenLanguage,"corrupt preference reopens mandatory English selection");
        var catalog=new TextCatalog(File.ReadAllText("Assets/OneMoreFloor/Resources/Localization/Strings.txt"));
        GameLanguage language;
        Check(!GameLanguages.TryParse(null,out language) && language==GameLanguage.English,"missing preference defaults to English and requires a choice");
        Check(!GameLanguages.TryParse("invalid",out language) && language==GameLanguage.English,"invalid saved preference cannot bypass language selection");
        var codes=new HashSet<string>(); var names=new HashSet<string>();
        for(int i=0;i<GameLanguages.Count;i++)
        {
            language=(GameLanguage)i; string code=GameLanguages.Code(language); codes.Add(code); names.Add(GameLanguages.NativeName(language));
            Check(GameLanguages.TryParse(code,out var restored) && restored==language,"saved language code round trip: "+code);
            var englishSet=new HashSet<string>(); bool valid=true;
            foreach(string key in catalog.Keys)
            {
                string text=catalog.Get(language,key);
                string english=catalog.Get(GameLanguage.English,key);
                var placeholders=Regex.Matches(text,@"\{\d+\}").Select(m=>m.Value).OrderBy(x=>x).ToArray();
                var expected=Regex.Matches(english,@"\{\d+\}").Select(m=>m.Value).OrderBy(x=>x).ToArray();
                valid &= !string.IsNullOrWhiteSpace(text) && placeholders.SequenceEqual(expected) && !text.Contains("\\n");
                if(placeholders.Length>0)
                {
                    var arguments=new object[]{50,6,28,4};
                    string formatted=catalog.Get(language,key,arguments);
                    valid &= !Regex.IsMatch(formatted,@"\{\d+\}");
                }
                if(language!=GameLanguage.Russian) valid &= !Regex.IsMatch(text,"[А-Яа-яЁё]");
                englishSet.Add(text);
            }
            Check(valid,"all UI translations complete, formatted and free of leaked Russian: "+code);
            var levelNames=new HashSet<string>();
            for(int number=1;number<=Campaign.LevelCount;number++) levelNames.Add(catalog.Get(language,"level."+number));
            Check(levelNames.Count==50,"all 50 level names translated and distinct: "+code);
            foreach(TrafficPattern mode in Enum.GetValues(typeof(TrafficPattern)))
                Check(!string.IsNullOrWhiteSpace(catalog.Get(language,"traffic."+mode)),"traffic explanation: "+code+" "+mode);
        }
        Check(codes.Count==7 && names.Count==7,"seven distinct codes and native labels");
        Check(catalog.Get(GameLanguage.English,"app.title")=="One More Floor" && catalog.Get(GameLanguage.Russian,"app.title")=="Ещё один этаж","English default title and Russian title are correct");
        Check(catalog.Get(GameLanguage.Turkish,"menu.house").Contains("BİNA") && GameLanguages.NativeName(GameLanguage.Turkish)=="Türkçe","Turkish dotted I and native name retained without casing conversion");
        Check(catalog.Get(GameLanguage.Chinese,"language.title")=="请选择语言" && GameLanguages.Code(GameLanguage.Chinese)=="zh-Hans","Chinese uses a consistent Simplified Chinese locale");
        Check(catalog.Characters(true).Contains('语') && catalog.Characters(false).Contains('İ') && catalog.Characters(false).Contains('ç'),"font character lists include Chinese and accented Latin");
        bool dynamicKeys=true;
        foreach(string path in Directory.GetFiles("Assets/OneMoreFloor/Runtime","*.cs"))
        {
            string source=File.ReadAllText(path);
            foreach(Match match in Regex.Matches(source,"Loc\\.T\\(\\\"([^\\\"]+)\\\""))
            {
                string key=match.Groups[1].Value;
                if(key.EndsWith(".")) continue;
                dynamicKeys &= catalog.Keys.Contains(key);
            }
        }
        Check(dynamicKeys,"all literal runtime lookup keys exist");
        foreach(string key in new[]{"feedback.floor","feedback.delivered","feedback.boarded","feedback.lost","feedback.box","feedback.won","settings.soundOn","settings.soundOff","building.thanks","building.left","coach.deliver","coach.pickup"})
            Check(catalog.Keys.Contains(key),"conditional key exists: "+key);
        const string header="key\ten\tru\ttr\tes\tde\tfr\tzh-Hans\n";
        Reject(()=>new TextCatalog("invalid"),"invalid header rejected");
        Reject(()=>new TextCatalog(header+"a\ta\ta"),"incomplete language row rejected");
        Reject(()=>new TextCatalog(header+"a\ta\ta\ta\ta\ta\ta\t"),"empty translation rejected");
        Reject(()=>new TextCatalog(header+"a\ta\ta\ta\ta\ta\ta\ta\na\ta\ta\ta\ta\ta\ta\ta"),"duplicate key rejected");
        // Event meaning never depends on the visible language or wording.
        var game=new GameSession(new GameRules { SecondsPerFloor=.1f,StopSeconds=.1f,InitialSpawnSeconds=1000,MinimumSpawnSeconds=1000 },1);
        var events=new List<GameFeedback>(); game.Feedback+=message=>events.Add(message);
        game.Queues.TryEnqueue(0,new Passenger(1,100)); game.SelectFloor(0); game.Tick(.01f);
        Check(events.Any(e=>e.Kind==FeedbackKind.Boarded && e.Count==1),"boarding emits a typed event");
        game.SelectFloor(1); for(int i=0;i<30;i++) game.Tick(.01f);
        Check(events.Any(e=>e.Kind==FeedbackKind.Delivered && e.Count==1 && e.Multiplier==1),"delivery event carries count and multiplier for every language");
        game.Queues.TryEnqueue(2,new Passenger(0,.001f)); game.Tick(.01f);
        Check(events.Any(e=>e.Kind==FeedbackKind.Lost),"loss emits a typed event for language-independent sound");
        Console.WriteLine($"{passed} localization checks passed; {catalog.Keys.Count()} entries × {GameLanguages.Count} languages.");
    }
}
