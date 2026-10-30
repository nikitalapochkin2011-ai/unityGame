using System;
using System.Collections.Generic;
using OneMoreFloor;

static class CampaignTests
{
    static int passed;
    static void Check(bool condition,string name)
    { if(!condition) throw new Exception(name); passed++; Console.WriteLine("PASS campaign: "+name); }
    static void Run(GameSession game,float seconds)
    { for(int i=0;i<(int)Math.Ceiling(seconds/.01f);i++) game.Tick(.01f); }
    static GameSession Isolated(int goal=100,bool guided=false)
    {
        return new GameSession(new GameRules { SecondsPerFloor=.1f,StopSeconds=.1f },42,
            new LevelDefinition { ActiveFloors=3,DeliveryGoal=goal,Guided=guided,SpawnSeconds=1000,FinalSpawnSeconds=1000,PatienceSeconds=1000 });
    }
    static void Drive(GameSession game)
    {
        if(game.State!=PlayState.Playing || game.Lift.Moving || game.Lift.NextFloor.HasValue) return;
        int floor=-1; float best=float.MaxValue;
        // Unload riders first; otherwise visit the closest nonempty queue.
        if(game.Lift.Riders.Count>0)
        {
            foreach(var rider in game.Lift.Riders)
            {
                float cost=Math.Abs(rider.Destination-game.Lift.Position);
                if(cost<best) { floor=rider.Destination; best=cost; }
            }
        }
        else for(int f=0;f<game.Level.ActiveFloors;f++)
        {
            if(game.Queues.Floors[f].Count==0) continue;
            float cost=Math.Abs(f-game.Lift.Position);
            if(cost<best) { floor=f; best=cost; }
        }
        if(floor>=0) game.SelectFloor(floor);
    }
    public static void PrintCatalog()
    {
        var culture=System.Globalization.CultureInfo.InvariantCulture;
        Console.WriteLine("# Баланс 50 уровней\n\nСформировано из Campaign.CreateLevels(). Интервалы меняются на ±15%; пустой дом пополняется примерно за 0.8 секунды. Лимит активных включает ожидающих и едущих людей, независимо от числа мест.\n");
        Console.WriteLine("| № | Название | Этажи | Цель | Профиль | Интервал, с | Терпение, с | Коробки | Активных максимум |\n|---|---|---|---:|---|---|---:|---|---:|");
        var levels=Campaign.CreateLevels();
        for(int i=0;i<levels.Length;i++)
        {
            var l=levels[i];
            Console.WriteLine($"| {i+1} | {l.Title} | 1–{l.ActiveFloors} | {l.DeliveryGoal} | {Campaign.TrafficDescription(l.Traffic)} | {l.SpawnSeconds.ToString("0.##",culture)} → {l.FinalSpawnSeconds.ToString("0.##",culture)} | {l.PatienceSeconds} | {(l.BoxChance*100).ToString("0.#",culture)}% | {l.MaxActivePassengers} |");
        }
    }
    public static void MainChecks()
    {
        var levels=Campaign.CreateLevels();
        Check(levels.Length==50 && levels[0].Guided && !levels[1].Guided,"fifty levels with live tutorial only on level one");
        Check(levels[0].ActiveFloors==3 && levels[1].ActiveFloors==3 && levels[2].ActiveFloors==4 && levels[3].ActiveFloors==5 && levels[4].ActiveFloors==6,"floors 4/5/6 open only on levels 3/4/5");
        Check(levels[0].BoxChance==0 && levels[1].BoxChance==0 && levels[2].BoxChance>0,"boxes begin at level three");
        var progress=new CampaignProgress();
        Check(progress.Completed==0 && progress.Unlocked==1 && !progress.IsUnlocked(0) && !progress.IsUnlocked(2),"only level one is initially accessible");
        Check(!progress.Complete(3) && progress.Completed==0,"cannot unlock by completing a locked level");
        for(int n=1;n<=Campaign.LevelCount;n++)
        {
            Check(progress.Complete(n) && progress.Completed==n && progress.Unlocked==Math.Min(Campaign.LevelCount,n+1),"victory unlocks next level "+n);
            Check(!progress.Complete(n) && progress.Completed==n,"replaying level does not skip unlocks "+n);
        }
        Check(new CampaignProgress(progress.Completed).Completed==50 && new CampaignProgress(999).Unlocked==50 && new CampaignProgress(-3).Unlocked==1,"saved progress reloads and corrupt values clamp");
        var game=Isolated(); game.SelectFloor(3); game.SelectFloor(5);
        Check(!game.Lift.NextFloor.HasValue,"locked floor taps ignored in model");
        game.SelectFloor(1); Run(game,.02f); game.SelectFloor(2); game.SelectFloor(5);
        Check(game.Lift.TargetFloor==1 && game.Lift.NextFloor==2,"locked tap cannot replace a valid queued trip");
        Run(game,.5f); Check(game.Lift.CurrentFloor==2,"valid queued trip completes unchanged");
        var level=levels[2].Copy(); game=new GameSession(new GameRules(),21,level,3); level.ActiveFloors=6;
        Check(game.Level.ActiveFloors==4,"session snapshots level settings");
        for(int d=0;d<100;d++)
        {
            var curve=Campaign.Difficulty(levels[2],3,d);
            if(curve.ActiveFloors!=4 || curve.SpawnInterval<levels[2].FinalSpawnSeconds || curve.SpawnInterval>levels[2].SpawnSeconds) throw new Exception("campaign difficulty cap");
        }
        Check(true,"delivery progress changes tempo gently without opening extra floors");

        game=Isolated(); var box=new Passenger(1,1000,true,4); var normal=new Passenger(1,1000);
        game.Queues.TryEnqueue(0,box); game.Queues.TryEnqueue(0,normal); game.Queues.TryEnqueue(0,new Passenger(2,1000,true));
        game.SelectFloor(0); Run(game,.05f);
        Check(game.Lift.OccupiedSpaces==3 && game.Lift.Riders.Count==2 && game.Queues.Floors[0].Count==1,"box uses two spaces, capacity measured in spaces");
        Check(box.SpaceRequired==2 && normal.SpaceRequired==1 && box.Appearance==4,"passenger type and appearance retained");
        float patience=box.Remaining; Run(game,.3f);
        Check(box.Remaining==patience,"box rider patience frozen");
        game=Isolated(); for(int i=0;i<3;i++) game.Lift.Riders.Add(new Passenger(1,1000));
        box=new Passenger(2,1000,true); normal=new Passenger(2,1000);
        game.Queues.TryEnqueue(0,box); game.Queues.TryEnqueue(0,normal);
        for(int i=0;i<80;i++) game.SelectFloor(0); Run(game,.05f);
        Check(game.Lift.OccupiedSpaces==3 && game.Queues.Floors[0].Count==2 && ReferenceEquals(game.Queues.Floors[0][0],box),"one free space never boards box or skips FIFO");
        game.SelectFloor(1); Run(game,.25f); game.SelectFloor(0); Run(game,.35f);
        Check(game.Lift.OccupiedSpaces==3 && game.Lift.Riders.Count==2 && game.Queues.Floors[0].Count==0,"box boards after enough space is freed");
        game=Isolated(); for(int i=0;i<3;i++) game.Queues.TryEnqueue(0,new Passenger(1,1000,true));
        game.SelectFloor(0); Run(game,.05f);
        Check(game.Lift.Riders.Count==2 && game.Lift.OccupiedSpaces==4 && game.Queues.Floors[0].Count==1,"two boxes fill four places, third waits");
        game=Isolated(); for(int i=0;i<4;i++) game.Lift.Riders.Add(new Passenger(1,1000));
        box=new Passenger(2,1000,true); game.Queues.TryEnqueue(1,box);
        game.SelectFloor(1); Run(game,.15f);
        Check(game.Delivered==4 && game.Lift.Riders.Count==1 && game.Lift.OccupiedSpaces==2,"unload full cabin before boarding box");
        game=Isolated(); game.Lift.Riders.Add(new Passenger(1,1000,true));
        game.SelectFloor(1); Run(game,.15f);
        Check(game.Delivered==1 && game.Score==10,"box is one delivery with normal reward");
        game=Isolated(); box=new Passenger(1,.01f,true); game.Queues.TryEnqueue(0,box); Run(game,.02f);
        Check(game.Lost==1 && game.Queues.Floors[0].Count==0,"expired box passenger costs one loss");

        game=Isolated(1); game.Lift.Riders.Add(new Passenger(1,1000));
        game.Queues.TryEnqueue(1,new Passenger(2,1000)); game.SelectFloor(1); Run(game,.2f);
        Check(game.State==PlayState.Won && game.Delivered==1 && game.Queues.Floors[1].Count==1 && game.Lift.Riders.Count==0,"goal wins before any unnecessary boarding");
        float elapsed=game.Elapsed; game.SelectFloor(2); game.Resume(); game.Pause(); Run(game,20);
        Check(game.State==PlayState.Won && game.Elapsed==elapsed && !game.Lift.Moving,"victory is terminal despite taps, pause and resume");
        game=Isolated(); for(int i=0;i<3;i++) game.Queues.TryEnqueue(0,new Passenger(1,.01f)); Run(game,.02f);
        Check(game.State==PlayState.GameOver && game.Delivered==0,"three real losses fail campaign level");
        game=Isolated();
        Check(game.State==PlayState.Playing && game.Spawned==0 && game.Lost==0 && game.Delivered==0 && game.Lift.OccupiedSpaces==0,"retry starts fresh with no previous level state");
        game=new GameSession(new GameRules(),38,levels[0],1); Run(game,150);
        int waiting=0,origin=-1; Passenger first=null;
        for(int f=0;f<3;f++) if(game.Queues.Floors[f].Count>0) { origin=f; first=game.Queues.Floors[f][0]; waiting+=game.Queues.Floors[f].Count; }
        Check(waiting==1 && game.Spawned==1 && game.Lost==0 && first.Remaining==80,"live tutorial waits indefinitely with exactly one random passenger");
        game.Pause(); elapsed=game.Elapsed; Run(game,5);
        Check(game.Elapsed==elapsed && game.IsGuiding,"pause preserves live tutorial"); game.Resume();
        game.SelectFloor(origin); Run(game,4); game.SelectFloor(first.Destination); Run(game,4);
        Check(game.Delivered==1 && !game.IsGuiding,"guided pickup and delivery hand off to normal gameplay");
        Run(game,12); Check(game.Spawned>1,"normal spawning resumes after first delivery");
        bool variedGuidance=false; int firstGuidedFloor=-1;
        for(int seed=0;seed<20;seed++)
        {
            game=new GameSession(new GameRules(),seed,levels[0],1); Run(game,2);
            for(int f=0;f<3;f++) if(game.Queues.Floors[f].Count>0)
            { if(firstGuidedFloor<0) firstGuidedFloor=f; else variedGuidance |= f!=firstGuidedFloor; }
        }
        Check(variedGuidance,"tutorial's first passenger also appears on random floors");

        var origins=new HashSet<int>(); var destinations=new HashSet<int>(); var looks=new HashSet<int>();
        var firstSignatures=new HashSet<string>(); var firstBoxPositions=new HashSet<string>(); bool allRoutes=true,boxesOnEveryFloor=true;
        var seenBoxes=new bool[6]; int boxCount=0,normalCount=0,total=0;
        for(int seed=0;seed<160;seed++)
        {
            var sample=levels[4].Copy(); sample.DeliveryGoal=200; sample.PatienceSeconds=10000;
            game=new GameSession(new GameRules(),seed,sample,5); bool firstSpawn=true,firstBox=true;
            for(int i=0;i<6000;i++)
            {
                game.Tick(.05f);
                for(int f=0;f<6;f++) while(game.Queues.Floors[f].Count>0)
                {
                    var p=game.Queues.TakeFirst(f); total++; origins.Add(f); destinations.Add(p.Destination); looks.Add(p.Appearance);
                    allRoutes &= p.Destination>=0 && p.Destination<6 && p.Destination!=f;
                    if(firstSpawn) { firstSignatures.Add(f+":"+p.Destination+":"+p.HasBox+":"+p.Appearance); firstSpawn=false; }
                    if(p.HasBox) { boxCount++; seenBoxes[f]=true; if(firstBox) { firstBoxPositions.Add(f+":"+game.Spawned); firstBox=false; } }
                    else normalCount++;
                }
            }
        }
        foreach(bool seen in seenBoxes) boxesOnEveryFloor &= seen;
        Check(allRoutes && origins.Count==6 && destinations.Count==6 && looks.Count==8,"random appearances and valid origins/destinations span every unlocked floor");
        Check(boxesOnEveryFloor && firstBoxPositions.Count>12 && boxCount>total*.20 && boxCount<total*.30 && normalCount>0,"box floors and first appearance vary across seeded attempts at configured probability");
        Check(firstSignatures.Count>80,"retries produce varied first passengers and routes");
        bool deterministic=true; var a=new GameSession(new GameRules(),19,levels[4],5); var b=new GameSession(new GameRules(),19,levels[4],5);
        for(int i=0;i<100;i++) { a.Tick(.05f); b.Tick(.05f); }
        for(int f=0;f<6;f++)
        {
            deterministic &= a.Queues.Floors[f].Count==b.Queues.Floors[f].Count;
            for(int i=0;i<a.Queues.Floors[f].Count;i++)
            {
                var pa=a.Queues.Floors[f][i]; var pb=b.Queues.Floors[f][i];
                deterministic &= pa.Destination==pb.Destination && pa.HasBox==pb.HasBox && pa.Appearance==pb.Appearance;
            }
        }
        Check(deterministic,"explicit test seeds reproduce a session for debugging");
        var fullHouseLevel=levels[2].Copy(); fullHouseLevel.ActiveFloors=3; fullHouseLevel.QueueLimit=2; fullHouseLevel.MaxActivePassengers=8;
        game=new GameSession(new GameRules(),7,fullHouseLevel,3); game.Pause(); Run(game,50);
        Check(game.Spawned==0 && game.Elapsed==0,"pause stops campaign generator"); game.Resume();
        // Fill all active floors; failed arrivals must never consume the finite supply.
        for(int f=0;f<3;f++) for(int i=0;i<game.QueueLimit;i++) game.Queues.TryEnqueue(f,new Passenger((f+1)%3,1000));
        Run(game,20); Check(game.Spawned==0 && game.Lost==0,"full queues skip spawns without consuming level budget");
        game.Queues.TakeFirst(2); Run(game,10);
        Check(game.Spawned==1 && game.Queues.Floors[2].Count==game.QueueLimit,"freed floor slot accepts the next finite arrival");
        var budgetLevel=levels[1].Copy(); budgetLevel.DeliveryGoal=1;
        game=new GameSession(new GameRules(),8,budgetLevel,2);
        for(int i=0;i<2000;i++)
        {
            game.Tick(.05f);
            for(int f=0;f<3;f++) while(game.Queues.Floors[f].Count>0) game.Queues.TakeFirst(f);
        }
        Check(game.Spawned==game.SpawnBudget && game.SpawnBudget==3,"finite supply includes exactly two spare passengers");
        game=Isolated(2); game.Queues.TryEnqueue(0,new Passenger(1,.01f)); game.Queues.TryEnqueue(0,new Passenger(1,.01f));
        Run(game,.02f); game.Lift.Riders.Add(new Passenger(1,1000)); game.Lift.Riders.Add(new Passenger(1,1000,true));
        game.SelectFloor(1); Run(game,.25f);
        Check(game.State==PlayState.Won && game.Lost==2 && game.Delivered==2,"two losses still permit victory on the delivery goal");
        bool cabinGaps=true;
        foreach(int[] sizes in new[]{ new[]{2,2},new[]{2,1,1},new[]{1,2,1},new[]{1,1,2},new[]{1,1,1,1} })
        {
            float lastRight=BuildingLayout.RidersLeft; int slot=0;
            foreach(int spaces in sizes)
            {
                float center=BuildingLayout.RiderX(slot,spaces,4),width=BuildingLayout.RiderWidth(spaces,4);
                cabinGaps &= center-width*.5f>=lastRight && center+width*.5f<BuildingLayout.DoorRight;
                lastRight=center+width*.5f; slot+=spaces;
            }
        }
        Check(cabinGaps,"all four-space mixes keep passenger roots separate inside glass doors");

        Check(new CampaignProgress(5).Unlocked==6,"existing five completed levels immediately unlock level six");
        bool pages=true;
        for(int n=1;n<=50;n++) pages &= Campaign.PageForLevel(n)==(n-1)/5;
        Check(pages && Campaign.PageCount==10 && Campaign.PageForLevel(-100)==0 && Campaign.PageForLevel(999)==9,"all 50 level cards fit ten bounded pages");
        var old=new LevelDefinition[5];
        float[] oldInitial={9,8,7.8f,7.6f,7.4f},oldFinal={8.5f,7.5f,7.2f,7,6.8f},oldPatience={80,70,74,80,86};
        int[] oldQueue={2,3,4,5,5};
        for(int i=0;i<5;i++)
        {
            old[i]=levels[i].Copy(); old[i].SpawnSeconds=oldInitial[i]; old[i].FinalSpawnSeconds=oldFinal[i];
            old[i].PatienceSeconds=oldPatience[i]; old[i].QueueLimit=oldQueue[i];
        }
        var upgraded=Campaign.UpgradeLevels(old,0,6);
        Check(upgraded.Length==50 && upgraded[0].SpawnSeconds==levels[0].SpawnSeconds && upgraded[4].SpawnSeconds==levels[4].SpawnSeconds,"untouched old presets automatically gain new fast balance and 45 levels");
        old[1].Title="My edited stage"; old[1].DeliveryGoal=19; old[1].MaxActivePassengers=0; old[1].EmptyHouseDelay=0;
        upgraded=Campaign.UpgradeLevels(old,0,6);
        Check(upgraded[1].Title=="My edited stage" && upgraded[1].DeliveryGoal==19 && upgraded[1].MaxActivePassengers==6 && upgraded[1].EmptyHouseDelay==.8f,"upgrade preserves custom levels and repairs missing new fields");
        upgraded[12].SpawnSeconds=6; upgraded[12].FinalSpawnSeconds=5;
        var reloaded=Campaign.UpgradeLevels(upgraded,Campaign.BalanceVersion,6);
        Check(reloaded[12].SpawnSeconds==6 && reloaded.Length==50 && !ReferenceEquals(reloaded[12],upgraded[12]),"current edited balance survives another load without aliasing");
        Check(Campaign.UpgradeLevels(null,0,6).Length==50,"missing campaign settings receive full defaults");
        var signatures=new HashSet<string>(); var titles=new HashSet<string>(); var modes=new HashSet<TrafficPattern>();
        foreach(var definition in levels)
        {
            titles.Add(definition.Title); modes.Add(definition.Traffic);
            signatures.Add($"{definition.ActiveFloors}|{definition.DeliveryGoal}|{definition.SpawnSeconds}|{definition.FinalSpawnSeconds}|{definition.PatienceSeconds}|{definition.BoxChance}|{definition.MaxActivePassengers}|{definition.Traffic}");
        }
        Check(titles.Count==50 && signatures.Count==50 && modes.Count==5,"50 distinct named rule sets alternate five different traffic profiles");
        var fastLevel=levels[1].Copy();
        game=new GameSession(new GameRules { SecondsPerFloor=.1f,StopSeconds=.1f },312,fastLevel,2);
        Run(game,.9f); int pickup=-1; Passenger passenger=null;
        for(int f=0;f<3;f++) if(game.Queues.Floors[f].Count>0) { pickup=f; passenger=game.Queues.Floors[f][0]; }
        Check(passenger!=null,"first random passenger appears within one second");
        game.SelectFloor(pickup); Run(game,.35f); game.SelectFloor(passenger.Destination);
        while(game.Delivered==0) game.Tick(.01f);
        int beforeRefill=game.Spawned; Check(game.ActivePassengerCount==0,"refill test delivered the last person while normal timer had time remaining");
        Run(game,.9f);
        Check(game.Spawned==beforeRefill+1,"empty house refills within one second after a real delivery, not seven seconds");
        game=new GameSession(new GameRules(),312,levels[1],2); Run(game,5);
        Check(game.Spawned>=2 && game.Delivered==0,"new people arrive independently of deliveries");
        game=new GameSession(new GameRules(),312,levels[4],5); Run(game,50);
        Check(game.ActivePassengerCount==game.Level.MaxActivePassengers && game.Lost==0 && game.Spawned==game.Level.MaxActivePassengers,"faster arrivals stop at active passenger limit without hidden supply or losses");
        game.Pause(); int spawnAtPause=game.Spawned; elapsed=game.Elapsed; Run(game,10);
        Check(game.Spawned==spawnAtPause && game.Elapsed==elapsed,"pause also freezes accelerated traffic at the active limit");

        bool allLevelRandomness=true,profileRoutes=true,burstsSeen=false,allAppearancesRandom=true;
        for(int number=1;number<=50;number++)
        {
            var probe=levels[number-1].Copy(); probe.Guided=false; probe.DeliveryGoal=1000;
            var observedOrigins=new HashSet<int>(); var observedLooks=new HashSet<int>(); var firstChoices=new HashSet<string>();
            for(int seed=0;seed<12;seed++)
            {
                game=new GameSession(new GameRules(),seed+181,probe,number); bool firstArrival=true;
                for(int frame=0;frame<2400;frame++)
                {
                    int spawnBefore=game.Spawned; game.Tick(.05f);
                    if(game.Spawned-spawnBefore==2) burstsSeen=true;
                    for(int f=0;f<probe.ActiveFloors;f++) while(game.Queues.Floors[f].Count>0)
                    {
                        var p=game.Queues.TakeFirst(f); observedOrigins.Add(f); observedLooks.Add(p.Appearance);
                        if(firstArrival) { firstChoices.Add(f+":"+p.Destination+":"+p.Appearance+":"+p.HasBox); firstArrival=false; }
                        int distance=Math.Abs(p.Destination-f);
                        profileRoutes &= p.Destination>=0 && p.Destination<probe.ActiveFloors && distance>0;
                        if(probe.Traffic==TrafficPattern.Neighbors) profileRoutes &= distance<=2;
                        if(probe.Traffic==TrafficPattern.LongTrips) profileRoutes &= distance>=probe.ActiveFloors/2;
                    }
                }
            }
            allLevelRandomness &= observedOrigins.Count==probe.ActiveFloors && firstChoices.Count>=6;
            allAppearancesRandom &= observedLooks.Count==8;
        }
        Check(allLevelRandomness && allAppearancesRandom,"every level uses all unlocked origin floors, all eight appearances and varied first passengers");
        Check(profileRoutes && burstsSeen,"short and long route rules hold, wave levels actually spawn pairs");

        int simulated=0;
        for(int levelNumber=1;levelNumber<=Campaign.LevelCount;levelNumber++)
        {
            int wins=0; float duration=0; int losses=0;
            for(int seed=0;seed<50;seed++)
            {
                game=new GameSession(new GameRules(),seed,levels[levelNumber-1],levelNumber);
                float think=0;
                for(int frame=0;frame<18000 && !game.Finished;frame++)
                {
                    // A simple driver reacts every 1.25 seconds; no clairvoyance or route optimization.
                    think-=.05f; if(think<=0) { Drive(game); think=1.25f; }
                    game.Tick(.05f); simulated++;
                    if(game.Lift.OccupiedSpaces>4 || game.Lost>3 || game.Lift.Position<0 || game.Lift.Position>=game.Level.ActiveFloors || game.ActivePassengerCount>game.Level.MaxActivePassengers || game.Spawned>game.SpawnBudget)
                        throw new Exception("campaign capacity/floor invariant");
                    for(int f=0;f<6;f++)
                    {
                        if(game.Queues.Floors[f].Count>game.QueueLimit || (f>=game.Level.ActiveFloors && game.Queues.Floors[f].Count>0))
                            throw new Exception("campaign queue invariant");
                    }
                }
                if(game.State==PlayState.Won) wins++;
                duration+=game.Elapsed; losses+=game.Lost;
            }
            Console.WriteLine($"SIM level {levelNumber}: {wins}/50 wins; mean seconds {duration/50:F1}; mean losses {losses/50f:F2}");
            Check(wins==50,"modest level "+levelNumber+" beatable across 50 varied attempts by simple driver");
        }
        Console.WriteLine($"{passed} campaign checks passed; {simulated} simulated campaign frames.");
    }
}
