"""New vector-authored game sprites. Run with Pillow; existing artwork is never rewritten."""
import math, uuid
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]/'Assets/OneMoreFloor/Resources/Art'
class Canvas:
    def __init__(self,w,h):
        self.w,self.h=w,h; self.im=Image.new('RGBA',(w*3,h*3)); self.d=ImageDraw.Draw(self.im)
    def r(self,b,c,rad=0,outline=None,width=1):
        b=tuple(round(v*3) for v in b)
        if rad:self.d.rounded_rectangle(b,round(rad*3),c,outline,round(width*3))
        else:self.d.rectangle(b,c,outline,round(width*3))
    def e(self,b,c):self.d.ellipse(tuple(round(v*3) for v in b),c)
    def p(self,pts,c):self.d.polygon([(round(x*3),round(y*3)) for x,y in pts],c)
    def l(self,pts,c,w=2):self.d.line([(round(x*3),round(y*3)) for x,y in pts],c,round(w*3),joint='curve')
    def save(self,name):
        self.im.resize((self.w*2,self.h*2),Image.Resampling.LANCZOS).save(ROOT/(name+'.png'))
        meta=ROOT/(name+'.png.meta')
        if not meta.exists():
            template=(ROOT/'person_0.png.meta').read_text()
            import re
            template=re.sub(r'guid: [0-9a-f]{32}', 'guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/expansion/'+name).hex,template,count=1)
            template=re.sub(r'spriteID: [0-9a-f]+','spriteID: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/sprite/'+name).hex,template)
            meta.write_text(template)

def flower(c,x,y,s=1,col='#f4a5b8'):
    for a in range(5):
        t=a*math.tau/5; xx=x+math.cos(t)*5*s; yy=y+math.sin(t)*5*s
        c.e((xx-4*s,yy-4*s,xx+4*s,yy+4*s),col)
    c.e((x-2*s,y-2*s,x+2*s,y+2*s),'#ffe1a0')
def star(c,x,y,s,col):
    c.p([(x,y-s),(x+s*.25,y-s*.25),(x+s,y),(x+s*.25,y+s*.25),(x,y+s),(x-s*.25,y+s*.25),(x-s,y),(x-s*.25,y-s*.25)],col)
def leaf(c,x,y,s=1):c.p([(x,y),(x+12*s,y-14*s),(x+20*s,y-13*s),(x+18*s,y-3*s)],'#80ad86')
def gear(c,x,y,r,col):
    c.p([(x+math.cos(i*math.tau/48)*(r if i%4<2 else r*.79),y+math.sin(i*math.tau/48)*(r if i%4<2 else r*.79)) for i in range(48)],col)
    c.e((x-r*.5,y-r*.5,x+r*.5,y+r*.5),'#3b4850');c.e((x-r*.2,y-r*.2,x+r*.2,y+r*.2),col)
def log(c,pts,w=16):
    c.l(pts,'#503a32',w+5);c.l(pts,'#9e7954',w);c.l([(x-2,y-2) for x,y in pts],'#c29a67',2)
    for x,y in (pts[0],pts[-1]):
        c.e((x-w*.48,y-w*.48,x+w*.48,y+w*.48),'#caa878');c.e((x-w*.25,y-w*.25,x+w*.25,y+w*.25),'#8e6646')
