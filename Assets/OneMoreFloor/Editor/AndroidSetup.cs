using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OneMoreFloor.Editor
{
    // No account credentials or Android-module assembly references are needed.
    public static class AndroidSetup
    {
        const string DefaultId="com.nikitalapochkin.onemorefloor";
        static readonly NamedBuildTarget Android=NamedBuildTarget.Android;

        internal static void ApplyPlayerSettings()
        {
            if(string.IsNullOrEmpty(PlayerSettings.companyName)) PlayerSettings.companyName="Independent Studio";
            if(string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName=="One More Floor")
                PlayerSettings.productName="Ещё один этаж";
            string id=PlayerSettings.GetApplicationIdentifier(Android);
            // Never replace an identifier the owner has already chosen/published.
            if(string.IsNullOrEmpty(id) || id=="com.independentstudio.onemorefloor")
                PlayerSettings.SetApplicationIdentifier(Android,DefaultId);
            if(string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion=="0.1.0")
                PlayerSettings.bundleVersion="1.0.0";
            PlayerSettings.Android.bundleVersionCode=Math.Max(1,PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait=true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;
            PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.SetScriptingBackend(Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
        }

        [MenuItem("Ещё один этаж/Android/Подготовить проект для Android")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorUtility.DisplayDialog("Подготовка Android","Сначала остановите игру кнопкой Play.","Понятно"); return; }
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyPlayerSettings(); AssetDatabase.SaveAssets();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneSetup.ScenePath)==null) SceneSetup.CreateScene();
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneSetup.ScenePath)==null)
            {
                EditorUtility.DisplayDialog("Настройки сохранены","Дождитесь импорта и подготовки шрифтов, создайте игровую сцену через меню «Ещё один этаж» и повторите эту команду.","Понятно");
                return;
            }
            var scenes=new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(SceneSetup.ScenePath,true) };
            foreach(var scene in EditorBuildSettings.scenes)
                if(scene.path!=SceneSetup.ScenePath) scenes.Add(scene);
            EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))
            {
                EditorUtility.DisplayDialog("Настройки Android сохранены",
                    "В этой установке Unity нет Android Build Support. Закройте редактор, в Unity Hub откройте Установки → Управление → Добавить модули и установите Android Build Support вместе с SDK & NDK Tools и OpenJDK. Затем снова выполните эту команду.","Понятно");
                return;
            }
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))
            {
                Debug.LogError("Настройки сохранены, но переключение Android не завершилось. Проверьте Console и File → Build Profiles.");
                return;
            }
            ShowReport();
        }

        [MenuItem("Ещё один этаж/Android/Проверить настройки Android")]
        public static void ShowReport()
        {
            var text=new StringBuilder();
            text.AppendLine("Редактор: "+Application.unityVersion);
            text.AppendLine("Название: "+PlayerSettings.productName);
            text.AppendLine("Package Name: "+PlayerSettings.GetApplicationIdentifier(Android));
            text.AppendLine("Версия: "+PlayerSettings.bundleVersion+" / код "+PlayerSettings.Android.bundleVersionCode);
            text.AppendLine("Android Build Support: "+(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android) ? "установлен" : "не установлен"));
            text.AppendLine("Активная платформа: "+EditorUserBuildSettings.activeBuildTarget);
            text.AppendLine("Backend: "+PlayerSettings.GetScriptingBackend(Android));
            text.AppendLine("Архитектура: "+PlayerSettings.Android.targetArchitectures);
            text.AppendLine("Ориентация: "+PlayerSettings.defaultInterfaceOrientation);
            text.AppendLine("Min API: "+PlayerSettings.Android.minSdkVersion+"; Target API: "+PlayerSettings.Android.targetSdkVersion);
            bool sceneReady=EditorBuildSettings.scenes.Length>0 && EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path==SceneSetup.ScenePath;
            text.AppendLine("Game первая в глобальном списке сцен: "+(sceneReady ? "да" : "нет"));
            string androidRoot=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines","AndroidPlayer");
            text.AppendLine("Встроенный SDK: "+(Directory.Exists(Path.Combine(androidRoot,"SDK")) ? "папка найдена" : "папка не найдена"));
            text.AppendLine("Встроенный NDK: "+(Directory.Exists(Path.Combine(androidRoot,"NDK")) ? "папка найдена" : "папка не найдена"));
            text.AppendLine("Встроенный OpenJDK: "+(Directory.Exists(Path.Combine(androidRoot,"OpenJDK")) ? "папка найдена" : "папка не найдена"));
            text.AppendLine();
            text.AppendLine("APK/AAB ещё не собран. Перед Google Play проверьте требуемый Target API. Если используете отдельный Build Profile со своим списком сцен, добавьте Game и туда.");
            Debug.Log(text.ToString());
            EditorUtility.DisplayDialog("Настройки Android",text.ToString(),"Понятно");
        }
    }
}
