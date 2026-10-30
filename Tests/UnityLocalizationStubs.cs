// Minimal SDK substitutes for testing the actual Loc wrapper outside Unity.
// These do not emulate Unity UI, on-device persistence, font import or Play Mode.
using System.Collections.Generic;
using System.IO;
namespace UnityEngine
{
    public sealed class TextAsset
    {
        public readonly string text;
        public TextAsset(string value) { text=value; }
    }
    public static class Resources
    {
        public static T Load<T>(string path) where T:class
        {
            string filename="Assets/OneMoreFloor/Resources/"+path+".txt";
            return File.Exists(filename) ? new TextAsset(File.ReadAllText(filename)) as T : null;
        }
    }
    public static class PlayerPrefs
    {
        static readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public static int Saves { get; private set; }
        public static string GetString(string key,string fallback) { return values.TryGetValue(key,out var value) ? value : fallback; }
        public static void SetString(string key,string value) { values[key]=value; }
        public static void Save() { Saves++; }
        public static void ClearForTests() { values.Clear(); Saves=0; }
    }
}