# Same head/body proportions, dot eyes, blush, rounded shoes and muted clothes as the originals.
for i in range(8):
    c=Canvas(164,240); v=i%4; granny=i<4
    skin=['#f2c59e','#bf825e','#e9af82','#efbc93'][v]
    shirt=(['#9a7caa','#709b8c','#c77a70','#b09a64'] if granny else ['#6b849c','#9a735a','#6d8976','#897d9c'])[v]
    hair=['#e1dbd2','#aaa9a4','#f2ede0','#bdb9b1'][v]
    c.e((25,217,137,233),(20,32,45,38))
    for x in (48,88):c.r((x,153,x+27,213),'#475263',8);c.r((x-7,207,x+29,221),'#303c4d',6)
    if granny:
        c.p([(46,134),(118,134),(132,196),(32,196)],shirt);c.l([(57,150),(49,185)],'#ffffff50',2);c.l([(105,150),(112,185)],'#ffffff50',2)
    for x in (27,116):c.r((x,105,x+20,163),skin,10)
    c.r((31,91,130,155),shirt,17);c.r((44,113,119,173),shirt,9)
    c.r((69,75,94,104),skin,8)
    c.p([(62,96),(81,110),(100,96)],'#fff1df');c.l([(81,112),(81,168)],'#3d465550',2)
    for y in (119,135,151):c.e((79,y,83,y+4),'#e8d4a2')
    c.e((42,19,121,98),hair)
    if granny and v in (0,2):c.e((98,7,129,36),hair)
    for x in (38,108):c.e((x,54,x+16,74),skin)
    c.r((49,34,115,94),skin,25)
    if granny:
        if v==1:c.p([(40,60),(45,28),(79,18),(118,31),(125,64),(110,44),(79,39),(53,50)],hair)
        else:
            c.p([(46,56),(45,31),(69,20),(109,25),(120,51),(99,38),(82,45),(64,36)],hair)
            for x in (51,66,81,96):c.e((x,25,x+20,45),hair)
        if v==2:
            c.p([(40,58),(44,25),(81,13),(121,29),(127,62),(113,42),(79,30),(52,43)],'#91abb8')
            c.p([(50,85),(70,95),(84,117),(65,110)],'#91abb8');c.p([(112,86),(93,95),(80,117),(102,106)],'#91abb8')
        if v==3:
            c.p([(42,101),(61,97),(80,131),(99,97),(121,101),(103,141),(60,141)],'#e2c58a')
    else:
        c.r((46,46,54,66),hair,3);c.r((110,46,118,66),hair,3)
        if v==1:
            c.r((39,27,124,43),'#755a48',6);c.r((51,12,112,33),'#94765a',8)
        if v==2:c.e((66,25,103,37),hair)
        if v==3:c.p([(48,38),(58,24),(110,23),(119,48),(106,38)],hair)
    for x in (65,94):c.e((x,59,x+5,65),'#31323a')
    c.l([(62,53),(72,52)],hair,3);c.l([(92,52),(103,53)],hair,3)
    c.l([(56,69),(63,71)],'#b07d67',1);c.l([(102,71),(108,69)],'#b07d67',1)
    if v!=2:
        for x in (57,87):c.r((x,53,x+20,71),'#ffffff18',5,'#59616b',2)
        c.l([(77,59),(87,59)],'#59616b',2)
    if not granny and v in (0,2):
        c.p([(70,76),(81,72),(93,77),(89,81),(81,78),(73,81)],hair)
    if not granny and v==3:c.p([(60,79),(71,88),(96,88),(108,78),(100,100),(80,105),(65,98)],hair)
    c.l([(76,82),(82,84),(89,81)],'#9b5748',2)
    for x in (55,99):c.e((x,72,x+11,77),'#ed967c60')
    if granny:
        if v==0:flower(c,112,113,.65)
        if v==1:c.r((105,153,136,188),'#b98b57',4);c.l([(109,154),(110,145),(129,145),(132,154)],'#7c654c',3)
        if v==3:c.l([(52,107),(78,118),(109,106)],'#ead6ad',3)
    else:
        if v==0:c.p([(59,100),(71,105),(70,168),(50,168)],'#52687e');c.p([(103,100),(91,105),(93,168),(116,168)],'#52687e')
        if v==1:c.r((95,117,112,136),'#c5a17d',3)
        if v==2:c.r((31,95,49,132),'#c7b17c',4);c.r((110,95,130,132),'#c7b17c',4)
    c.save('person_'+str(8+i))
