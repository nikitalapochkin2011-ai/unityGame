"""Offline art/layout inspection, NOT a screenshot or substitute for Unity Play Mode."""
from pathlib import Path
import re
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
constants={n:float(v) for n,v in re.findall(r'(\w+)\s*=\s*([.\d]+)f',(root/'Assets/OneMoreFloor/Runtime/BuildingLayout.cs').read_text())}
art=root/'Assets/OneMoreFloor/Resources/Art'
W,H=1200,212
scale=W/(600*.99)
canvas=Image.new('RGBA',(1280,440),'#e8dfca')
draw=ImageDraw.Draw(canvas)
regular=root/'Assets/OneMoreFloor/Resources/Fonts/DejaVuSans.ttf'
bold=root/'Assets/OneMoreFloor/Resources/Fonts/DejaVuSans-Bold.ttf'
f=ImageFont.truetype(str(bold),26)
small=ImageFont.truetype(str(regular),18)
draw.text((40,16),'Семь мест в очереди · одинаковый размер людей',font=f,fill='#203e4d')
draw.text((40,53),'Иллюстрация из ресурсов проекта; не кадр Unity.',font=small,fill='#527779')
xbase,ybase=40,100

def bounds(x0,y0,x1,y1):return (round(xbase+x0*W),round(ybase+(1-y1)*H),round(xbase+x1*W),round(ybase+(1-y0)*H))
def sprite(name,x0,y0,x1,y1,preserve=False):
    x,y,x2,y2=bounds(x0,y0,x1,y1)
    im=Image.open(art/(name+'.png')); w,h=x2-x,y2-y
    if preserve:
        im.thumbnail((w,h),Image.Resampling.LANCZOS); x+=(w-im.width)//2; y+=(h-im.height)//2
    else:im=im.resize((w,h),Image.Resampling.LANCZOS)
    canvas.alpha_composite(im,(x,y))

draw.rounded_rectangle((xbase,ybase,xbase+W,ybase+H),10,fill='#ccb39b')
draw.rectangle(bounds(.003,.008,.069,.992),fill='#203e4d')
draw.rectangle(bounds(.731,.008,.982,.992),fill='#1b3542')
sprite('hallway',constants['HallLeft'],.055,constants['HallRight'],.97)
sprite('cabin',constants['LiftLeft'],.025,constants['LiftRight'],.99)
for i in range(7):
    center=constants['WaitingRight']-(constants['WaitingRight']-constants['WaitingLeft'])*(i+.5)/7
    height=constants['PersonHeight']; width=constants['PersonWidth']; floor=.08
    sprite('person_'+str(i),center-width/2,floor+.06*height,center+width/2,floor+.67*height,True)
    bx=round(xbase+center*W); by=round(ybase+(1-floor-.7*height)*H)
    bw,bh=round(32*scale),round(28*scale)
    draw.rounded_rectangle((bx-bw//2,by-bh,bx+bw//2,by),radius=10,fill='#fff2dc')
    draw.text((bx,by-bh/2),str(i%5+2),font=ImageFont.truetype(str(bold),round(22*scale)),fill='#203e4d',anchor='mm')
    draw.rounded_rectangle(bounds(center-width*.4,floor+.015*height,center+width*.4,floor+.055*height),3,fill='#387d7a')
# Left floor button and a separate counter, matching the Unity floor plan.
draw.rounded_rectangle(bounds(.01,.29,.063,.83),10,fill='#387d7a')
draw.text((xbase+.0365*W,ybase+.43*H),'3',font=ImageFont.truetype(str(bold),48),fill='#fff2dc',anchor='mm')
draw.text((xbase+.036*W,ybase+.82*H),'7/7',font=ImageFont.truetype(str(bold),19),fill='#e5b965',anchor='mm')
draw.text((40,342),'Светлый камень · дубовые панели · латунь · шалфейные двери',font=small,fill='#527779')
draw.text((40,375),'Первый пассажир справа, ближе к лифту. Все этажи используют этот интерьер.',font=small,fill='#527779')
canvas.convert('RGB').save(root/'Docs/SevenPassengerPreview.png')
print('Saved Docs/SevenPassengerPreview.png')
