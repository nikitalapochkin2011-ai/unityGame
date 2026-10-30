using System;

namespace OneMoreFloor
{
    [Serializable]
    public sealed class LevelDefinition
    {
        public string Title;
        public int ActiveFloors = 3, DeliveryGoal = 6, QueueLimit = 3;
        public float SpawnSeconds = 9, FinalSpawnSeconds = 8.5f, PatienceSeconds = 80, BoxChance;
        public bool Guided;

        public LevelDefinition Copy()
        {
            return (LevelDefinition)MemberwiseClone();
        }
        public void Validate(int floors)
        {
            ActiveFloors = Math.Max(2, Math.Min(floors, ActiveFloors));
            DeliveryGoal = Math.Max(1, DeliveryGoal);
            QueueLimit = Math.Max(1, Math.Min(GameRules.MaxWaitingPerFloor, QueueLimit));
            SpawnSeconds = Math.Max(2, SpawnSeconds);
            FinalSpawnSeconds = Math.Max(2, Math.Min(SpawnSeconds, FinalSpawnSeconds));
            PatienceSeconds = Math.Max(10, PatienceSeconds);
            BoxChance = Math.Max(0, Math.Min(1, BoxChance));
        }
    }

    public static class Campaign
    {
        public const int LevelCount = 5;
        public static LevelDefinition[] CreateLevels()
        {
            return new[]
            {
                Make("Первый рабочий день",3,6,9,8.5f,80,2,0,true),
                Make("Знакомые лица",3,10,8,7.5f,70,3,0),
                Make("Новоселье",4,12,7.8f,7.2f,74,4,.20f),
                Make("Дом полон историй",5,14,7.6f,7,80,5,.23f),
                Make("Хозяин лифта",6,16,7.4f,6.8f,86,5,.25f)
            };
        }
        static LevelDefinition Make(string title,int floors,int goal,float initial,float final,float patience,int queue,float box,bool guided=false)
        {
            return new LevelDefinition { Title=title, ActiveFloors=floors, DeliveryGoal=goal,
                SpawnSeconds=initial, FinalSpawnSeconds=final, PatienceSeconds=patience,
                QueueLimit=queue, BoxChance=box, Guided=guided };
        }
        public static DifficultyState Difficulty(LevelDefinition level,int number,int delivered)
        {
            float t=Math.Max(0,Math.Min(1,(float)delivered/level.DeliveryGoal));
            return new DifficultyState(number,level.ActiveFloors,level.ActiveFloors-1,
                level.SpawnSeconds+(level.FinalSpawnSeconds-level.SpawnSeconds)*t);
        }
    }

    // Storage-independent progress: only a victory on the next unlocked level advances it.
    public sealed class CampaignProgress
    {
        public int Completed { get; private set; }
        public int Unlocked { get { return Math.Min(Campaign.LevelCount,Completed+1); } }
        public CampaignProgress(int completed=0) { Completed=Math.Max(0,Math.Min(Campaign.LevelCount,completed)); }
        public bool IsUnlocked(int number) { return number>=1 && number<=Unlocked; }
        public bool Complete(int number)
        {
            if(!IsUnlocked(number) || number!=Completed+1) return false;
            Completed=number; return true;
        }
    }
}
