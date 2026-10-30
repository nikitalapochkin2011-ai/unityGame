using System;

namespace OneMoreFloor
{
    public enum TrafficPattern { Mixed, Neighbors, LongTrips, Waves, MovingDay }

    [Serializable]
    public sealed class LevelDefinition
    {
        public string Title;
        public int ActiveFloors = 3, DeliveryGoal = 6, QueueLimit = 3;
        public float SpawnSeconds = 3.6f, FinalSpawnSeconds = 3.2f, PatienceSeconds = 80, BoxChance;
        public int MaxActivePassengers = 6;
        public float EmptyHouseDelay = .8f;
        public TrafficPattern Traffic;
        public bool Guided;

        public LevelDefinition Copy() { return (LevelDefinition)MemberwiseClone(); }
        public void Validate(int floors)
        {
            ActiveFloors = Math.Max(2, Math.Min(floors, ActiveFloors));
            DeliveryGoal = Math.Max(1, DeliveryGoal);
            QueueLimit = Math.Max(1, Math.Min(GameRules.MaxWaitingPerFloor, QueueLimit));
            SpawnSeconds = Math.Max(2, SpawnSeconds);
            FinalSpawnSeconds = Math.Max(2, Math.Min(SpawnSeconds, FinalSpawnSeconds));
            PatienceSeconds = Math.Max(10, PatienceSeconds);
            BoxChance = Math.Max(0, Math.Min(1, BoxChance));
            MaxActivePassengers = Math.Max(2, Math.Min(12, MaxActivePassengers));
            EmptyHouseDelay = Math.Max(.2f, Math.Min(1, EmptyHouseDelay));
            if(!Enum.IsDefined(typeof(TrafficPattern),Traffic)) Traffic=TrafficPattern.Mixed;
        }
    }

    public static class Campaign
    {
        public const int LevelCount = 50, LevelsPerPage = 5, BalanceVersion = 2;
        public const int PageCount = (LevelCount+LevelsPerPage-1)/LevelsPerPage;
        public static int PageForLevel(int number)
        { return (Math.Max(1,Math.Min(LevelCount,number))-1)/LevelsPerPage; }

