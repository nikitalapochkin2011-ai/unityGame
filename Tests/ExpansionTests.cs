using System;
using System.Collections.Generic;
using OneMoreFloor;
static class ExpansionTests
{
    static int checks;
    static void Check(bool condition,string name) { if(!condition) throw new Exception(name); checks++; Console.WriteLine("PASS expansion: "+name); }
    static GameSession Game(LevelDefinition level=null)
    { return new GameSession(new GameRules {InitialSpawnSeconds=10000, MinimumSpawnSeconds=10000, PatienceSeconds=1000,StopSeconds=.1f},33,level); }
    static void Stop(GameSession g) { g.SelectFloor(0); g.Tick(.02f); }
    sealed class Sequence : Random
    {
        readonly Queue<int> values;
        public Sequence(params int[] values) { this.values=new Queue<int>(values); }
        public override int Next(int max) { var n=values.Dequeue(); if(n<0 || n>=max)throw new Exception("Unexpected rarity random range");return n; }
    }
    public static void Run()
    {
        for(int i=0;i<16;i++) Check(new Passenger(1,40,false,i).Appearance==i,"all sixteen human appearances survive model construction "+i);
        var raccoon=new Passenger(0,40,false,0,true);
        Check(raccoon.IsRaccoon && raccoon.Appearance==16 && raccoon.DeliveryUnits==1 && raccoon.SpaceRequired==1 && !raccoon.HasBox,"raccoon uses one slot, its own sprite and one normal delivery unit");
        var g=Game();g.Lift.Riders.Add(raccoon);Stop(g);
        Check(g.Score==10 && g.Delivered==1 && g.Streak==1 && g.Lift.Riders.Count==0,"raccoon delivery awards ordinary points, progress and streak");
        g.Tick(.2f);Check(g.Score==10 && g.Delivered==1,"delivered raccoon cannot be counted twice on later frames");
        var a=Game();var b=Game();
        for(int i=0;i<4;i++) { a.Lift.Riders.Add(new Passenger(0,40)); b.Lift.Riders.Add(new Passenger(0,40)); }
        Stop(a);Stop(b);a.Lift.Riders.Add(new Passenger(0,40,isRaccoon:true));b.Lift.Riders.Add(new Passenger(0,40));
        a.Tick(.25f);b.Tick(.25f);Stop(a);Stop(b);
        Check(a.Score==b.Score && a.Delivered==5 && a.Streak==b.Streak && a.Score==50,"raccoon and human are identical at a multiplier boundary");
        var level=Campaign.CreateLevels()[0].Copy(); level.DeliveryGoal=1;
        g=Game(level);g.Lift.Riders.Add(new Passenger(0,40,isRaccoon:true));Stop(g);
        Check(g.State==PlayState.Won && g.Delivered==1 && g.Score==10,"raccoon can satisfy the level goal on the winning stop");
        g=Game();g.Queues.TryEnqueue(1,new Passenger(0,.001f,isRaccoon:true));g.Tick(.02f);
        Check(g.Lost==1 && g.Delivered==0 && g.Score==0,"an expired raccoon is one lost passenger and awards no delivery");
        int total=0,special=0,dinosaurs=0; var looks=new HashSet<int>(); bool valid=true;
        for(int seed=0;seed<2400;seed++)
        {
            g=new GameSession(new GameRules {InitialSpawnSeconds=.2f,MinimumSpawnSeconds=.2f,PatienceSeconds=1000},seed);
            for(int frame=0;frame<80;frame++)
            {
                g.Tick(.1f);
                for(int f=0;f<g.Queues.Floors.Count;f++) while(g.Queues.Floors[f].Count>0)
                {
                    var p=g.Queues.TakeFirst(f);total++;
                    if(p.IsDinosaur){dinosaurs++;valid &= p.Appearance==17 && p.SpaceRequired==1 && !p.HasBox && !p.IsAlien;}
                    if(p.IsRaccoon) {special++;valid &= p.SpaceRequired==1 && !p.HasBox && p.Appearance==16;}
                    else if(!p.IsDinosaur)looks.Add(p.Appearance);
                }
            }
        }
        Check(total>70000 && Math.Abs(special/(double)total-.04)<.003 && looks.Count==16 && valid,"real endless spawner produces approximately 4% raccoons and all sixteen humans: "+special+"/"+total);
        Check(Math.Abs(dinosaurs/(double)total-.005)<.001,"real spawning samples approximately 0.5% dinosaurs: "+dinosaurs+"/"+total);
        for(int bit=0;bit<30;bit++)
        {
            int kind=bit/10,index=bit%10;
            var old=Cosmetics.Load("1|123|"+(1<<bit)+"|0|0|0|777");
            bool exact=true;
            for(int k=0;k<3;k++)for(int i=0;i<25;i++) exact &= old.Owns((CosmeticKind)k,i)==(i==0 || (k==kind && i==index));
            Check(exact && old.Coins==123 && old.NextFree==777 && Cosmetics.Load(old.Save()).Save()==old.Save(),"legacy bit migrates to the same category and index "+bit);
        }
        var migrated=Cosmetics.Load("1|55|1073741823|9|8|7|777");
        Check(!migrated.Complete && migrated.Selected(CosmeticKind.Background)==9 && migrated.Selected(CosmeticKind.Floor)==8 && migrated.Selected(CosmeticKind.Lift)==7,"complete legacy collection preserves selections and leaves new models locked");
        var all=Cosmetics.Load("2|99|33554431.33554431.33554431|24|24|24|777");
        Check(all.Complete && all.Selected(CosmeticKind.Lift)==24 && Cosmetics.Load(all.Save()).Save()==all.Save(),"highest new ownership bits and selections persist without integer wraparound");
        foreach(string bad in new[]{"2|99|33554432.0.0|0|0|0|777","2|99|1.1|0|0|0|777","2|99|-1.1.1|0|0|0|777","2|99|1.x.1|0|0|0|777"})
            Check(Cosmetics.Load(bad).Coins==0,"invalid new ownership snapshot defaults safely");
        int[] tiers=new int[4];for(int i=0;i<25;i++)tiers[(int)Cosmetics.Rarity(i)]++;
        Check(tiers[0]==10 && tiers[1]==5 && tiers[2]==5 && tiers[3]==5,"every category contains 10 common and five of each added rarity");
        for(int roll=0;roll<100;roll++)
        {
            var prize=new Cosmetics().Spin(true,100,new Sequence(99,roll,0));
            int expected=roll<50 ? 0 : roll<75 ? 1 : roll<90 ? 2 : 3;
            Check(!prize.IsCoins && (int)Cosmetics.Rarity(prize.CosmeticId%25)==expected,"exact rarity probability boundary "+roll);
        }
        var noCommon=Cosmetics.Load("2|0|1023.1023.1023|0|0|0|0");
        Check(noCommon.RarityChance(CosmeticRarity.Common)==0 && noCommon.RarityChance(CosmeticRarity.Rare)==50,"exhausted common rarity is removed and displayed odds renormalize");
        for(int roll=0;roll<50;roll++)
        {
            var state=Cosmetics.Load(noCommon.Save());var prize=state.Spin(true,100,new Sequence(99,roll,0));
            Check((int)Cosmetics.Rarity(prize.CosmeticId%25)==(roll<25 ? 1 : roll<40 ? 2 : 3),"remaining rarities keep their relative weights "+roll);
        }
        var last=Cosmetics.Load("2|0|33554431.33554431.16777215|0|0|0|0");
        Check(last.RarityChance(CosmeticRarity.Legendary)==100 && last.Spin(true,100,new Sequence(99,4,0)).CosmeticId==74 && last.Complete,"last missing legendary is reachable with no duplicate or empty-tier roll");
        Console.WriteLine(checks+" expansion checks passed.");
    }
}
