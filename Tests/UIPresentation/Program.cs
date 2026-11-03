// Exercises production presentation code with SDK substitutes. This is not a
// Unity/Play Mode test: native input, SDF rendering and lifecycle are unverified.
using System.Reflection;
using OneMoreFloor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

int passed=0;
void Check(bool condition,string name){if(!condition)throw new Exception(name);passed++;}
const BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,flags).Invoke(o,args);
T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,flags).GetValue(o);
void Set(object o,string name,object value)=>o.GetType().GetField(name,flags).SetValue(o,value);
void Step(Behaviour component,int count=20){for(int i=0;i<count;i++)GameObject.Invoke(component,"Update");}
Time.unscaledDeltaTime=.01f;
var ui=new GameUI();var isolated=new GameObject("Button test");int clicks=0;
var button=ui.Button("Action",isolated.transform,"Играть",.1f,.1f,.9f,.2f,()=>clicks++,UITheme.Terra);
var motion=button.GetComponent<UIButtonMotion>();var shape=button.GetComponent<UIShapeImage>();
var pointer=new PointerEventData{pointerId=-1};
motion.OnPointerEnter(pointer);Step(motion);
Check(button.transform.localScale.x==1 && clicks==0,"hover is quiet and never invokes action");
motion.OnPointerDown(pointer);Step(motion);
Check(Math.Abs(button.transform.localScale.x-.98f)<.0001f && clicks==0,"pressed feedback is .98 without invoking gameplay");
motion.OnPointerExit(pointer);Step(motion);
Check(button.transform.localScale.x==1,"dragging off cancels pressed feedback");
motion.OnSelect(new BaseEventData());Step(motion);
Check(shape.Capture().Vertices.Count>0 && button.transform.localScale.x==1,"keyboard selection renders without a scale jump");
motion.OnDeselect(new BaseEventData());button.onClick.Invoke();
Check(clicks==1,"one listener invocation produces exactly one action");
button.interactable=false;Step(motion);motion.OnPointerDown(pointer);Step(motion);
Check(shape.Disabled && button.transform.localScale.x==1 && Math.Abs(button.GetComponent<CanvasGroup>().alpha-.62f)<.001f,"disabled button rejects pressed feedback and dims all content");
button.interactable=true;Step(motion);
Check(!shape.Disabled && button.GetComponent<CanvasGroup>().alpha==1,"button recovers after availability changes");
Check(button.GetComponent<Shadow>()==null,"new buttons have no hard offset shadow component");
var window=new GameObject("Window");var panel=new GameObject("Panel");panel.transform.SetParent(window.transform,false);
var entrance=panel.AddComponent<UIPanelEntrance>();Step(entrance,30);
Check(panel.transform.localScale.x==1 && panel.GetComponent<CanvasGroup>().alpha==1,"modal entrance settles at full opacity and scale");
UIWindowMotion.Hide(window);var exit=window.GetComponent<UIWindowMotion>();
Check(!window.GetComponent<CanvasGroup>().interactable && !window.GetComponent<CanvasGroup>().blocksRaycasts,"close immediately releases input");
Step(exit,3);UIWindowMotion.Show(window);Step(exit,30);Step(entrance,30);
Check(window.activeSelf && !UIWindowMotion.IsClosing(window) && panel.transform.localScale.x==1,"reopen cancels pending dismissal");
UIWindowMotion.Hide(window);Step(exit,20);
Check(!window.activeSelf,"reverse transition completes within 200 ms");
UIWindowMotion.Show(window);Step(exit,8);
Check(window.GetComponent<CanvasGroup>().alpha>0 && window.GetComponent<CanvasGroup>().alpha<1 && window.GetComponent<CanvasGroup>().interactable,"modal and scrim fade together without postponing input");
Step(exit,20);Check(window.GetComponent<CanvasGroup>().alpha==1,"window fade settles at full opacity");

