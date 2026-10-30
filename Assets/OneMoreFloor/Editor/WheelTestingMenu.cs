using UnityEditor;
using UnityEngine;

namespace OneMoreFloor.Editor
{
    public static class WheelTestingMenu
    {
        const string Menu="Ещё один этаж/Тестирование/Бесконечные вращения";
        [MenuItem(Menu)]
        static void Toggle()
        {
            bool enabled=!EditorPrefs.GetBool(GameApp.WheelTestPreference,false);
            EditorPrefs.SetBool(GameApp.WheelTestPreference,enabled);
            Debug.Log(enabled ? "Бесконечные вращения включены. Нажмите Play и откройте колесо. Тестовые монеты и оформления не сохраняются; обычное сохранение останется прежним. После сбора всех оформлений в тесте выпадают только монеты." : "Бесконечные вращения выключены. Следующий Play загрузит обычное сохранение.");
        }
        [MenuItem(Menu,true)]
        static bool Validate()
        {
            UnityEditor.Menu.SetChecked(Menu,EditorPrefs.GetBool(GameApp.WheelTestPreference,false));
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
