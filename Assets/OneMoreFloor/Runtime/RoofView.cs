using UnityEngine;
using UnityEngine.UI;
namespace OneMoreFloor
{
    public sealed class RoofView
    {
        public const float BoardingSeconds=1.4f,FlightSeconds=1.45f;
        readonly RectTransform ship;
        readonly CanvasGroup opacity;
        readonly GameObject occupant;
        readonly Image callLight;
        GameSession session;
        float age;
        public bool Complete { get; private set; }
        public RoofView(GameUI ui,RectTransform root,GameRules rules,System.Action<int> select)
        {
            ui.Picture("Rooftop terrace and open headhouse",root,"rooftop_landing",-.005f,.992f,1,1.19f,false);
            ui.Panel("Roof call post",root,.003f,1,.069f,1.175f,GameUI.Ink);
            var landing=ui.Panel("Roof landing tap area",root,.075f,1.005f,.704f,1.16f,new Color(1,1,1,0),false);
            landing.raycastTarget=true;var tap=landing.gameObject.AddComponent<Button>();tap.targetGraphic=landing;
            tap.onClick.AddListener(()=>{ui.ClickSound?.Invoke();select(rules.Floors);});
            var call=ui.Button("Call roof",root,Loc.T("roof.short"),.010f,1.035f,.067f,1.155f,()=>select(rules.Floors),GameUI.Hex("355563"),22);
            callLight=call.GetComponent<Image>();
            var viewport=ui.Rect("Roof flight viewport",root,0,0,1,1.20f);
            viewport.gameObject.AddComponent<RectMask2D>();
            ship=ui.Picture("Waiting flying saucer",viewport,"rooftop_ufo",.265f,1.025f/1.2f,.665f,1.158f/1.2f).rectTransform;
            var window=ui.Rect("Saucer passenger window",ship,.40f,.48f,.60f,.84f);
            window.gameObject.AddComponent<RectMask2D>();
            ui.Picture("Seated alien",window,"person_18",0,-.65f,1,1).color=new Color(.92f,1,.92f,.86f);
            occupant=window.gameObject;occupant.SetActive(false);
            opacity=ship.gameObject.AddComponent<CanvasGroup>();opacity.blocksRaycasts=false;opacity.interactable=false;
            ship.gameObject.SetActive(false);
        }
        public void Reset()
        { session=null;age=0;Complete=false;opacity.alpha=1;ship.gameObject.SetActive(false);occupant.SetActive(false); }
        public void Tick(GameSession game,float dt)
        {
            if(session!=game){Reset();session=game;ship.gameObject.SetActive(game.HasAlienFinale);}
            callLight.color=game.Lift.NextFloor==game.RoofFloor || (game.Lift.Moving && game.Lift.TargetFloor==game.RoofFloor) ? GameUI.Coral :
                game.Lift.CurrentFloor==game.RoofFloor ? GameUI.Teal : GameUI.Hex("355563");
            if(!game.HasAlienFinale)return;
            if(!game.AlienDelivered)
            {GameUI.Place(ship,.265f,1.025f/1.2f,.665f,1.158f/1.2f);return;}
            age+=dt;occupant.SetActive(age>=BoardingSeconds-.15f);
            float flight=Mathf.Clamp01((age-BoardingSeconds)/FlightSeconds),ease=flight*flight;
            float x=.265f+ease*.12f,y=(1.025f+ease*.32f)/1.2f;
            GameUI.Place(ship,x,y,x+.4f,y+.133f/1.2f);
            opacity.alpha=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((flight-.48f)/.52f));
            Complete=age>=BoardingSeconds+FlightSeconds+.1f;
            if(Complete)ship.gameObject.SetActive(false);
        }
    }
}
