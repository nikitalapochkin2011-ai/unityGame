using System;

namespace OneMoreFloor
{
    // SDK callbacks can repeat or arrive out of order. One request can settle only once;
    // closing a video without its earned callback must never award anything.
    public sealed class RewardedRequest
    {
        readonly Action<bool> completed;
        bool earned, finished;
        public bool Finished { get { return finished; } }
        public RewardedRequest(Action<bool> completed) { this.completed=completed; }
        public void Earn() { if(!finished) earned=true; }
        public void Close() { Finish(earned); }
        public void Fail() { Finish(earned); }
        void Finish(bool reward)
        {
            if(finished) return;
            finished=true; completed?.Invoke(reward);
        }
    }
}
