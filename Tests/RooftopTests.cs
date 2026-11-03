using System;
using System.Collections.Generic;
using System.Linq;
using OneMoreFloor;
static class RooftopTests
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS rooftop: "+name);}
    static void Step(GameSession g,float seconds){for(int i=0;i<(int)Math.Ceiling(seconds/.01f);i++)g.Tick(.01f);}
    static void Drive(GameSession g)
    {
        if(g.Lift.Moving)return;
        if(g.Lift.Riders.Count>0){g.SelectFloor(g.Lift.Riders[0].Destination);return;}
        for(int f=0;f<g.Queues.Floors.Count;f++)if(g.Queues.Floors[f].Count>0){g.SelectFloor(f);return;}
    }
    public static void Run()
    {
        int dinosaurs=0,raccoons=0,humans=0;
        for(int roll=0;roll<Passenger.AppearanceRollSize;roll++)
        {
            int value=Passenger.SpecialAppearance(roll);
            if(value==Passenger.DinosaurAppearance)dinosaurs++;else if(value==Passenger.RaccoonAppearance)raccoons++;else humans++;
        }
        Check(dinosaurs==50 && raccoons==400 && humans==9550,"exact mutually exclusive 0.5% dinosaur / 4% raccoon / 95.5% human distribution");
        bool rejects=true;foreach(int bad in new[]{-1,10000,int.MaxValue}){try{Passenger.SpecialAppearance(bad);rejects=false;}catch(ArgumentOutOfRangeException){}}
        Check(rejects,"invalid probability rolls are rejected");
        var dino=new Passenger(1,40,true,isDinosaur:true);var alien=new Passenger(6,40,true,isAlien:true);
        Check(dino.IsDinosaur && !dino.IsRaccoon && !dino.IsAlien && dino.Appearance==17 && dino.SpaceRequired==1 && !dino.HasBox,"dinosaur's ring fits one passenger slot and is never hidden by a parcel");
        Check(alien.IsAlien && alien.Appearance==18 && alien.SpaceRequired==1 && !alien.HasBox && alien.DeliveryUnits==1,"alien is one passenger, never a double reward");
        var quiet=new LevelDefinition{ActiveFloors=3,DeliveryGoal=100,PatienceSeconds=1000,SpawnSeconds=10000,FinalSpawnSeconds=10000};
        var game=new GameSession(new GameRules{SecondsPerFloor=.1f,StopSeconds=.1f},1,quiet,1);
        int bells=0,lastStop=-1;game.Arrived+=floor=>{bells++;lastStop=floor;};
        game.SelectFloor(0);Step(game,.15f);Check(bells==0,"reselecting the current floor does not ring the arrival bell");
        game.SelectFloor(game.RoofFloor);game.SelectFloor(4);
        Check(game.Lift.NextFloor==game.RoofFloor,"locked residential floor cannot replace the unlocked roof destination");
        Step(game,.15f);game.Pause();float pos=game.Lift.Position;Step(game,.5f);
        Check(game.Lift.Position==pos && bells==0,"pause freezes an in-flight roof trip and emits no false arrival");
        game.Resume();Step(game,.8f);
        Check(game.RoofFloor==6 && game.Queues.Floors.Count==6 && game.Lift.CurrentFloor==6 && bells==1 && lastStop==6,"roof is a seventh stop available on level one without adding a residential queue");
        for(int i=0;i<4;i++){game.SelectFloor(6);Step(game,.15f);}
        Check(bells==1,"standing on the roof and repeated taps do not duplicate arrival sounds");
        game.SelectFloor(1);game.SelectFloor(7);Step(game,.8f);
        Check(game.Lift.CurrentFloor==1 && bells==2,"roof return works; out-of-range input cannot replace valid travel");
        game=new GameSession(new GameRules{InitialSpawnSeconds=10000,MinimumSpawnSeconds=10000},1);
        game.Lift.Riders.Add(dino);game.SelectFloor(1);Step(game,2);
        Check(game.Delivered==1 && game.Score==10,"rare dinosaur keeps ordinary scoring and delivery progress");
        // Full deterministic runs exercise final spawning, substitutions, scoring and the roof service path.
        var levels=Campaign.CreateLevels();
        for(int seed=0;seed<24;seed++)for(int forcedLosses=0;forcedLosses<3;forcedLosses++)
        {
            game=new GameSession(new GameRules(),seed,levels[9],10);
            var observed=new HashSet<Passenger>();int losses=0,alienCount=0;bool last=true,afterAlien=false;
            for(int frame=0;frame<18000 && !game.Finished;frame++)
            {
                foreach(var q in game.Queues.Floors)if(q.Count>0 && losses<forcedLosses && !q[0].IsAlien){q[0].Remaining=.001f;losses++;break;}
                Drive(game);game.Tick(.05f);
                foreach(var p in game.Queues.Floors.SelectMany(q=>q).Concat(game.Lift.Riders))
                {
                    if(!observed.Add(p))continue;
                    if(p.IsAlien){alienCount++;afterAlien=true;last &= game.Delivered==game.Level.DeliveryGoal-1 && game.ActivePassengerCount==1 && p.Destination==game.RoofFloor;}
                    else if(afterAlien)last=false;
                }
            }
            Check(game.State==PlayState.Won && game.AlienDelivered && alienCount==1 && last && game.Lift.CurrentFloor==game.RoofFloor &&
                game.Delivered==game.Level.DeliveryGoal && game.ActivePassengerCount==0 && game.Spawned==game.Level.DeliveryGoal+forcedLosses && game.Spawned<=game.SpawnBudget,
                "alien is exclusively last and roof delivery finishes level 10, seed "+seed+", replacements "+forcedLosses);
        }
        foreach(int number in new[]{1,2,9,11,25,50})
        {
            game=new GameSession(new GameRules(),181,levels[number-1],number);bool anyAlien=false;
            for(int frame=0;frame<18000 && !game.Finished;frame++)
            {Drive(game);game.Tick(.05f);anyAlien |= game.Queues.Floors.SelectMany(q=>q).Concat(game.Lift.Riders).Any(p=>p.IsAlien);}
            Check(game.State==PlayState.Won && !game.HasAlienFinale && !anyAlien && !game.AlienDelivered,"no probabilistic alien outside level 10: "+number);
        }
        // Loss of the final visitor uses the same finite reserve and rewarded rescue as everyone else.
        foreach(bool rescue in new[]{false,true})
        {
            game=new GameSession(new GameRules(),31,levels[9],10);int forced=0;bool expired=false,restored=false;Passenger final=null;
            for(int frame=0;frame<18000 && game.State!=PlayState.Won;frame++)
            {
                foreach(var q in game.Queues.Floors)
                {
                    if(q.Count==0)continue;
                    if(rescue && forced<2 && !q[0].IsAlien){q[0].Remaining=.001f;forced++;break;}
                    if(q[0].IsAlien && !expired){final=q[0];final.Remaining=.001f;expired=true;break;}
                }
                Drive(game);game.Tick(.05f);
                if(game.State==PlayState.GameOver)
                {
                    bool kept=game.Queues.Floors.Any(q=>q.Any(p=>ReferenceEquals(p,final)));
                    restored=kept && game.ContinueAfterAd() && Math.Abs(final.Remaining-game.PassengerPatience*.3f)<.01f;
                    if(!restored)break;
                }
            }
            Check(expired && game.State==PlayState.Won && game.AlienDelivered && game.ActivePassengerCount==0 &&
                game.Spawned==game.Level.DeliveryGoal+(rescue ? 2 : 1) && (!rescue || restored),
                rescue ? "third lost alien is retained and rescued with 30% patience, with no duplicate spawn" : "expired final alien is replaced from the reserve by another final alien");
        }
        var one=levels[9].Copy();one.DeliveryGoal=1;
        game=new GameSession(new GameRules(),17,one,10);bool firstAlien=false;
        for(int i=0;i<1800 && !game.Finished;i++)
        {Drive(game);game.Tick(.05f);firstAlien |= game.Queues.Floors.SelectMany(q=>q).Any(p=>p.IsAlien);}
        Check(firstAlien && game.State==PlayState.Won && game.AlienDelivered && game.Spawned==1,"custom one-passenger level 10 still guarantees its final visitor");
        foreach(var cue in new[]{CozySound.Bell(),CozySound.Celebration()})
        {
            float peak=0,jump=0;bool finite=true;
            for(int i=0;i<cue.Length;i++){finite &= !float.IsNaN(cue[i]) && !float.IsInfinity(cue[i]);peak=Math.Max(peak,Math.Abs(cue[i]));if(i>0)jump=Math.Max(jump,Math.Abs(cue[i]-cue[i-1]));}
            Check(cue.Length>30000 && cue.Length<60000 && finite && peak>.015f && peak<.2f,"original PCM cue has useful gentle headroom and valid duration");
            Check(Math.Abs(cue[0])<.00001f && Math.Abs(cue[cue.Length-1])<.00001f && jump<.05f,"PCM cue has a soft attack/release and no sharp click or clap transient");
        }
        Check(CozySound.Celebration().SequenceEqual(CozySound.Celebration()),"airy celebration is deterministic and independent of gameplay randomness");
        Console.WriteLine(checks+" rooftop, rare passenger and audio checks passed.");
    }
}
