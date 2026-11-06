using System;
using System.Globalization;

namespace OneMoreFloor
{
    public sealed class AchievementDefinition
    {
        public readonly int Id,Target,Reward,FinaleLevel;
        public bool IsRooftop { get { return FinaleLevel>0; } }
        internal AchievementDefinition(int id,int target,int reward,int finaleLevel=0)
        {Id=id;Target=target;Reward=reward;FinaleLevel=finaleLevel;}
    }
    // Stable IDs and a storage-independent lifetime counter. Replays count, lost passengers do not.
    public sealed class Achievements
    {
        public const int DeliveryCount=6,Count=16;
        static readonly AchievementDefinition[] definitions=CreateDefinitions();
        int roofMask,claimedMask;
        public int TotalDelivered { get; private set; }
        static AchievementDefinition[] CreateDefinitions()
        {
            int[] targets={50,100,200,500,1000,2000};var result=new AchievementDefinition[Count];
            for(int i=0;i<DeliveryCount;i++)result[i]=new AchievementDefinition(i,targets[i],5*(i+1));
            for(int i=DeliveryCount;i<Count;i++)result[i]=new AchievementDefinition(i,1,5,(i-DeliveryCount+1)*10);
            return result;
        }
        public static AchievementDefinition Definition(int id)
        {if(id<0 || id>=Count)throw new ArgumentOutOfRangeException(nameof(id));return definitions[id];}
        public bool Earned(int id)
        {var d=Definition(id);return d.IsRooftop ? (roofMask&(1<<(id-DeliveryCount)))!=0 : TotalDelivered>=d.Target;}
        public bool Claimed(int id) {Definition(id);return (claimedMask&(1<<id))!=0;}
        public int Progress(int id) {var d=Definition(id);return d.IsRooftop ? (Earned(id) ? 1 : 0) : Math.Min(TotalDelivered,d.Target);}
        public int EarnedCount {get{int n=0;for(int i=0;i<Count;i++)if(Earned(i))n++;return n;}}
        public int PendingCount {get{int n=0;for(int i=0;i<Count;i++)if(Earned(i) && !Claimed(i))n++;return n;}}
        int EarnedMask {get{int mask=0;for(int i=0;i<Count;i++)if(Earned(i))mask|=1<<i;return mask;}}
        // Return only newly crossed milestones, including multiple thresholds in one stop.
        public int RecordDeliveries(int count,int rooftopLevel=0)
        {
            if(count<=0)return 0;
            int before=EarnedMask;TotalDelivered=(int)Math.Min(int.MaxValue,(long)TotalDelivered+count);
            var finale=RooftopFinales.ForLevel(rooftopLevel);
            if(finale!=null)roofMask|=1<<(finale.LevelNumber/10-1);
            return EarnedMask&~before;
        }
        public int Claim(int id,Cosmetics wallet)
        {
            if(wallet==null)throw new ArgumentNullException(nameof(wallet));
            var d=Definition(id);if(!Earned(id) || Claimed(id))return 0;
            // Leave the reward available if a saturated wallet cannot accept it completely.
            if((long)wallet.Coins+d.Reward>int.MaxValue)return 0;
            wallet.AddCoins(d.Reward);claimedMask|=1<<id;return d.Reward;
        }
        public string Save()
        {return string.Join("|","1",TotalDelivered.ToString(CultureInfo.InvariantCulture),roofMask.ToString(CultureInfo.InvariantCulture),claimedMask.ToString(CultureInfo.InvariantCulture));}
        public static Achievements Load(string value)
        {return TryLoad(value,out var result) ? result : new Achievements();}
        public static bool TryLoad(string value,out Achievements result)
        {
            result=new Achievements();var p=(value ?? "").Split('|');
            if(p.Length!=4 || p[0]!="1" || !int.TryParse(p[1],NumberStyles.None,CultureInfo.InvariantCulture,out var delivered) || delivered<0 ||
                !int.TryParse(p[2],NumberStyles.None,CultureInfo.InvariantCulture,out var roofs) || roofs<0 || roofs>=1<<10 ||
                !int.TryParse(p[3],NumberStyles.None,CultureInfo.InvariantCulture,out var claimed) || claimed<0 || claimed>=1<<Count)return false;
            int roofCount=0;for(int bits=roofs;bits>0;bits>>=1)roofCount+=bits&1;
            if(delivered<roofCount)return false;
            var parsed=new Achievements{TotalDelivered=delivered,roofMask=roofs};
            if((claimed&~parsed.EarnedMask)!=0)return false;
            parsed.claimedMask=claimed;result=parsed;return true;
        }
    }
    // Currency and claimed rewards live in ONE PlayerPrefs value, preventing a partial claim save.
    public sealed class PlayerAccount
    {
        public readonly Cosmetics Wallet;
        public readonly Achievements Milestones;
        public PlayerAccount(Cosmetics wallet,Achievements milestones){Wallet=wallet;Milestones=milestones;}
        public string Save(){return "1\n"+Wallet.Save()+"\n"+Milestones.Save();}
        public static PlayerAccount Load(string snapshot,string legacyWallet)
        {
            var p=(snapshot ?? "").Split('\n');
            if(p.Length==3 && p[0]=="1" && Achievements.TryLoad(p[2],out var milestones))
            {
                var wallet=Cosmetics.Load(p[1]);
                if(wallet.Save()==p[1])return new PlayerAccount(wallet,milestones);
            }
            return new PlayerAccount(Cosmetics.Load(legacyWallet),new Achievements());
        }
    }
}
