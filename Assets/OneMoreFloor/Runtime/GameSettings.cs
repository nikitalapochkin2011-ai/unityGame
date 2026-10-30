using UnityEngine;
namespace OneMoreFloor
{
    [CreateAssetMenu(menuName = "Ещё один этаж/Настройки игры")]
    public sealed class GameSettings : ScriptableObject
    {
        public GameRules Rules = new GameRules();
        public LevelDefinition[] Levels = Campaign.CreateLevels();
        public int CampaignBalanceVersion;
        public void Prepare()
        {
            if(Rules==null) Rules=new GameRules();
            Rules.Validate();
            // Existing GameSettings assets gain the campaign without a manual scene rebuild.
            if(Levels==null || Levels.Length!=Campaign.LevelCount || CampaignBalanceVersion<Campaign.BalanceVersion)
                Levels=Campaign.UpgradeLevels(Levels,CampaignBalanceVersion,Rules.Floors);
            CampaignBalanceVersion=Campaign.BalanceVersion;
            for(int i=0;i<Levels.Length;i++)
            {
                if(Levels[i]==null) Levels[i]=Campaign.CreateLevels()[i];
                Levels[i].Validate(Rules.Floors);
            }
        }
        void OnValidate() { Prepare(); }
    }
}
