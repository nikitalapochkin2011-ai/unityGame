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
        public static float WaitingX(int index)
        {
            if(index<0 || index>=GameRules.MaxWaitingPerFloor) throw new ArgumentOutOfRangeException(nameof(index));
            return WaitingRight-(WaitingRight-WaitingLeft)*(index+.5f)/GameRules.MaxWaitingPerFloor;
        }
    }
}
