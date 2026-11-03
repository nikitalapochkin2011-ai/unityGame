using UnityEngine;
namespace OneMoreFloor
{
    public enum UIButtonStyle { Primary,Secondary,Icon,Danger,Quiet,DarkPrimary,DarkSecondary,Card,Tab }
    public static class UITheme
    {
        public static readonly Color Background=GameUI.Hex("F8F6EE"),Surface=GameUI.Hex("FFFDF7"),Secondary=GameUI.Hex("F0EEE4");
        public static readonly Color Ink=GameUI.Hex("203D36"),Green=GameUI.Hex("214D43"),Soft=GameUI.Hex("E4EBE2"),Muted=GameUI.Hex("6C776C");
        public static readonly Color Terra=GameUI.Hex("BF6650"),Gold=GameUI.Hex("D8B474"),Border=GameUI.Hex("E3E4D9"),Deep=GameUI.Hex("183B35"),DarkSurface=GameUI.Hex("2A4B40"),DarkAction=GameUI.Hex("315B4B");
        public static readonly Color Scrim=new Color(.08f,.18f,.14f,.56f),Focus=new Color(.45f,.60f,.48f,.78f);
        public const float ButtonRadius=18,SmallRadius=14,CardRadius=20,ModalRadius=24,BadgeRadius=10,Stroke=1;
        public const float CardShadow=.045f,ModalShadow=.075f,ButtonShadow=.035f,ShadowBlur=7,ShadowOffset=2;
        public const float PressScale=.98f,PressDuration=.12f,ReleaseDuration=.15f,ColorDuration=.14f,ModalIn=.30f,ModalOut=.16f,ModalScale=.965f;
        public const int ButtonText=22,SecondaryText=20,CardTitle=23,Body=19,Caption=13,Heading=32;
        public const float Gap=12,Inset=20;
    }
}