# Upright raccoon: striped tail remains inside one ordinary passenger slot.
c=Canvas(164,240)
c.e((25,217,137,233),(20,32,45,38))
c.l([(110,190),(139,180),(143,155),(130,137)],'#515b68',26)
c.l([(132,187),(144,176)],'#bbc0bf',13);c.l([(142,160),(134,146)],'#bbc0bf',12)
c.e((40,95,120,210),'#88959c');c.e((57,121,105,199),'#ced1c7')
for x in (38,105):c.r((x,114,x+21,166),'#72828a',10);c.r((x+1,151,x+19,171),'#384855',8)
for x in (47,88):c.r((x,190,x+28,220),'#384855',10)
c.p([(39,56),(35,14),(67,33)],'#657480');c.p([(99,33),(130,14),(126,59)],'#657480')
c.p([(43,41),(42,25),(58,37)],'#cfb4a5');c.p([(108,37),(123,25),(122,44)],'#cfb4a5')
c.e((33,29,132,115),'#9ba7aa');c.p([(32,61),(60,56),(82,69),(105,56),(134,61),(113,89),(83,100),(50,88)],'#e4e0ce')
c.p([(38,61),(68,54),(79,70),(68,84),(44,79)],'#354652');c.p([(86,70),(100,54),(128,61),(123,79),(98,84)],'#354652')
for x in (58,101):c.e((x,64,x+6,71),'#172c38');c.e((x+1,64,x+3,66),'#fff4df')
c.e((65,80,102,105),'#eee6d0');c.r((76,80,91,89),'#263b46',5);c.l([(83,88),(83,95),(76,98)],'#56616a',2);c.l([(83,95),(91,98)],'#56616a',2)
c.p([(48,106),(82,124),(115,106),(104,133),(82,139),(56,130)],'#b66b50');c.p([(94,127),(116,146),(95,154)],'#cf8f66')
c.save('person_16')

# Full replacement models. Each themed cabin and hallway has its own authored geometry.
palettes=[
('#123f57','#267d8c','#edcb83','#c4e0dc'),('#174d56','#398b89','#f3917d','#e5e8cc'),
('#443153','#785582','#d9bb81','#e4d7df'),('#69404a','#b86f82','#efd4a3','#f3d9cf'),('#172e59','#365b8e','#d59b72','#c4d6e5'),
('#2a5665','#6b939e','#e1b977','#e8f0e5'),('#355348','#769b78','#dab883','#dfe6c2'),('#184958','#438594','#d9bd87','#b8e4df'),('#302c58','#605581','#d5bc80','#d3cbe5'),('#5d2d48','#98506c','#e1b977','#efd3c9'),
('#384c3a','#72865c','#c9a16d','#e3e3bc'),('#522f3a','#98534b','#e1b369','#ecc9a4'),('#32444b','#567176','#d5a65f','#e0d2af'),('#3c4165','#7c82a6','#d2bfe2','#dae9ed'),('#497583','#89b3b8','#e5c584','#f0ead8')]

