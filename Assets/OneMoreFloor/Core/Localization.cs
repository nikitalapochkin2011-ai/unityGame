using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OneMoreFloor
{
    public enum GameLanguage { English, Russian, Turkish, Spanish, German, French, Chinese }

    public static class GameLanguages
    {
        public const int Count=7;
        static readonly string[] codes={"en","ru","tr","es","de","fr","zh-Hans"};
        static readonly string[] names={"English","Русский","Türkçe","Español","Deutsch","Français","简体中文"};
        public static string Code(GameLanguage language) { return codes[(int)language]; }
        public static string NativeName(GameLanguage language) { return names[(int)language]; }
        public static bool TryParse(string code,out GameLanguage language)
        {
            language=GameLanguage.English;
            for(int i=0;i<codes.Length;i++) if(string.Equals(code,codes[i],StringComparison.Ordinal))
            { language=(GameLanguage)i; return true; }
            return false;
        }
        public static bool IsChineseCharacter(char c) { return c>='\u2e80'; }
    }

    // No network or platform dependency. Every translation is shipped in the project.
    public sealed class TextCatalog
    {
        readonly Dictionary<string,string[]> entries=new Dictionary<string,string[]>(StringComparer.Ordinal);
        public IEnumerable<string> Keys { get { return entries.Keys; } }
        public TextCatalog(string source)
        {
            using(var reader=new StringReader(source))
            {
                string header=reader.ReadLine();
                if(header==null || header.TrimStart('\ufeff')!="key\ten\tru\ttr\tes\tde\tfr\tzh-Hans")
                    throw new FormatException("Invalid localization header");
                string line; int number=1;
                while((line=reader.ReadLine())!=null)
                {
                    number++; if(string.IsNullOrWhiteSpace(line)) continue;
                    var cells=line.Split('\t');
                    if(cells.Length!=GameLanguages.Count+1 || string.IsNullOrWhiteSpace(cells[0]))
                        throw new FormatException("Invalid localization row "+number);
                    var values=new string[GameLanguages.Count];
                    for(int i=0;i<values.Length;i++)
                    {
                        if(string.IsNullOrWhiteSpace(cells[i+1])) throw new FormatException("Empty translation: "+cells[0]);
                        values[i]=cells[i+1].Replace("\\n","\n");
                    }
                    if(entries.ContainsKey(cells[0])) throw new FormatException("Duplicate localization key: "+cells[0]);
                    entries.Add(cells[0],values);
                }
            }
        }
        public string Get(GameLanguage language,string key,params object[] args)
        {
            if((int)language<0 || (int)language>=GameLanguages.Count) throw new ArgumentOutOfRangeException(nameof(language));
            string text=entries[key][(int)language];
            return args.Length==0 ? text : string.Format(CultureInfo.InvariantCulture,text,args);
        }
        public string Characters(bool chinese)
        {
            var chars=new SortedSet<char>();
            foreach(var values in entries.Values) foreach(string text in values) foreach(char c in text)
                if(!char.IsControl(c) && GameLanguages.IsChineseCharacter(c)==chinese) chars.Add(c);
            for(int i=0;i<GameLanguages.Count;i++) foreach(char c in GameLanguages.NativeName((GameLanguage)i))
                if(GameLanguages.IsChineseCharacter(c)==chinese) chars.Add(c);
            var result=new StringBuilder(); foreach(char c in chars) result.Append(c); return result.ToString();
        }
    }
}
