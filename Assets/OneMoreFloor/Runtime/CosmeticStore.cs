using UnityEngine;
namespace OneMoreFloor
{
    public static class CosmeticStore
    {
        const string Key="OneMoreFloor.Cosmetics.v1";
        public static Cosmetics Load() { return Cosmetics.Load(PlayerPrefs.GetString(Key,"")); }
        public static void Save(Cosmetics state) { PlayerPrefs.SetString(Key,state.Save()); PlayerPrefs.Save(); }
    }
}
