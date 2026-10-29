using System;
using OneMoreFloor;
class Program
{
    static int passed;
    static void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
    static GameSession Game() { return new GameSession(new GameRules { InitialSpawnSeconds=10000, MinimumSpawnSeconds=10000, PatienceSeconds=1000, SecondsPerFloor=.1f, StopSeconds=.1f }, 42); }
    static void Run(GameSession g, float seconds) { for(int i=0;i<(int)Math.Ceiling(seconds/.01f);i++) g.Tick(.01f); }
    static void Main()
    {
        var g=Game(); var first=new Passenger(1,1000); g.Queues.Floors[0].Add(first);
        for(int i=0;i<5;i++) g.Queues.Floors[0].Add(new Passenger(2,1000));
        g.SelectFloor(0); Run(g,.05f);
        Check(g.Lift.Riders.Count==4 && g.Queues.Floors[0].Count==2,"capacity four");
        Check(ReferenceEquals(first,g.Lift.Riders[0]),"FIFO boarding");
        float patience=first.Remaining; Run(g,.2f); Check(first.Remaining==patience,"rider patience frozen");
        g.SelectFloor(1); Run(g,.02f); Check(g.Lift.Moving,"trip started");
        g.SelectFloor(5); g.SelectFloor(2); g.SelectFloor(2);
        Check(g.Lift.TargetFloor==1 && g.Lift.NextFloor==2,"last tap wins without interrupting active trip");
        Check(g.Delivered==0,"no service while moving");
        g.Queues.Floors[1].Add(new Passenger(4,1000)); Run(g,.1f);
        Check(g.Delivered==1 && g.Lift.Riders.Count==4 && g.Queues.Floors[1].Count==0,"exit before boarding at full stop");
        Run(g,.3f); Check(g.Lift.CurrentFloor==2 && !g.Lift.Moving,"queued next stop reached");
        g=Game(); g.SelectFloor(5); Run(g,.1f); g.Pause();
        float position=g.Lift.Position, elapsed=g.Elapsed; Run(g,5);
        Check(g.Lift.Position==position && g.Elapsed==elapsed,"pause freezes movement and time");
        g.SelectFloor(2); Check(!g.Lift.NextFloor.HasValue,"paused input ignored"); g.Resume(); Run(g,1);
        Check(g.Lift.CurrentFloor==5,"resume completes trip");
        g=Game();
        for(int trip=0;trip<3;trip++)
        {
            int target=trip%2==0 ? 1:0;
            for(int i=0;i<4;i++) g.Lift.Riders.Add(new Passenger(target,1000));
            g.SelectFloor(target); Run(g,.4f);
        }
        Check(g.Delivered==12 && g.Multiplier==3 && g.Score==210,"streak multiplier thresholds and scoring");
        g.Queues.Floors[3].Add(new Passenger(0,.01f)); Run(g,.02f);
        Check(g.Lost==1 && g.Streak==0 && g.Multiplier==1,"loss resets streak");
        g.Queues.Floors[3].Add(new Passenger(0,.01f)); g.Queues.Floors[3].Add(new Passenger(0,.01f)); Run(g,.02f);
        Check(g.Lost==3 && g.State==PlayState.GameOver,"third loss ends game");
        elapsed=g.Elapsed; g.Resume(); g.SelectFloor(4); Run(g,1); Check(g.Elapsed==elapsed,"game over immutable");
        g=Game(); Check(g.Score==0 && g.Lost==0 && g.Lift.Riders.Count==0 && g.Elapsed==0,"restart clean session");
        g.SelectFloor(-1); g.SelectFloor(6); Check(!g.Lift.NextFloor.HasValue,"invalid floors ignored");
        g.SelectFloor(0); for(int i=0;i<100;i++) g.SelectFloor(0); Run(g,.5f);
        Check(!g.Lift.Moving && !g.Lift.NextFloor.HasValue,"repeated same-floor taps settle");
        g=new GameSession(new GameRules { PatienceSeconds=10000 },42); Run(g,300);
        int count=0; foreach(var q in g.Queues.Floors) count+=q.Count;
        Check(count>=59 && count<=61 && g.Difficulty.Level==1,"waiting alone does not increase difficulty or spawn rate");
        bool earlyRoutes=true;
        for(int f=0;f<g.Queues.Floors.Length;f++) foreach(var p in g.Queues.Floors[f])
            earlyRoutes &= f<3 && p.Destination<3 && Math.Abs(p.Destination-f)==1;
        Check(earlyRoutes,"initial spawns stay on lower three floors with adjacent destinations");
        var rules=new GameRules(); rules.Validate();
        var initial=DifficultyCurve.AtScore(rules,0);
        Check(initial.ActiveFloors==3 && initial.MaximumRouteDistance==1 && initial.SpawnInterval==5,"starting difficulty defaults");
        Check(DifficultyCurve.AtScore(rules,149).ActiveFloors==3 && DifficultyCurve.AtScore(rules,150).ActiveFloors==4 &&
            DifficultyCurve.AtScore(rules,300).ActiveFloors==5 && DifficultyCurve.AtScore(rules,450).ActiveFloors==6,"new floors unlock at score thresholds");
        var previous=DifficultyCurve.AtScore(rules,0); bool monotonic=true;
        for(int score=1;score<=2000;score++)
        {
            var next=DifficultyCurve.AtScore(rules,score);
            monotonic &= next.SpawnInterval<=previous.SpawnInterval && next.ActiveFloors>=previous.ActiveFloors &&
                next.MaximumRouteDistance>=previous.MaximumRouteDistance && next.Level>=previous.Level;
            monotonic &= next.ActiveFloors<=6 && next.MaximumRouteDistance<next.ActiveFloors && next.SpawnInterval>=1.5f && next.Level<=6;
            previous=next;
        }
        Check(monotonic,"difficulty grows monotonically and remains bounded for all sampled scores");
        var maximum=DifficultyCurve.AtScore(rules,int.MaxValue);
        Check(maximum.Level==6 && maximum.ActiveFloors==6 && maximum.MaximumRouteDistance==5 && maximum.SpawnInterval==1.5f,"maximum score cannot exceed difficulty limits");
        Check(DifficultyCurve.AtScore(rules,-100).SpawnInterval==5,"negative score input clamps to initial difficulty");
        g=Game();
        for(int trip=0;g.Score<rules.DifficultyScoreCap;trip++)
        {
            foreach(var q in g.Queues.Floors) q.Clear(); g.Lift.Riders.Clear();
            int target=g.Lift.CurrentFloor==0 ? 1:0;
            for(int i=0;i<4;i++) g.Lift.Riders.Add(new Passenger(target,10000));
            g.SelectFloor(target); Run(g,.4f);
        }
        Check(g.Difficulty.Level==6 && g.Difficulty.ActiveFloors==6,"actual deliveries advance difficulty through earned score");
        foreach(var q in g.Queues.Floors) q.Clear(); g.Lift.Riders.Clear();
        g.Rules.InitialSpawnSeconds=5; g.Rules.MinimumSpawnSeconds=1.5f;
        Run(g,300); int highCount=0; bool higherFloor=false,longRoute=false,validRoutes=true;
        for(int f=0;f<6;f++) foreach(var p in g.Queues.Floors[f])
        {
            highCount++; higherFloor|=f>=3; longRoute|=Math.Abs(p.Destination-f)>=4;
            validRoutes &= p.Destination>=0 && p.Destination<6 && p.Destination!=f;
        }
        Check(highCount>=199 && highCount<=201 && highCount>count*3,"earned score produces a higher but capped actual spawn rate");
        Check(higherFloor && longRoute && validRoutes,"high-score spawns reach upper floors and long valid routes");
        var beforeLoss=g.Difficulty; g.Queues.Floors[0].Add(new Passenger(1,.001f)); Run(g,.01f);
        Check(g.Lost==1 && g.Streak==0 && g.Difficulty.Level==beforeLoss.Level,"losing resets streak without rolling back score difficulty");
        float frozen=g.Elapsed; g.Pause(); Run(g,3);
        Check(g.Elapsed==frozen && g.Difficulty.Level==6,"pause freezes a high-difficulty session");
        g=new GameSession(new GameRules(),42);
        Check(g.Score==0 && g.Difficulty.Level==1 && g.Difficulty.ActiveFloors==3,"restart resets difficulty with score");
        var smallRules=new GameRules { Floors=2, StartingFloors=9, StartingRouteDistance=99, DifficultyScoreCap=0 }; smallRules.Validate();
        var small=DifficultyCurve.AtScore(smallRules,10000);
        Check(small.ActiveFloors==2 && small.MaximumRouteDistance==1,"settings validation keeps two-floor projects valid");
        var rng = new Random(17); int simulated = 0;
        for (int session=0;session<25;session++)
        {
            g=new GameSession(new GameRules(),session);
            for(int frame=0;frame<18000 && g.State!=PlayState.GameOver;frame++)
            {
                if(frame%19==0) g.SelectFloor(rng.Next(6));
                if(frame%113==0) { g.Pause(); float t=g.Elapsed; g.Tick(.2f); if(g.Elapsed!=t) throw new Exception("stress pause"); g.Resume(); }
                g.Tick(1f/30f);
                if(g.Lift.Riders.Count>4 || g.Lost>3 || g.Multiplier>3 || g.Lift.Position<0 || g.Lift.Position>5) throw new Exception("stress invariant");
                foreach(var q in g.Queues.Floors) foreach(var p in q) if(p.Remaining<=0) throw new Exception("expired queue rider");
                simulated++;
            }
        }
        Check(simulated>1000,"25 seeded sessions preserve invariants under random taps and pauses");
        Console.WriteLine(passed + " checks passed; stress frames: " + simulated);
    }
}
