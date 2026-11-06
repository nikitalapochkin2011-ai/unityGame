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
Step(exit,30);Check(window.GetComponent<CanvasGroup>().alpha==1,"window fade settles at full opacity");

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
// Intermediate frames must stay monotonic, not merely finish at the right endpoint.
var smoothPanel=new GameObject("Smooth modal test");var smooth=smoothPanel.AddComponent<UIPanelEntrance>();
float previousAlpha=0,previousScale=0;bool monotonic=true;Time.unscaledDeltaTime=.01f;
for(int i=0;i<35;i++){GameObject.Invoke(smooth,"Update");float a=smoothPanel.GetComponent<CanvasGroup>().alpha,k=smoothPanel.transform.localScale.x;monotonic &= a>=previousAlpha && a<=1 && k>=previousScale && k<=1;previousAlpha=a;previousScale=k;}
Check(monotonic && previousAlpha==1 && previousScale==1,"modal opacity and scale are monotonic across intermediate frames without spring overshoot");
// Drive a real level-10 session through the runtime presentation bridge.
Screen.width=600;Screen.height=1060;Screen.safeArea=new Rect(0,0,600,1060);
Time.unscaledDeltaTime=.05f;Set(app,"currentLevel",10);Set(app,"progress",new CampaignProgress(9));Set(app,"sound",true);
app.Settings.Levels[9].DeliveryGoal=1;Call(app,"NewGame");game=Get<GameSession>(app,"game");
var saucer=root.GetComponentsInChildren<Image>().Single(x=>x.gameObject.name=="Waiting flying saucer");
Check(saucer.gameObject.activeInHierarchy,"saucer is waiting on the roof from the start of level 10");
var source=Get<AudioSource>(app,"audioSource");int bellStart=source.Played.Count(x=>x=="Soft lift bell");
for(int frame=0;frame<1800 && !game.Finished;frame++)
{
 if(!game.Lift.Moving)
 {
  if(game.Lift.Riders.Count>0)game.SelectFloor(game.Lift.Riders[0].Destination);
  else for(int f=0;f<game.Queues.Floors.Count;f++)if(game.Queues.Floors[f].Count>0){game.SelectFloor(f);break;}
 }
 Call(app,"Update");foreach(var m in root.GetComponentsInChildren<UIWindowMotion>())GameObject.Invoke(m,"Update");
}
Check(game.State==PlayState.Won && game.AlienDelivered && Get<bool>(app,"awaitingRoofFinale"),"roof delivery starts the boarding epilogue before the victory panel");
Check(Get<CampaignProgress>(app,"progress").Completed==10,"level progress is saved before the cinematic finishes");
Check(source.Played.Count(x=>x=="Soft lift bell")>bellStart,"real arrival event reaches the gentle bell cue");
Check(!root.GetComponentsInChildren<UIConfetti>().Any(),"confetti waits until the ship departs and the victory panel appears");
for(int i=0;i<70;i++)Call(app,"Update");
var burst=root.GetComponentsInChildren<UIConfetti>().Single();
Check(!Get<bool>(app,"awaitingRoofFinale") && !saucer.gameObject.activeSelf && source.Played.Count(x=>x=="Airy celebration")==1,"ship departs once, then victory plays exactly one airy celebration");
Set(burst,"age",.48f);var particles=burst.Capture();
Check(!burst.raycastTarget && particles.Vertices.Count>0 && particles.Vertices.Count<=256 && particles.Triangles.All(t=>t.a>=0 && t.b<particles.Vertices.Count && t.c<particles.Vertices.Count) && particles.Vertices.All(v=>float.IsFinite(v.position.x) && float.IsFinite(v.position.y)),"two-sided confetti has a finite small mesh and cannot capture taps");
for(int i=0;i<10;i++)Call(app,"Update");
Check(source.Played.Count(x=>x=="Airy celebration")==1,"later won frames do not replay victory audio");
int played=source.Played.Count;Set(app,"sound",false);Call(app,"WinLevel");
Check(source.Played.Count==played,"muting sound also mutes the new celebration cue");
Call(app,"NewGame");Set(app,"currentLevel",1);Call(app,"NewGame");
Check(!root.GetComponentsInChildren<Image>().Any(x=>x.gameObject.name=="Waiting flying saucer" && x.gameObject.activeInHierarchy),"saucer is absent outside the level-10 finale");
var notice=new GameObject("Notice test");var fade=notice.AddComponent<UIToastMotion>();
Check(notice.GetComponent<CanvasGroup>().alpha==0 && !notice.GetComponent<CanvasGroup>().blocksRaycasts,"new transient notices begin transparent and do not block input");
Step(fade,5);Check(notice.GetComponent<CanvasGroup>().alpha>.9f,"transient notice fades in smoothly");
// Every new milestone must bind unique art, animate the same retained guest and end once.
var flightPositions=new List<string>();
foreach(int number in new[]{20,30,40,50,60,70,80,90,100})
{
 Time.unscaledDeltaTime=.05f;Set(app,"currentLevel",number);Set(app,"progress",new CampaignProgress(number-1));Set(app,"sound",true);
 app.Settings.Levels[number-1].DeliveryGoal=1;Call(app,"NewGame");game=Get<GameSession>(app,"game");
 var craft=root.GetComponentsInChildren<Image>().Single(x=>x.gameObject.name=="Waiting rooftop vehicle");
 var rider=craft.transform.children.Single(t=>t.gameObject.name=="Boarded rooftop vehicle").gameObject.GetComponent<Image>();
 Check(craft.gameObject.activeInHierarchy && craft.sprite.AssetName==game.Finale.VehicleAsset,"correct unique craft waits from the start of level "+number);
 Check(rider.sprite.AssetName==game.Finale.BoardedAsset && rider.color.a==0,"complete occupied artwork is transparent until boarding: "+number);
 int celebrations=source.Played.Count(x=>x=="Airy celebration");
 for(int frame=0;frame<1800 && !game.Finished;frame++)
 {
  if(!game.Lift.Moving)
  {
   if(game.Lift.Riders.Count>0)game.SelectFloor(game.Lift.Riders[0].Destination);
   else for(int f=0;f<game.Queues.Floors.Count;f++)if(game.Queues.Floors[f].Count>0){game.SelectFloor(f);break;}
  }
  Call(app,"Update");foreach(var m in root.GetComponentsInChildren<UIWindowMotion>())GameObject.Invoke(m,"Update");
 }
 Check(game.State==PlayState.Won && game.FinalGuestDelivered && Get<bool>(app,"awaitingRoofFinale") && Get<CampaignProgress>(app,"progress").Completed==number,"roof epilogue starts and progress saves before departure: "+number);
 Check(!root.GetComponentsInChildren<UIConfetti>().Any(),"victory particles wait for the matching departure: "+number);
 for(int frame=0;frame<40;frame++)Call(app,"Update");
 Check(rider.gameObject.activeInHierarchy && rider.color.a==1,"the matching guest appears naturally seated in the complete occupied illustration: "+number);
 Check(rider.sprite.rect.width==craft.sprite.rect.width && rider.sprite.rect.height==craft.sprite.rect.height,"boarding artwork has the same registration canvas: "+number);
 var flight=(RectTransform)craft.transform.parent;
 flightPositions.Add(flight.anchorMin.x+"/"+flight.anchorMin.y+"/"+craft.transform.localEulerAngles.z);
 Check(craft.GetComponent<AspectRatioFitter>().aspectRatio>0 && ((RectTransform)craft.transform).Bounds.width>0 && ((RectTransform)craft.transform).Bounds.height>0,"craft aspect ratio is valid and geometry is nonempty: "+number);
 for(int frame=0;frame<40;frame++)Call(app,"Update");
 Check(!Get<bool>(app,"awaitingRoofFinale") && !craft.gameObject.activeSelf && source.Played.Count(x=>x=="Airy celebration")==celebrations+1 && root.GetComponentsInChildren<UIConfetti>().Count()==1,"one departure, one victory sound and one confetti burst: "+number);
 for(int frame=0;frame<10;frame++)Call(app,"Update");
 Check(source.Played.Count(x=>x=="Airy celebration")==celebrations+1,"completed frames never replay the milestone celebration: "+number);
 if(number==50)
 {
  root.GetComponentsInChildren<Button>().Single(b=>b.gameObject.name=="Advance").onClick.Invoke();
  Check(Get<int>(app,"currentLevel")==51 && root.GetComponentsInChildren<Button>().Any(b=>b.gameObject.name=="Begin level"),"level 50 advances into the new season instead of ending the campaign");
 }

}
Check(flightPositions.Distinct().Count()==9,"all nine non-alien aircraft have distinct takeoff motion");
Check(Get<CampaignProgress>(app,"progress").Completed==100 && root.GetComponentsInChildren<Button>().Any(b=>b.gameObject.name=="Advance"),"level 100 keeps its final campaign victory controls and progress");
root.GetComponentsInChildren<Button>().Single(b=>b.gameObject.name=="Advance").onClick.Invoke();
Check(Get<int>(app,"levelPage")==19 && root.GetComponentsInChildren<Button>().Any(b=>b.gameObject.name=="Level 100") && !root.GetComponentsInChildren<Button>().Single(b=>b.gameObject.name=="Next levels").interactable,"the final campaign action opens page 20 and cannot navigate beyond level 100");
var retainedAds=app.Settings.Ads;var retainedRules=app.Settings.Rules;
var originalSeason=app.Settings.Levels.Take(50).Select(l=>l.Copy()).ToArray();originalSeason[7].Title="Retained custom title";originalSeason[7].PatienceSeconds=287;
app.Settings.Levels=originalSeason;app.Settings.CampaignBalanceVersion=2;app.Settings.Prepare();
Check(app.Settings.Levels.Length==100 && app.Settings.Levels[7].Title=="Retained custom title" && app.Settings.Levels[7].PatienceSeconds==287 && ReferenceEquals(retainedAds,app.Settings.Ads) && ReferenceEquals(retainedRules,app.Settings.Rules),"actual settings migration appends 50 stages while retaining edits, ads and common lift rules");
Console.WriteLine(passed+" UI presentation checks passed with SDK substitutes; Unity rendering, native input and lifecycle remain unverified.");
