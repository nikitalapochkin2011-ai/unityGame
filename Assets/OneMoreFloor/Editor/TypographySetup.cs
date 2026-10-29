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
        public static bool Ready { get { return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"GameSans SDF.asset")!=null &&
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder+"GameSans Bold SDF.asset")!=null; } }
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
                CreateFont("DejaVuSans","GameSans SDF");
                CreateFont("DejaVuSans-Bold","GameSans Bold SDF");
                AssetDatabase.SaveAssets();
                Debug.Log("SDF-шрифты с кириллицей готовы. Можно запускать игру.");
                return Ready;
            }
            catch(Exception error) { Debug.LogException(error); return false; }
            finally { preparing=false; }
        }
        static void CreateFont(string sourceName,string assetName)
        {
            string path=Folder+assetName+".asset";
            if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null) return;
            var source=AssetDatabase.LoadAssetAtPath<Font>(Folder+sourceName+".ttf");
            if(source==null) throw new InvalidOperationException("Missing bundled font: "+sourceName);
            var font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
            if(font==null) throw new InvalidOperationException("Unable to create SDF font: "+sourceName);
            font.name=assetName;
            var coverage=new StringBuilder();
            for(char c=' ';c<='~';c++) coverage.Append(c);
            for(char c='А';c<='я';c++) coverage.Append(c);
            coverage.Append("Ёё№×→←↑↓✓!…–—•");
            string missing;
            if(!font.TryAddCharacters(coverage.ToString(),out missing))
                throw new InvalidOperationException("Missing required glyphs in "+sourceName+": "+missing);
            AssetDatabase.CreateAsset(font,path);
            font.material.name=assetName+" Material";
            AssetDatabase.AddObjectToAsset(font.material,font);
            for(int i=0;i<font.atlasTextures.Length;i++)
            {
                var texture=font.atlasTextures[i]; texture.name=assetName+" Atlas "+i;
                texture.filterMode=FilterMode.Bilinear;
                AssetDatabase.AddObjectToAsset(texture,font);
            }
            EditorUtility.SetDirty(font);
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