def cabin(idx):
    c=Canvas(600,400);dark,mid,gold,light=palettes[idx-10]
    if idx<20:
        c.r((7,8,593,394),dark,9);c.r((17,17,583,385),gold,6);c.r((25,25,575,376),mid,4)
    elif idx==20:
        c.p([(34,66),(139,36),(293,47),(453,35),(565,67),(558,373),(401,383),(194,377),(29,371)],'#745c41')
    elif idx==21:
        c.p([(23,101),(64,39),(153,28),(194,10),(236,30),(362,30),(406,10),(449,28),(535,39),(577,101),(565,375),(527,397),(73,397),(35,375)],dark)
        c.p([(23,133),(2,194),(20,249),(49,233),(58,158)],mid)
        c.p([(577,133),(598,194),(580,249),(551,233),(542,158)],mid)
    elif idx==22:
        for x in (25,540):
            c.r((x,70,x+35,365),dark,12);c.r((x+7,78,x+29,358),gold,8)
            c.r((x-5,137,x+40,151),mid,3);c.r((x-5,284,x+40,298),mid,3)
        c.r((49,40,551,83),dark,16);c.r((62,47,538,68),gold,8)
        c.r((39,357,561,383),gold,10)
    elif idx==23:
        c.p([(6,82),(61,16),(254,29),(300,1),(346,29),(539,16),(594,82),(576,371),(489,399),(111,399),(24,371)],'#9ca8c8')
        c.p([(6,82),(61,16),(83,108),(64,351),(24,371)],'#d8e4e5')
        c.p([(539,16),(594,82),(576,371),(535,354),(517,108)],'#6f739c')
        c.p([(111,399),(64,351),(535,354),(489,399)],'#bccadd')
    else:
        c.r((63,83,537,354),mid,10)
        c.p([(28,76),(300,0),(572,76)],gold)

    c.r((46,64,554,365),dark,4)
    c.p([(51,71),(547,71),(507,106),(93,106)],light)
    c.p([(51,71),(93,106),(93,324),(51,360)],mid)
    c.p([(547,71),(507,106),(507,324),(547,360)],mid)
    c.r((93,106,507,324),light,3)
    c.p([(93,324),(507,324),(547,360),(51,360)],dark)
    for x in (190,300,410):c.l([(x,109),(x,320)],mid,2)
    if idx==20:
        for x in range(97,500,34):
            c.r((x,107,x+30,323),'#a0a57d',2)
            c.l([(x+6,118),(x+8,175),(x+5,238),(x+7,308)],'#798560',1)
        for x in (61,521):c.l([(x,112),(x+7,323)],'#c0a274',5)
    if idx<15:
        # Raised panelling, bevels and a woven floor insert add depth to rare materials.
        for x in (111,222,333):
            c.r((x,229,x+91,270),gold,4);c.r((x+3,232,x+88,267),mid,3)
            c.l([(x+7,235),(x+84,235)],light,1)
        c.p([(173,334),(424,334),(454,354),(145,354)],mid)
        c.l([(176,336),(422,336),(441,350),(160,350),(176,336)],gold,2)
    c.r((79,285,521,293),gold,3);c.r((81,283,519,286),'#fff1d4',2)
    c.r((156,80,444,87),'#fff1ce',3)
    c.r((198,19,402,61),dark,5);c.r((211,28,389,52),'#182f3d',3)
    c.r((562,158,578,285),dark,4)
    for y in (178,208,238,268):c.e((565,y,573,y+7),gold)
    c.r((51,360,549,369),gold,2)
    if idx<15:
        # Jewel palettes with crafted geometric inlays and brass edge work.
        for x in (38,560):
            for y in (99,180,261):c.p([(x,y-8),(x+5,y),(x,y+8),(x-5,y)],gold)
        c.r((245,134,355,214),gold,6);c.r((252,141,348,207),dark,4)
        star(c,300,175,23,light)
    theme(c,idx,'lift')
    c.r((198,19,402,61),dark,5);c.r((211,28,389,52),'#182f3d',3)
    c.save('lift_'+str(idx))
    # Tall menu asset is rendered from the same vector construction.
    c.im.resize((640,1040),Image.Resampling.LANCZOS).save(ROOT/('lobby_lift_'+str(idx)+'.png'))
    meta=ROOT/('lobby_lift_'+str(idx)+'.png.meta')
    if not meta.exists():
        import re
        meta.write_text(re.sub(r'guid: [0-9a-f]{32}','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/lobby/'+str(idx)).hex,(ROOT/('lift_'+str(idx)+'.png.meta')).read_text(),count=1))

