using System.Reflection;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OneMoreFloor;

static class Capture
{
 static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
 static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,flags).Invoke(o,args);
 static void Set(object o,string name,object value)=>o.GetType().GetField(name,flags).SetValue(o,value);
 static T Get<T>(object o,string name)=>(T)o.GetType().GetField(name,flags).GetValue(o);
 static float[] C(Color c)=>new[]{c.r,c.g,c.b,c.a};
 static List<object> Export(Transform transform)
 {
  var result=new List<object>();if(!transform.gameObject.activeInHierarchy)return result;
  var r=((RectTransform)transform).Bounds;
  foreach(var component in transform.gameObject.components)
  {
   var d=new Dictionary<string,object>{{"name",transform.gameObject.name},{"x",r.xMin},{"y",1060-r.yMax},{"w",r.width},{"h",r.height}};
   if(component is TMP_Text text){if(string.IsNullOrEmpty(text.text))continue;d["type"]="text";d["text"]=text.text;d["color"]=C(text.color);d["size"]=text.fontSize;d["min"]=text.enableAutoSizing?text.fontSizeMin:text.fontSize;d["align"]=text.alignment.ToString();d["bold"]=text.font.name.Contains("Bold");}
   else if(component is UIShapeImage shape){d["type"]="shape";d["color"]=C(shape.Disabled ? Color.Lerp(shape.color,shape.Dark ? UITheme.Deep : UITheme.Background,shape.Dark ? .26f : .32f) : shape.color);d["radius"]=Math.Min(shape.Radius,Math.Min(r.width,r.height)/2);d["stroke"]=shape.StrokeWidth;d["edge"]=C(shape.StrokeColor);d["shadowOpacity"]=shape.ShadowOpacity;d["shadowBlur"]=shape.ShadowBlur;d["shadowOffset"]=shape.ShadowOffset;}
   else if(component is Image image){d["type"]="image";d["asset"]=image.sprite?.AssetName;d["contain"]=image.preserveAspect;d["color"]=C(image.color);d["radius"]=image.sprite?.AssetName=="panel"?Math.Min(24,Math.Min(r.width,r.height)/2):0;var shadow=image.GetComponent<Shadow>();if(shadow!=null){d["shadow"]=C(shadow.effectColor);d["shadowY"]=-shadow.effectDistance.y;}}
   else if(component is UISurfaceGraphic g){d["type"]="gradient";d["top"]=C(g.Top);d["bottom"]=C(g.Bottom);d["radius"]=Math.Min(g.Radius,Math.Min(r.width,r.height)/2);}
   else if(component is Graphic graphic){d["type"]="mesh";var mesh=graphic.Capture();d["v"]=mesh.Vertices.Select(x=>new{p=new[]{x.position.x,x.position.y},c=C(x.color)}).ToArray();d["tri"]=mesh.Triangles.Select(t=>new[]{t.a,t.b,t.c}).ToArray();}
   else continue;
   var masks=new List<float[]>();for(var t=transform;t!=null;t=t.parent)if(t.GetComponent<RectMask2D>()!=null){var b=((RectTransform)t).Bounds;masks.Add(new[]{b.xMin,1060-b.yMax,b.width,b.height});}if(masks.Count>0)d["clip"]=masks;
   float opacity=1;for(var t=transform;t!=null;t=t.parent){var g=t.GetComponent<CanvasGroup>();if(g!=null)opacity*=g.alpha;}d["opacity"]=opacity;
   result.Add(d);
  }
  foreach(var child in transform.children.ToArray())result.AddRange(Export(child));return result;
 }
 public static class Fonts{public static TMP_FontAsset Regular=new(),Bold=new();}
 public static void Main(string[] args)
 {
  if(args.Length>3){Screen.width=int.Parse(args[2]);Screen.height=int.Parse(args[3]);float top=args.Length>4?float.Parse(args[4]):0,bottom=args.Length>5?float.Parse(args[5]):0;Screen.safeArea=new Rect(0,bottom,Screen.width,Screen.height-top-bottom);}
  string lang=args.Length>1?args[1]:"ru";PlayerPrefs.SetString("OneMoreFloor.Language",lang);PlayerPrefs.SetInt("OneMoreFloor.Campaign.Completed",2);PlayerPrefs.SetInt("OneMoreFloor.Best",1840);PlayerPrefs.SetInt("OneMoreFloor.BoxExplained",1);PlayerPrefs.SetString("OneMoreFloor.Cosmetics.v1","2|160|1082375.1082375.1082375|0|0|0|0");
  var root=new GameObject("Preview root");var app=root.AddComponent<GameApp>();Call(app,"Start");
  string folder=args[0];Directory.CreateDirectory(folder);
  void Save(string name){Set(app,"screenAge",UITheme.ModalIn);Call(app,"Update");
   foreach(var motion in root.GetComponentsInChildren<UIButtonMotion>())for(int i=0;i<12;i++)GameObject.Invoke(motion,"Update");
   foreach(var entrance in root.GetComponentsInChildren<UIPanelEntrance>())for(int i=0;i<20;i++)GameObject.Invoke(entrance,"Update");
   foreach(var exit in root.GetComponentsInChildren<UIWindowMotion>())for(int i=0;i<20;i++)GameObject.Invoke(exit,"Update");
   File.WriteAllText(Path.Combine(folder,name+".json"),JsonSerializer.Serialize(Export(root.transform)));}
  if(args.Length>6 && args[6]=="rooftop")
  {
   Time.unscaledDeltaTime=.05f;Set(app,"currentLevel",10);Set(app,"progress",new CampaignProgress(9));Call(app,"NewGame");Save("roof-start");
   var final=Get<GameSession>(app,"game");typeof(GameSession).GetProperty("Delivered").SetValue(final,final.Level.DeliveryGoal-1);
   int previewScore=0;for(int delivered=0;delivered<final.Delivered;delivered++)previewScore+=10*Math.Min(3,1+delivered/5);
   typeof(GameSession).GetProperty("Score").SetValue(final,previewScore);typeof(GameSession).GetProperty("Streak").SetValue(final,final.Delivered);
   for(int i=0;i<50 && final.ActivePassengerCount==0;i++)Call(app,"Update");for(int i=0;i<6;i++)Call(app,"Update");Save("roof-visitor");
   for(int i=0;i<1000 && final.Lift.Riders.Count==0;i++)
   {for(int f=0;f<final.Queues.Floors.Count;f++)if(final.Queues.Floors[f].Count>0){final.SelectFloor(f);break;}Call(app,"Update");}
   Save("roof-cabin");
   final.SelectFloor(final.RoofFloor);for(int i=0;i<1000 && !final.Finished;i++)Call(app,"Update");Save("roof-boarding");
   for(int i=0;i<36;i++)Call(app,"Update");Save("roof-takeoff");
   for(int i=0;i<30;i++)Call(app,"Update");foreach(var burst in root.GetComponentsInChildren<UIConfetti>())Set(burst,"age",.45f);Save("roof-victory");
   Console.WriteLine("Captured six rooftop source-layout previews; not Unity screenshots.");return;
  }
  if(args.Length>6){Set(app,"cosmetics",Cosmetics.Load("2|"+(args[6]=="cooldown"?160:0)+"|1.1.1|0|0|0|"+(args[6]=="cooldown"?DateTimeOffset.UtcNow.ToUnixTimeSeconds()+86399:0)));Call(app,"ShowWheel",null,0);Save("wheel-"+args[6]);return;}
  Save("menu");Call(app,"ShowLevels");Save("levels");Call(app,"ChooseLevel",3);Save("briefing");Call(app,"ShowWheel",null,0);Save("wheel");Call(app,"ShowCollection",2,3,0);Save("collection");Call(app,"ShowCollection",0,0,0);Save("collection-common");Call(app,"ShowCollection",0,1,0);Save("collection-rare");Call(app,"ShowCollection",1,2,0);Save("collection-epic");Call(app,"ShowCollection",0,0,1);Save("collection-page2");Call(app,"CollectionDetail",2,20,3,0);Save("detail");
  Call(app,"ShowMenu");Call(app,"ShowSettings");Save("settings");Get<RectTransform>(app,"settingsLayer").gameObject.SetActive(false);Call(app,"ShowLanguagePicker",true);Save("language");Get<RectTransform>(app,"languageLayer").gameObject.SetActive(false);
  Call(app,"ShowTutorial",false);for(int i=0;i<4;i++){Set(app,"tutorialPage",i);Call(app,"RenderTutorial");Save("tutorial-"+(i+1));}Call(app,"ShowBoxIntroduction");Save("box");
  Set(app,"currentLevel",3);Call(app,"NewGame");var game=Get<GameSession>(app,"game");
  for(int f=0;f<4;f++)for(int i=0;i<3;i++)game.Queues.TryEnqueue(f,new Passenger((f+1)%4,70,false,(f*3+i)%16,isRaccoon:f==2&&i==0,isDinosaur:f==1&&i==0));
  Call(app,"Render",.3f);Save("game");Call(app,"RenderPauseScreen");Save("pause");
  foreach(var queue in game.Queues.Floors)while(queue.Count>0)game.Queues.TakeFirst(game.Queues.Floors.ToList().IndexOf(queue));game.Lift.Riders.Clear();
  typeof(GameSession).GetProperty("Score").SetValue(game,260);typeof(GameSession).GetProperty("Delivered").SetValue(game,12);typeof(GameSession).GetProperty("State").SetValue(game,PlayState.Won);
  Call(app,"Render",.3f);Call(app,"WinLevel");foreach(var burst in root.GetComponentsInChildren<UIConfetti>())Set(burst,"age",.45f);Save("win");Call(app,"NewGame");game=Get<GameSession>(app,"game");for(int i=0;i<3;i++)game.Queues.TryEnqueue(0,new Passenger(1,.001f));game.Tick(.02f);Call(app,"Render",.3f);Call(app,"EndGame");Save("loss");
  Console.WriteLine("Captured 21 layouts from the actual runtime C# with SDK substitutes. These are not Unity screenshots.");
 }
}
