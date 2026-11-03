using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    // Small UI factory shared by the screens and building; all positions are local anchors.
    public sealed class GameUI
    {
        public static readonly Color Ink = Hex("233E3B"), Teal = Hex("2E776C"), Coral = Hex("BC5A42");
        public static readonly Color Cream = Hex("FFFDF5"), Muted = Hex("64766D"), Gold = Hex("E7BC70");
        public static readonly Color Paper=Hex("F4F0E6"), Soft=Hex("E5EBDF"), Border=Hex("DBE3D6"), Deep=Hex("183B35");
        readonly TMP_FontAsset regularFont = Resources.Load<TMP_FontAsset>("Fonts/GameLatin SDF");
        readonly TMP_FontAsset boldFont = Resources.Load<TMP_FontAsset>("Fonts/GameLatin Bold SDF");
        public GameUI()
        {
            var chinese=Resources.Load<TMP_FontAsset>("Fonts/GameHan SDF");
            var chineseBold=Resources.Load<TMP_FontAsset>("Fonts/GameHan Bold SDF");
            if(regularFont==null || boldFont==null || chinese==null || chineseBold==null)
                throw new System.InvalidOperationException("Шрифты ещё не подготовлены. Остановите Play и выполните: Ещё один этаж → Подготовить качественный текст.");
            // Native language names include Chinese even before a language is selected.
            if(regularFont.fallbackFontAssetTable==null) regularFont.fallbackFontAssetTable=new List<TMP_FontAsset>();
            if(boldFont.fallbackFontAssetTable==null) boldFont.fallbackFontAssetTable=new List<TMP_FontAsset>();
            if(!regularFont.fallbackFontAssetTable.Contains(chinese)) regularFont.fallbackFontAssetTable.Add(chinese);
            if(!boldFont.fallbackFontAssetTable.Contains(chineseBold)) boldFont.fallbackFontAssetTable.Add(chineseBold);
        }
        public System.Action ClickSound;
        readonly Dictionary<string, Sprite> art = new Dictionary<string, Sprite>();
        public static Color Hex(string value) { Color color; ColorUtility.TryParseHtmlString("#" + value, out color); return color; }
        public Sprite Art(string name)
        {
            Sprite sprite;
            if (!art.TryGetValue(name, out sprite))
            {
                sprite = Resources.Load<Sprite>("Art/" + name);
                if (sprite == null) throw new System.InvalidOperationException("Missing game sprite: Resources/Art/" + name);
                art.Add(name, sprite);
            }
            return sprite;
        }
        public RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); Place(rect, x0, y0, x1, y1); return rect;
        }
        public static void Place(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        public Image Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color, bool rounded = true)
        {
            var rect = Rect(name,parent,x0,y0,x1,y1); var image = rect.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            if (rounded) { image.sprite = Art("panel"); image.type = Image.Type.Sliced; }
            return image;
        }
        public Image Picture(string name, Transform parent, string asset, float x0, float y0, float x1, float y1, bool preserve = true)
        {
            var rect = Rect(name,parent,x0,y0,x1,y1); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art(asset); image.preserveAspect = preserve; image.raycastTarget = false; return image;
        }
        public Text Text(string name, Transform parent, string value, float x0, float y0, float x1, float y1,
            int size = 26, Color? color = null, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var rect = Rect(name,parent,x0,y0,x1,y1); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = bold ? boldFont : regularFont; text.text = value; text.color = color ?? (IsArtwork(parent) ? Ink : UITheme.Ink); text.fontSize = size;
            text.alignment = Alignment(alignment);
            text.enableAutoSizing = true; text.fontSizeMin = Mathf.Min(size,Mathf.Max(12,size*.72f)); text.fontSizeMax = size;
            text.raycastTarget = false; text.richText = false; text.margin = Vector4.zero;
            text.overflowMode = TextOverflowModes.Truncate;
            return text;
        }
        static TextAlignmentOptions Alignment(TextAnchor value)
        {
            switch(value)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.Center;
            }
        }
        public RectTransform Gradient(string name,Transform parent,float x0,float y0,float x1,float y1,Color top,Color bottom,float radius=0)
        {
            var rect=Rect(name,parent,x0,y0,x1,y1);var graphic=rect.gameObject.AddComponent<UISurfaceGraphic>();
            graphic.Top=top;graphic.Bottom=bottom;graphic.Radius=radius;graphic.raycastTarget=false;return rect;
        }
        public UIIconGraphic Icon(string name,Transform parent,UIIcon icon,float x0,float y0,float x1,float y1,Color? tint=null)
        {
            var graphic=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<UIIconGraphic>();
            graphic.Icon=icon;graphic.color=tint ?? (IsArtwork(parent) ? Ink : UITheme.Ink);graphic.raycastTarget=false;return graphic;
        }
        static bool IsArtwork(Transform node)
        {
            for(var t=node;t!=null;t=t.parent)if(t.GetComponent<UIArtwork>()!=null)return true;
            return false;
        }
        public Image Surface(string name,Transform parent,float x0,float y0,float x1,float y1,Color fill,float radius=UITheme.CardRadius,bool depth=false,bool outlined=true,bool dark=false)
        {
            if(IsArtwork(parent))return Panel(name,parent,x0,y0,x1,y1,fill);
            var image=Rect(name,parent,x0,y0,x1,y1).gameObject.AddComponent<UIShapeImage>();
            image.color=fill;image.raycastTarget=false;image.Radius=radius;image.Dark=dark;
            image.StrokeWidth=outlined ? UITheme.Stroke : 0;
            image.StrokeColor=dark ? new Color(.72f,.78f,.65f,.12f) : UITheme.Border;
            image.ShadowOpacity=depth ? UITheme.CardShadow : 0;return image;
        }
        public Image Card(string name,Transform parent,float x0,float y0,float x1,float y1,Color? tint=null)
        {return Surface(name,parent,x0,y0,x1,y1,tint ?? UITheme.Surface,UITheme.CardRadius,true);}
        public Image ModalSurface(string name,Transform parent,float x0,float y0,float x1,float y1)
        {
            var image=(UIShapeImage)Surface(name,parent,x0,y0,x1,y1,UITheme.Surface,UITheme.ModalRadius,true);
            image.ShadowOpacity=UITheme.ModalShadow;image.ShadowBlur=10;image.ShadowOffset=3;return image;
        }
        public Image Progress(string name,Transform parent,float x0,float y0,float x1,float y1,float value,Color? tint=null)
        {
            var track=Surface(name,parent,x0,y0,x1,y1,UITheme.Border,1000,false,false);
            return Surface(name+" fill",track.transform,0,0,Mathf.Clamp01(value),1,tint ?? UITheme.Green,1000,false,false);
        }
        public Text Pill(string name,Transform parent,string label,float x0,float y0,float x1,float y1,Color? fill=null,Color? tint=null,int size=UITheme.Caption)
        {
            bool artwork=IsArtwork(parent);var card=Surface(name,parent,x0,y0,x1,y1,fill ?? UITheme.Soft,UITheme.BadgeRadius,false,false);
            return Text(name+" label",card.transform,label,.06f,.08f,.94f,.92f,size,tint ?? UITheme.Green,TextAnchor.MiddleCenter,artwork);
        }
        public Text SectionHeader(string name,Transform parent,string label,float x0,float y0,float x1,float y1)
        {
            var row=Rect(name,parent,x0,y0,x1,y1);
            Panel(name+" rule",row,.46f,.49f,1,.51f,UITheme.Border,false);
            return Text(name+" title",row,label,0,.05f,.44f,.95f,16,UITheme.Muted,TextAnchor.MiddleLeft);
        }
        static Color LabelColor(Color value)
        {return value.r*.2126f+value.g*.7152f+value.b*.0722f>.64f ? UITheme.Ink : UITheme.Surface;}
        public Button IconButton(string name, Transform parent, string asset, float x0, float y0, float x1, float y1,
            System.Action action, Color? background = null)
        {
            if(asset=="gear")return SymbolButton(name,parent,UIIcon.Settings,x0,y0,x1,y1,action,background);
            var fill=background ?? UITheme.Green;var button=Button(name,parent,"",x0,y0,x1,y1,action,fill);
            Picture(name+" icon",button.transform,asset,.20f,.20f,.80f,.80f).color=LabelColor(fill);return button;
        }
        public Button SymbolButton(string name,Transform parent,UIIcon symbol,float x0,float y0,float x1,float y1,System.Action action,Color? background=null)
        {
            var fill=background ?? UITheme.Green;
            var button=Button(name,parent,"",x0,y0,x1,y1,action,fill,UITheme.ButtonText,true,UIButtonStyle.Icon);
            Icon(name+" icon",button.transform,symbol,.20f,.20f,.80f,.80f,LabelColor(fill));return button;
        }
        public Button ActionButton(string name,Transform parent,string label,UIIcon icon,float x0,float y0,float x1,float y1,System.Action action,Color? fill=null,int size=UITheme.ButtonText,UIButtonStyle? style=null)
        {
            Color background=fill ?? UITheme.Green;var button=Button(name,parent,label.TrimEnd(' ','→','←'),x0,y0,x1,y1,action,background,size,true,style);
            bool trailing=icon==UIIcon.ArrowRight;
            Place(button.GetComponentInChildren<Text>().rectTransform,trailing ? .06f : .16f,.06f,trailing ? .84f : .92f,.94f);
            Icon(name+" symbol",button.transform,icon,trailing ? .875f : .045f,.22f,trailing ? .95f : .13f,.78f,style==UIButtonStyle.DarkSecondary ? UITheme.Gold : style==UIButtonStyle.DarkPrimary ? UITheme.Surface : LabelColor(background));return button;
        }
        public Button Button(string name,Transform parent,string label,float x0,float y0,float x1,float y1,System.Action action,Color? background=null,int size=UITheme.ButtonText,bool raised=true,UIButtonStyle? style=null)
        {
            if(IsArtwork(parent))return ArtworkButton(name,parent,label,x0,y0,x1,y1,action,background,size);
            Color fill=background ?? UITheme.Green;
            var role=style ?? (!raised ? UIButtonStyle.Tab : Similar(fill,UITheme.Terra) ? UIButtonStyle.Primary : UIButtonStyle.Secondary);
            bool dark=role==UIButtonStyle.DarkPrimary || role==UIButtonStyle.DarkSecondary;
            float radius=role==UIButtonStyle.Icon ? 1000 : role==UIButtonStyle.Card ? UITheme.CardRadius : role==UIButtonStyle.Tab ? UITheme.SmallRadius : UITheme.ButtonRadius;
            var image=(UIShapeImage)Surface(name,parent,x0,y0,x1,y1,fill,radius,false,role!=UIButtonStyle.Primary && role!=UIButtonStyle.Tab,dark);
            image.raycastTarget=true;
            image.ShadowOpacity=role==UIButtonStyle.Primary ? UITheme.ButtonShadow : role==UIButtonStyle.Card ? .025f : 0;
            if(dark){image.StrokeColor=new Color(UITheme.Gold.r,UITheme.Gold.g,UITheme.Gold.b,role==UIButtonStyle.DarkPrimary ? .28f : .18f);}
            if(role==UIButtonStyle.Danger){image.StrokeColor=new Color(UITheme.Terra.r,UITheme.Terra.g,UITheme.Terra.b,.24f);}
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;
            colors.highlightedColor=new Color(1.035f,1.035f,1.025f,1);colors.pressedColor=new Color(.94f,.96f,.94f,1);
            colors.disabledColor=Color.white;colors.fadeDuration=UITheme.ColorDuration;button.colors=colors;
            button.onClick.AddListener(()=>{ClickSound?.Invoke();action?.Invoke();});
            var text=Text("Label",image.transform,label,.07f,.08f,.93f,.92f,size,role==UIButtonStyle.Danger ? UITheme.Terra : dark ? (role==UIButtonStyle.DarkSecondary ? UITheme.Gold : UITheme.Surface) : LabelColor(fill),TextAnchor.MiddleCenter,role==UIButtonStyle.Primary && size>=25);
            image.gameObject.AddComponent<UIButtonMotion>().Configure(false,role==UIButtonStyle.Card ? 1 : .62f);
            return button;
        }
        static bool Similar(Color a,Color b){return Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)<.01f;}
        Button ArtworkButton(string name, Transform parent, string label, float x0, float y0, float x1, float y1,
            System.Action action, Color? background = null, int size = 28)
        {
            Color fill=background ?? Teal;
            var image = Panel(name,parent,x0,y0,x1,y1,fill); image.raycastTarget = true;
            {
                var shadow=image.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.10f,.20f,.17f,.17f);shadow.effectDistance=new Vector2(0,-3.5f);shadow.useGraphicAlpha=true;
                Gradient("Soft button highlight",image.transform,.005f,.025f,.995f,.985f,new Color(1,1,1,.10f),new Color(1,1,1,0),20);
            }
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;colors.normalColor=Color.white;
            colors.highlightedColor = new Color(1.04f,1.04f,1.04f,1);
            colors.pressedColor = new Color(.88f,.92f,.89f,1);colors.selectedColor=Color.white;
            colors.disabledColor=new Color(.78f,.81f,.78f,1);colors.fadeDuration = .12f; button.colors = colors;
            button.onClick.AddListener(() => {ClickSound?.Invoke();action?.Invoke();});
            Text("Label",image.transform,label,.07f,.06f,.93f,.94f,size,Cream,TextAnchor.MiddleCenter,true);
            image.gameObject.AddComponent<UIButtonMotion>().Configure(true,1);return button;
        }
    }
}
