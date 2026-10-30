using System;

namespace OneMoreFloor
{
    // Shared, testable floor plan. Seven fixed slots never depend on queue length.
    public static class BuildingLayout
    {
        public const float HallLeft=.075f, HallRight=.704f;
        public const float WaitingLeft=.094f, WaitingRight=.694f;
        public const float PersonWidth=.075f, PersonHeight=.79f;
        public const float LiftLeft=.716f, LiftRight=.996f;
        public const float DoorLeft=.744f, DoorRight=.969f;
        public const float RidersLeft=.749f, RidersWidth=.217f;
        public static float WaitingX(int index)
        {
            if(index<0 || index>=GameRules.MaxWaitingPerFloor) throw new ArgumentOutOfRangeException(nameof(index));
            return WaitingRight-(WaitingRight-WaitingLeft)*(index+.5f)/GameRules.MaxWaitingPerFloor;
        }
        public static float RiderX(int firstSlot,int spaces,int capacity)
        {
            if(firstSlot<0 || spaces<1 || capacity<1 || firstSlot+spaces>capacity)
                throw new ArgumentOutOfRangeException(nameof(firstSlot));
            return RidersLeft+RidersWidth/capacity*(firstSlot+spaces*.5f);
        }
        public static float RiderWidth(int spaces,int capacity)
        {
            if(spaces<1 || capacity<1 || spaces>capacity) throw new ArgumentOutOfRangeException(nameof(spaces));
            return RidersWidth/capacity*.98f*spaces;
        }
    }
}
