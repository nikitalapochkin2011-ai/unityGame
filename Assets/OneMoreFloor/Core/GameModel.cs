using System;
using System.Collections.Generic;

namespace OneMoreFloor
{
    [Serializable]
    public class GameRules
    {
        public const int MaxWaitingPerFloor = 7;
        public int Floors = 6, Capacity = 4, LossLimit = 3, DeliveryPoints = 10;
        public float SecondsPerFloor = 1.25f, StopSeconds = 0.65f;
        public float PatienceSeconds = 32f, InitialSpawnSeconds = 5f, MinimumSpawnSeconds = 1.5f;
        public int DifficultyScoreCap = 450, StartingFloors = 3, StartingRouteDistance = 1;
        public void Validate()
        {
            Floors = Math.Max(2, Floors); Capacity = Math.Max(1, Capacity); LossLimit = Math.Max(1, LossLimit);
            SecondsPerFloor = Math.Max(.1f, SecondsPerFloor); StopSeconds = Math.Max(.1f, StopSeconds);
            PatienceSeconds = Math.Max(1, PatienceSeconds); MinimumSpawnSeconds = Math.Max(.2f, MinimumSpawnSeconds);
            InitialSpawnSeconds = Math.Max(MinimumSpawnSeconds, InitialSpawnSeconds);
            DifficultyScoreCap = Math.Max(1, DifficultyScoreCap);
            StartingFloors = Math.Max(2, Math.Min(Floors, StartingFloors));
            StartingRouteDistance = Math.Max(1, Math.Min(StartingFloors - 1, StartingRouteDistance));
            DeliveryPoints = Math.Max(1, DeliveryPoints);
        }
    }
    public sealed class Passenger
    {
        public const int HumanAppearanceCount=16, RaccoonAppearance=16;
        public const int RaccoonChancePercent=4, DinosaurAppearance=17, AlienAppearance=18;
        public const int AppearanceRollSize=10000, DinosaurChanceBasisPoints=50;
        // One independent roll: exactly 0.5% dinosaur, 4% raccoon. The alien is scripted.
        public static int SpecialAppearance(int roll)
        {
            if(roll<0 || roll>=AppearanceRollSize)throw new ArgumentOutOfRangeException(nameof(roll));
            return roll<DinosaurChanceBasisPoints ? DinosaurAppearance :
                roll<DinosaurChanceBasisPoints+RaccoonChancePercent*100 ? RaccoonAppearance : -1;
        }
        public bool IsDinosaur { get; private set; }
        public bool IsAlien { get; private set; }
        public bool IsFinalGuest { get; private set; }
        public bool IsRaccoon { get; private set; }
        public int DeliveryUnits { get { return 1; } }
        public int Destination { get; private set; }
        public bool HasBox { get; private set; }
        public int SpaceRequired { get { return HasBox ? 2 : 1; } }
        public int Appearance { get; private set; }
        public float Remaining { get; internal set; }
        public Passenger(int destination, float patience, bool hasBox=false, int appearance=0, bool isRaccoon=false, bool isDinosaur=false, bool isAlien=false, int finaleAppearance=-1)
        {
            if(finaleAppearance>=0 && RooftopFinales.ForAppearance(finaleAppearance)==null)throw new ArgumentOutOfRangeException(nameof(finaleAppearance));
            if(finaleAppearance<0 && isAlien)finaleAppearance=AlienAppearance;
            Destination=destination;Remaining=patience;IsFinalGuest=finaleAppearance>=0;IsAlien=finaleAppearance==AlienAppearance;
            IsDinosaur=!IsFinalGuest && isDinosaur;IsRaccoon=!IsFinalGuest && !isDinosaur && isRaccoon;
            HasBox=hasBox && !IsDinosaur && !IsFinalGuest;
            Appearance=IsFinalGuest ? finaleAppearance : IsDinosaur ? DinosaurAppearance : IsRaccoon ? RaccoonAppearance : Math.Max(0,appearance)%HumanAppearanceCount;
        }
    }
    public sealed class Elevator
    {
        readonly GameRules rules;
        readonly bool roofAccess;
        public readonly List<Passenger> Riders = new List<Passenger>();
        public int OccupiedSpaces
        {
            get { int count=0; foreach(var passenger in Riders) count+=passenger.SpaceRequired; return count; }
        }
        public int CurrentFloor { get; private set; }
        public int TargetFloor { get; private set; }
        public int? NextFloor { get; private set; }
        public float Position { get; private set; }
        public bool Moving { get; private set; }
        float stopRemaining;
        public Elevator(GameRules rules,bool roofAccess=false) { this.rules=rules;this.roofAccess=roofAccess; }
        public void Select(int floor)
        {
            if (floor < 0 || floor >= rules.Floors+(roofAccess ? 1 : 0)) return;
            NextFloor = floor; // Last tap wins; the active trip is immutable.
        }
        internal bool Tick(float dt)
        {
            if (Moving)
            {
                float distance = dt / rules.SecondsPerFloor;
                if (Math.Abs(TargetFloor - Position) <= distance)
                {
                    Position = TargetFloor; CurrentFloor = TargetFloor; Moving = false;
                    stopRemaining = rules.StopSeconds; return true;
                }
                Position += Math.Sign(TargetFloor - Position) * distance;
                return false;
            }
            stopRemaining = Math.Max(0, stopRemaining - dt);
            if (stopRemaining > 0 || !NextFloor.HasValue) return false;
            TargetFloor = NextFloor.Value; NextFloor = null;
            if (TargetFloor == CurrentFloor) { stopRemaining = rules.StopSeconds; return true; }
            Moving = true;
            return false;
        }
    }
    public sealed class PassengerQueues
    {
        readonly List<Passenger>[] queues;
        public IReadOnlyList<IReadOnlyList<Passenger>> Floors { get; private set; }
        public PassengerQueues(int count)
        {
            queues = new List<Passenger>[count];
            var views = new IReadOnlyList<Passenger>[count];
            for (int i = 0; i < count; i++)
            {
                queues[i] = new List<Passenger>(GameRules.MaxWaitingPerFloor);
                views[i] = queues[i].AsReadOnly();
            }
            Floors = Array.AsReadOnly(views);
        }
        public bool TryEnqueue(int floor, Passenger passenger)
        {
            if (floor < 0 || floor >= queues.Length || passenger == null) return false;
            var queue = queues[floor];
            if (queue.Count >= GameRules.MaxWaitingPerFloor) return false;
            queue.Add(passenger); return true;
        }
        internal Passenger TakeFirst(int floor)
        {
            var queue = queues[floor]; var passenger = queue[0]; queue.RemoveAt(0); return passenger;
        }
        internal void Age(float dt, Action<int,Passenger> lost, Func<bool> ended)
        {
            for(int floor=0;floor<queues.Length;floor++)
            {
                var queue=queues[floor];
                for (int i = 0; i < queue.Count;)
                {
                    queue[i].Remaining -= dt;
                    if (queue[i].Remaining > 0) { i++; continue; }
                    // Keep the terminal passenger in the same slot while the ad offer
                    // is open. Earlier losses leave normally and free a queue slot.
                    lost(floor,queue[i]); if(ended()) return;
                    queue.RemoveAt(i);
                }
            }
        }
    }
    public sealed class PassengerSpawner
    {
        readonly Random random, specialRandom;
        public int Spawned { get; private set; }
        float countdown = 1.5f;
        public PassengerSpawner(int seed) { random = new Random(seed); specialRandom=new Random(unchecked(seed ^ 0x51EC7A)); }
        internal void Tick(float dt, GameSession game)
        {
            if(game.Level!=null && (Spawned>=game.SpawnBudget || !game.NeedsMorePassengers || (game.IsGuiding && Spawned>0))) return;
            var difficulty=game.Difficulty;
            if(game.Level!=null && game.ActivePassengerCount==0)
                countdown=Math.Min(countdown,game.Level.EmptyHouseDelay);
            if(game.Level==null) countdown=Math.Min(countdown,difficulty.SpawnInterval);
            countdown-=dt;
            if(countdown>0) return;
            // A full house waits for a free slot; it doesn't build up a hidden backlog.
            if(game.Level!=null && game.ActivePassengerCount>=game.Level.MaxActivePassengers)
            { countdown=0; return; }
            int batch=game.Level!=null && !game.IsGuiding && game.Level.Traffic==TrafficPattern.Waves && random.NextDouble()<.6 ? 2 : 1;
            for(int i=0;i<batch;i++)
            {
                if(game.Level!=null && (Spawned>=game.SpawnBudget || !game.NeedsMorePassengers || game.ActivePassengerCount>=game.Level.MaxActivePassengers)) break;
                if(!Spawn(game,difficulty)) break;
            }
            countdown+=difficulty.SpawnInterval*(game.Level==null ? 1 : .85f+(float)random.NextDouble()*.30f);
        }
        bool Spawn(GameSession game,DifficultyState difficulty)
        {
            // Every origin remains random among unlocked floors with room.
            int available=0;
            for(int f=0;f<difficulty.ActiveFloors;f++)
                if(game.Queues.Floors[f].Count<game.QueueLimit) available++;
            if(available==0) return false;
            int choice=random.Next(available),floor=0;
            for(int f=0;f<difficulty.ActiveFloors;f++)
            {
                if(game.Queues.Floors[f].Count>=game.QueueLimit) continue;
                if(choice--==0) { floor=f; break; }
            }
            int min=Math.Max(0,floor-difficulty.MaximumRouteDistance);
            int max=Math.Min(difficulty.ActiveFloors-1,floor+difficulty.MaximumRouteDistance);
            int destination;
            if(game.Level!=null && game.Level.Traffic==TrafficPattern.LongTrips)
            {
                int minimumDistance=Math.Max(1,game.Level.ActiveFloors/2),choices=0;
                for(int f=0;f<difficulty.ActiveFloors;f++) if(Math.Abs(f-floor)>=minimumDistance) choices++;
                int selected=random.Next(choices); destination=0;
                for(int f=0;f<difficulty.ActiveFloors;f++)
                    if(Math.Abs(f-floor)>=minimumDistance && selected--==0) { destination=f; break; }
            }
            else
            {
                destination=random.Next(min,max);
                if(destination>=floor) destination++;
            }
            bool box=game.Level!=null && game.Rules.Capacity>=2 && random.NextDouble()<game.Level.BoxChance;
            int appearance=random.Next(Passenger.HumanAppearanceCount);
            int special=Passenger.SpecialAppearance(specialRandom.Next(Passenger.AppearanceRollSize));
            bool finale=game.FinalGuestReady;
            var passenger=finale ? new Passenger(game.RoofFloor,game.PassengerPatience,finaleAppearance:game.Finale.Appearance) :
                new Passenger(destination,game.PassengerPatience,box,appearance,isRaccoon:special==Passenger.RaccoonAppearance,isDinosaur:special==Passenger.DinosaurAppearance);
            if(!game.Queues.TryEnqueue(floor,passenger))return false;
            Spawned++; return true;
        }
    }

