using UnityEngine;
namespace OneMoreFloor
{
    public enum UIButtonStyle { Primary,Secondary,Icon,Danger,Quiet,DarkPrimary,DarkSecondary,Card,Tab }
    public static class UITheme
    {
        public static bool IsDark { get; private set; }
        public static void UseDark(bool dark){IsDark=dark;}
        public static void LoadPreference(){UseDark(PlayerPrefs.GetInt("OneMoreFloor.DarkTheme",0)==1);}
        public static void SavePreference(bool dark){UseDark(dark);PlayerPrefs.SetInt("OneMoreFloor.DarkTheme",dark ? 1 : 0);PlayerPrefs.Save();}
        static readonly Color lightBackground=GameUI.Hex("F8F6EE"),nightBackground=GameUI.Hex("111A17"),lightSurface=GameUI.Hex("FFFDF7"),nightSurface=GameUI.Hex("1D2A24"),lightSecondary=GameUI.Hex("F0EEE4"),nightSecondary=GameUI.Hex("18221E");
        static readonly Color lightInk=GameUI.Hex("203D36"),nightInk=GameUI.Hex("F1EEE5"),lightGreen=GameUI.Hex("214D43"),nightGreen=GameUI.Hex("ACCFB4"),lightSoft=GameUI.Hex("E4EBE2"),nightSoft=GameUI.Hex("2D3D32");
        static readonly Color lightMuted=GameUI.Hex("6C776C"),nightMuted=GameUI.Hex("B2BEAD"),lightBorder=GameUI.Hex("E3E4D9"),nightBorder=GameUI.Hex("3B4C40"),lightWash=GameUI.Hex("E5ECDD"),nightWash=GameUI.Hex("16221B");
        public static Color Background {get{return IsDark ? nightBackground : lightBackground;}}
        public static Color Surface {get{return IsDark ? nightSurface : lightSurface;}}
        public static Color Secondary {get{return IsDark ? nightSecondary : lightSecondary;}}
        public static Color Ink {get{return IsDark ? nightInk : lightInk;}}
        public static Color Green {get{return IsDark ? nightGreen : lightGreen;}}
        public static Color Soft {get{return IsDark ? nightSoft : lightSoft;}}
        public static Color Muted {get{return IsDark ? nightMuted : lightMuted;}}
        public static Color Border {get{return IsDark ? nightBorder : lightBorder;}}
        public static Color MenuWash {get{return IsDark ? nightWash : lightWash;}}
        public static readonly Color Terra=GameUI.Hex("B25D49"),Gold=GameUI.Hex("D8B474"),Deep=GameUI.Hex("183B35"),DarkSurface=GameUI.Hex("2A4B40"),DarkAction=GameUI.Hex("315B4B");
        // Semantic foregrounds stay legible even on surfaces whose color does not follow the theme.
        public static readonly Color OnDark=GameUI.Hex("FFFDF7"),DarkInk=GameUI.Hex("203D36"),NightDanger=GameUI.Hex("EDAC92");
        public static Color DangerInk {get{return IsDark ? NightDanger : Terra;}}
        public static Color Scrim {get{return IsDark ? new Color(.02f,.06f,.04f,.68f) : new Color(.08f,.18f,.14f,.56f);}}
        public static readonly Color Focus=new Color(.45f,.60f,.48f,.78f);
        public static Color ContrastText(Color background)
        {return background.r*.2126f+background.g*.7152f+background.b*.0722f>.64f ? DarkInk : OnDark;}
        public const float ButtonRadius=18,SmallRadius=14,CardRadius=20,ModalRadius=24,BadgeRadius=10,Stroke=1;
        public const float CardShadow=.045f,ModalShadow=.075f,ButtonShadow=.035f,ShadowBlur=7,ShadowOffset=2;
        public const float PressScale=.98f,PressDuration=.12f,ReleaseDuration=.15f,ColorDuration=.14f,ModalIn=.30f,ModalOut=.16f,ModalScale=.965f;
        public const int ButtonText=22,SecondaryText=20,CardTitle=23,Body=19,Caption=13,Heading=32;
        public const float Gap=12,Inset=20;
    }
}
