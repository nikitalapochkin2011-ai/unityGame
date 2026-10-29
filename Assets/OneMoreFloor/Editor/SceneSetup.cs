using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace OneMoreFloor.Editor
{
    public static class SceneSetup
    {
        const string ScenePath = "Assets/OneMoreFloor/Scenes/Game.unity";
        const string SettingsPath = "Assets/OneMoreFloor/GameSettings.asset";
        [MenuItem("Ещё один этаж/Создать или открыть игровую сцену")]
        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GameSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            if (!AssetDatabase.IsValidFolder("Assets/OneMoreFloor/Scenes")) AssetDatabase.CreateFolder("Assets/OneMoreFloor", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            camera.GetComponent<Camera>().orthographic = true;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = new Color(.94f,.96f,.99f);
            camera.transform.position = new Vector3(0,0,-10);
            new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            new GameObject("One More Floor", typeof(GameApp)).GetComponent<GameApp>().Settings = settings;
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.companyName = "Independent Studio"; PlayerSettings.productName = "Ещё один этаж";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.independentstudio.onemorefloor");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            AssetDatabase.SaveAssets();
            Debug.Log("Сцена готова. Нажмите Play. Настройки: " + SettingsPath);
        }
    }
}
