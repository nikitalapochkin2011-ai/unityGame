using System;
using System.Collections.Generic;
using OneMoreFloor;
static class CosmeticsTests
{
    static int passed;
    static void Check(bool value,string name) { if(!value) throw new Exception(name); passed++; Console.WriteLine("PASS cosmetics: "+name); }
    sealed class Roll : Random { readonly int value; public Roll(int value) { this.value=value; } public override int Next(int max) { return value%max; } }
    public static void Run()
    {
        var c=new Cosmetics(); var rng=new Random(1987);
        for(int k=0;k<3;k++) for(int i=0;i<10;i++) Check(c.Owns((CosmeticKind)k,i)==(i==0),"only original starts unlocked "+k+"/"+i);
        Check(c.Coins==0 && c.FreeIn(100)==0,"first free spin immediately available");
        Check(!c.Select(CosmeticKind.Floor,1) && !c.Select((CosmeticKind)9,0),"locked and invalid selections rejected");
        string before=c.Save();
        Check(c.Spin(false,100,rng)==-1 && c.Save()==before,"insufficient funds do not mutate state");
        int reward=c.Spin(true,100,rng);
        Check(reward>=0 && reward%10!=0 && c.FreeIn(100)==86400,"free awards a new style and starts 24h timer");
        before=c.Save();
        Check(c.Spin(true,86499,rng)==-1 && before==c.Save(),"free rejected one second before deadline");
        Check(c.FreeIn(50)==86450,"clock rollback does not grant a free spin");
        Check(c.Spin(true,86500,rng)>=0,"free available at exact deadline");
        Check(c.OnDelivered(1,new Roll(11))==1 && c.OnDelivered(1,new Roll(12))==0,"coin probability boundary is 12 percent");
        c.OnDelivered(49,new Roll(0)); Check(c.Coins==50,"coins accumulate once per delivered passenger");
        long timer=c.NextFree; reward=c.Spin(false,86501,rng);
        Check(reward>=0 && c.Coins==0 && c.NextFree==timer,"paid spin costs exactly 50 and preserves free deadline");
        Check(c.Select((CosmeticKind)(reward/10),reward%10),"new reward selectable");
        c.OnDelivered(7,new Roll(0));
        CosmeticStore.Save(c); var restored=CosmeticStore.Load();
        Check(restored.Save()==c.Save(),"actual PlayerPrefs adapter restores coins ownership selections and timer");
        Check(UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Campaign.Completed","")=="5","cosmetics preserve campaign preferences");
        c=new Cosmetics(); var seen=new HashSet<int>();
        for(int i=0;i<27;i++)
        {
            int id=c.Spin(true,100+i*86400L,rng);
            Check(id>=0 && seen.Add(id),"unique reward "+i);
            c=Cosmetics.Load(c.Save());
        }
        Check(c.Complete && seen.Count==27,"all 27 rewards fill 30-item collection including defaults");
        c.OnDelivered(100,new Roll(0)); before=c.Save();
        Check(c.Spin(false,99999999,rng)==-1 && c.Spin(true,99999999,rng)==-1 && before==c.Save(),"complete collection never charges coins or free timer");
        foreach(string broken in new[]{"", "bad", "1|-1|1|0|0|0|0", "1|5|2147483647|0|0|0|0", "1|5|1|0|0|0|-1"})
            Check(Cosmetics.Load(broken).Coins==0,"invalid snapshot safely defaults: "+broken);
        var valid=Cosmetics.Load("1|2147483647|0|9|99|-1|86400");
        Check(valid.Selected(CosmeticKind.Background)==0 && valid.Owns(CosmeticKind.Lift,0),"restore repairs locked selections and ensures original styles");
        Check(valid.OnDelivered(10,new Roll(0))==0 && valid.Coins==int.MaxValue,"currency cannot overflow");
        Check(new Cosmetics().Spin(true,-1,rng)==-1,"invalid time rejected");
        var a=new Cosmetics(); var b=new Cosmetics(); var ra=new Random(77); var rb=new Random(77);
        for(int i=0;i<30;i++) a.OnDelivered(4,ra);
        b.OnDelivered(120,rb); Check(a.Coins==b.Coins,"batched delivery events count each passenger exactly once");
        Console.WriteLine(passed+" cosmetics checks passed.");
    }
}
