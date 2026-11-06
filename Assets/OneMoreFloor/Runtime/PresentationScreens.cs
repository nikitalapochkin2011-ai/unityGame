using System;
using UnityEngine;
using UnityEngine.UI;
using Text = TMPro.TMP_Text;

namespace OneMoreFloor
{
    public sealed partial class GameApp
    {
        void DismissOverlay(){UIWindowMotion.Hide(overlay.gameObject);}
        void PlayNextLevel(){if(!RequireLanguage())ChooseLevel(progress.Unlocked);}
        GameUI CreateUI(){return new GameUI {ClickSound=()=>Play(tapTone)};}
        void ScreenHeader(string title,string subtitle,Action back,bool dark=false)
        {
            var color=dark ? UITheme.OnDark : UITheme.Ink;
            var header=ui.Rect("Page header",overlay,.055f,.885f,.945f,.982f);
            ui.SymbolButton("Back",header,UIIcon.ArrowLeft,0,.43f,.105f,.98f,back,dark ? UITheme.DarkSurface : UITheme.Surface);
            ui.Text("Page title",header,title,.15f,.40f,1,.98f,UITheme.Heading,color,TextAnchor.MiddleLeft,true);
            ui.Text("Page subtitle",header,subtitle,.15f,.015f,1,.40f,15,dark ? GameUI.Hex("B6CFC0") : UITheme.Muted,TextAnchor.MiddleLeft);
        }
        void CollectionDetail(int kind,int index,int rarity,int page)
        {
            ClearOverlay(true);wheelDisk=null;
            var card=Modal(Category(kind),ItemName(index));
            var preview=ui.Panel("Large cosmetic preview",card,.06f,.36f,.94f,.65f,UITheme.Soft).rectTransform;
            CosmeticStyle.Preview(ui,preview,(CosmeticKind)kind,index);
            ui.Pill("Rarity",card,Loc.T("rarity."+rarity),.25f,.295f,.75f,.345f,UITheme.Soft,CosmeticStyle.RarityInk(index),16);
            ui.Text("Unlock detail",card,Loc.T("collection.detail"),.07f,.22f,.93f,.292f,16,UITheme.Muted);
            bool owned=cosmetics.Owns((CosmeticKind)kind,index),selected=cosmetics.Selected((CosmeticKind)kind)==index;
            var select=ui.Button("Equip this cosmetic",card,Loc.T(selected ? "collection.selected" : owned ? "collection.select" : "collection.locked"),.09f,.11f,.91f,.215f,()=>
            {
                if(!cosmetics.Select((CosmeticKind)kind,index))return;
                SaveCosmetics();RebuildCosmeticUI();ShowCollection(kind,rarity,page);
            },UITheme.Green,23);
            select.interactable=owned && !selected;
            ui.Button("Back to collection",card,Loc.T("common.back"),.09f,.025f,.91f,.095f,()=>ShowCollection(kind,rarity,page),UITheme.Soft,20);
        }
    }
}
