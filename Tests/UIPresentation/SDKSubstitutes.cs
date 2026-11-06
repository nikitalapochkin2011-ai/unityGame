// SDK substitutes for presentation logic tests. Not a Unity implementation.
// Tests explicitly invoke lifecycle methods; rendering, native input and shaders are not emulated.
using System.Collections;
using System.Reflection;
namespace UnityEngine
{
 [AttributeUsage(AttributeTargets.Class,Inherited=true)]public class RequireComponent:Attribute{public RequireComponent(Type type){}}
 public class CreateAssetMenuAttribute:Attribute{public string menuName;}
 public class Object
 {
  public string name="";public bool _previewDestroyed;
  public static void Destroy(Object obj){if(obj is GameObject g){g.SetActive(false);g.transform.SetParent(null,false);}if(obj!=null)obj._previewDestroyed=true;}
  public static implicit operator bool(Object obj)=>obj!=null && !obj._previewDestroyed;
 }
 public class Component:Object
 {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>()where T:class=>gameObject.GetComponent<T>();
  public T GetComponentInChildren<T>()where T:class=>gameObject.GetComponentInChildren<T>();
  public T[] GetComponentsInChildren<T>()where T:class=>gameObject.GetComponentsInChildren<T>();
 }
 public class Behaviour:Component{public bool enabled=true;}
 public class MonoBehaviour:Behaviour{public Coroutine StartCoroutine(IEnumerator e)=>new Coroutine();}
 public class Coroutine{}
 public class ScriptableObject:Object{public static T CreateInstance<T>()where T:new()=>new T();}
 public class GameObject:Object
 {
  public readonly List<Component> components=new();public readonly RectTransform transform;public bool activeSelf=true;
  public bool activeInHierarchy=>activeSelf && (transform.parent==null || transform.parent.gameObject.activeInHierarchy);
  public GameObject(string value,params Type[] types){name=value;transform=new RectTransform{gameObject=this};components.Add(transform);foreach(var t in types)AddComponent(t);}
  public T AddComponent<T>()where T:Component=>(T)AddComponent(typeof(T));
  public Component AddComponent(Type type)
  {
   if(type==typeof(RectTransform) || type==typeof(Transform))return transform;
   var c=(Component)Activator.CreateInstance(type);c.gameObject=this;components.Add(c);
   if(c is UnityEngine.UI.Graphic && GetComponent<CanvasRenderer>()==null)AddComponent<CanvasRenderer>();
   Invoke(c,"Awake");if(activeInHierarchy)Invoke(c,"OnEnable");return c;
  }
  public static void Invoke(object c,string method)=>c.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(c,null);
  public T GetComponent<T>()where T:class=>components.OfType<T>().FirstOrDefault();
  public T GetComponentInChildren<T>()where T:class=>GetComponentsInChildren<T>().FirstOrDefault();
  public T[] GetComponentsInChildren<T>()where T:class=>components.OfType<T>().Concat(transform.children.Where(x=>x.gameObject.activeSelf).SelectMany(x=>x.gameObject.GetComponentsInChildren<T>())).ToArray();
  public void SetActive(bool value){activeSelf=value;}
 }
 public class Transform:Component,IEnumerable
 {
  public Transform parent;public readonly List<Transform> children=new();public Vector3 localScale=Vector3.one,localEulerAngles;
  public void SetParent(Transform value,bool world=false){parent?.children.Remove(this);parent=value;parent?.children.Add(this);}
  public int GetSiblingIndex()=>parent?.children.IndexOf(this)??0;
  public void SetSiblingIndex(int i){if(parent==null)return;parent.children.Remove(this);parent.children.Insert(Math.Clamp(i,0,parent.children.Count),this);}
  public IEnumerator GetEnumerator()=>children.ToArray().GetEnumerator();
 }
 public class RectTransform:Transform
 {
  public Vector2 anchorMin=Vector2.zero,anchorMax=new(1,1),pivot=new(.5f,.5f),sizeDelta,anchoredPosition;
  public Vector2 offsetMin{get=>anchoredPosition-new Vector2(sizeDelta.x*pivot.x,sizeDelta.y*pivot.y);set{var d=value-offsetMin;sizeDelta-=d;anchoredPosition+=new Vector2(d.x*(1-pivot.x),d.y*(1-pivot.y));}}
  public Vector2 offsetMax{get=>anchoredPosition+new Vector2(sizeDelta.x*(1-pivot.x),sizeDelta.y*(1-pivot.y));set{var d=value-offsetMax;sizeDelta+=d;anchoredPosition+=new Vector2(d.x*pivot.x,d.y*pivot.y);}}
  public Rect Bounds
  {
   get{
    if(parent==null)return new Rect(0,0,1060f*Screen.width/Screen.height,1060);
    var p=((RectTransform)parent).Bounds;var lo=offsetMin;var hi=offsetMax;
    var r=new Rect(p.xMin+p.width*anchorMin.x+lo.x,p.yMin+p.height*anchorMin.y+lo.y,p.width*(anchorMax.x-anchorMin.x)+hi.x-lo.x,p.height*(anchorMax.y-anchorMin.y)+hi.y-lo.y);
    var fitter=GetComponent<UnityEngine.UI.AspectRatioFitter>();
    if(fitter!=null && fitter.aspectMode==UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent){float w=Math.Min(r.width,r.height*fitter.aspectRatio),h=w/fitter.aspectRatio;return new Rect(r.xMin+(r.width-w)/2,r.yMin+(r.height-h)/2,w,h);}
    return r;
   }
  }
  public Rect rect{get{var b=Bounds;return new Rect(-b.width*pivot.x,-b.height*pivot.y,b.width,b.height);}}
 }
 public struct Vector2
 {
  public float x,y;public Vector2(float a,float b){x=a;y=b;}public static Vector2 zero=>new(0,0);
  public static Vector2 operator+(Vector2 a,Vector2 b)=>new(a.x+b.x,a.y+b.y);public static Vector2 operator-(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);
  public static Vector2 operator*(Vector2 a,float b)=>new(a.x*b,a.y*b);public static Vector2 operator/(Vector2 a,float b)=>new(a.x/b,a.y/b);
  public static float Distance(Vector2 a,Vector2 b)=>Mathf.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y));public static Vector2 Lerp(Vector2 a,Vector2 b,float n)=>a+(b-a)*Mathf.Clamp01(n);
  public static implicit operator Vector3(Vector2 a)=>new(a.x,a.y,0);
 }
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new(1,1,1);public static Vector3 zero=>new(0,0,0);public static Vector3 operator*(Vector3 a,float n)=>new(a.x*n,a.y*n,a.z*n);public static implicit operator Vector2(Vector3 a)=>new(a.x,a.y);}
 public struct Vector2Int{public int x,y;public Vector2Int(int a,int b){x=a;y=b;}public static Vector2Int zero=>new(0,0);public static bool operator==(Vector2Int a,Vector2Int b)=>a.x==b.x&&a.y==b.y;public static bool operator!=(Vector2Int a,Vector2Int b)=>!(a==b);public override bool Equals(object v)=>v is Vector2Int b&&this==b;public override int GetHashCode()=>x^y;}
 public struct Vector4{public float x,y,z,w;public Vector4(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}public static Vector4 zero=>new(0,0,0,0);}
 public struct Rect
 {
  public float xMin,yMin,width,height;public Rect(float a,float b,float c,float d){xMin=a;yMin=b;width=c;height=d;}public float x=>xMin;public float y=>yMin;public float xMax=>xMin+width;public float yMax=>yMin+height;public Vector2 center=>new(xMin+width/2,yMin+height/2);
  public static bool operator==(Rect a,Rect b)=>a.xMin==b.xMin&&a.yMin==b.yMin&&a.width==b.width&&a.height==b.height;public static bool operator!=(Rect a,Rect b)=>!(a==b);public override bool Equals(object o)=>o is Rect b&&this==b;public override int GetHashCode()=>width.GetHashCode();
 }
 public struct Color
 {
  public float r,g,b,a;public Color(float x,float y,float z,float alpha=1){r=x;g=y;b=z;a=alpha;}public static Color white=>new(1,1,1);public static Color operator*(Color x,Color y)=>new(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a);
  public static Color Lerp(Color x,Color y,float f){f=Mathf.Clamp01(f);return new(x.r+(y.r-x.r)*f,x.g+(y.g-x.g)*f,x.b+(y.b-x.b)*f,x.a+(y.a-x.a)*f);}
 }
 public static class ColorUtility{public static bool TryParseHtmlString(string s,out Color c){uint n=Convert.ToUInt32(s.TrimStart('#'),16);c=new((n>>16&255)/255f,(n>>8&255)/255f,(n&255)/255f);return true;}}
 public static class Mathf
 {
  public const float PI=(float)Math.PI,Deg2Rad=PI/180;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);public static float Exp(float v)=>(float)Math.Exp(v);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Abs(float v)=>Math.Abs(v);
  public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
  public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float Lerp(float a,float b,float n)=>a+(b-a)*Clamp01(n);
  public static float MoveTowards(float a,float b,float d)=>a+Math.Sign(b-a)*Math.Min(Math.Abs(b-a),d);public static float SmoothStep(float a,float b,float n)=>Lerp(a,b,n*n*(3-2*n));public static float SmoothDamp(float a,float b,ref float v,float smooth,float max,float dt)=>Lerp(a,b,1-Exp(-dt/smooth));
 }
 public enum TextAnchor{UpperLeft,UpperCenter,UpperRight,MiddleLeft,MiddleCenter,MiddleRight,LowerLeft,LowerCenter,LowerRight}
 public enum ScreenOrientation{Portrait}public enum RenderMode{ScreenSpaceOverlay}[Flags]public enum AdditionalCanvasShaderChannels{TexCoord1=1}
 public class Canvas:Behaviour{public RenderMode renderMode;public bool pixelPerfect;public AdditionalCanvasShaderChannels additionalShaderChannels;}
 public class CanvasGroup:Behaviour{public float alpha=1;public bool blocksRaycasts=true,interactable=true;}
 public class CanvasRenderer:Component{public void SetColor(Color color){}}
 public class Sprite:Object{public string AssetName;public Rect rect;}
 public class TextAsset:Object{public string text;public TextAsset(string s){text=s;}}
 public static class Resources
 {
  public static T Load<T>(string path)where T:class
  {if(typeof(T)==typeof(Sprite))
   {
    // PNG header dimensions make sprite aspect checks use the real committed illustrations.
    var bytes=File.ReadAllBytes("Assets/OneMoreFloor/Resources/"+path+".png");
    int w=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4));
    int h=System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4));
    return new Sprite{AssetName=path.Split('/').Last(),rect=new Rect(0,0,w,h)} as T;
   }if(typeof(T)==typeof(TextAsset))return new TextAsset(File.ReadAllText("Assets/OneMoreFloor/Resources/"+path+".txt")) as T;if(typeof(T)==typeof(TMPro.TMP_FontAsset))return new TMPro.TMP_FontAsset{name=path} as T;return null;}
 }
 public static class Screen{public static int width=600,height=1060;public static Rect safeArea=new(0,0,600,1060);public static ScreenOrientation orientation;}
 public static class Application{public static int targetFrameRate;public static string dataPath="Assets";public static bool isFocused=true;}
 public static class Time{public static float unscaledDeltaTime=.3f,unscaledTime=1;}
 public static class PlayerPrefs
 {static Dictionary<string,string> v=new();public static void SetInt(string k,int n)=>v[k]=n.ToString();public static int GetInt(string k,int fallback)=>v.TryGetValue(k,out var s)&&int.TryParse(s,out var n)?n:fallback;public static string GetString(string k,string fallback)=>v.GetValueOrDefault(k,fallback);public static void SetString(string k,string x)=>v[k]=x;public static void Save(){}}
 public static class Debug{public static void LogWarning(string s)=>Console.Error.WriteLine(s);public static void Log(string s)=>Console.Error.WriteLine(s);}
 public class AudioClip:Object{public static AudioClip Create(string n,int a,int b,int c,bool d)=>new AudioClip{name=n};public void SetData(float[] n,int p){}}
 public class AudioSource:Behaviour{public bool playOnAwake;public readonly List<string> Played=new();public void PlayOneShot(AudioClip c){Played.Add(c.name);}public void Stop(){}}
}
namespace UnityEngine.UI
{
 using UnityEngine;
 public class Graphic:Behaviour
 {
  public Color color=Color.white;public bool raycastTarget;public RectTransform rectTransform=>(RectTransform)transform;public CanvasRenderer canvasRenderer=>GetComponent<CanvasRenderer>();
  protected virtual void OnEnable(){}
  public virtual void SetVerticesDirty(){}public virtual void SetAllDirty(){}protected virtual void OnPopulateMesh(VertexHelper h){}
  public VertexHelper Capture(){var h=new VertexHelper();OnPopulateMesh(h);return h;}
 }
 public class MaskableGraphic:Graphic{}
 public class Image:MaskableGraphic{public Sprite sprite;public bool preserveAspect;public Type type;public enum Type{Sliced}}
 public struct ColorBlock{public Color normalColor,highlightedColor,pressedColor,disabledColor,selectedColor;public float fadeDuration;}
 public class Button:Behaviour{public Graphic targetGraphic;public bool interactable=true;public ColorBlock colors;public ClickEvent onClick=new();public bool IsInteractable(){if(!interactable)return false;for(var t=transform;t!=null;t=t.parent){var g=t.GetComponent<CanvasGroup>();if(g!=null&&!g.interactable)return false;}return true;}}
 public class ClickEvent{readonly List<Action> actions=new();public void AddListener(Action a){actions.Add(a);}public void Invoke(){foreach(var action in actions.ToArray())action();}}
 public class Shadow:Behaviour{public Color effectColor;public Vector2 effectDistance;public bool useGraphicAlpha;}
 public class CanvasScaler:Behaviour{public enum ScaleMode{ScaleWithScreenSize}public ScaleMode uiScaleMode;public Vector2 referenceResolution;public float matchWidthOrHeight;}
 public class GraphicRaycaster:Behaviour{}public class RectMask2D:Behaviour{}
 public class AspectRatioFitter:Behaviour{public enum AspectMode{FitInParent}public AspectMode aspectMode;public float aspectRatio;}
 public class VertexHelper
 {public List<(Vector2 position,Color color)> Vertices=new();public List<(int a,int b,int c)> Triangles=new();public int currentVertCount=>Vertices.Count;public void Clear(){Vertices.Clear();Triangles.Clear();}public void AddVert(Vector3 p,Color c,Vector2 uv)=>Vertices.Add((p,c));public void AddTriangle(int a,int b,int c)=>Triangles.Add((a,b,c));}
}
namespace UnityEngine.EventSystems
{public class BaseEventData{}public class PointerEventData:BaseEventData{public int pointerId;}public interface ISelectHandler{void OnSelect(BaseEventData d);}public interface IDeselectHandler{void OnDeselect(BaseEventData d);}public interface IPointerDownHandler{void OnPointerDown(PointerEventData d);}public interface IPointerUpHandler{void OnPointerUp(PointerEventData d);}public interface IPointerEnterHandler{void OnPointerEnter(PointerEventData d);}public interface IPointerExitHandler{void OnPointerExit(PointerEventData d);}}
namespace TMPro
{
 using UnityEngine;using UnityEngine.UI;
 public enum TextAlignmentOptions{TopLeft,Top,TopRight,Left,Center,Right,BottomLeft,Bottom,BottomRight}public enum TextOverflowModes{Truncate}
 public class TMP_FontAsset:Object{public List<TMP_FontAsset> fallbackFontAssetTable=new();}
 public class TMP_Text:MaskableGraphic{public TMP_FontAsset font;public string text="";public float fontSize,fontSizeMin,fontSizeMax;public bool enableAutoSizing,richText;public Vector4 margin;public TextAlignmentOptions alignment;public TextOverflowModes overflowMode;}
 public class TextMeshProUGUI:TMP_Text{}
}
namespace UnityEditor{public static class EditorPrefs{public static bool GetBool(string k,bool v)=>v;}public static class EditorUtility{public static bool DisplayDialog(string a,string b,string c,string d)=>false;}}