        public static LevelDefinition[] CreateLevels()
        {
            var levels=new LevelDefinition[LevelCount];
            levels[0]=Make("Первый рабочий день",3,6,3.6f,3.2f,80,2,0,3,true);
            levels[1]=Make("Знакомые лица",3,10,3.3f,3,80,3,0,4);
            levels[2]=Make("Новоселье",4,12,3.5f,3.2f,95,4,.20f,5);
            levels[3]=Make("Дом полон историй",5,14,3.6f,3.3f,110,4,.23f,5);
            levels[4]=Make("Хозяин лифта",6,16,3.8f,3.5f,120,5,.25f,6);
            string[] names={
                "Доброе утро", "Соседи по площадке", "В гости наверх", "Встречи у лифта", "Первые коробки",
                "День открытых дверей", "Короткий визит", "С верхнего этажа", "Дружная компания", "Смена адреса",
                "Дом просыпается", "За чашкой чая", "Вверх и обратно", "После прогулки", "Переезд без спешки",
                "Тёплый полдень", "Через одну дверь", "Дальний звонок", "Время встреч", "Новые жильцы",
                "Жизнь кипит", "Забежать к соседу", "Панорама города", "Час общения", "Большие посылки",
                "Вечер в доме", "Рядом с вами", "На другом конце", "Все возвращаются", "Ключи от квартиры",
                "Оживлённый подъезд", "Близкие друзья", "Маршрут через дом", "Приятная суета", "Вещи для уюта",
                "Огни в окнах", "Минутная встреча", "Вид с высоты", "Соседи собрались", "Дом обжит",
                "Последний круг", "Добрые знакомые", "От первого до крыши", "Праздник во дворе", "Пятьдесят историй"
            };
            for(int i=5;i<LevelCount;i++)
            {
                int chapter=(i-5)/5, variant=(i-5)%5;
                var pattern=(TrafficPattern)variant;
                // Five alternating traffic profiles, with gentle changes within each new chapter.
                float interval=3.65f-chapter*.055f;
                float boxes=.16f+chapter*.012f;
                int floorCount=6;
                if(pattern==TrafficPattern.Neighbors) { interval-=.4f; boxes=.12f+chapter*.008f; }
                if(pattern==TrafficPattern.LongTrips) { interval+=.35f; boxes=.12f+chapter*.009f; }
                if(pattern==TrafficPattern.Waves) interval+=1.35f;
                if(pattern==TrafficPattern.MovingDay) { interval+=.25f; boxes=.32f+chapter*.012f; }
                var level=Make(names[i-5],floorCount,16+chapter+variant%3*2,interval,interval-.25f,
                    125+chapter*3+(pattern==TrafficPattern.LongTrips ? 15 : 0),5,boxes,
                    pattern==TrafficPattern.Waves ? 7 : 6);
                level.Traffic=pattern; levels[i]=level;
            }
            return levels;
        }
        static LevelDefinition Make(string title,int floors,int goal,float initial,float final,float patience,int queue,float box,int activePassengers,bool guided=false)
        {
            return new LevelDefinition { Title=title, ActiveFloors=floors, DeliveryGoal=goal,
                SpawnSeconds=initial, FinalSpawnSeconds=final, PatienceSeconds=patience,
                QueueLimit=queue, BoxChance=box, MaxActivePassengers=activePassengers, Guided=guided };
        }
        public static string TrafficDescription(TrafficPattern pattern)
        {
            switch(pattern)
            {
                case TrafficPattern.Neighbors: return "Короткие визиты: поездки на 1–2 этажа";
                case TrafficPattern.LongTrips: return "Дальние поездки: через половину дома и дальше";
                case TrafficPattern.Waves: return "Небольшие волны: иногда приходят сразу двое";
                case TrafficPattern.MovingDay: return "Переезды: чаще встречаются соседи с коробкой";
                default: return "Жизнь дома: короткие и дальние поездки вперемешку";
            }
        }
        public static DifficultyState Difficulty(LevelDefinition level,int number,int delivered)
        {
            float t=Math.Max(0,Math.Min(1,(float)delivered/level.DeliveryGoal));
            int distance=level.Traffic==TrafficPattern.Neighbors ? Math.Min(2,level.ActiveFloors-1) : level.ActiveFloors-1;
            return new DifficultyState(number,level.ActiveFloors,distance,
                level.SpawnSeconds+(level.FinalSpawnSeconds-level.SpawnSeconds)*t);
        }
        public static LevelDefinition[] UpgradeLevels(LevelDefinition[] saved,int balanceVersion,int floors)
        {
            var defaults=CreateLevels();
            if(saved==null) return defaults;
            int count=Math.Min(saved.Length,defaults.Length);
            for(int i=0;i<count;i++)
            {
                if(saved[i]==null) continue;
                // Replace untouched v1 presets, retain the user's edited level settings.
                if(balanceVersion<BalanceVersion && IsOldPreset(saved[i],i,defaults[i].Title)) continue;
                defaults[i]=saved[i].Copy();
                if(balanceVersion<BalanceVersion && defaults[i].MaxActivePassengers<=0) defaults[i].MaxActivePassengers=6;
                if(balanceVersion<BalanceVersion && defaults[i].EmptyHouseDelay<=0) defaults[i].EmptyHouseDelay=.8f;
                defaults[i].Validate(floors);
            }
            return defaults;
        }
        static bool IsOldPreset(LevelDefinition level,int index,string expectedTitle)
        {
            if(index>=5) return false;
            float[] oldSpawn={9,8,7.8f,7.6f,7.4f}, oldFinal={8.5f,7.5f,7.2f,7,6.8f}, oldPatience={80,70,74,80,86};
            int[] oldGoals={6,10,12,14,16},oldFloors={3,3,4,5,6},oldQueues={2,3,4,5,5};
            float[] oldBoxes={0,0,.20f,.23f,.25f};
            return level.Title==expectedTitle && level.ActiveFloors==oldFloors[index] &&
                level.DeliveryGoal==oldGoals[index] && level.QueueLimit==oldQueues[index] &&
                Math.Abs(level.SpawnSeconds-oldSpawn[index])<.001f && Math.Abs(level.FinalSpawnSeconds-oldFinal[index])<.001f &&
                Math.Abs(level.PatienceSeconds-oldPatience[index])<.001f && Math.Abs(level.BoxChance-oldBoxes[index])<.001f && level.Guided==(index==0);
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
