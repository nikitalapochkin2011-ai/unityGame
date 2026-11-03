"""Rasterize captured C# UI layout and real sprite/mesh data; not a Unity screenshot."""
from pathlib import Path
from functools import lru_cache
from PIL import Image,ImageDraw,ImageFont,ImageChops
import json,math,re,sys
ROOT=Path(__file__).resolve().parents[2]
ART=ROOT/'Assets/OneMoreFloor/Resources/Art';FONTS=ROOT/'Assets/OneMoreFloor/Resources/Fonts'
S=2;SIZE=(600*S,1060*S)
issues=[]
@lru_cache(160)
def font(size,bold,han=False):
 return ImageFont.truetype(str(FONTS/('OMFHan-Bold.otf' if bold else 'OMFHan-Regular.otf') if han else FONTS/('DejaVuSans-Bold.ttf' if bold else 'DejaVuSans.ttf')),size)
def color(v):return tuple(max(0,min(255,round(x*255))) for x in v)
def box(e):return tuple(round(x*S) for x in (e['x'],e['y'],e['x']+e['w'],e['y']+e['h']))
def wrapped(text,f,width):
 out=[]
 for paragraph in text.split('\n'):
  if not paragraph:out.append('');continue
  words=list(paragraph) if any('\u2e80'<=c for c in paragraph) else paragraph.split(' ')
  join='' if any('\u2e80'<=c for c in paragraph) else ' '
  line=''
  for word in words:
   nextline=(line+join+word) if line else word
   if f.getlength(nextline)<=width or not line:line=nextline
   else:out.append(line);line=word
  out.append(line)
 return out

def render(path,out):
 global SIZE
 data=json.loads(path.read_text());viewWidth=round(next(e['w'] for e in data if e['name']=='Evening sky'));SIZE=(viewWidth*S,1060*S);im=Image.new('RGBA',SIZE,'#f4f0e6')
 for e in data:
  if e['w']<=0 or e['h']<=0:continue
  b=box(e);layer=Image.new('RGBA',SIZE);d=ImageDraw.Draw(layer);typ=e['type']
  if typ=='text':
   if e['w']<2 or e['h']<2:continue
   han=any('\u2e80'<=c for c in e['text']);size=round(e['size']*S);small=math.ceil(e['min']*S)
   for n in range(size,small-1,-1):
    f=font(max(1,n),e['bold'],han);lines=wrapped(e['text'],f,e['w']*S)
    if max((f.getlength(t) for t in lines),default=0)<=e['w']*S+.5 and len(lines)*n*1.10<=e['h']*S+1:break
   else:
    issues.append({'screen':path.stem,'name':e['name'],'text':e['text'],'bounds':[e['x'],e['y'],e['w'],e['h']],'size':n/S})
   lineheight=n*1.10;align=e['align'];top=b[1] if align.startswith('Top') else b[3]-len(lines)*lineheight if align.startswith('Bottom') else (b[1]+b[3]-len(lines)*lineheight)/2
   for i,t in enumerate(lines):
    width=f.getlength(t);x=b[0] if align.endswith('Left') or align=='Left' else b[2]-width if align.endswith('Right') or align=='Right' else (b[0]+b[2]-width)/2
    d.text((round(x),round(top+i*lineheight+lineheight*.5)),t,font=f,fill=color(e['color']),anchor='lm')
  elif typ=='gradient':
   w=max(1,b[2]-b[0]);h=max(1,b[3]-b[1]);grad=Image.new('RGBA',(w,h));gd=ImageDraw.Draw(grad)
   for y in range(h):
    t=y/max(1,h-1);c=[e['top'][i]*(1-t)+e['bottom'][i]*t for i in range(4)];gd.line((0,y,w,y),fill=color(c))
   if e['radius']>0:
    mask=Image.new('L',(w,h));ImageDraw.Draw(mask).rounded_rectangle((0,0,w-1,h-1),e['radius']*S,fill=255);grad.putalpha(ImageChops.multiply(grad.getchannel('A'),mask))
   layer.paste(grad,(b[0],b[1]))
  elif typ=='shape':
   radius=e['radius']*S
   for i in range(6,0,-1):
    if e['shadowOpacity']<=0:break
    shadow=Image.new('RGBA',SIZE);spread=e['shadowBlur']*i/6*S;offset=e['shadowOffset']*S
    ImageDraw.Draw(shadow).rounded_rectangle((round(b[0]-spread),round(b[1]-spread+offset),round(b[2]+spread),round(b[3]+spread+offset)),radius=radius+spread,fill=color([.125,.239,.212,e['shadowOpacity']*(7-i)/21]))
    layer=Image.alpha_composite(layer,shadow)
   d=ImageDraw.Draw(layer);stroke=e['stroke']*S
   if stroke>0 and e['edge'][3]>0:
    d.rounded_rectangle(b,radius=radius,fill=color(e['edge']))
    d.rounded_rectangle((round(b[0]+stroke),round(b[1]+stroke),round(b[2]-stroke),round(b[3]-stroke)),radius=max(0,radius-stroke),fill=color(e['color']))
   else:d.rounded_rectangle(b,radius=radius,fill=color(e['color']))
  elif typ=='image':
   radius=e['radius']*S
   if 'shadow' in e:d.rounded_rectangle((b[0],b[1]+round(e['shadowY']*S),b[2],b[3]+round(e['shadowY']*S)),radius=radius,fill=color(e['shadow']))
   if not e['asset'] or e['asset']=='panel':d.rounded_rectangle(b,radius=radius,fill=color(e['color']))
   else:
    source=Image.open(ART/(e['asset']+'.png')).convert('RGBA')
    if e['contain']:
     source.thumbnail((max(1,b[2]-b[0]),max(1,b[3]-b[1])),Image.Resampling.LANCZOS);point=(round((b[0]+b[2]-source.width)/2),round((b[1]+b[3]-source.height)/2))
    else:source=source.resize((max(1,b[2]-b[0]),max(1,b[3]-b[1])),Image.Resampling.LANCZOS);point=(b[0],b[1])
    source=ImageChops.multiply(source,Image.new('RGBA',source.size,color(e['color'])));layer.paste(source,point)
  else:
   cx=(e['x']+e['w']/2)*S;cy=(e['y']+e['h']/2)*S
   for tri in e['tri']:
    vs=[e['v'][i] for i in tri];d.polygon([(round(cx+v['p'][0]*S),round(cy-v['p'][1]*S)) for v in vs],fill=color(vs[0]['c']))
  for clip in e.get('clip',[]):
   mask=Image.new('L',SIZE);cd=ImageDraw.Draw(mask);x,y,w,h=clip;cd.rectangle(tuple(round(v*S) for v in (x,y,x+w,y+h)),fill=255);layer.putalpha(ImageChops.multiply(layer.getchannel('A'),mask))
  if e.get('opacity',1)<1:layer.putalpha(layer.getchannel('A').point(lambda v:round(v*e['opacity'])))
  im=Image.alpha_composite(im,layer)
 out.parent.mkdir(parents=True,exist_ok=True);im.convert('RGB').resize((viewWidth,1060),Image.Resampling.LANCZOS).save(out)
 return im

if __name__=='__main__':
 folder=Path(sys.argv[1]);dest=Path(sys.argv[2]);dest.mkdir(parents=True,exist_ok=True)
 for path in sorted(folder.glob('*.json')):render(path,dest/(path.stem+'.png'))
 Path(str(dest)+'-text-report.json').write_text(json.dumps(issues,ensure_ascii=False,indent=2))
 print('Rendered',len(list(folder.glob('*.json'))),'source-layout previews; possible text fit issues:',len(issues))
