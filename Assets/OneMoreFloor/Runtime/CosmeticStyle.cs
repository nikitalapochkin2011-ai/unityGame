using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // Original ten materials stay intact; authored replacement sprites define new silhouettes.
    public static class CosmeticStyle
    {
        static readonly string[] Colors={"FFFFFF","86C9ED","F3B1A4","A4D9AC","CEB0E9","EFCB79","8CD6D6","B5BFE5","CFAD91","A5AEBC"};
        public static Color Tint(int index) { return index>=10 ? Color.white : GameUI.Hex(Colors[Mathf.Clamp(index,0,9)]); }
        public static Color DoorTint(int index)
        {
            string[] colors={"267D8C","398B89","785582","B86F82","365B8E","6B939E","769B78","438594","605581","98506C","9E7954","98534B","D5A65F","7C82A6","D9E2D8"};
            return index<10 ? Tint(index) : GameUI.Hex(colors[Mathf.Clamp(index-10,0,14)]);
        }
        public static Color RarityColor(int index)
        {
            string[] colors={"586A70","216E9C","8750A6","AC752D"};
            return GameUI.Hex(colors[(int)Cosmetics.Rarity(index)]);
        }
        public static string Asset(CosmeticKind kind,int index)
        {
            string original=kind==CosmeticKind.Background ? "skyline" : kind==CosmeticKind.Floor ? "hallway" : "cabin";
            string prefix=kind==CosmeticKind.Background ? "background_" : kind==CosmeticKind.Floor ? "interior_" : "lift_";
            return index<10 ? original : prefix+index;
        }
        public static void Preview(GameUI ui,Transform parent,CosmeticKind kind,int index)
        {
            ui.Picture("Style model",parent,Asset(kind,index),0,0,1,1,false).color=Tint(index);
            if(kind==CosmeticKind.Background) Background(ui,parent,index);
            else Detail(ui,parent,index,kind==CosmeticKind.Lift);
        }
        public static void Background(GameUI ui,Transform parent,int index)
        {
            if(index==0) return;
            if(index>=10) { ui.Picture("Themed background",parent,Asset(CosmeticKind.Background,index),0,0,1,1,false); return; }
            ui.Panel("Sky material",parent,0,0,1,1,Color.Lerp(GameUI.Hex("203E4D"),Tint(index),.68f),false);
            // Distinct skyline silhouettes and celestial arrangements, with no gameplay geometry.
            for(int i=0;i<12;i++)
            {
                float x=i/12f, h=.12f+((i*7+index*3)%11)*.025f;
                ui.Panel("Neighborhood",parent,x,0,x+.075f,h,Color.Lerp(Tint(index),GameUI.Ink,.65f),false);
            }
            for(int i=0;i<index+2;i++)
            {
                float x=.06f+((i*17+index*11)%87)/100f, y=.44f+((i*13+index*7)%50)/100f;
                ui.Picture("Sky light",parent,"circle",x,y,x+.018f,y+.014f).color=GameUI.Cream;
            }
            ui.Picture("Moon",parent,"circle",.73f,.75f,.85f,.84f).color=Tint(index);
        }
        public static void Detail(GameUI ui,Transform parent,int index,bool lift)
        {
            if(index==0 || index>=10) return; // New models include their own trim and details.
            // Different trim arrangements supplement the material colour in each unlock.
            for(int i=0;i<index%3+1;i++)
            {
                float x=.08f+i*.10f;
                ui.Panel("Material inlay",parent,x,.13f,x+.025f,.78f,Color.Lerp(Tint(index),GameUI.Gold,.45f),false);
            }
            ui.Panel("Material trim",parent,.04f,.08f,.96f,.095f,Tint(index),false);
            if(lift) ui.Text("Cabin motif",parent,(index+1).ToString(),.72f,.80f,.9f,.91f,12,GameUI.Cream);
        }
    }
}