    public enum PlayState { Playing, Paused, GameOver, Won }
    public enum FeedbackKind { Floor, Delivered, Boarded, Lost, BoxBlocked, Won }
    public readonly struct GameFeedback
    {
        public readonly FeedbackKind Kind;
        public readonly int Count, Multiplier;
        public GameFeedback(FeedbackKind kind,int count=0,int multiplier=1)
        { Kind=kind; Count=count; Multiplier=multiplier; }
    }
    public sealed class GameSession
    {
        public readonly GameRules Rules;
        public readonly Elevator Lift;
        public readonly PassengerQueues Queues;
        readonly PassengerSpawner spawner;
        public readonly LevelDefinition Level;
        public readonly int LevelNumber;
        public const int ReservePassengers=3;
        public int SpawnBudget { get { return Level==null ? int.MaxValue : (int)Math.Min(int.MaxValue,(long)Level.DeliveryGoal+ReservePassengers); } }
        // Replacements use the reserve only after someone actually leaves. Do not
        // populate a surplus queue once all remaining deliveries are already present.
        public FinaleDefinition Finale { get { return Level==null ? null : RooftopFinales.ForLevel(LevelNumber); } }
        public bool HasRooftopFinale { get { return Finale!=null; } }
        public int RoofFloor { get { return Rules.Floors; } }
        public bool FinalGuestDelivered { get; private set; }
        public bool FinalGuestReady { get { return HasRooftopFinale && Delivered>=Level.DeliveryGoal-1 && ActivePassengerCount==0; } }
        // Original level-10 queries remain specific to the alien for compatibility.
        public bool HasAlienFinale { get { return HasRooftopFinale && LevelNumber==10; } }
        public bool AlienDelivered { get { return HasAlienFinale && FinalGuestDelivered; } }
        public bool FinalAlienReady { get { return HasAlienFinale && FinalGuestReady; } }
        public bool NeedsMorePassengers
        {
            get
            {
                if(Level==null)return true;
                int goal=HasRooftopFinale && Delivered<Level.DeliveryGoal-1 ? Level.DeliveryGoal-1 : Level.DeliveryGoal;
                return (long)Delivered+ActivePassengerCount<goal;
            }
        }
        Passenger revivalPassenger;
        int revivalFloor;
        public bool CanContinueAfterAd { get { return State==PlayState.GameOver && revivalPassenger!=null; } }
        public bool ContinueAfterAd()
        {
            if(!CanContinueAfterAd) return false;
            bool present=false; foreach(var passenger in Queues.Floors[revivalFloor]) if(ReferenceEquals(passenger,revivalPassenger)) { present=true; break; }
            if(!present) return false;
            revivalPassenger.Remaining=PassengerPatience*.30f;
            revivalPassenger=null; Lost=Math.Max(0,Rules.LossLimit-1);
            State=PlayState.Playing; return true;
        }
        public int Spawned { get { return spawner.Spawned; } }
        public int ActivePassengerCount
        {
            get
            {
                int count=Lift.Riders.Count;
                foreach(var queue in Queues.Floors) count+=queue.Count;
                return count;
            }
        }
        public int QueueLimit { get { return Level==null ? GameRules.MaxWaitingPerFloor : Level.QueueLimit; } }
        public float PassengerPatience { get { return Level==null ? Rules.PatienceSeconds : Level.PatienceSeconds; } }
        public bool IsGuiding { get { return Level!=null && Level.Guided && Delivered==0; } }
        public bool Finished { get { return State==PlayState.GameOver || State==PlayState.Won; } }
        public PlayState State { get; private set; }
        public int Score { get; private set; }
        public int Delivered { get; private set; }
        public int Lost { get; private set; }
        public int Streak { get; private set; }
        public int Multiplier { get { return Math.Min(3, 1 + Streak / 5); } }
        public float Elapsed { get; private set; }
        public DifficultyState Difficulty { get { return Level==null ? DifficultyCurve.AtScore(Rules,Score) : Campaign.Difficulty(Level,LevelNumber,Delivered); } }
        public event Action<GameFeedback> Feedback;
        public event Action<int> Arrived;
        public GameSession(GameRules rules, int seed, LevelDefinition level=null, int levelNumber=1)
        {
            Rules = rules; rules.Validate();
            Level=level==null ? null : level.Copy(); if(Level!=null) Level.Validate(rules.Floors);
            LevelNumber=Math.Max(1,Math.Min(Campaign.LevelCount,levelNumber)); Lift = new Elevator(rules,true);
            Queues = new PassengerQueues(rules.Floors); spawner = new PassengerSpawner(seed);
        }
        public void SelectFloor(int floor)
        {
            if(State!=PlayState.Playing) return;
            if(floor!=RoofFloor && Level!=null && (floor<0 || floor>=Level.ActiveFloors)) return;
            Lift.Select(floor);
        }
        public void Pause() { if (State == PlayState.Playing) State = PlayState.Paused; }
        public void Resume() { if (State == PlayState.Paused) State = PlayState.Playing; }
        public void Tick(float dt)
        {
            if (State != PlayState.Playing || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            // Fixed small steps make long frames obey the same queue/arrival ordering.
            dt = Math.Min(dt, .25f);
            while (dt > .00001f && State == PlayState.Playing)
            {
                float step = Math.Min(dt, .02f); dt -= step; Elapsed += step;
                if(!IsGuiding) Queues.Age(step, LosePassenger, () => Finished);
                if (State == PlayState.GameOver) break;
                spawner.Tick(step, this);
                bool wasMoving=Lift.Moving;
                if(Lift.Tick(step))
                {
                    if(wasMoving && Arrived!=null)Arrived(Lift.CurrentFloor);
                    ServiceFloor();
                }
            }
        }
        void LosePassenger(int floor,Passenger passenger)
        {
            Lost++; Streak = 0; Emit(FeedbackKind.Lost);
            if (Lost >= Rules.LossLimit)
            {
                revivalFloor=floor; revivalPassenger=passenger;
                State = PlayState.GameOver;
            }
        }
        void ServiceFloor()
        {
            int delivered = 0, boarded = 0;
            for (int i = Lift.Riders.Count - 1; i >= 0; i--)
                if (Lift.Riders[i].Destination == Lift.CurrentFloor)
                {
                    if(Lift.Riders[i].IsFinalGuest && Lift.CurrentFloor==RoofFloor)FinalGuestDelivered=true;
                    Lift.Riders.RemoveAt(i);
                    Score += Rules.DeliveryPoints * Multiplier;
                    Streak++; Delivered++; delivered++;
                }
            if(Level!=null && Delivered>=Level.DeliveryGoal)
            {
                State=PlayState.Won; Emit(FeedbackKind.Won); return;
            }
            // The roof is an extra destination, never a residential spawn queue.
            if(Lift.CurrentFloor==RoofFloor)
            { Emit(delivered>0 ? FeedbackKind.Delivered : FeedbackKind.Floor,delivered>0 ? delivered : RoofFloor+1);return; }
            var queue = Queues.Floors[Lift.CurrentFloor];
            while (queue.Count>0 && Lift.OccupiedSpaces+queue[0].SpaceRequired<=Rules.Capacity)
            { Lift.Riders.Add(Queues.TakeFirst(Lift.CurrentFloor)); boarded++; }
            if (delivered > 0) Emit(FeedbackKind.Delivered,delivered);
            else if (boarded > 0) Emit(FeedbackKind.Boarded,boarded);
            else Emit(FeedbackKind.Floor,Lift.CurrentFloor+1);
            if(queue.Count>0 && queue[0].HasBox && Rules.Capacity-Lift.OccupiedSpaces==1)
                Emit(FeedbackKind.BoxBlocked);
        }
        void Emit(FeedbackKind kind,int count=0) { if(Feedback!=null) Feedback(new GameFeedback(kind,count,Multiplier)); }
    }
}
