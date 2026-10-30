using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace OneMoreFloor.Editor
{
    [InitializeOnLoad]
    public static class TypographySetup
    {
        const string Folder="Assets/OneMoreFloor/Resources/Fonts/";
        static bool importing, preparing, reported;
        public static bool Ready
        {
            get
            {
                var text=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/OneMoreFloor/Resources/Localization/Strings.txt");
                if(text==null) return false;
                var catalog=new TextCatalog(text.text);
                return Covers(Font("GameLatin SDF"),LatinCoverage(catalog)) && Covers(Font("GameLatin Bold SDF"),LatinCoverage(catalog)) &&
                    Covers(Font("GameHan SDF"),catalog.Characters(true)) && Covers(Font("GameHan Bold SDF"),catalog.Characters(true));
            }
        }
        static bool Covers(TMP_FontAsset font,string characters)
        {
            if(font==null) return false;
            foreach(char character in characters) if(!font.HasCharacter(character)) return false;
            return true;
        }
        static TMP_FontAsset Font(string name) { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+name+".asset"); }
        static TypographySetup()
        {
            EditorApplication.delayCall+=PrepareWhenIdle;
            AssetDatabase.importPackageCompleted+=Imported;
            AssetDatabase.importPackageFailed+=(name,error)=>{ importing=false; Debug.LogError("TMP resources: "+error); };
            AssetDatabase.importPackageCancelled+=name=>{ importing=false; };
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.ExitingEditMode || Ready) return;
                EditorApplication.isPlaying=false;
                Debug.Log("Подготавливаем чёткие шрифты. Дождитесь импорта и нажмите Play ещё раз.");
                EditorApplication.delayCall+=PrepareWhenIdle;
            };
        }
        static void Imported(string name)
        {
            if(!name.Contains("TMP Essential Resources")) return;
            importing=false; EditorApplication.delayCall+=PrepareWhenIdle;
        }
        static void PrepareWhenIdle()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            if(EditorApplication.isUpdating || EditorApplication.isCompiling)
            { EditorApplication.delayCall+=PrepareWhenIdle; return; }
            EnsureReady();
        }
        [MenuItem("Ещё один этаж/Подготовить качественный текст")]
        public static void PrepareFromMenu() { reported=false; EnsureReady(); }
        public static bool EnsureReady()
        {
            if(Ready) return true;
            if(importing || preparing) return false;
            // Unity 6 ships TMP with uGUI. Older package revisions require their own
            // bundled Essential Resources; import them locally, without a network request.
            if(Shader.Find("TextMeshPro/Mobile/Distance Field")==null || TMP_Settings.instance==null)
            {
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_FontAsset).Assembly);
                string[] archives=package==null ? new string[0] : Directory.GetFiles(package.resolvedPath,"TMP Essential Resources.unitypackage",SearchOption.AllDirectories);
                if(archives.Length==0)
                {
                    if(!reported) Debug.LogError("Не найдены TMP Essential Resources. Откройте Window → TextMeshPro → Import TMP Essential Resources, затем Ещё один этаж → Подготовить качественный текст.");
                    reported=true; return false;
                }
                importing=true; AssetDatabase.ImportPackage(archives[0],false); return false;
            }
            preparing=true;
            try
            {
                var text=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/OneMoreFloor/Resources/Localization/Strings.txt");
                if(text==null) throw new InvalidOperationException("Missing shipped translation catalogue");
                var catalog=new TextCatalog(text.text);
                string latin=LatinCoverage(catalog),han=catalog.Characters(true);
                var chinese=CreateFont("OMFHan-Regular.otf","GameHan SDF",han,48,2048);
                var chineseBold=CreateFont("OMFHan-Bold.otf","GameHan Bold SDF",han,48,2048);
                var regular=CreateFont("DejaVuSans.ttf","GameLatin SDF",latin,64,1024);
                var bold=CreateFont("DejaVuSans-Bold.ttf","GameLatin Bold SDF",latin,64,1024);
                regular.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{chinese};
                bold.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{chineseBold};
                EditorUtility.SetDirty(regular); EditorUtility.SetDirty(bold);
                AssetDatabase.SaveAssets();
                Debug.Log("SDF-шрифты для 7 языков готовы. Можно запускать игру.");
                return Ready;
            }
            catch(Exception error) { Debug.LogException(error); return false; }
            finally { preparing=false; }
        }
        static string LatinCoverage(TextCatalog catalog)
        {
            var coverage=new StringBuilder(catalog.Characters(false));
            for(char c=' ';c<='~';c++) coverage.Append(c);
            for(char c='А';c<='я';c++) coverage.Append(c);
            coverage.Append("Ёё№×→←↑↓✓!…–—•");
            return coverage.ToString();
        }
        static TMP_FontAsset CreateFont(string sourceName,string assetName,string characters,int sampling,int atlas)
        {
            string path=Folder+assetName+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            var source=AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(Folder+sourceName);
            if(source==null) throw new InvalidOperationException("Missing bundled font: "+sourceName);
            var font=existing ?? TMP_FontAsset.CreateFontAsset(source,sampling,8,GlyphRenderMode.SDFAA,atlas,atlas,AtlasPopulationMode.Dynamic,true);
            if(font==null) throw new InvalidOperationException("Unable to create SDF font: "+sourceName);
            font.name=assetName;
            string missing;
            if(!font.TryAddCharacters(characters,out missing))
                throw new InvalidOperationException("Missing required glyphs in "+sourceName+": "+missing);
            if(existing==null) AssetDatabase.CreateAsset(font,path);
            font.material.name=assetName+" Material";
            if(!AssetDatabase.Contains(font.material)) AssetDatabase.AddObjectToAsset(font.material,font);
            for(int i=0;i<font.atlasTextures.Length;i++)
            {
                var texture=font.atlasTextures[i]; texture.name=assetName+" Atlas "+i;
                texture.filterMode=FilterMode.Bilinear;
                if(!AssetDatabase.Contains(texture)) AssetDatabase.AddObjectToAsset(texture,font);
            }
            EditorUtility.SetDirty(font);
            return font;
        }
    }
    public sealed class TypographyBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return -100; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            if(!TypographySetup.EnsureReady()) throw new BuildFailedException("Подготовьте SDF-шрифты через меню Ещё один этаж → Подготовить качественный текст, затем повторите сборку.");
        }
    }
}
