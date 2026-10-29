using System;

namespace OneMoreFloor
{
    public readonly struct DifficultyState
    {
        public readonly int Level, ActiveFloors, MaximumRouteDistance;
        public readonly float SpawnInterval;
        public DifficultyState(int level, int floors, int distance, float interval)
        { Level = level; ActiveFloors = floors; MaximumRouteDistance = distance; SpawnInterval = interval; }
    }

    // Only earned score advances the curve: waiting, pausing and losing never make it harder.
    public static class DifficultyCurve
    {
        public static DifficultyState AtScore(GameRules rules, int score)
        {
            double progress = Math.Max(0, Math.Min(1, (double)score / rules.DifficultyScoreCap));
            int floors = rules.StartingFloors + (int)Math.Floor((rules.Floors - rules.StartingFloors) * progress);
            int distance = rules.StartingRouteDistance + (int)Math.Floor((rules.Floors - 1 - rules.StartingRouteDistance) * progress);
            distance = Math.Min(floors - 1, distance);
            float interval = (float)(rules.InitialSpawnSeconds + (rules.MinimumSpawnSeconds - rules.InitialSpawnSeconds) * progress);
            return new DifficultyState(1 + (int)Math.Floor(progress * 5), floors, distance, interval);
        }
    }
}
