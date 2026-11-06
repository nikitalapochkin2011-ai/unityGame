using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    public sealed class RoofView
    {
        public const float BoardingSeconds=1.4f,FlightSeconds=1.45f;
        readonly GameUI ui;
        readonly RectTransform building,frame,ship;
        readonly Image vehicle,boarded,callLight;
        readonly AspectRatioFitter aspect;
        readonly CanvasGroup opacity;
        GameSession session;
        FinaleDefinition finale;
        float age;
        public bool Complete { get; private set; }
        public RoofView(GameUI ui,RectTransform root,GameRules rules,System.Action<int> select)
        {
            this.ui=ui;building=root;
            ui.Picture("Rooftop terrace and open headhouse",root,"rooftop_landing",-.005f,.992f,1,1.19f,false);
            ui.Panel("Roof call post",root,.003f,1,.069f,1.175f,GameUI.Ink);
            var landing=ui.Panel("Roof landing tap area",root,.075f,1.005f,.704f,1.16f,new Color(1,1,1,0),false);
            landing.raycastTarget=true;var tap=landing.gameObject.AddComponent<Button>();tap.targetGraphic=landing;
            tap.onClick.AddListener(()=>{ui.ClickSound?.Invoke();select(rules.Floors);});
            var call=ui.Button("Call roof",root,Loc.T("roof.short"),.010f,1.035f,.067f,1.155f,()=>select(rules.Floors),GameUI.Hex("355563"),22);
            callLight=call.GetComponent<Image>();
            var viewport=ui.Rect("Roof flight viewport",root,0,0,1,1.20f);
            viewport.gameObject.AddComponent<RectMask2D>();
            frame=ui.Rect("Rooftop vehicle flight",viewport,.265f,1.012f/1.2f,.665f,1.19f/1.2f);
            vehicle=ui.Picture("Waiting flying saucer",frame,"rooftop_ufo",0,0,1,1,false);ship=vehicle.rectTransform;
            aspect=ship.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            // The complete occupied illustration supplies the hull occlusion and matching seated pose.
            // Both states have identical canvases; no tiny portrait is pasted over a separate vehicle.
            boarded=ui.Picture("Boarded rooftop vehicle",ship,"rooftop_ufo_boarded",0,0,1,1,false);
            boarded.color=new Color(1,1,1,0);
            opacity=ship.gameObject.AddComponent<CanvasGroup>();opacity.blocksRaycasts=false;opacity.interactable=false;
            ship.gameObject.SetActive(false);
        }
        public void Reset()
        {session=null;finale=null;age=0;Complete=false;opacity.alpha=1;boarded.color=new Color(1,1,1,0);ship.localEulerAngles=Vector3.zero;ship.gameObject.SetActive(false);}
        void Bind(FinaleDefinition definition)
        {
            finale=definition;vehicle.sprite=ui.Art(definition.VehicleAsset);boarded.sprite=ui.Art(definition.BoardedAsset);
            vehicle.gameObject.name=definition.LevelNumber==10 ? "Waiting flying saucer" : "Waiting rooftop vehicle";
            aspect.aspectRatio=vehicle.sprite.rect.width/vehicle.sprite.rect.height;
            ship.gameObject.SetActive(true);PlaceFlight(0,0,0);
        }
        void PlaceFlight(float x,float y,float tilt)
        {
            // Ground every silhouette on the terrace, including narrow tall balloons and wide wings.
            // Derive the frame height from real art and the current viewport instead of hand-tuned seats.
            float height=Mathf.Min(.16f,.4f*building.rect.width/Mathf.Max(1,building.rect.height)/aspect.aspectRatio);
            const float bottom=1.025f;
            GameUI.Place(frame,.265f+x,(bottom+y)/1.2f,.665f+x,(bottom+y+height)/1.2f);
            ship.localEulerAngles=new Vector3(0,0,tilt);
        }
        public void Tick(GameSession game,float dt)
        {
            if(session!=game){Reset();session=game;if(game.HasRooftopFinale)Bind(game.Finale);}
            callLight.color=game.Lift.NextFloor==game.RoofFloor || (game.Lift.Moving && game.Lift.TargetFloor==game.RoofFloor) ? GameUI.Coral :
                game.Lift.CurrentFloor==game.RoofFloor ? GameUI.Teal : GameUI.Hex("355563");
            if(!game.HasRooftopFinale)return;
            if(!game.FinalGuestDelivered){PlaceFlight(0,0,0);return;}
            age+=dt;
            boarded.color=new Color(1,1,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((age-(BoardingSeconds-.18f))/.18f)));
            float flight=Mathf.Clamp01((age-BoardingSeconds)/FlightSeconds),ease=flight*flight,x=0,y=0,tilt=0;
            switch(finale.LevelNumber)
            {
                case 20: x=ease*.30f;y=ease*.24f;tilt=12*flight;break;
                case 30: y=ease*.48f;break;
                case 40: x=-ease*.22f;y=ease*.25f+Mathf.Sin(flight*Mathf.PI)*.018f;tilt=-flight*9;break;
                case 50: float drift=Mathf.SmoothStep(0,1,flight);x=-drift*.12f;y=drift*.22f;tilt=Mathf.Sin(flight*Mathf.PI)*-3;break;
                case 60: x=-ease*.27f;y=ease*.28f;tilt=-7*flight;break;
                case 70: y=Mathf.SmoothStep(0,1,flight)*.30f;x=Mathf.Sin(flight*Mathf.PI)*.018f;tilt=-2*flight;break;
                case 80: y=ease*.37f;x=-ease*.06f;tilt=-5*flight;break;
                case 90: x=-ease*.20f;y=ease*.20f;tilt=Mathf.Sin(flight*Mathf.PI)*-5;break;
                case 100: x=ease*.10f;y=ease*.40f;tilt=3*flight;break;
                default: x=ease*.12f;y=ease*.32f;break;
            }
            PlaceFlight(x,y,tilt);
            opacity.alpha=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((flight-.48f)/.52f));
            Complete=age>=BoardingSeconds+FlightSeconds+.1f;if(Complete)ship.gameObject.SetActive(false);
        }
    }
}
