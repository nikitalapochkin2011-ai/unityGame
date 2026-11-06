using System.Reflection;
using OneMoreFloor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
static class ThemeAchievementTests
{
 static int passed;const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);passed++;}
 static object Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,Flags).Invoke(o,args);
 static T Get<T>(object o,string field)=>(T)o.GetType().GetField(field,Flags).GetValue(o);
 static void Set(object o,string field,object value)=>o.GetType().GetField(field,Flags).SetValue(o,value);
 static double L(Color c){double Linear(float v)=>v<=.04045 ? v/12.92 : Math.Pow((v+.055)/1.055,2.4);return .2126*Linear(c.r)+.7152*Linear(c.g)+.0722*Linear(c.b);}
 static double Contrast(Color a,Color b)=>(Math.Max(L(a),L(b))+.05)/(Math.Min(L(a),L(b))+.05);
 public static int Run(GameApp previousApp)
 {
  for(int id=6;id<16;id++)Check(Get<Achievements>(previousApp,"achievements").Earned(id),"real runtime milestone delivery earned its matching roof achievement: "+id);
  PlayerPrefs.ClearForTests();PlayerPrefs.SetString("OneMoreFloor.Language","ru");PlayerPrefs.SetInt("OneMoreFloor.BoxExplained",1);
  var root=new GameObject("Theme and achievement tests");var app=root.AddComponent<GameApp>();Call(app,"Start");
  Button Button(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.gameObject.name==name);
  Check(!UITheme.IsDark,"first launch remains light");
  var settings=(RectTransform)Button("Menu settings").transform;var flag=(RectTransform)Button("Achievements").transform;
  Check(settings.Bounds.yMin>flag.Bounds.yMax && settings.Bounds.yMax<=1060,"settings is above the achievement flag and both stay inside the screen");
  Check(!Get<RectTransform>(app,"achievementAlert").gameObject.activeSelf,"there is no false pending badge on a fresh profile");
  var oldIllustration=root.GetComponentsInChildren<Image>().Where(x=>x.gameObject.name=="Lobby elevator" || x.gameObject.name=="Lobby window").ToDictionary(x=>x.gameObject.name,x=>(x.sprite.AssetName,x.color.r,x.color.g,x.color.b));
  Button("Menu settings").onClick.Invoke();Button("Theme switch").onClick.Invoke();
  Check(UITheme.IsDark && PlayerPrefs.GetInt("OneMoreFloor.DarkTheme",0)==1 && Get<RectTransform>(app,"settingsLayer").gameObject.activeSelf,"theme changes immediately, saves and retains settings");
  Check(root.GetComponentsInChildren<Image>().Where(x=>oldIllustration.ContainsKey(x.gameObject.name)).All(x=>oldIllustration[x.gameObject.name]==(x.sprite.AssetName,x.color.r,x.color.g,x.color.b)),"theme never recolors lobby artwork or changes its models");
  Check(root.GetComponentsInChildren<Image>().Single(x=>x.gameObject.name=="Evening sky").color.r<.1f,"outer safe-area background becomes near black too");
  foreach(bool dark in new[]{false,true})
  {
   UITheme.UseDark(dark);
   foreach(var pair in new[]{(UITheme.Ink,UITheme.Surface),(UITheme.Muted,UITheme.Surface),(UITheme.Green,UITheme.Surface),(UITheme.Ink,UITheme.Background)})
    Check(Contrast(pair.Item1,pair.Item2)>=4.5,"body and secondary text retain AA contrast in theme "+dark);
   foreach(var fill in new[]{UITheme.Surface,UITheme.Soft,UITheme.Green,UITheme.Terra,UITheme.DarkAction,UITheme.Deep})
    Check(Contrast(UITheme.ContrastText(fill),fill)>=4.5,"button text stays legible on semantic background in theme "+dark);
  }
  UITheme.UseDark(true);Call(app,"ShowMenu");Set(app,"currentLevel",2);Call(app,"NewGame");var game=Get<GameSession>(app,"game");
  var passenger=new Passenger(1,100);game.Lift.Riders.Add(passenger);game.SelectFloor(1);Time.unscaledDeltaTime=.05f;Call(app,"Update");
  Call(app,"ShowSettings");float time=game.Elapsed,position=game.Lift.Position,patience=passenger.Remaining;var profile=Get<Achievements>(app,"achievements");
  Button("Theme switch").onClick.Invoke();
  Check(!UITheme.IsDark && ReferenceEquals(game,Get<GameSession>(app,"game")) && game.State==PlayState.Paused && game.Elapsed==time && game.Lift.Position==position && passenger.Remaining==patience,"switching while paused preserves the exact session, route, patience and elapsed time");
  Check(ReferenceEquals(profile,Get<Achievements>(app,"achievements")),"presentation rebuild retains the same lifetime achievement ledger");
  UITheme.SavePreference(true);var anotherRoot=new GameObject("Reloaded theme");var another=anotherRoot.AddComponent<GameApp>();Call(another,"Start");
  Check(UITheme.IsDark,"dark preference restores on the next app start");
  // Deliver a box, a raccoon and a dinosaur at one winning stop: three people, four spaces.
  Set(app,"achievements",Achievements.Load("1|49|0|0"));Set(app,"cosmetics",new Cosmetics());app.Settings.Levels[1].DeliveryGoal=3;Set(app,"currentLevel",2);Call(app,"NewGame");game=Get<GameSession>(app,"game");
  game.Lift.Riders.Add(new Passenger(1,100,true));game.Lift.Riders.Add(new Passenger(1,100,isRaccoon:true));game.Lift.Riders.Add(new Passenger(1,100,isDinosaur:true));game.SelectFloor(1);
  Check(Get<Achievements>(app,"achievements").TotalDelivered==49,"boarding alone never increments lifetime deliveries");
  for(int frame=0;frame<100 && !game.Finished;frame++)Call(app,"Update");
  Check(game.State==PlayState.Won && Get<Achievements>(app,"achievements").TotalDelivered==52 && Get<Achievements>(app,"achievements").Earned(0),"the winning stop counts all three actual people, including box and rare models");
  for(int frame=0;frame<20;frame++)Call(app,"Update");
  Check(Get<Achievements>(app,"achievements").TotalDelivered==52 && CosmeticStore.LoadAccount().Milestones.TotalDelivered==52,"won frames do not count the same deliveries again and lifetime progress is already saved");
  Call(app,"ShowMenu");Check(Get<RectTransform>(app,"achievementAlert").gameObject.activeSelf,"new unclaimed achievement lights the flag badge");
  Button("Achievements").onClick.Invoke();int before=Get<Cosmetics>(app,"cosmetics").Coins;var claim=Button("Claim achievement 0");claim.onClick.Invoke();claim.onClick.Invoke();
  Check(Get<Cosmetics>(app,"cosmetics").Coins==before+5 && CosmeticStore.LoadAccount().Milestones.Claimed(0) && CosmeticStore.Load().Coins==before+5,"claim click grants five coins once and persists payment together with consumed reward");
  Call(app,"ShowMenu");Check(!Get<RectTransform>(app,"achievementAlert").gameObject.activeSelf,"flag badge clears when the last pending reward is claimed");
  var full=new Achievements();full.RecordDeliveries(2000);for(int n=10;n<=100;n+=10)full.RecordDeliveries(1,n);Set(app,"achievements",full);
  var visited=new HashSet<string>();
  for(int category=0;category<2;category++)for(int page=0;page<(category==0 ? 2 : 3);page++)
  {
   Call(app,"ShowAchievements",category,page);
   foreach(var card in root.GetComponentsInChildren<Image>().Where(x=>x.gameObject.name.StartsWith("Achievement card ")))visited.Add(card.gameObject.name);
  }
  Check(visited.Count==16,"both categories and their pages expose all sixteen separate achievements");
  Set(app,"currentLevel",2);Call(app,"NewGame");game=Get<GameSession>(app,"game");var three=Achievements.Load("1|49|0|0");Set(app,"achievements",three);Call(app,"QueueAchievementNotices",three.RecordDeliveries(151));
  float alpha=0,y=2;bool smooth=true;for(int frame=0;frame<8;frame++)
  {
   Call(app,"TickAchievementNotice",.03f);var next=Get<CanvasGroup>(app,"achievementNoticeOpacity").alpha;var rect=Get<RectTransform>(app,"achievementNotice");smooth &= next>=alpha && rect.anchorMin.y<=y;alpha=next;y=rect.anchorMin.y;
  }
  Check(smooth && alpha>.99f && Get<Queue<int>>(app,"achievementNotices").Count==2,"achievement banners slide in monotonically and multiple unlocks queue without stacking");
  Check(!Get<CanvasGroup>(app,"achievementNoticeOpacity").blocksRaycasts && !Get<RectTransform>(app,"achievementNotice").GetComponent<Image>().raycastTarget,"the notification never intercepts elevator or pause input");
  float age=Get<float>(app,"achievementNoticeAge");game.Pause();Call(app,"TickAchievementNotice",.1f);
  Check(Get<float>(app,"achievementNoticeAge")==age && !Get<RectTransform>(app,"achievementNotice").gameObject.activeSelf,"pause temporarily hides the banner and preserves its remaining display time");game.Resume();
  for(int frame=0;frame<36;frame++)Call(app,"TickAchievementNotice",.1f);
  Check(Get<int>(app,"activeAchievement")==1,"the next queued achievement starts after the first exits");
  Check(game.Elapsed==0,"notification animations never advance the game clock");
  Call(app,"ShowMenu");Call(app,"TickAchievementNotice",.1f);
  Check(Get<Queue<int>>(app,"achievementNotices").Count==0 && !Get<RectTransform>(app,"achievementNotice").gameObject.activeSelf && Get<RectTransform>(app,"achievementAlert").gameObject.activeSelf,"leaving the level dismisses toasts while keeping the persistent pending flag");
  // Debug wheel currency must remain separate from the persisted achievement reward.
  Set(app,"wheelTestMode",true);var sandbox=new Cosmetics();sandbox.AddCoins(2000);Set(app,"cosmetics",sandbox);
  var canonical=new Cosmetics();canonical.AddCoins(17);var pending=new Achievements();pending.RecordDeliveries(50);Set(app,"achievements",pending);CosmeticStore.SaveAccount(canonical,pending);
  Call(app,"ClaimAchievement",0,0,0);
  Check(CosmeticStore.Load().Coins==22 && sandbox.Coins==2005 && CosmeticStore.LoadAccount().Milestones.Claimed(0),"editor wheel preview cannot leak its fake coin balance into an achievement claim");
  Call(app,"SaveCosmetics");Check(CosmeticStore.Load().Coins==22,"subsequent preview saves retain the canonical wallet and claimed flag");
  foreach(var viewport in new[]{(360,640,0,0),(360,800,28,18),(390,844,47,34),(430,932,59,34),(542,678,0,0),(862,667,0,0),(986,659,0,0)})
  {
   Screen.width=viewport.Item1;Screen.height=viewport.Item2;Screen.safeArea=new Rect(0,viewport.Item4,Screen.width,Screen.height-viewport.Item3-viewport.Item4);Call(app,"ApplySafeArea");Call(app,"ShowMenu");
   var utilities=new[]{Button("Menu settings"),Button("Achievements")};
   Check(utilities.All(b=>{var r=((RectTransform)b.transform).Bounds;return r.width*Screen.height/1060>=43.9f && r.height*Screen.height/1060>=43.9f;}),"new circular menu controls keep at least 44 screen pixels of tap area: "+viewport);
   var upper=((RectTransform)utilities[0].transform).Bounds;var lower=((RectTransform)utilities[1].transform).Bounds;
   Check(upper.yMin>lower.yMax && upper.yMax<=Get<RectTransform>(app,"safe").Bounds.yMax+.1f,"stacked controls never overlap or leave the safe area: "+viewport);
   var safeBounds=Get<RectTransform>(app,"safe").Bounds;
   Check(utilities.All(b=>{var r=((RectTransform)b.transform).Bounds;return r.width<=safeBounds.width*.18f && r.height<=safeBounds.height*.10f && Math.Abs(r.width-r.height)<.1f;}),"menu controls remain small circles rather than filling their screen parent: "+viewport);
   foreach(bool dark in new[]{false,true})
   {
    UITheme.UseDark(dark);
    for(int category=0;category<2;category++)for(int page=0;page<(category==0 ? 2 : 3);page++)
    {
     Call(app,"ShowAchievements",category,page);
     var left=((RectTransform)Button("Previous achievements").transform).Bounds;
     var right=((RectTransform)Button("Next achievements").transform).Bounds;
     float cardsBottom=root.GetComponentsInChildren<Image>().Where(x=>x.gameObject.name.StartsWith("Achievement card ")).Min(x=>((RectTransform)x.transform).Bounds.yMin);
     Check(left.xMax<safeBounds.xMin+safeBounds.width*.30f && right.xMin>safeBounds.xMin+safeBounds.width*.70f && left.yMax<cardsBottom && right.yMax<cardsBottom && Math.Abs(left.width-left.height)<.1f && Math.Abs(right.width-right.height)<.1f,
      "achievement arrows stay at the lower corners and never cover cards: "+viewport+" theme "+dark+" category "+category+" page "+page);
    }
   }

  }
  Console.WriteLine(passed+" theme and achievement presentation checks passed with SDK substitutes.");return passed;
 }
}
