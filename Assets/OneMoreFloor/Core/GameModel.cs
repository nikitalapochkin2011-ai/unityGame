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
        public int Destination { get; private set; }
        public float Remaining { get; internal set; }
        public Passenger(int destination, float patience) { Destination = destination; Remaining = patience; }
    }
    public sealed class Elevator
    {
        readonly GameRules rules;
        public readonly List<Passenger> Riders = new List<Passenger>();
        public int CurrentFloor { get; private set; }
        public int TargetFloor { get; private set; }
        public int? NextFloor { get; private set; }
        public float Position { get; private set; }
        public bool Moving { get; private set; }
        float stopRemaining;
        public Elevator(GameRules rules) { this.rules = rules; }
        public void Select(int floor)
        {
            if (floor < 0 || floor >= rules.Floors) return;
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
        internal void Age(float dt, Action lost, Func<bool> ended)
        {
            foreach (var queue in queues)
                for (int i = 0; i < queue.Count;)
                {
                    queue[i].Remaining -= dt;
                    if (queue[i].Remaining > 0) { i++; continue; }
                    queue.RemoveAt(i); lost(); if (ended()) return;
                }
        }
    }
    public sealed class PassengerSpawner
    {
        readonly Random random;
        float countdown = 1.5f;
        public PassengerSpawner(int seed) { random = new Random(seed); }
        internal void Tick(float dt, GameSession game)
        {
            var difficulty = game.Difficulty;
            countdown = Math.Min(countdown, difficulty.SpawnInterval) - dt;
            if (countdown > 0) return;
            // Draw uniformly from floors with a free waiting slot. A full building
            // skips this scheduled arrival without creating an invisible passenger/loss.
            int available = 0;
            for (int f = 0; f < difficulty.ActiveFloors; f++)
                if (game.Queues.Floors[f].Count < GameRules.MaxWaitingPerFloor) available++;
            countdown += difficulty.SpawnInterval;
            if (available == 0) return;
            int choice = random.Next(available), floor = 0;
            for (int f = 0; f < difficulty.ActiveFloors; f++)
            {
                if (game.Queues.Floors[f].Count >= GameRules.MaxWaitingPerFloor) continue;
                if (choice-- == 0) { floor = f; break; }
            }
            int min = Math.Max(0, floor - difficulty.MaximumRouteDistance);
            int max = Math.Min(difficulty.ActiveFloors - 1, floor + difficulty.MaximumRouteDistance);
            // Select uniformly from the reachable floors, excluding the origin, without retries.
            int destination = random.Next(min, max);
            if (destination >= floor) destination++;
            game.Queues.TryEnqueue(floor, new Passenger(destination, game.Rules.PatienceSeconds));
        }
    }
    public enum PlayState { Playing, Paused, GameOver }
    public sealed class GameSession
    {
        public readonly GameRules Rules;
        public readonly Elevator Lift;
        public readonly PassengerQueues Queues;
        readonly PassengerSpawner spawner;
        public PlayState State { get; private set; }
        public int Score { get; private set; }
        public int Delivered { get; private set; }
        public int Lost { get; private set; }
        public int Streak { get; private set; }
        public int Multiplier { get { return Math.Min(3, 1 + Streak / 5); } }
        public float Elapsed { get; private set; }
        public DifficultyState Difficulty { get { return DifficultyCurve.AtScore(Rules, Score); } }
        public event Action<string> Feedback;
        public GameSession(GameRules rules, int seed)
        {
            Rules = rules; rules.Validate(); Lift = new Elevator(rules);
            Queues = new PassengerQueues(rules.Floors); spawner = new PassengerSpawner(seed);
        }
        public void SelectFloor(int floor) { if (State == PlayState.Playing) Lift.Select(floor); }
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
                Queues.Age(step, LosePassenger, () => State == PlayState.GameOver);
                if (State == PlayState.GameOver) break;
                spawner.Tick(step, this);
                if (Lift.Tick(step)) ServiceFloor();
            }
        }
        void LosePassenger()
        {
            Lost++; Streak = 0; Emit("Пассажир ушёл!");
            if (Lost >= Rules.LossLimit) State = PlayState.GameOver;
        }
        void ServiceFloor()
        {
            int delivered = 0, boarded = 0;
            for (int i = Lift.Riders.Count - 1; i >= 0; i--)
                if (Lift.Riders[i].Destination == Lift.CurrentFloor)
                {
                    Lift.Riders.RemoveAt(i); Score += Rules.DeliveryPoints * Multiplier;
                    Streak++; Delivered++; delivered++;
                }
            var queue = Queues.Floors[Lift.CurrentFloor];
            while (queue.Count > 0 && Lift.Riders.Count < Rules.Capacity)
            { Lift.Riders.Add(Queues.TakeFirst(Lift.CurrentFloor)); boarded++; }
            if (delivered > 0) Emit("Доставлено: " + delivered + "  ×" + Multiplier);
            else if (boarded > 0) Emit("Вошли: " + boarded);
            else Emit("Этаж " + (Lift.CurrentFloor + 1));
        }
        void Emit(string message) { if (Feedback != null) Feedback(message); }
    }
}
