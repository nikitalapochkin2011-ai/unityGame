using System;
using System.Collections.Generic;
using System.Linq;
using OneMoreFloor;
static class FinaleTests
{
    static int checks;
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);checks++;Console.WriteLine("PASS finale: "+name);}
    static IEnumerable<Passenger> Active(GameSession game)=>game.Queues.Floors.SelectMany(q=>q).Concat(game.Lift.Riders);
    static void Drive(GameSession g)
    {
        if(g.Lift.Moving)return;
        if(g.Lift.Riders.Count>0){g.SelectFloor(g.Lift.Riders[0].Destination);return;}
        for(int f=0;f<g.Queues.Floors.Count;f++)if(g.Queues.Floors[f].Count>0){g.SelectFloor(f);return;}
    }
    public static void Run()
    {
        var levels=Campaign.CreateLevels();var looks=new HashSet<int>();var craft=new HashSet<string>();
        var catalog=new TextCatalog(System.IO.File.ReadAllText("Assets/OneMoreFloor/Resources/Localization/Strings.txt"));
        foreach(int number in new[]{10,20,30,40,50,60,70,80,90,100})
        {
            var definition=RooftopFinales.ForLevel(number);
            Check(definition!=null && definition.LevelNumber==number && looks.Add(definition.Appearance) && craft.Add(definition.VehicleAsset) && ReferenceEquals(definition,RooftopFinales.ForAppearance(definition.Appearance)),"unique guest, vehicle and reverse lookup for level "+number);
            Check(catalog.Keys.Contains(definition.CharacterKey) && catalog.Keys.Contains(definition.VehicleKey),"localized guest and craft metadata for level "+number);
            var p=new Passenger(6,40,true,isRaccoon:true,isDinosaur:true,finaleAppearance:definition.Appearance);
            Check(p.IsFinalGuest && p.IsAlien==(number==10) && !p.IsDinosaur && !p.IsRaccoon && !p.HasBox && p.SpaceRequired==1 && p.DeliveryUnits==1 && p.Appearance==definition.Appearance,"final guest has one normal reward and exclusive appearance flags: "+number);
            for(int seed=0;seed<18;seed++)for(int forcedLosses=0;forcedLosses<3;forcedLosses++)
            {
                var game=new GameSession(new GameRules(),seed+508,levels[number-1],number);int losses=0,specialCount=0;bool finalSeen=false,ordered=true;
                var seen=new HashSet<Passenger>();
                for(int frame=0;frame<18000 && !game.Finished;frame++)
                {
                    foreach(var q in game.Queues.Floors)if(q.Count>0 && losses<forcedLosses && !q[0].IsFinalGuest){q[0].Remaining=.001f;losses++;break;}
                    Drive(game);game.Tick(.05f);
                    foreach(var passenger in Active(game))
                    {
                        if(!seen.Add(passenger))continue;
                        if(passenger.IsFinalGuest)
                        {
                            specialCount++;finalSeen=true;
                            ordered &= passenger.Appearance==definition.Appearance && passenger.Destination==game.RoofFloor && game.Delivered==game.Level.DeliveryGoal-1 && game.ActivePassengerCount==1;
                        }
                        else if(finalSeen)ordered=false;
                    }
                }
                Check(game.State==PlayState.Won && game.FinalGuestDelivered && specialCount==1 && ordered && game.ActivePassengerCount==0 &&
                    game.Delivered==game.Level.DeliveryGoal && game.Spawned==game.Level.DeliveryGoal+forcedLosses && game.Spawned<=game.SpawnBudget && game.Lift.CurrentFloor==game.RoofFloor,
                    "exclusive last guest, exact goal, roof arrival and replacements: level "+number+" seed "+seed+" losses "+forcedLosses);
            }
            foreach(bool rescue in new[]{false,true})
            {
                var game=new GameSession(new GameRules(),723,levels[number-1],number);int forced=0;bool expired=false,restored=false;Passenger last=null;bool correct=true;
                for(int frame=0;frame<18000 && game.State!=PlayState.Won;frame++)
                {
                    foreach(var q in game.Queues.Floors)
                    {
                        if(q.Count==0)continue;
                        if(rescue && forced<2 && !q[0].IsFinalGuest){q[0].Remaining=.001f;forced++;break;}
                        if(q[0].IsFinalGuest && !expired){last=q[0];last.Remaining=.001f;expired=true;break;}
                    }
                    Drive(game);game.Tick(.05f);
                    foreach(var p0 in Active(game))if(p0.IsFinalGuest)correct &= p0.Appearance==definition.Appearance;
                    if(game.State==PlayState.GameOver)
                    {
                        bool kept=Active(game).Any(p0=>ReferenceEquals(p0,last));
                        restored=kept && game.ContinueAfterAd() && Math.Abs(last.Remaining-game.PassengerPatience*.3f)<.01f;if(!restored)break;
                    }
                }
                Check(correct && expired && game.State==PlayState.Won && game.FinalGuestDelivered && game.ActivePassengerCount==0 &&
                    game.Spawned==game.Level.DeliveryGoal+(rescue ? 2 : 1) && (!rescue || restored),
                    (rescue ? "rewarded rescue retains the same story guest: " : "reserve replacement retains the same story identity: ")+number);
            }
            var tiny=levels[number-1].Copy();tiny.DeliveryGoal=1;
            var solo=new GameSession(new GameRules(),98,tiny,number);
            for(int frame=0;frame<3000 && !solo.Finished;frame++){Drive(solo);solo.Tick(.05f);}
            Check(solo.State==PlayState.Won && solo.FinalGuestDelivered && solo.Spawned==1 && solo.Score==10,"one-passenger custom milestone stays valid and pays ordinary points: "+number);
        }
        Check(looks.Count==10 && craft.Count==10,"all ten milestone guests and aircraft are unique");
        for(int number=1;number<=Campaign.LevelCount;number++)
        {
            if(number%10==0)continue;
            var tiny=levels[number-1].Copy();tiny.DeliveryGoal=1;
            var game=new GameSession(new GameRules(),61,tiny,number);bool special=false;
            for(int frame=0;frame<3000 && !game.Finished;frame++){Drive(game);game.Tick(.05f);special |= Active(game).Any(p=>p.IsFinalGuest);}
            Check(!game.HasRooftopFinale && game.Finale==null && !special && !game.FinalGuestDelivered && game.State==PlayState.Won,"ordinary level never creates a scripted guest: "+number);
        }
        Check(RooftopFinales.ForLevel(0)==null && RooftopFinales.ForLevel(110)==null && RooftopFinales.ForAppearance(17)==null && RooftopFinales.ForAppearance(28)==null,"catalog does not wrap invalid levels or ordinary appearances");
        bool rejected=false;try{new Passenger(6,20,finaleAppearance:28);}catch(ArgumentOutOfRangeException){rejected=true;}
        Check(rejected,"unrecognized finale appearance cannot silently become a duplicate human");
        Console.WriteLine(checks+" ten-finale checks passed.");
    }
}
