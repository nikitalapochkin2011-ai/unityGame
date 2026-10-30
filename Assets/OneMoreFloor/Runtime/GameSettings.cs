using UnityEngine;
namespace OneMoreFloor
{
    [CreateAssetMenu(menuName = "Ещё один этаж/Настройки игры")]
    public sealed class GameSettings : ScriptableObject
    {
        public GameRules Rules = new GameRules();
        public LevelDefinition[] Levels = Campaign.CreateLevels();
        public void Prepare()
        {
            if(Rules==null) Rules=new GameRules();
            Rules.Validate();
            // Existing GameSettings assets gain the campaign without a manual scene rebuild.
            var defaults=Campaign.CreateLevels();
            if(Levels==null || Levels.Length!=Campaign.LevelCount) Levels=defaults;
            for(int i=0;i<Levels.Length;i++)
            {
                if(Levels[i]==null) Levels[i]=defaults[i];
                Levels[i].Validate(Rules.Floors);
            }
        }
        void OnValidate() { Prepare(); }
    }
}
