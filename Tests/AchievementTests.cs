using System;
using System.Linq;
using OneMoreFloor;
static class AchievementTests
{
    static int passed;
    static void Check(bool value,string label){if(!value)throw new Exception(label);passed++;Console.WriteLine("PASS achievements: "+label);}
    public static void Run()
    {
        var a=new Achievements();var wallet=new Cosmetics();int[] goals={50,100,200,500,1000,2000};
        Check(a.TotalDelivered==0 && a.PendingCount==0 && a.EarnedCount==0,"new profile begins without fabricated lifetime deliveries");
        Check(a.RecordDeliveries(0,10)==0 && a.RecordDeliveries(-9,20)==0 && a.TotalDelivered==0 && a.EarnedCount==0,"empty and negative batches cannot earn a roof or delivery achievement");
        for(int id=0;id<6;id++)
        {
            var d=Achievements.Definition(id);Check(d.Target==goals[id] && d.Reward==5*(id+1) && !d.IsRooftop,"stable delivery target and reward: "+id);
            a.RecordDeliveries(d.Target-a.TotalDelivered-1);
            Check(!a.Earned(id) && a.Claim(id,wallet)==0,"no early reward below threshold: "+d.Target);
            Check(a.RecordDeliveries(1)==1<<id && a.Earned(id) && a.PendingCount==1,"exact threshold emits one new achievement: "+d.Target);
            int before=wallet.Coins;Check(a.Claim(id,wallet)==d.Reward && wallet.Coins==before+d.Reward && a.Claimed(id) && a.PendingCount==0,"claim transfers the exact reward once: "+id);
            Check(a.Claim(id,wallet)==0 && wallet.Coins==before+d.Reward,"double click cannot duplicate coins: "+id);
            a=Achievements.Load(a.Save());Check(a.Claimed(id) && a.TotalDelivered==d.Target && a.Claim(id,wallet)==0,"reload preserves total and consumed reward: "+id);
        }
        Check(wallet.Coins==105,"six delivery achievements pay 105 coins total");
        for(int n=10;n<=100;n+=10)
        {
            int id=6+n/10-1;var d=Achievements.Definition(id);
            Check(d.FinaleLevel==n && d.Reward==5 && d.IsRooftop,"unique roof definition: "+n);
            Check(a.RecordDeliveries(1,n)==1<<id && a.Earned(id) && a.Progress(id)==1,"roof visit earns its matching achievement: "+n);
            Check(a.RecordDeliveries(1,n)==0 && a.Claim(id,wallet)==5 && a.Claim(id,wallet)==0,"replaying roof finale cannot repeat its reward: "+n);
        }
        Check(a.EarnedCount==16 && a.PendingCount==0 && wallet.Coins==155,"all sixteen achievements have total reward 155 coins");
        a=new Achievements();Check(a.RecordDeliveries(2000)==63 && a.PendingCount==6,"one batch can cross multiple delivery thresholds");
        Check(a.RecordDeliveries(1,11)==0 && a.RecordDeliveries(1,110)==0 && a.EarnedCount==6,"ordinary and out-of-range levels cannot award a roof achievement");
        Check(a.RecordDeliveries(int.MaxValue)==0 && a.TotalDelivered==int.MaxValue && a.RecordDeliveries(100)==0 && a.TotalDelivered==int.MaxValue,"lifetime counter saturates without overflow or repeated unlocks");
        foreach(string malformed in new[]{"","2|50|0|0","1|-1|0|0","1|49|0|1","1|0|1024|0","1|0|1|0","1|0|0|65536","1|0|0|-1","1|abc|0|0","1|50|0|999999999999"})
            Check(Achievements.Load(malformed).TotalDelivered==0 && Achievements.Load(malformed).PendingCount==0,"invalid snapshot is rejected: "+malformed);
        var maxWallet=Cosmetics.Load("2|2147483647|1.1.1|0|0|0|0");a=new Achievements();a.RecordDeliveries(50);
        Check(a.Claim(0,maxWallet)==0 && !a.Claimed(0) && a.PendingCount==1,"a saturated wallet retains the full claim instead of silently losing coins");
        UnityEngine.PlayerPrefs.ClearForTests();const string legacy="2|137|3.5.9|1|2|3|900000";
        UnityEngine.PlayerPrefs.SetString("OneMoreFloor.Cosmetics.v1",legacy);UnityEngine.PlayerPrefs.SetString("OneMoreFloor.Campaign.Completed","50");
        var profile=CosmeticStore.LoadAccount();
        Check(profile.Wallet.Save()==legacy && profile.Milestones.TotalDelivered==0,"old currency, ownership, selections and ad deadline migrate without inventing past statistics");
        profile.Milestones.RecordDeliveries(50);profile.Milestones.Claim(0,profile.Wallet);CosmeticStore.SaveAccount(profile.Wallet,profile.Milestones);
        profile=CosmeticStore.LoadAccount();
        Check(profile.Wallet.Coins==142 && profile.Milestones.Claimed(0) && profile.Milestones.Claim(0,profile.Wallet)==0,"coin payment and consumed achievement restore from the same atomic snapshot");
        string oldMilestones=profile.Milestones.Save();profile.Wallet.Spin(false,1000000,new Random(31));CosmeticStore.Save(profile.Wallet);
        var reloaded=CosmeticStore.LoadAccount();
        Check(reloaded.Wallet.Save()==profile.Wallet.Save() && reloaded.Milestones.Save()==oldMilestones,"wheel spends and coin rewards retain the achievement ledger");
        Check(UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Campaign.Completed","")=="50" && UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Cosmetics.v1","")==legacy,"migration leaves campaign and old save untouched");
        Console.WriteLine(passed+" achievement checks passed.");
    }
}
