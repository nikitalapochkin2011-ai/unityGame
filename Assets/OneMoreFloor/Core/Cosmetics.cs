using System;
using System.Globalization;

namespace OneMoreFloor
{
    public enum CosmeticKind { Background, Floor, Lift }
    public enum CosmeticRarity { Common, Rare, Epic, Legendary }
    public sealed class SpinReward
    {
        public readonly int CosmeticId, CoinAmount;
        public bool IsCoins { get { return CosmeticId<0; } }
        public SpinReward(int cosmeticId,int coinAmount) { CosmeticId=cosmeticId; CoinAmount=coinAmount; }
    }
    public static class FortuneWheelLayout
    {
        public const int Sectors=12;
        // Even slots show a coin; odd slots show the same mystery decoration symbol.
        public static int KindAt(int sector) { return sector%2==0 ? -1 : 0; }
        public static int SectorFor(SpinReward reward,Random random)
        { return 2*random.Next(Sectors/2)+(reward.IsCoins ? 0 : 1); }
        public static float AngleForSector(int sector) { return sector*(360f/Sectors); }
        public static int SectorUnderPointer(float rotation)
        {
            double angle=(rotation%360+360)%360;
            return (int)Math.Floor(angle/(360.0/Sectors)+.5)%Sectors;
        }
        public static float RotationAt(float start,int sector,float progress)
        {
            float offset=(AngleForSector(sector)-start)%360;
            if(offset<0) offset+=360;
            float p=Math.Max(0,Math.Min(1,progress));
            return start+(6*360+offset)*(1-(1-p)*(1-p)*(1-p));
        }
    }
    // Independent of GameSession: this RNG and currency can never change routes or balance.
    public sealed class Cosmetics
    {
        public const int PerKind=25, Total=3*PerKind, SpinCost=50;
        public const long Cooldown=86400;
        readonly bool[] owned=new bool[Total];
        readonly int[] selected=new int[3];
        public Cosmetics() { for(int k=0;k<3;k++) owned[k*PerKind]=true; }
        public int Coins { get; private set; }
        public long NextFree { get; private set; }
        public bool Complete { get { foreach(bool item in owned) if(!item) return false; return true; } }
        public static CosmeticRarity Rarity(int index)
        { return index<10 ? CosmeticRarity.Common : index<15 ? CosmeticRarity.Rare : index<20 ? CosmeticRarity.Epic : CosmeticRarity.Legendary; }
        public bool Owns(CosmeticKind kind,int index) { return Valid(kind,index) && owned[(int)kind*PerKind+index]; }
        static bool Valid(CosmeticKind kind,int index) { return (int)kind>=0 && (int)kind<3 && index>=0 && index<PerKind; }
        static readonly int[] TierWeights={50,25,15,10};
        public int AvailableRarityWeight(CosmeticRarity rarity)
        {
            int tier=(int)rarity; if(tier<0 || tier>=TierWeights.Length) return 0;
            for(int id=0;id<Total;id++) if(!owned[id] && Rarity(id%PerKind)==rarity) return TierWeights[tier];
            return 0;
        }
        public double RarityChance(CosmeticRarity rarity)
        {
            int total=0; for(int r=0;r<4;r++) total+=AvailableRarityWeight((CosmeticRarity)r);
            return total==0 ? 0 : AvailableRarityWeight(rarity)*100.0/total;
        }
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
        public SpinReward SpinAfterAd(bool rewardEarned,long utcSeconds,Random random)
        { return rewardEarned ? Spin(true,utcSeconds,random) : null; }
        // Resolve and apply once, before presentation. Legacy ownership is migrated by category when loading.
        public SpinReward Spin(bool free,long utcSeconds,Random random)
        { return SpinInternal(free,utcSeconds,random,false); }
#if UNITY_EDITOR || OMF_TESTS
        // Editor-only session sandbox: no cost or cooldown; callers must not persist it.
        public SpinReward SpinPreview(long utcSeconds,Random random)
        { return SpinInternal(true,utcSeconds,random,true); }
#endif
        SpinReward SpinInternal(bool free,long utcSeconds,Random random,bool preview)
        {
            if(utcSeconds<0 || utcSeconds>253402214399L || (!preview && (Complete || (free ? FreeIn(utcSeconds)>0 : Coins<SpinCost)))) return null;
            SpinReward reward;
            if(Complete || random.Next(100)<50)
            {
                // Conditional coin distribution: 60% in 20..29, 40% in 30..50, inclusive.
                int amount=random.Next(100)<60 ? 20+random.Next(10) : 30+random.Next(21);
                reward=new SpinReward(-1,amount);
            }
            else
            {
                // Choose a rarity, then one missing item within it. Exhausted tiers
                // are removed and the remaining weights are renormalized: no duplicates.
                int totalWeight=0; var weights=new int[4];
                for(int r=0;r<4;r++) { weights[r]=AvailableRarityWeight((CosmeticRarity)r); totalWeight+=weights[r]; }
                int tierRoll=random.Next(totalWeight),tier=0;
                while(tierRoll>=weights[tier]) { tierRoll-=weights[tier]; tier++; }
                int remaining=0;
                for(int i=0;i<Total;i++) if(!owned[i] && (int)Rarity(i%PerKind)==tier) remaining++;
                int pick=random.Next(remaining),id=-1;
                for(int i=0;i<Total;i++) if(!owned[i] && (int)Rarity(i%PerKind)==tier && pick--==0) { id=i; break; }
                reward=new SpinReward(id,0);
            }
            if(!preview) { if(free) NextFree=utcSeconds+Cooldown; else Coins-=SpinCost; }
            if(reward.IsCoins) Coins=(int)Math.Min(int.MaxValue,(long)Coins+reward.CoinAmount);
            else owned[reward.CosmeticId]=true;
            return reward;
        }
        public string Save()
        {
            var masks=new string[3];
            for(int k=0;k<3;k++)
            {
                int mask=0; for(int i=0;i<PerKind;i++) if(owned[k*PerKind+i]) mask|=1<<i;
                masks[k]=mask.ToString(CultureInfo.InvariantCulture);
            }
            return string.Join("|","2",Coins.ToString(CultureInfo.InvariantCulture),string.Join(".",masks),selected[0],selected[1],selected[2],NextFree.ToString(CultureInfo.InvariantCulture));
        }
        public static Cosmetics Load(string value)
        {
            var result=new Cosmetics();
            var p=(value ?? "").Split('|');
            if(p.Length!=7 || (p[0]!="1" && p[0]!="2") || !int.TryParse(p[1],NumberStyles.None,CultureInfo.InvariantCulture,out var coins) || coins<0 ||
                !long.TryParse(p[6],NumberStyles.None,CultureInfo.InvariantCulture,out var next) || next<0 || next>253402300799L) return result;
            var masks=new int[3];
            if(p[0]=="1")
            {
                if(!int.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out var legacy) || legacy<0 || (legacy & ~((1<<30)-1))!=0) return result;
                for(int k=0;k<3;k++) masks[k]=(legacy>>(k*10))&1023;
            }
            else
            {
                var parts=p[2].Split('.'); if(parts.Length!=3) return result;
                for(int k=0;k<3;k++)
                    if(!int.TryParse(parts[k],NumberStyles.None,CultureInfo.InvariantCulture,out masks[k]) || masks[k]<0 || masks[k]>=(1<<PerKind)) return result;
            }
            result.Coins=coins; result.NextFree=next;
            for(int k=0;k<3;k++)
            {
                for(int i=0;i<PerKind;i++) result.owned[k*PerKind+i]=i==0 || (masks[k]&(1<<i))!=0;
                if(int.TryParse(p[3+k],out var index)) result.Select((CosmeticKind)k,index);
            }
            return result;
        }
    }
}
