"""Original game artwork. Optional authoring tool; Pillow is not a game dependency."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
ROOT = Path(__file__).resolve().parents[1] / 'Assets/OneMoreFloor/Resources/Art'
ROOT.mkdir(parents=True, exist_ok=True)
S=4
OUTPUT_SCALE=2

def art(name, size, painter):
    im=Image.new('RGBA',(size[0]*S,size[1]*S)); d=ImageDraw.Draw(im)
    def rect(box, fill, r=0, outline=None, width=1):
        box=tuple(int(v*S) for v in box)
        if r: d.rounded_rectangle(box,r*S,fill,outline,width*S)
        else: d.rectangle(box,fill,outline,width*S)
    def ellipse(box, fill): d.ellipse(tuple(int(v*S) for v in box), fill)
    def poly(points, fill): d.polygon([(int(x*S),int(y*S)) for x,y in points],fill)
    def line(points, fill, w=1): d.line([(int(x*S),int(y*S)) for x,y in points],fill,w*S,joint='curve')
    painter(rect,ellipse,poly,line)
    im.resize((size[0]*OUTPUT_SCALE,size[1]*OUTPUT_SCALE),Image.Resampling.LANCZOS).save(ROOT/(name+'.png'))

# Antialiased nine-slice UI pieces: the transparent corner radius is 24 px.
art('panel',(96,96),lambda r,e,p,l:r((0,0,95,95),'white',24))
art('circle',(96,96),lambda r,e,p,l:e((0,0,95,95),'white'))
art('triangle',(48,48),lambda r,e,p,l:p([(2,2),(46,24),(2,46)],'white'))

skins=['#f2c59e','#bf825e','#e9af82','#8d5945','#f4cfb0','#d79970','#aa7354','#efbc93']
shirts=['#ef9076','#5faba3','#e5b755','#8c8bc0','#70a0ba','#d37c8b','#88a780','#c69060']
hair=['#49332f','#272c34','#744b32','#2a2429','#b97b40','#43332f','#292a31','#6b4135']
for i in range(8):
    def person(r,e,p,l,i=i):
        skin,shirt,h=skins[i],shirts[i],hair[i]
        e((25,217,137,233),(20,32,45,38))
        # Legs and individual shoes, then arms and clothing.
        r((48,151,75,213),'#35465c',9); r((88,151,115,213),'#35465c',9)
        r((41,207,77,221),'#253247',6); r((86,207,123,221),'#253247',6)
        r((27,105,47,163),skin,10); r((116,105,136,163),skin,10)
        r((31,91,130,155),shirt,17); r((44,113,119,173),shirt,9)
        # Clothing seams, cuffs, pocket / collar.
        r((32,126,47,136),'#ffffff55',3); r((116,126,130,136),'#ffffff55',3)
        r((93,115,111,132),'#ffffff40',4)
        l([(81,111),(81,164)],'#20304424',2)
        r((69,75,94,104),skin,8)
        p([(63,96),(81,110),(99,96)],'#fff1df')
        if i%2: r((78,104,85,126),'#34546a',3)
        e((42,19,121,98),h)
        e((38,54,54,74),skin);e((108,54,125,74),skin)
        r((49,34,115,94),skin,25)
        # Hair silhouettes vary, while all passengers keep the same gameplay rules.
        if i%3==0:
            p([(46,54),(46,29),(70,16),(109,23),(119,48),(96,37),(74,44),(63,37)],h)
            r((45,43,54,63),h,4)
        elif i%3==1:
            e((42,16,120,58),h);e((39,15,65,46),h);e((98,20,123,50),h)
            r((44,38,53,68),h,4)
        else:
            p([(46,57),(44,30),(67,20),(112,28),(119,53),(99,42),(57,48)],h)
        e((65,59,70,65),'#31323a');e((94,59,99,65),'#31323a')
        if i in (1,4):
            r((57,53,77,70),'#ffffff18',5,'#3b424e',2);r((87,53,107,70),'#ffffff18',5,'#3b424e',2)
            l([(77,59),(87,59)],'#3b424e',2)
        l([(76,79),(82,82),(89,78)],'#9b5748',2)
        e((55,72,66,77),'#ed967c60');e((99,72,109,77),'#ed967c60')
        if i==3: e((101,6,132,37),h)
        if i==6:
            r((41,26,119,42),'#dcae61',7);r((52,13,109,32),'#dcae61',9)
    art('person_'+str(i),(164,240),person)

def window(r,e,p,l):
    r((3,5,157,155),'#382f3835',12);r((0,0,154,150),'#ebe0c8',10)
    r((9,9,145,139),'#587f88',5)
    r((15,15,139,133),'#a9c3c4',3)
    p([(17,18),(76,18),(17,85)],'#e9e6ca70')
    r((69,12,80,138),'#e8dec8',2);r((11,70,143,78),'#e8dec8',2)
    p([(15,15),(47,15),(30,91),(15,100)],'#d49b7a')
    p([(107,15),(139,15),(139,100),(122,91)],'#d49b7a')
    r((0,139,154,151),'#f9ebd4',4)
art('window',(160,160),window)

def plant(r,e,p,l):
    l([(59,85),(59,22)],'#4e796b',5)
    for x,y,flip in [(35,30,0),(77,16,1),(30,55,0),(83,43,1),(39,75,0),(78,67,1)]:
        e((x-12,y-12,x+14,y+19),'#639c7b' if flip else '#80b28b')
        l([(x,y+12),(59,y+26)],'#5a8d73',3)
    p([(30,88),(90,88),(81,131),(40,131)],'#c47e60');r((26,85,94,98),'#e0a17b',5)
    l([(45,104),(48,122)],'#f2c79c',3)
art('plant',(120,140),plant)

def skyline(r,e,p,l):
    # Clear upper sky; layered evening silhouettes along the bottom.
    for x,w,h in [(0,80,150),(75,110,240),(180,70,180),(250,100,320),(360,65,215),(430,100,270),(535,95,165),(640,110,240),(755,70,180),(820,160,305)]:
        r((x,600-h,x+w,600),'#b3cbc9',5)
        for wx in range(x+12,x+w-6,24):
            for wy in range(620-h,580,34): r((wx,wy,wx+8,wy+15),'#eee3b8aa',2)
    for x,w,h in [(0,120,90),(140,90,130),(300,110,80),(480,130,120),(690,100,110),(850,140,75)]:
        r((x,600-h,x+w,600),'#87aaa8',4)
art('skyline',(1000,600),skyline)

def cabin_painter(width,height):
    def paint(r,e,p,l):
        def box(a,b,c,d,color,radius=0): r((a*width,b*height,c*width,d*height),color,radius)
        def shape(points,color): p([(x*width,y*height) for x,y in points],color)
        # A straight, recessed stainless-steel frame, not an inflated rounded box.
        box(.003,.003,.997,.997,'#172a34',7)
        box(.012,.013,.988,.985,'#a8b1b2',5)
        for i in range(96):
            shade=178+int(32*math.sin(i/95*math.pi))
            box(.022+i*.00996,.018,.032+i*.00996,.97,(shade,shade+4,min(255,shade+5),255))
        box(.069,.145,.935,.947,'#233b43',2)
        # Interior perspective, wall seams and recessed ceiling strip.
        shape([(.078,.155),(.925,.155),(.857,.235),(.147,.235)],'#bcc8c7')
        shape([(.078,.155),(.147,.235),(.147,.85),(.078,.94)],'#75898e')
        shape([(.925,.155),(.857,.235),(.857,.85),(.925,.94)],'#627980')
        box(.147,.235,.857,.85,'#9eb0b2',2)
        for x in [.324,.502,.680]:box(x,.235,x+.003,.85,'#7d969c')
        box(.186,.256,.813,.275,'#d0dcda',1)
        shape([(.147,.85),(.857,.85),(.925,.94),(.078,.94)],'#495d64')
        for x in [.28,.5,.72]:shape([(x,.85),(x+.008,.85),(x+.03,.94),(x+.02,.94)],'#6e8287')
        box(.135,.71,.868,.725,'#546c74',2)
        box(.135,.704,.868,.714,'#e4e9e4',2)
        box(.24,.17,.76,.19,'#fff4cf',2)
        box(.326,.032,.674,.128,'#1a2c33',3)
        box(.339,.046,.661,.111,'#102129',2)
        # A small control panel beside the doors.
        box(.94,.38,.969,.72,'#5c737a',2)
        for y in [.42,.49,.56,.63]:
            e((.949*width,y*height,.962*width,(y+.012)*height),'#e8e9da')
        box(.075,.942,.931,.956,'#dce3df',1)
        box(.075,.96,.931,.972,'#586d73',1)
        # Fasteners and highlights make the frame read as metal at phone scale.
        for x in [.042,.956]:
            for y in [.045,.94]:e((x*width,y*height,(x+.009)*width,(y+.012)*height),'#516a72')
    return paint
art('cabin',(600,400),cabin_painter(600,400))
art('lobby_cabin',(320,520),cabin_painter(320,520))

def door(r,e,p,l):
    # Full-height glass, thin metal stiles and a visible centre joint.
    r((0,0,159,339),'#566e77',2)
    r((3,2,156,337),'#d1dbd9',1)
    r((7,7,152,313),(158,204,215,20),1)
    p([(10,10),(52,10),(145,304),(114,304)],(237,250,251,16))
    l([(12,9),(151,9)],'#f1f5ef',1)
    l([(6,7),(6,314)],'#ecf1eb',1)
    l([(155,7),(155,329)],'#344d58',2)
    r((144,143,148,192),'#c2d1cf',2)
    r((7,315,152,333),'#aabbbc',1)
    l([(10,320),(149,320)],'#d8e1dc',1)
art('door',(160,340),door)

def gear(r,e,p,l):
    points=[]
    for i in range(80):
        angle=2*math.pi*i/80
        radius=57 if i%8 in [0,1,2,3] else 46
        points.append((64+math.cos(angle)*radius,64+math.sin(angle)*radius))
    p(points,'white');e((25,25,103,103),'white');e((46,46,82,82),(0,0,0,0))
art('gear',(128,128),gear)

def roof(r,e,p,l):
    r((10,36,590,90),'#35515c',8);r((0,22,600,46),'#e5c398',7)
    r((22,49,578,82),'#203e4d',6)
    for x in range(35,580,30):r((x,53,x+12,65),'#b4c4b65a',3)
    r((186,0,414,37),'#f0dcc0',10)
art('roof',(600,96),roof)
print('Generated',len(list(ROOT.glob('*.png'))),'original PNG assets')
