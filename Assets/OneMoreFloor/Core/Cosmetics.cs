using System;
using System.Globalization;

namespace OneMoreFloor
{
    public enum CosmeticKind { Background, Floor, Lift }
    public sealed class SpinReward
    {
        public readonly int CosmeticId, CoinAmount;
        public bool IsCoins { get { return CosmeticId<0; } }
        public SpinReward(int cosmeticId,int coinAmount) { CosmeticId=cosmeticId; CoinAmount=coinAmount; }
    }
    public static class FortuneWheelLayout
    {
        public const int Sectors=12;
        public static int KindAt(int sector) { return sector%2==0 ? -1 : sector/2%3; }
        public static int SectorFor(SpinReward reward,Random random)
        { return reward.IsCoins ? 2*random.Next(6) : 2*(reward.CosmeticId/10+3*random.Next(2))+1; }
    }
    // Independent of GameSession: this RNG and currency can never change routes or balance.
    public sealed class Cosmetics
    {
        public const int PerKind=10, SpinCost=50;
        public const long Cooldown=86400;
        const int All=(1<<30)-1;
        const int Defaults=1 | (1<<10) | (1<<20);
        int owned=Defaults;
        readonly int[] selected=new int[3];
        public int Coins { get; private set; }
        public long NextFree { get; private set; }
        public bool Complete { get { return owned==All; } }
        public bool Owns(CosmeticKind kind,int index) { return Valid(kind,index) && (owned & (1<<((int)kind*10+index)))!=0; }
        static bool Valid(CosmeticKind kind,int index) { return (int)kind>=0 && (int)kind<3 && index>=0 && index<10; }
        public int Selected(CosmeticKind kind) { return selected[(int)kind]; }
        public bool Select(CosmeticKind kind,int index)
        { if(!Owns(kind,index)) return false; selected[(int)kind]=index; return true; }
        public long FreeIn(long utcSeconds) { return Math.Max(0,NextFree-Math.Max(0,utcSeconds)); }
        public int OnDelivered(int passengers,Random random)
        {
            int added=0;
            for(int i=0;i<passengers;i++) if(random.Next(100)<12 && Coins<int.MaxValue) { Coins++; added++; }
            return added;
        }
        // Resolve and apply once, before presentation. Existing save format remains compatible.
        public SpinReward Spin(bool free,long utcSeconds,Random random)
        {
            if(Complete || utcSeconds<0 || utcSeconds>253402214399L || (free ? FreeIn(utcSeconds)>0 : Coins<SpinCost)) return null;
            SpinReward reward;
            if(random.Next(100)<50)
            {
                // Conditional coin distribution: 60% in 20..29, 40% in 30..50, inclusive.
                int amount=random.Next(100)<60 ? 20+random.Next(10) : 30+random.Next(21);
                reward=new SpinReward(-1,amount);
            }
            else
            {
                int remaining=0;
                for(int i=0;i<30;i++) if((owned & (1<<i))==0) remaining++;
                int pick=random.Next(remaining),id=-1;
                for(int i=0;i<30;i++) if((owned & (1<<i))==0 && pick--==0) { id=i; break; }
                reward=new SpinReward(id,0);
            }
            if(free) NextFree=utcSeconds+Cooldown; else Coins-=SpinCost;
            if(reward.IsCoins) Coins=(int)Math.Min(int.MaxValue,(long)Coins+reward.CoinAmount);
            else owned|=1<<reward.CosmeticId;
            return reward;
        }
        public string Save()
        { return string.Join("|","1",Coins.ToString(CultureInfo.InvariantCulture),owned.ToString(CultureInfo.InvariantCulture),selected[0],selected[1],selected[2],NextFree.ToString(CultureInfo.InvariantCulture)); }
        public static Cosmetics Load(string value)
        {
            var result=new Cosmetics();
            var p=(value ?? "").Split('|');
            if(p.Length!=7 || p[0]!="1" || !int.TryParse(p[1],NumberStyles.None,CultureInfo.InvariantCulture,out var coins) || coins<0 ||
                !int.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out var mask) || mask<0 || (mask & ~All)!=0 ||
                !long.TryParse(p[6],NumberStyles.None,CultureInfo.InvariantCulture,out var next) || next<0 || next>253402300799L) return result;
            result.Coins=coins; result.owned=mask|Defaults; result.NextFree=next;
            for(int i=0;i<3;i++) if(int.TryParse(p[3+i],out var index)) result.Select((CosmeticKind)i,index);
            return result;
        }
    }
}
