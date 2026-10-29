using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreFloor.Editor
{
    [InitializeOnLoad]
    public static class FirstOpenSetup
    {
        const string ScenePath = "Assets/OneMoreFloor/Scenes/Game.unity";
        const string AttemptKey = "OneMoreFloor.FirstOpenAttempted";

        static FirstOpenSetup()
        {
            AssetDatabase.importPackageCompleted += name => EditorApplication.delayCall += Prepare;
            EditorApplication.delayCall += Prepare;
        }

        static void Prepare()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Prepare;
                return;
            }
            if (!TypographySetup.EnsureReady()) return;
            if (File.Exists(ScenePath) || SessionState.GetBool(AttemptKey, false)) return;
            SessionState.SetBool(AttemptKey, true);
            var active = SceneManager.GetActiveScene();
            // A custom scene or unsaved user work must never be replaced on import.
            if (!active.IsValid() || active.isDirty || !string.IsNullOrEmpty(active.path) || SceneManager.sceneCount != 1)
            {
                Debug.Log("Создайте игровую сцену через меню: Ещё один этаж → Создать или открыть игровую сцену.");
                return;
            }
            SceneSetup.CreateScene();
        }
    }
}