def theme(c,idx,kind):
    # Details stay at the edges, leaving passenger silhouettes and floor display clear.
    hall=kind=='hall'; sx=800/600 if hall else 1; sy=.5 if hall else 1
    class Scaled:
        def r(self,b,*a,**k):c.r((b[0]*sx,b[1]*sy,b[2]*sx,b[3]*sy),*a,**k)
        def e(self,b,*a):c.e((b[0]*sx,b[1]*sy,b[2]*sx,b[3]*sy),*a)
        def l(self,p,col,w=2):c.l([(x*sx,y*sy) for x,y in p],col,w)
        def p(self,p,col):c.p([(x*sx,y*sy) for x,y in p],col)
    q=Scaled()
    if idx==15: # Winter: snow cap, corner garland, small baubles.
        q.r((17,13,583,30),'#eef5e9',9)
        for x in (40,70,102,142,465,509,550):q.e((x,16,x+32,42),'#eef5e9')
        q.l([(400,73),(441,95),(481,94),(523,72),(553,48)],'#3f7563',9)
        for x,y in ((411,82),(447,97),(483,95),(522,76),(544,56)):
            q.e((x-5,y-1,x+5,y+9),'#e7b866');q.e((x-2,y+12,x+8,y+22),'#bf6960')
        for x,y in ((80,123),(515,144),(99,240)):q.l([(x-6,y),(x+6,y)],'#eaf3e8',2);q.l([(x,y-8),(x,y+8)],'#eaf3e8',2)
    if idx in (16,20):
        q.l([(34,360),(42,250),(28,152),(56,54),(145,26)],'#567448',5)
        q.l([(555,363),(570,240),(548,140),(566,53),(476,25)],'#567448',5)
        for x,y in ((44,290),(32,196),(45,106),(105,29),(556,267),(552,176),(562,79),(500,27)):
            leaf(q,x,y,.9);flower(q,x+8,y-17,1.1,'#e9afc2' if idx==20 else '#e8c6a0')
    if idx==17:
        q.r((118,117,478,202),'#225f70',12);q.r((125,124,471,196),'#8fc5c5',9)
        for x,y,col in ((170,151,'#e9b478'),(263,174,'#e9d6ad'),(386,148,'#e48e7e')):
            q.e((x,y,x+31,y+16),col);q.p([(x+26,y+8),(x+42,y-2),(x+42,y+18)],col)
        for x in (143,310,450):q.e((x,132,x+5,138),'#e1f4dd');q.l([(x,193),(x-8,176),(x+5,163)],'#5f987b',3)
    if idx==18:
        q.r((110,111,490,205),'#38365c',12)
        for x,y in ((134,132),(188,172),(260,133),(337,175),(453,141)):star(q,x,y,6,'#ead39c')
        q.e((340,121,384,165),'#e5d5b6');q.e((351,116,390,153),'#38365c')
        q.l([(134,132),(188,172),(260,133)],'#ac9bbb',1)
    if idx==19:
        q.p([(69,69),(158,69),(133,215),(92,257),(70,180)],'#843d57');q.p([(531,69),(442,69),(467,215),(508,257),(530,180)],'#843d57')
        q.l([(75,72),(92,191),(105,231)],'#be6c7a',4);q.l([(525,72),(508,191),(495,231)],'#be6c7a',4)
        q.r((82,215,130,225),'#e3bd75',4);q.r((470,215,518,225),'#e3bd75',4)
        q.r((208,323,392,352),'#9b5268',7)
    if idx==20:
        # The frame is assembled from irregular logs, with protruding branch ends.
        log(q,[(17,380),(28,268),(18,161),(38,25)],19)
        log(q,[(582,380),(568,268),(583,150),(563,22)],19)
        log(q,[(5,73),(112,32),(224,44),(300,18),(398,39),(489,30),(596,62)],22)
        log(q,[(13,376),(143,369),(301,379),(459,368),(589,376)],17)
        log(q,[(29,165),(6,133)],9);log(q,[(576,199),(598,165)],9)
        # Leaves and a restrained cluster of blossoms on the upper and side beams.
        for x,y in ((74,46),(96,38),(131,37),(543,116),(561,144)):
            leaf(q,x,y);flower(q,x,y-9,1.2,'#d8a9e0')
    if idx==21:
        q.p([(14,153),(8,75),(47,36),(73,0),(96,45),(145,61),(114,91),(67,89),(49,151)],'#ad6a50')
        q.p([(586,153),(592,75),(553,36),(527,0),(504,45),(455,61),(486,91),(533,89),(551,151)],'#ad6a50')
        for x in (47,546):q.e((x,60,x+9,70),'#f2ce7e')
        for x in (72,516):q.p([(x,101),(x+10,119),(x,137),(x-10,119)],'#dba765')
        q.p([(165,20),(193,2),(220,20),(248,2),(276,20)],'#e6bd78');q.p([(324,20),(352,2),(380,20),(408,2),(435,20)],'#e6bd78')
    if idx==22:
        for x,y,r in ((47,52,47),(550,57,46),(558,328,37),(40,337,34)):gear(q,x,y,r,'#d4a766')
        q.e((253,106,347,200),'#d4a766');q.e((261,114,339,192),'#eee1bd')
        for a in range(12):
            t=a*math.tau/12;q.e((298+math.cos(t)*30,151+math.sin(t)*30,302+math.cos(t)*30,155+math.sin(t)*30),'#586069')
        q.l([(300,130),(300,154),(320,166)],'#4b5159',3)
        for x in (77,523):q.l([(x,353),(x,214),(x+12,200)],'#ba884c',8)
    if idx==23:
        for x,y,h in ((30,344,100),(558,344,127),(62,83,78),(516,82,61)):
            q.p([(x-16,y),(x-19,y-h*.62),(x,y-h),(x+22,y-h*.55),(x+16,y)],'#9fb9ce')
            q.p([(x,y-h),(x+22,y-h*.55),(x+16,y),(x,y-13)],'#c7d6e2');q.l([(x,y-h),(x,y-13),(x-16,y)],'#eee8e8',2)
        for x in range(139,477,48):q.p([(x,73),(x+16,40),(x+32,73)],'#b8b7d1')
        star(q,300,162,30,'#9c94bd')
    if idx==24:
        for x in (36,532):
            q.r((x,85,x+31,365),'#d9e2d8',10);q.r((x-7,82,x+38,95),'#e1c17f',6);q.r((x-7,351,x+38,368),'#e1c17f',6)
            for dx in (7,16,24):q.l([(x+dx,107),(x+dx,343)],'#acbfc0',2)
        q.p([(28,76),(300,0),(572,76)],'#e5c788');q.p([(75,69),(300,17),(525,69)],'#d9e5db')
        for x,y in ((5,354),(37,372),(92,379),(479,379),(540,371),(580,354)):
            q.e((x-19,y-14,x+25,y+15),'#eaf0e5')
        star(q,300,158,28,'#d7b16c')

