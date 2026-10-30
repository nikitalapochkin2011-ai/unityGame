using System;
using System.Collections.Generic;
using OneMoreFloor;
static class CosmeticsTests
{
    static int passed;
    static void Check(bool value,string name) { if(!value) throw new Exception(name); passed++; Console.WriteLine("PASS cosmetics: "+name); }
    sealed class Roll : Random { readonly int value; public Roll(int value) { this.value=value; } public override int Next(int max) { return value%max; } }
    sealed class DecorationRoll : Random
    {
        readonly Random random=new Random(1987);
        public override int Next(int max) { return max==100 ? 99 : random.Next(max); }
    }
    sealed class Sequence : Random
    {
        readonly Queue<int> values;
        public Sequence(params int[] values) { this.values=new Queue<int>(values); }
        public override int Next(int max) { int n=values.Dequeue(); if(n<0 || n>=max) throw new Exception("Unexpected random range"); return n; }
    }
    public static void Run()
    {
        var c=new Cosmetics(); var rng=new DecorationRoll();
        for(int k=0;k<3;k++) for(int i=0;i<10;i++) Check(c.Owns((CosmeticKind)k,i)==(i==0),"only original starts unlocked "+k+"/"+i);
        Check(c.Coins==0 && c.FreeIn(100)==0,"first free spin immediately available");
        Check(!c.Select(CosmeticKind.Floor,1) && !c.Select((CosmeticKind)9,0),"locked and invalid selections rejected");
        string before=c.Save();
        Check(c.Spin(false,100,rng)==null && c.Save()==before,"insufficient funds do not mutate state");
        var reward=c.Spin(true,100,rng);
        Check(reward!=null && !reward.IsCoins && reward.CosmeticId%10!=0 && c.FreeIn(100)==86400,"free awards a new style and starts 24h timer");
        before=c.Save();
        Check(c.Spin(true,86499,rng)==null && before==c.Save(),"free rejected one second before deadline");
        Check(c.FreeIn(50)==86450,"clock rollback does not grant a free spin");
        Check(c.Spin(true,86500,rng)!=null,"free available at exact deadline");
        Check(c.OnDelivered(1,new Roll(11))==1 && c.OnDelivered(1,new Roll(12))==0,"coin probability boundary is 12 percent");
        c.OnDelivered(49,new Roll(0)); Check(c.Coins==50,"coins accumulate once per delivered passenger");
        long timer=c.NextFree; reward=c.Spin(false,86501,rng);
        Check(reward!=null && c.Coins==0 && c.NextFree==timer,"paid spin costs exactly 50 and preserves free deadline");
        Check(c.Select((CosmeticKind)(reward.CosmeticId/10),reward.CosmeticId%10),"new reward selectable");
        c.OnDelivered(7,new Roll(0));
        CosmeticStore.Save(c); var restored=CosmeticStore.Load();
        Check(restored.Save()==c.Save(),"actual PlayerPrefs adapter restores coins ownership selections and timer");
        Check(UnityEngine.PlayerPrefs.GetString("OneMoreFloor.Campaign.Completed","")=="5","cosmetics preserve campaign preferences");
        c=new Cosmetics(); var seen=new HashSet<int>();
        for(int i=0;i<27;i++)
        {
            int id=c.Spin(true,100+i*86400L,rng).CosmeticId;
            Check(id>=0 && seen.Add(id),"unique reward "+i);
            c=Cosmetics.Load(c.Save());
        }
        Check(c.Complete && seen.Count==27,"all 27 rewards fill 30-item collection including defaults");
        c.OnDelivered(100,new Roll(0)); before=c.Save();
        Check(c.Spin(false,99999999,rng)==null && c.Spin(true,99999999,rng)==null && before==c.Save(),"complete collection never charges coins or free timer");
        foreach(string broken in new[]{"", "bad", "1|-1|1|0|0|0|0", "1|5|2147483647|0|0|0|0", "1|5|1|0|0|0|-1"})
            Check(Cosmetics.Load(broken).Coins==0,"invalid snapshot safely defaults: "+broken);
        var valid=Cosmetics.Load("1|2147483647|0|9|99|-1|86400");
        Check(valid.Selected(CosmeticKind.Background)==0 && valid.Owns(CosmeticKind.Lift,0),"restore repairs locked selections and ensures original styles");
        Check(valid.OnDelivered(10,new Roll(0))==0 && valid.Coins==int.MaxValue,"currency cannot overflow");
        Check(new Cosmetics().Spin(true,-1,rng)==null,"invalid time rejected");
        var a=new Cosmetics(); var b=new Cosmetics(); var ra=new Random(77); var rb=new Random(77);
        for(int i=0;i<30;i++) a.OnDelivered(4,ra);
        b.OnDelivered(120,rb); Check(a.Coins==b.Coins,"batched delivery events count each passenger exactly once");
        // Exhaust every outer and conditional probability boundary, then every payout value.
        for(int outer=0;outer<100;outer++)
        {
            var state=new Cosmetics();
            var award=state.Spin(true,100,new Sequence(outer,0,0));
            Check(award.IsCoins==(outer<50),"50 percent reward branch "+outer);
        }
        for(int inner=0;inner<100;inner++)
        {
            var award=new Cosmetics().Spin(true,100,new Sequence(0,inner,0));
            Check(award.CoinAmount==(inner<60 ? 20 : 30),"60/40 conditional coin branch "+inner);
        }
        for(int amount=20;amount<=50;amount++)
        {
            var state=new Cosmetics();
            var award=state.Spin(true,100,new Sequence(0,amount<30 ? 0 : 60,amount<30 ? amount-20 : amount-30));
            Check(award.CoinAmount==amount && state.Coins==amount && Cosmetics.Load(state.Save()).Coins==amount,"inclusive saved payout "+amount);
            for(int k=0;k<3;k++) Check(!state.Owns((CosmeticKind)k,1),"coin payout does not unlock styles");
        }
        var paid=Cosmetics.Load("1|50|1049601|0|0|0|1000");
        var prize=paid.Spin(false,100,new Sequence(0,60,20));
        Check(prize.CoinAmount==50 && paid.Coins==50 && paid.NextFree==1000,"paid coin award applies fee before payout and preserves timer");
        var capped=Cosmetics.Load("1|2147483640|1049601|0|0|0|0");
        capped.Spin(true,100,new Sequence(0,60,20));
        Check(capped.Coins==int.MaxValue,"coin prize saturates rather than overflows");
        var distribution=new int[3]; var statsRandom=new Random(423);
        for(int i=0;i<20000;i++)
        {
            var award=new Cosmetics().Spin(true,100,statsRandom);
            distribution[!award.IsCoins ? 0 : award.CoinAmount<30 ? 1 : 2]++;
        }
        Check(Math.Abs(distribution[0]/20000.0-.5)<.02 && Math.Abs(distribution[1]/20000.0-.3)<.02 && Math.Abs(distribution[2]/20000.0-.2)<.02,"seeded simulation matches 50/30/20 overall odds");
        var sectorCounts=new int[4];
        for(int i=0;i<FortuneWheelLayout.Sectors;i++) sectorCounts[FortuneWheelLayout.KindAt(i)+1]++;
        Check(sectorCounts[0]==6 && sectorCounts[1]==6 && sectorCounts[2]==0 && sectorCounts[3]==0,"disk has six coin and six mystery sectors");
        for(int id=-1;id<30;id++)
            for(int variant=0;variant<6;variant++)
            {
                var award=new SpinReward(id,id<0 ? 20 : 0);
                int sector=FortuneWheelLayout.SectorFor(award,new Sequence(variant));
                Check(sector>=0 && sector<12 && FortuneWheelLayout.KindAt(sector)==(id<0 ? -1 : 0),"pointer lands on actual prize category "+id+"/"+variant);
            }
        foreach(float start in new[]{0f,30f,150f,359.9f})
            for(int sector=0;sector<12;sector++)
            {
                Check(FortuneWheelLayout.RotationAt(start,sector,0)==start,"spin begins at the displayed angle");
                float previous=start; bool monotone=true;
                for(int step=1;step<=100;step++)
                { float next=FortuneWheelLayout.RotationAt(start,sector,step/100f); monotone &= next>=previous; previous=next; }
                Check(monotone && previous>=start+2160,"disk turns six full revolutions without reversing");
                Check(FortuneWheelLayout.SectorUnderPointer(previous)==sector,"fixed top pointer selects the actual stopping sector");
            }
        var sandbox=Cosmetics.Load("1|0|1049601|0|0|0|999999");
        string realSave=sandbox.Save();
        var preview=sandbox.SpinPreview(100,new DecorationRoll());
        Check(preview!=null && !preview.IsCoins && sandbox.Coins==0 && sandbox.NextFree==999999,"editor preview ignores timer and charges no coins");
        for(int i=0;i<26;i++) Check(sandbox.SpinPreview(100,new DecorationRoll())!=null,"editor preview remains repeatable at the same time");
        Check(sandbox.Complete,"preview can test entire collection");
        Check(sandbox.SpinPreview(100,new Sequence(0,0)).IsCoins,"preview continues with coins after full collection");
        Check(Cosmetics.Load(realSave).Coins==0 && !Cosmetics.Load(realSave).Complete && Cosmetics.Load(realSave).NextFree==999999,"preview leaves original snapshot unchanged");
        Check(Cosmetics.Load(realSave).Spin(true,100,new Random(5))==null,"normal spins still enforce original timer");
        Console.WriteLine(passed+" cosmetics checks passed.");
    }
}
