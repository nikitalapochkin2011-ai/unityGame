using UnityEngine;
using UnityEngine.UI;

namespace OneMoreFloor
{
    // Ten coordinated materials. Index zero deliberately preserves all original artwork.
    public static class CosmeticStyle
    {
        static readonly string[] Colors={"FFFFFF","86C9ED","F3B1A4","A4D9AC","CEB0E9","EFCB79","8CD6D6","B5BFE5","CFAD91","A5AEBC"};
        public static Color Tint(int index) { return GameUI.Hex(Colors[index]); }
        public static void Background(GameUI ui,Transform parent,int index)
        {
            if(index==0) return;
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
            if(index==0) return;
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
