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
        readonly Color roomTint;
        readonly Image[] callLights, rooms;
        readonly Text[] queueCounts;
        readonly Text[] lockedLabels;
        readonly Button[] callButtons, hallButtons;
        readonly Dictionary<Passenger, PassengerView> people = new Dictionary<Passenger, PassengerView>();
        readonly HashSet<Passenger> present = new HashSet<Passenger>();
        readonly List<Passenger> removed = new List<Passenger>();
        readonly List<PassengerView> departures = new List<PassengerView>();
        readonly List<FloatingLabel> labels = new List<FloatingLabel>();
        float doorOpen;
        sealed class FloatingLabel { public Text Text; public Vector2 Position; public float Time; }

        public BuildingView(GameUI ui, RectTransform root, GameRules rules, System.Action<int> select,int floorStyle=0,int liftStyle=0)
        {
            this.ui = ui; this.rules = rules; roomTint=CosmeticStyle.Tint(floorStyle);
            ui.Panel("Building shadow",root,.015f,-.012f,1,1.015f,new Color(0,0,0,.18f));
            ui.Panel("Facade",root,0,0,1,1,GameUI.Hex("D5B89B"));
            ui.Panel("Call panel",root,.003f,.008f,.069f,.992f,GameUI.Ink);
            ui.Panel("Shaft masonry",root,.71f,.004f,.999f,1,GameUI.Hex("B99D85"),false);
            ui.Panel("Elevator shaft",root,.731f,.008f,.982f,.992f,GameUI.Hex("1B3542"),false);
            ui.Panel("Left guide rail",root,.736f,.01f,.742f,.99f,GameUI.Hex("6F8585"),false);
            ui.Panel("Right guide rail",root,.971f,.01f,.978f,.99f,GameUI.Hex("6F8585"),false);
            callLights = new Image[rules.Floors]; rooms = new Image[rules.Floors]; queueCounts = new Text[rules.Floors];
            lockedLabels=new Text[rules.Floors]; callButtons=new Button[rules.Floors]; hallButtons=new Button[rules.Floors];
            for (int f=0; f<rules.Floors; f++)
            {
                int floor = f;
                var row = ui.Rect("Floor "+(f+1),root,0,(float)f/rules.Floors,1,(float)(f+1)/rules.Floors);
                // Every floor uses the same detailed interior and materials.
                var room = ui.Picture("Unified hallway interior",row,"hallway",BuildingLayout.HallLeft,.055f,BuildingLayout.HallRight,.97f,false);
                room.color=roomTint; CosmeticStyle.Detail(ui,row,floorStyle,false);
                rooms[f] = room; room.raycastTarget = true;
                var tap = room.gameObject.AddComponent<Button>(); tap.targetGraphic = room; tap.onClick.AddListener(()=>select(floor));
                hallButtons[f]=tap;
                ui.Panel("Brass lift threshold",row,.704f,.055f,.716f,.88f,GameUI.Hex("C8B183"),false);
                ui.Panel("Floor edge",row,.071f,.01f,.999f,.055f,GameUI.Hex("E5D4B5"),false);
                var call = ui.Button("Call "+(f+1),row,(f+1).ToString(),.01f,.29f,.063f,.83f,()=>select(floor),GameUI.Hex("355563"),28);
                callLights[f] = call.GetComponent<Image>();
                callButtons[f]=call;
                queueCounts[f] = ui.Text("Waiting count",row,"",.003f,.09f,.069f,.27f,12,GameUI.Hex("D1DACD"));
                lockedLabels[f]=ui.Text("Locked floor",row,"",.17f,.37f,.63f,.66f,19,GameUI.Ink,TextAnchor.MiddleCenter,true);
            }
            cabin = ui.Rect("Moving elevator",root,BuildingLayout.LiftLeft,0,BuildingLayout.LiftRight,1f/rules.Floors);
            ui.Picture("Cabin",cabin,"cabin",0,.025f,1,.99f,false).color=CosmeticStyle.Tint(liftStyle);
            CosmeticStyle.Detail(ui,cabin,liftStyle,true);
            cabinDisplay = ui.Text("Cabin floor display",cabin,"1",.29f,.87f,.71f,.98f,18,GameUI.Gold,TextAnchor.MiddleCenter,true);
            cabinDisplay.fontSizeMin=10;
            actors = ui.Rect("People",root,0,0,1,1);
            doors = ui.Rect("Moving glass doors",root,BuildingLayout.DoorLeft,.065f/rules.Floors,BuildingLayout.DoorRight,.855f/rules.Floors);
            doors.gameObject.AddComponent<RectMask2D>();
            leftDoor = ui.Picture("Left door",doors,"door",0,0,.5f,1,false).rectTransform;
            leftDoor.GetComponent<Image>().color=CosmeticStyle.Tint(liftStyle);
            rightDoor = ui.Picture("Right door",doors,"door",.5f,0,1,1,false).rectTransform;
            rightDoor.GetComponent<Image>().color=CosmeticStyle.Tint(liftStyle);
            // Door edges sit above the people, while transparent glass keeps destinations readable.
            ui.Picture("Roof",root,"roof",-.005f,1.0f,1,1.085f,false);
            ui.Text("Building name",root,Loc.T("building.name"),.35f,1.046f,.7f,1.08f,17,GameUI.Ink,TextAnchor.MiddleCenter,true);
            ui.Panel("Foundation",root,-.005f,-.025f,1,.003f,GameUI.Hex("45636A"));
        }
        public void Reset()
        {
            foreach (var view in people.Values) view.Destroy(); people.Clear();
            foreach (var view in departures) view.Destroy(); departures.Clear();
            foreach (var label in labels) { label.Text.gameObject.SetActive(false); Object.Destroy(label.Text.gameObject); } labels.Clear();
            doorOpen=0;
        }
        PassengerView View(Passenger passenger)
        {
            PassengerView result;
            if (!people.TryGetValue(passenger,out result))
            {
                result=new PassengerView(ui,actors,passenger.Appearance); people.Add(passenger,result);
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
            GameUI.Place(cabin,BuildingLayout.LiftLeft,liftY,BuildingLayout.LiftRight,liftY+floorHeight);
            GameUI.Place(doors,BuildingLayout.DoorLeft,liftY+.065f*floorHeight,BuildingLayout.DoorRight,liftY+.855f*floorHeight);
            doorOpen = game.Lift.Moving ? 0 : Mathf.MoveTowards(doorOpen,1,dt*6);
            GameUI.Place(leftDoor,-doorOpen*.46f,0,.5f-doorOpen*.46f,1);
            GameUI.Place(rightDoor,.5f+doorOpen*.46f,0,1+doorOpen*.46f,1);
            cabinDisplay.text=(game.Lift.Moving ? "→ " : "")+(game.Lift.Moving ? game.Lift.TargetFloor+1 : game.Lift.CurrentFloor+1);
            for(int f=0;f<rules.Floors;f++)
            {
                var queue=game.Queues.Floors[f]; bool active=f<game.Difficulty.ActiveFloors;
                callButtons[f].interactable=hallButtons[f].interactable=active;
                lockedLabels[f].gameObject.SetActive(!active);
                lockedLabels[f].text=Loc.T("building.locked",f+1,f<3 ? 1 : f);
                bool selected=game.Lift.NextFloor==f || (game.Lift.Moving && game.Lift.TargetFloor==f);
                callLights[f].color=selected ? GameUI.Coral : (game.Lift.CurrentFloor==f && !game.Lift.Moving ? GameUI.Teal : GameUI.Hex("355563"));
                rooms[f].color = roomTint * (!active ? new Color(.48f,.55f,.53f,1) : (selected ? new Color(1,.98f,.92f,1) : Color.white));
                queueCounts[f].text=active ? queue.Count+"/"+game.QueueLimit : "—";
                queueCounts[f].color=queue.Count>=game.QueueLimit ? GameUI.Gold : GameUI.Hex("D1DACD");
                for(int i=0;i<queue.Count;i++)
                {
                    var view=View(queue[i]);
                    float x=BuildingLayout.WaitingX(i);
                    float y=(f+.08f)*floorHeight;
                    view.Set(queue[i],new Vector2(x,y),new Vector2(BuildingLayout.PersonWidth,floorHeight*BuildingLayout.PersonHeight),false,queue[i].Remaining/game.PassengerPatience);
                }
            }
            int occupiedSlot=0;
            for(int i=0;i<game.Lift.Riders.Count;i++)
            {
                var p=game.Lift.Riders[i];
                View(p).Set(p,new Vector2(BuildingLayout.RiderX(occupiedSlot,p.SpaceRequired,rules.Capacity),liftY+.105f*floorHeight),
                    new Vector2(BuildingLayout.RiderWidth(p.SpaceRequired,rules.Capacity),floorHeight*.72f),true,1);
                occupiedSlot+=p.SpaceRequired;
            }
            removed.Clear();
            foreach(var pair in people)
            {
                if (present.Contains(pair.Key)) continue;
                var view=pair.Value; bool delivered=view.WasRiding;
                var center=(view.Root.anchorMin+view.Root.anchorMax)*.5f;
                if(delivered) { view.Leave(true); departures.Add(view); }
                else view.Destroy(); // A replacement must not produce an eighth waiting silhouette.
                removed.Add(pair.Key);
                Float(Loc.T(delivered ? "building.thanks" : "building.left"),center,delivered ? GameUI.Teal : GameUI.Coral);
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
