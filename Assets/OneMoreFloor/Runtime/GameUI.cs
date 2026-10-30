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
        public static readonly Color Ink = Hex("203E4D"), Teal = Hex("387D7A"), Coral = Hex("D66C54");
        public static readonly Color Cream = Hex("FFF2DC"), Muted = Hex("7F999C"), Gold = Hex("E5B965");
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
            text.font = bold ? boldFont : regularFont; text.text = value; text.color = color ?? Ink; text.fontSize = size;
            text.alignment = Alignment(alignment);
            text.enableAutoSizing = true; text.fontSizeMin = Mathf.Max(12,size*.72f); text.fontSizeMax = size;
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
        public Button IconButton(string name, Transform parent, string asset, float x0, float y0, float x1, float y1,
            System.Action action, Color? background = null)
        {
            var button = Button(name,parent,"",x0,y0,x1,y1,action,background);
            var icon = Picture(name+" icon",button.transform,asset,.2f,.2f,.8f,.8f);
            icon.color = Cream;
            return button;
        }
        public Button Button(string name, Transform parent, string label, float x0, float y0, float x1, float y1,
            System.Action action, Color? background = null, int size = 28)
        {
            Panel(name+" shadow",parent,x0,y0-.006f,x1,y1-.006f,new Color(0,0,0,.12f));
            var image = Panel(name,parent,x0,y0,x1,y1,background ?? Teal); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1,1,1,.92f);
            colors.pressedColor = new Color(.78f,.85f,.84f,1); colors.fadeDuration = .09f; button.colors = colors;
            button.onClick.AddListener(() => action());
            Text("Label",image.transform,label,.07f,.06f,.93f,.94f,size,Cream,TextAnchor.MiddleCenter,true);
            return button;
        }
    }
}