def hall(idx):
    c=Canvas(800,200);dark,mid,gold,light=palettes[idx-10]
    c.r((0,0,800,200),light,4);c.r((0,0,800,11),dark);c.r((8,12,792,16),gold)
    c.r((0,118,800,173),mid);c.r((0,114,800,119),gold)
    for x in range(7,790,70):c.r((x,127,x+58,164),dark,3);c.r((x+4,130,x+54,160),mid,2)
    c.r((0,173,800,200),'#b9b9ac')
    for x in range(-100,900,100):c.l([(x+25,174),(x,200)],'#8b9c99',2)
    c.r((0,170,800,174),gold)
    for x in (35,660):
        c.r((x-5,28,x+108,171),gold,6);c.r((x,33,x+103,170),dark,4);c.r((x+8,41,x+95,165),mid,3)
        c.r((x+17,51,x+86,105),dark,4);c.r((x+17,115,x+86,153),dark,4);c.r((x+78,106,x+84,122),gold,3)
    c.r((251,28,410,112),gold,17);c.r((258,35,403,106),'#8fb4bd',13)
    c.l([(329,36),(329,106)],light,5);c.l([(259,74),(402,74)],light,5)
    for x in (199,600):c.r((x-3,44,x+3,100),gold,2);c.r((x-12,46,x+12,70),'#f7e6b7',5)
    c.r((450,126,559,132),dark,3);c.r((455,131,461,169),gold);c.r((549,131,555,169),gold)
    for x in (459,519):c.r((x,39,x+40,91),gold,3);c.r((x+5,44,x+35,86),light,2);star(c,x+20,65,11,mid)
    c.r((415,180,591,193),mid,4);c.r((422,183,584,190),gold,2)
    if idx<15:
        for x in range(167,639,22):
            if 245<x<420 or 450<x<562:continue
            for y in (34,90):c.p([(x,y-3),(x+3,y),(x,y+3),(x-3,y)],gold)
        c.r((488,112,516,127),gold,3)
        c.l([(502,113),(502,91)],dark,2)
        leaf(c,499,103,.7);leaf(c,493,109,-.6)
    theme(c,idx,'hall');c.save('interior_'+str(idx))

