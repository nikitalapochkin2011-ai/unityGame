using UnityEngine;
namespace OneMoreFloor
{
    public static class CosmeticStore
    {
        const string LegacyKey="OneMoreFloor.Cosmetics.v1",AccountKey="OneMoreFloor.Account.v1";
        public static PlayerAccount LoadAccount()
        {return PlayerAccount.Load(PlayerPrefs.GetString(AccountKey,""),PlayerPrefs.GetString(LegacyKey,""));}
        public static Cosmetics Load() { return LoadAccount().Wallet; }
        public static void Save(Cosmetics state) { SaveAccount(state,LoadAccount().Milestones); }
        public static void SaveAccount(Cosmetics wallet,Achievements achievements)
        {PlayerPrefs.SetString(AccountKey,new PlayerAccount(wallet,achievements).Save());PlayerPrefs.Save();}
    }
}
