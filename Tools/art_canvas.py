"""Shared original sprite-authoring helpers; Pillow is only an authoring dependency."""
from pathlib import Path
from PIL import Image,ImageDraw
import uuid,re
ROOT=Path(__file__).resolve().parents[1]/'Assets/OneMoreFloor/Resources/Art'
class Canvas:
 def __init__(self,w,h):self.w,self.h=w,h;self.im=Image.new('RGBA',(w*3,h*3));self.d=ImageDraw.Draw(self.im)
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
   text=(ROOT/'person_0.png.meta').read_text()
   text=re.sub(r'guid: [0-9a-f]{32}','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/rooftop/'+name).hex,text,count=1)
   text=re.sub(r'spriteID: [0-9a-f]+','spriteID: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/sprite/'+name).hex,text)
   meta.write_text(text)