def sky(idx):
    c=Canvas(1000,600);dark,mid,gold,light=palettes[idx-10]
    c.r((0,0,1000,600),light)
    c.e((710,64,812,166),gold)
    for layer,col in ((0,mid),(1,dark)):
        for i in range(12):
            x=i*91-20;h=80+(i*47+idx*31+layer*53)%185;y=600-h+layer*48
            c.r((x,y,x+79,601),col,3)
            for wx in range(x+12,x+70,22):
                for wy in range(int(y+18),585,30):c.r((wx,wy,wx+7,wy+11),gold,2)
    if idx==15:
        for x,y in ((80,89),(210,163),(350,65),(520,210),(650,104),(854,193),(917,67),(395,248)):c.e((x,y,x+5,y+5),'#f7f6ec')
        for i in range(12):c.r((i*91-20,600-(80+(i*47+idx*31)%185),i*91+59,605-(80+(i*47+idx*31)%185)),'#f7f6ec',3)
    if idx==16:
        for x in (30,199,450,874):
            c.l([(x+30,550),(x+30,235)],dark,13)
            for dx,dy in ((0,0),(-28,20),(32,16)):c.e((x+dx-15,200+dy,x+dx+80,306+dy),mid)
            for dx in (0,27,53):flower(c,x+dx,255,.9,'#e3b7aa')
    if idx==17:
        c.p([(0,229),(170,205),(355,236),(540,206),(710,228),(1000,195),(1000,600),(0,600)],'#5c9faaa0')
        for x,y in ((130,126),(380,204),(620,100),(830,249)):
            c.e((x,y,x+47,y+18),mid);c.p([(x+42,y+9),(x+61,y-5),(x+61,y+23)],mid)
            c.e((x+70,y-30,x+78,y-22),'#f1edce')
    if idx==18:
        for i in range(28):star(c,35+(i*139)%930,36+(i*47)%250,3+i%3,gold)
        c.l([(110,109),(259,156),(388,71),(507,178)],mid,2)
        for x,y in ((110,109),(259,156),(388,71),(507,178)):star(c,x,y,9,gold)
    if idx==19:
        for i in range(9):
            x=47+i*115;y=76+(i%3)*47;c.l([(x,0),(x,y)],mid,2);c.r((x-18,y,x+18,y+37),gold,10);c.r((x-12,y+5,x+12,y+29),'#f1d2a3',6)
    if idx==20:
        # Ancient trees form actual arches over a forest settlement.
        for x in (-40,870):
            log(c,[(x+70,620),(x+85,380),(x+51,180),(x+114,20)],65)
            log(c,[(x+61,229),(x+180,131),(x+250,104)],26)
            for xx,yy in ((x-14,70),(x+110,26),(x+190,87)):c.e((xx,yy,xx+178,yy+112),'#698d68')
        for x,y in ((80,184),(116,193),(915,280),(891,293),(215,110)):flower(c,x,y,1.8,'#dab2db')
        for x in (250,470,670):c.p([(x-50,411),(x,297),(x+60,411)],mid);c.r((x-25,407,x+35,584),dark,6)
    if idx==21:
        for x in (80,294,519,787):
            c.r((x,280,x+67,601),mid,4);c.p([(x-14,283),(x+33,187),(x+81,283)],dark)
            for xx in (x,x+24,x+48):c.r((xx,267,xx+17,290),gold,2)
        c.p([(407,97),(441,49),(466,83),(522,68),(572,101),(530,98),(493,129),(467,109),(428,116),(403,150),(376,145)],dark)
    if idx==22:
        for x,y,r in ((79,180,85),(920,200,96),(531,152,66)):gear(c,x,y,r,gold)
        for x in (226,732):
            c.r((x,227,x+105,600),mid,6);c.e((x-13,170,x+118,301),gold);c.e((x-3,180,x+108,291),light);c.l([(x+52,193),(x+52,236),(x+80,251)],dark,5)
    if idx==23:
        for i in range(9):
            x=30+i*122;y=570;h=115+(i*41)%177
            c.p([(x,y),(x-17,y-h*.65),(x+28,y-h),(x+73,y-h*.6),(x+58,y)],mid);c.p([(x+28,y-h),(x+73,y-h*.6),(x+58,y),(x+28,y-20)],gold)
        for x,y in ((130,100),(362,192),(519,53),(884,133)):star(c,x,y,12,'#fff0d4')
    if idx==24:
        c.r((230,330,760,475),mid,15)
        for x in (252,366,480,594,708):c.r((x,257,x+33,438),light,8);c.r((x-5,253,x+38,265),gold,5)
        c.p([(201,251),(497,113),(787,251)],gold);c.p([(239,242),(497,133),(749,242)],light)
        for x,y in ((65,455),(210,486),(418,492),(669,477),(838,453)):c.e((x-65,y-31,x+129,y+48),'#f2f0df')
    c.save('background_'+str(idx))
for index in range(10,25):cabin(index);hall(index);sky(index)
print('Generated 9 characters, 45 cosmetic models and 15 tall lobby cabin sprites.')
