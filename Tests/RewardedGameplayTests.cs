using System;
using System.Collections.Generic;
using System.IO;
using OneMoreFloor;

static class RewardedGameplayTests
{
    static int passed;
    static void Check(bool value,string label) { if(!value)throw new Exception(label);passed++;Console.WriteLine("PASS rewarded: "+label); }
    sealed class Roll : Random { public override int Next(int max) { return max-1; } }
    static void Tick(GameSession g,float seconds)
    { for(int i=0;i<(int)Math.Ceiling(seconds/.02f);i++)g.Tick(.02f); }
    static void Drive(GameSession g)
    {
        if(g.Lift.Moving || g.Lift.NextFloor.HasValue) return;
        if(g.Lift.Riders.Count>0) { g.SelectFloor(g.Lift.Riders[0].Destination);return; }
        for(int f=0;f<g.Queues.Floors.Count;f++)if(g.Queues.Floors[f].Count>0) {g.SelectFloor(f);return;}
    }
    static GameSession Game(int goal=6)
    {
        return new GameSession(new GameRules {SecondsPerFloor=.1f,StopSeconds=.1f},17,
            new LevelDefinition {DeliveryGoal=goal,ActiveFloors=3,PatienceSeconds=100,MaxActivePassengers=5,SpawnSeconds=2,FinalSpawnSeconds=2});
    }
    public static void Run()
    {
        // Cover every event order, including duplicates and a late earned callback.
        foreach(string events in new[]{"C","F","CC","FF","EC","ECC","EEF","FEC","CEF","ECF","EFCC","ECEC"})
        {
            int calls=0;bool earned=false;
            var request=new RewardedRequest(value=>{calls++;earned=value;});
            foreach(char e in events) {if(e=='E')request.Earn();else if(e=='C')request.Close();else request.Fail();}
            Check(calls==1 && earned==events.StartsWith("E") && request.Finished,"single grant and verified view for callback order "+events);
        }
        var c=new Cosmetics();string saved=c.Save();
        Check(c.SpinAfterAd(false,100,new Roll())==null && c.Save()==saved,"cancelled/unavailable ad changes neither ownership nor 24-hour timer");
        var reward=c.SpinAfterAd(true,100,new Roll());
        Check(reward!=null && c.FreeIn(100)==86400 && c.Coins==0,"completed ad grants exactly one daily spin with no coin charge");
        saved=c.Save();Check(c.SpinAfterAd(true,101,new Roll())==null && c.Save()==saved,"repeated completion cannot grant a second spin during cooldown");
        c=Cosmetics.Load(saved);
        Check(c.SpinAfterAd(true,86499,new Roll())==null && c.SpinAfterAd(true,86500,new Roll())!=null,"ad cooldown survives reload and unlocks at the exact 24-hour boundary");
        var seen=new HashSet<int>();c=new Cosmetics();
        for(int i=0;i<72;i++)
        {
            var prize=c.SpinAfterAd(true,100+86400L*i,new Roll());
            Check(!prize.IsCoins && seen.Add(prize.CosmeticId),"daily cosmetic reward is unique across saved reloads "+i);
            c=Cosmetics.Load(c.Save());
        }
        Check(c.Complete && seen.Count==72,"all missing models are obtainable without repeats");
        var g=Game();
        Check(!g.CanContinueAfterAd && !g.ContinueAfterAd(),"continue cannot be used before a loss");
        var first=new Passenger(1,.001f);var second=new Passenger(1,.001f);var third=new Passenger(1,.001f,true,12);
        var other=new Passenger(2,88,false,3);
        g.Queues.TryEnqueue(0,first);g.Queues.TryEnqueue(0,second);g.Queues.TryEnqueue(0,third);g.Queues.TryEnqueue(1,other);
        g.Tick(.02f);
        Check(g.State==PlayState.GameOver && g.Lost==3 && g.CanContinueAfterAd,"third expired passenger opens a resumable game over");
        Check(g.Queues.Floors[0].Count==1 && ReferenceEquals(g.Queues.Floors[0][0],third) && third.Remaining<=0,"the actual third passenger remains in the same queue slot");
        Check(!g.Queues.Floors[0].ContainsForTest(first) && !g.Queues.Floors[0].ContainsForTest(second),"first two departed passengers are not resurrected");
        float elapsed=g.Elapsed,otherPatience=other.Remaining;int spawned=g.Spawned,score=g.Score;
        Tick(g,30);
        Check(g.Elapsed==elapsed && other.Remaining==otherPatience && g.Spawned==spawned,"ad offer freezes all clocks, arrivals and the other passenger's patience");
        var cancelled=new RewardedRequest(earned=>{if(earned)g.ContinueAfterAd();});cancelled.Close();
        Check(g.State==PlayState.GameOver && g.Lost==3 && third.Remaining<=0,"skipping the continuation ad does not rescue anyone");
        var completed=new RewardedRequest(earned=>{if(earned)g.ContinueAfterAd();});completed.Earn();completed.Close();completed.Close();
        Check(g.State==PlayState.Playing && g.Lost==2 && Math.Abs(third.Remaining-30)<.001f && third.HasBox && third.Appearance==12,"completed ad restores exactly 30% to the same third passenger and leaves two losses");
        Check(g.Score==score && g.Spawned==spawned && other.Remaining==otherPatience && !g.ContinueAfterAd(),"rescue creates no new passenger, points or second grant");
        float restoredPatience=third.Remaining;g.Pause();Tick(g,5);Check(third.Remaining==restoredPatience,"restored passenger still obeys pause");g.Resume();
        Tick(g,.1f);Check(third.Remaining<30,"ordinary patience countdown resumes after rescue");
        third.Remaining=.001f;g.Tick(.02f);
        Check(g.State==PlayState.GameOver && g.CanContinueAfterAd && g.Lost==3,"a later third loss may offer a new independently rewarded continuation");
        // Simultaneous expiry must stop at the third loss, preserving FIFO and capacity.
        g=Game();for(int i=0;i<7;i++)g.Queues.TryEnqueue(0,new Passenger(1,.001f));g.Tick(.02f);
        Check(g.Lost==3 && g.Queues.Floors[0].Count==5,"simultaneous expiration removes only the first two before the offer freezes the queue");
        Check(g.ContinueAfterAd() && g.Queues.Floors[0].Count==5,"revival does not enqueue an eighth figure or duplicate the retained passenger");
        // Raccoon is now only an appearance, including the same parcel rules.
        var human=new Passenger(0,100,true,2);var raccoon=new Passenger(0,100,true,0,true);
        Check(raccoon.IsRaccoon && raccoon.DeliveryUnits==1 && raccoon.HasBox==human.HasBox && raccoon.SpaceRequired==human.SpaceRequired,"raccoon has ordinary scoring/capacity rules including optional parcel");
        // Real arrivals with a tiny goal must not fill up the reserve preemptively.
        foreach(int goal in new[]{1,2,3,6,12})
        {
            g=Game(goal);Tick(g,20);
            Check(g.SpawnBudget==goal+3 && g.Spawned<=goal && g.ActivePassengerCount<=goal,"reserve is a cap rather than a surplus crowd for goal "+goal);
        }
        var levels=Campaign.CreateLevels();int simulations=0;
        foreach(int number in new[]{1,2,3,10,25,50})
        for(int seed=0;seed<15;seed++)
        for(int losses=0;losses<=2;losses++)
        {
            var level=levels[number-1].Copy();level.Guided=false;
            g=new GameSession(new GameRules(),seed,level,number);
            int forced=0;
            for(int frame=0;frame<20000 && !g.Finished;frame++)
            {
                if(forced<losses)
                {
                    for(int f=0;f<g.Queues.Floors.Count && forced<losses;f++)
                        if(g.Queues.Floors[f].Count>0) {g.Queues.Floors[f][0].Remaining=.001f;forced++;}
                }
                else Drive(g);
                g.Tick(.05f);
                if(g.Spawned>g.SpawnBudget || g.ActivePassengerCount+g.Delivered>g.Level.DeliveryGoal)throw new Exception("surplus spawn or reserve overflow");
            }
            Check(g.State==PlayState.Won && g.Delivered==level.DeliveryGoal && g.ActivePassengerCount==0 && g.Spawned==level.DeliveryGoal+losses && g.Lost==losses,
                "goal finishes with no leftover crowd and exactly needed replacements: level "+number+" seed "+seed+" losses "+losses);
            simulations++;
        }
        for(int seed=0;seed<30;seed++)
        {
            var level=levels[2].Copy();level.Guided=false;
            g=new GameSession(new GameRules(),seed,level,3);
            int expired=0;
            for(int frame=0;frame<10000 && !g.Finished;frame++)
            {
                for(int f=0;f<g.Queues.Floors.Count;f++)
                    if(g.Queues.Floors[f].Count>0 && expired<3) {g.Queues.Floors[f][0].Remaining=.001f;expired++;break;}
                g.Tick(.05f);
            }
            Check(g.State==PlayState.GameOver && g.ContinueAfterAd(),"real third loss may be rescued for seed "+seed);
            for(int frame=0;frame<20000 && !g.Finished;frame++) {Drive(g);g.Tick(.05f);}
            Check(g.State==PlayState.Won && g.Lost==2 && g.Spawned==level.DeliveryGoal+2 && g.ActivePassengerCount==0 && !g.CanContinueAfterAd,
                "real continuation still finishes with no leftover queue or surplus spawn for seed "+seed);
        }
        g=Game();g.SelectFloor(2);g.Tick(.02f);g.Tick(.02f);
        for(int i=0;i<3;i++)g.Queues.TryEnqueue(1,new Passenger(0,.001f));
        g.Tick(.02f);float position=g.Lift.Position;int target=g.Lift.TargetFloor;
        Check(g.Lift.Moving && g.ContinueAfterAd() && g.Lift.Moving && g.Lift.Position==position && g.Lift.TargetFloor==target,
            "ad rescue preserves an in-flight elevator's position and immutable destination");
        // No ad probability text is instantiated above the wheel; the ad button calls
        // the earned-only path, rather than silently granting the old free reward.
        string wheel=File.ReadAllText("Assets/OneMoreFloor/Runtime/FortuneScreens.cs");
        Check(!wheel.Contains("ui.Text(\"Rarity odds\"") && !wheel.Contains("Loc.T(\"wheel.rarityOdds\""),"wheel screen no longer creates the percentage label");
        Check(wheel.Contains("SpinAfterAd(rewardEarned") && wheel.Contains("RewardedPlacement.DailyWheel"),"actual UI uses the completed-view reward gate");
        Console.WriteLine(passed+" rewarded gameplay checks passed; "+simulations+" complete arrival/replacement simulations.");
    }
    static bool ContainsForTest(this IReadOnlyList<Passenger> items,Passenger candidate)
    {foreach(var p in items)if(ReferenceEquals(p,candidate))return true;return false;}
}