PlayerPrefs.SetString("OneMoreFloor.Language","ru");PlayerPrefs.SetInt("OneMoreFloor.Campaign.Completed",2);PlayerPrefs.SetInt("OneMoreFloor.BoxExplained",1);
var root=new GameObject("App test");var app=root.AddComponent<GameApp>();Call(app,"Start");
Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.gameObject.name==name && b.gameObject.activeInHierarchy);
void Click(string name){var b=Find(name);Check(b.IsInteractable(),name+" accepts input");b.onClick.Invoke();}
Click("All levels");Click("Level 3");Click("Begin level");
var game=Get<GameSession>(app,"game");
Check(game!=null && game.State==PlayState.Playing,"menu and briefing retain start handlers");
Step(Get<RectTransform>(app,"overlay").GetComponent<UIWindowMotion>());
Click("Pause");Check(game.State==PlayState.Paused,"pause handler freezes session");
Click("Continue");Check(game.State==PlayState.Playing && UIWindowMotion.IsClosing(Get<RectTransform>(app,"overlay").gameObject),"resume applies immediately before visual close completes");
Call(app,"ShowMenu");Click("Menu settings");bool oldSound=Get<bool>(app,"sound");Click("Sound switch");
Check(Get<bool>(app,"sound")!=oldSound,"settings toggle still works");Click("Settings close");Step(Get<RectTransform>(app,"settingsLayer").GetComponent<UIWindowMotion>());
Click("Collection");root.GetComponentsInChildren<Button>().First(b=>b.gameObject.name=="Cosmetic card" && b.gameObject.activeInHierarchy).onClick.Invoke();Check(Find("Back to collection")!=null,"collection card opens existing detail handler");Click("Back to collection");
Call(app,"ShowMenu");Click("Wheel");
var ads=Get<RewardedAdsService>(app,"ads");ads.Configure(new RewardedAdsConfiguration{EditorSimulation=true});
long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
Set(app,"cosmetics",Cosmetics.Load("2|160|1.1.1|0|0|0|"+(now+86400)));
Call(app,"RefreshWheel");Check(!Get<Button>(app,"freeSpin").interactable && Get<Button>(app,"paidSpin").interactable,"24h cooldown disables only the ad button");
Check(Get<TMP_Text>(app,"freeCountdown").text.Contains(":"),"ad cooldown displays a countdown");
Set(app,"cosmetics",Cosmetics.Load("2|0|1.1.1|0|0|0|0"));Call(app,"RefreshWheel");
Check(Get<Button>(app,"freeSpin").interactable && !Get<Button>(app,"paidSpin").interactable,"ready ad remains independent of insufficient coin balance");
var free=Get<Button>(app,"freeSpin");var paid=Get<Button>(app,"paidSpin");
Check(!ReferenceEquals(free,paid) && ((RectTransform)free.transform).Bounds.yMax<((RectTransform)paid.transform).Bounds.yMin,"paid and ad spins are separate nonoverlapping controls");
string oldSave=Get<Cosmetics>(app,"cosmetics").Save();free.onClick.Invoke();GameObject.Invoke(ads,"Update");
Check(Get<Cosmetics>(app,"cosmetics").Save()==oldSave,"opening simulated ad without earning never consumes timer or currency");
// Native button delivery is not simulated. Geometry checks use the actual anchor layout.
foreach(var viewport in new[]{(360,640,0,0),(360,800,28,18),(390,844,47,34),(430,932,59,34)})
{
 Screen.width=viewport.Item1;Screen.height=viewport.Item2;Screen.safeArea=new Rect(0,viewport.Item4,Screen.width,Screen.height-viewport.Item3-viewport.Item4);
 Call(app,"ApplySafeArea");
 foreach(var screen in new[]{"ShowMenu","ShowLevels","ShowWheel","ShowCollection"})
 {
  if(screen=="ShowWheel")Call(app,screen,null,0);else if(screen=="ShowCollection")Call(app,screen,0,0,0);else Call(app,screen);
  var safe=Get<RectTransform>(app,"safe").Bounds;
  Check(root.GetComponentsInChildren<Button>().Where(b=>b.gameObject.activeInHierarchy && b.IsInteractable()).All(b=>{
   var r=((RectTransform)b.transform).Bounds;return r.width>0 && r.height>0 && r.xMin>=safe.xMin-.1f && r.xMax<=safe.xMax+.1f && r.yMin>=safe.yMin-.1f && r.yMax<=safe.yMax+.1f;
  }),screen+" buttons stay inside portrait safe area "+viewport);
 }
}
Console.WriteLine(passed+" UI presentation checks passed with SDK substitutes; Unity rendering, native input and lifecycle remain unverified.");
