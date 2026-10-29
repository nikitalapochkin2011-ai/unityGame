using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed class BuildingView
    {
        readonly GameUI ui;
        readonly GameRules rules;
        readonly RectTransform actors, cabin, doors, leftDoor, rightDoor;
        readonly Text cabinDisplay;
        readonly Image[] callLights, rooms, windows;
        readonly Text[] queueCounts;
        readonly Dictionary<Passenger, PassengerView> people = new Dictionary<Passenger, PassengerView>();
        readonly HashSet<Passenger> present = new HashSet<Passenger>();
        readonly List<Passenger> removed = new List<Passenger>();
        readonly List<PassengerView> departures = new List<PassengerView>();
        readonly List<FloatingLabel> labels = new List<FloatingLabel>();
        int appearance;
        float doorOpen;
        sealed class FloatingLabel { public Text Text; public Vector2 Position; public float Time; }

        public BuildingView(GameUI ui, RectTransform root, GameRules rules, System.Action<int> select)
        {
            this.ui = ui; this.rules = rules;
            ui.Panel("Building shadow",root,.035f,-.012f,.997f,1.015f,new Color(0,0,0,.18f));
            ui.Panel("Facade",root,0,0,.97f,1,GameUI.Hex("D5B89B"));
            ui.Panel("Call panel",root,.008f,.008f,.143f,.992f,GameUI.Ink);
            ui.Panel("Shaft masonry",root,.65f,.004f,.965f,1,GameUI.Hex("B99D85"),false);
            ui.Panel("Elevator shaft",root,.675f,.008f,.946f,.992f,GameUI.Hex("1B3542"),false);
            ui.Panel("Left guide rail",root,.681f,.01f,.69f,.99f,GameUI.Hex("6F8585"),false);
            ui.Panel("Right guide rail",root,.931f,.01f,.94f,.99f,GameUI.Hex("6F8585"),false);
            callLights = new Image[rules.Floors]; rooms = new Image[rules.Floors]; windows = new Image[rules.Floors]; queueCounts = new Text[rules.Floors];
            for (int f=0; f<rules.Floors; f++)
            {
                int floor = f;
                var row = ui.Rect("Floor "+(f+1),root,0,(float)f/rules.Floors,1,(float)(f+1)/rules.Floors);
                var room = ui.Panel("Hallway",row,.153f,.055f,.648f,.97f,
                    f%2==0 ? GameUI.Hex("EAD7BB") : GameUI.Hex("E3CCAD"),false);
                rooms[f] = room; room.raycastTarget = true;
                var tap = room.gameObject.AddComponent<Button>(); tap.targetGraphic = room; tap.onClick.AddListener(()=>select(floor));
                ui.Panel("Skirting",row,.153f,.055f,.648f,.12f,GameUI.Hex("B69274"),false);
                ui.Panel("Floor edge",row,.145f,.01f,.962f,.055f,GameUI.Hex("F7E6CA"),false);
                windows[f] = ui.Picture("Apartment window",row,"window",.175f,.31f,.355f,.91f);
                ui.Panel("Picture frame",row,.445f,.59f,.535f,.88f,GameUI.Hex("AE8565"));
                ui.Panel("Print paper",row,.455f,.615f,.525f,.855f,GameUI.Cream);
                ui.Panel("Print sunset",row,.47f,.68f,.511f,.79f,GameUI.Gold);
                ui.Picture("Plant",row,"plant",.557f,.13f,.636f,.56f);
                ui.Panel("Wall lamp",row,.592f,.73f,.618f,.84f,GameUI.Gold);
                ui.Panel("Warm lamplight",row,.567f,.55f,.64f,.75f,new Color(1,.8f,.5f,.12f));
                var call = ui.Button("Call "+(f+1),row,(f+1).ToString(),.022f,.29f,.127f,.83f,()=>select(floor),GameUI.Hex("355563"),34);
                callLights[f] = call.GetComponent<Image>();
                queueCounts[f] = ui.Text("Waiting count",row,"",.008f,.09f,.14f,.27f,14,GameUI.Hex("D1DACD"));
            }
            cabin = ui.Rect("Moving elevator",root,.659f,0,.962f,1f/rules.Floors);
            ui.Picture("Cabin",cabin,"cabin",0,.025f,1,.99f,false);
            cabinDisplay = ui.Text("Cabin floor display",cabin,"1",.29f,.87f,.71f,.98f,18,GameUI.Gold,TextAnchor.MiddleCenter,true);
            cabinDisplay.fontSizeMin=10;
            actors = ui.Rect("People",root,0,0,1,1);
            doors = ui.Rect("Moving glass doors",root,.689f,.065f/rules.Floors,.933f,.855f/rules.Floors);
            doors.gameObject.AddComponent<RectMask2D>();
            leftDoor = ui.Picture("Left door",doors,"door",0,0,.5f,1,false).rectTransform;
            rightDoor = ui.Picture("Right door",doors,"door",.5f,0,1,1,false).rectTransform;
            // Door edges sit above the people, while transparent glass keeps destinations readable.
            ui.Picture("Roof",root,"roof",-.02f,1.0f,.99f,1.085f,false);
            ui.Text("Building name",root,"ДОМ № 6",.31f,1.046f,.66f,1.08f,17,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Panel("Foundation",root,-.012f,-.025f,.985f,.003f,GameUI.Hex("45636A"));
        }
        public void Reset()
        {
            foreach (var view in people.Values) view.Destroy(); people.Clear();
            foreach (var view in departures) view.Destroy(); departures.Clear();
            foreach (var label in labels) { label.Text.gameObject.SetActive(false); Object.Destroy(label.Text.gameObject); } labels.Clear();
            appearance=0; doorOpen=0;
        }
        PassengerView View(Passenger passenger)
        {
            PassengerView result;
            if (!people.TryGetValue(passenger,out result))
            {
                result=new PassengerView(ui,actors,appearance++); people.Add(passenger,result);
            }
            present.Add(passenger); return result;
        }
        public void Render(GameSession game, float dt)
        {
            present.Clear(); float floorHeight=1f/rules.Floors;
            float visualPosition=game.Lift.Position;
            if(game.Lift.Moving)
            {
                float distance=Mathf.Abs(game.Lift.TargetFloor-game.Lift.CurrentFloor);
                float progress=distance>0 ? Mathf.Abs(game.Lift.Position-game.Lift.CurrentFloor)/distance : 1;
                visualPosition=Mathf.Lerp(game.Lift.CurrentFloor,game.Lift.TargetFloor,Mathf.SmoothStep(0,1,progress));
            }
            float liftY=visualPosition*floorHeight;
            GameUI.Place(cabin,.659f,liftY,.962f,liftY+floorHeight);
            GameUI.Place(doors,.689f,liftY+.065f*floorHeight,.933f,liftY+.855f*floorHeight);
            doorOpen = game.Lift.Moving ? 0 : Mathf.MoveTowards(doorOpen,1,dt*6);
            GameUI.Place(leftDoor,-doorOpen*.46f,0,.5f-doorOpen*.46f,1);
            GameUI.Place(rightDoor,.5f+doorOpen*.46f,0,1+doorOpen*.46f,1);
            cabinDisplay.text=(game.Lift.Moving ? "→ " : "")+(game.Lift.Moving ? game.Lift.TargetFloor+1 : game.Lift.CurrentFloor+1);
            for(int f=0;f<rules.Floors;f++)
            {
                var queue=game.Queues.Floors[f]; bool active=f<game.Difficulty.ActiveFloors;
                bool selected=game.Lift.NextFloor==f || (game.Lift.Moving && game.Lift.TargetFloor==f);
                callLights[f].color=selected ? GameUI.Coral : (game.Lift.CurrentFloor==f && !game.Lift.Moving ? GameUI.Teal : GameUI.Hex("355563"));
                rooms[f].color = !active ? GameUI.Hex("BDB8A9") : (selected ? GameUI.Hex("F0DEBF") : (f%2==0 ? GameUI.Hex("EAD7BB") : GameUI.Hex("E3CCAD")));
                windows[f].color=active ? Color.white : new Color(.7f,.75f,.75f,.65f);
                queueCounts[f].text=queue.Count==0 ? (active ? "" : "ПОЗЖЕ") : "ЖДУТ: "+queue.Count;
                int rows=queue.Count>5 ? 2 : 1;
                int columns=Mathf.Max(5,Mathf.CeilToInt(queue.Count/(float)rows));
                float spacing=.445f/columns;
                for(int i=0;i<queue.Count;i++)
                {
                    var view=View(queue[i]);
                    float x=.185f+spacing*.5f+(i%columns)*spacing;
                    float y=(f+(rows==1 ? .08f : .025f)+(i/columns)*.485f)*floorHeight;
                    view.Set(queue[i],new Vector2(x,y),new Vector2(Mathf.Min(.08f,spacing*.94f),floorHeight*(rows==1 ? .79f : .45f)),false,queue[i].Remaining/rules.PatienceSeconds);
                }
            }
            for(int i=0;i<game.Lift.Riders.Count;i++)
            {
                var p=game.Lift.Riders[i]; float spacing=.23f/rules.Capacity;
                View(p).Set(p,new Vector2(.696f+spacing*(i+.5f),liftY+.105f*floorHeight),
                    new Vector2(Mathf.Min(.065f,spacing*.98f),floorHeight*.72f),true,1);
            }
            removed.Clear();
            foreach(var pair in people)
            {
                if (present.Contains(pair.Key)) continue;
                var view=pair.Value; bool delivered=view.WasRiding;
                view.Leave(delivered); departures.Add(view); removed.Add(pair.Key);
                var center=(view.Root.anchorMin+view.Root.anchorMax)*.5f;
                Float(delivered ? "Спасибо!" : "Ушёл…",center,delivered ? GameUI.Teal : GameUI.Coral);
            }
            foreach(var p in removed) people.Remove(p);
            foreach(var view in people.Values) view.Tick(dt);
            for(int i=departures.Count-1;i>=0;i--)
            {
                departures[i].Tick(dt);
                if(departures[i].Finished) { departures[i].Destroy(); departures.RemoveAt(i); }
            }
            for(int i=labels.Count-1;i>=0;i--)
            {
                var label=labels[i]; label.Time+=dt;
                float y=label.Position.y+label.Time*.045f;
                GameUI.Place(label.Text.rectTransform,label.Position.x-.1f,y,label.Position.x+.1f,y+.035f);
                var color=label.Text.color; color.a=Mathf.Clamp01(1-label.Time/1.1f); label.Text.color=color;
                if(label.Time>=1.1f) { Object.Destroy(label.Text.gameObject); labels.RemoveAt(i); }
            }
        }
        void Float(string text,Vector2 position,Color color)
        {
            var label=ui.Text("Passenger feedback",actors,text,0,0,0,0,18,color,TextAnchor.MiddleCenter,true);
            labels.Add(new FloatingLabel { Text=label, Position=position });
        }
    }
}
