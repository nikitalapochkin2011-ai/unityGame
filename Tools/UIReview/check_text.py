from pathlib import Path
import json,math,sys
from render import font,wrapped,S
root=Path(sys.argv[1]);issues=[];scenes=0;labels=0
for path in root.glob('*/*.json'):
 scenes+=1
 for e in json.loads(path.read_text()):
  if e['type']!='text' or e['w']<=0 or e['h']<=0:continue
  labels+=1;han=any('\u2e80'<=c for c in e['text']);size=round(e['size']*S);small=min(size,math.ceil(e['min']*S))
  fits=False
  for n in range(size,small-1,-1):
   f=font(max(n,1),e['bold'],han);lines=wrapped(e['text'],f,e['w']*S)
   if max((f.getlength(t) for t in lines),default=0)<=e['w']*S+1 and len(lines)*n*1.10<=e['h']*S+1:fits=True;break
  if not fits:issues.append({'language':path.parent.name,'screen':path.stem,'name':e['name'],'text':e['text'],'bounds':[round(e['w'],1),round(e['h'],1)]})
(root/'text-fit-report.json').write_text(json.dumps(issues,ensure_ascii=False,indent=2))
print('Source-layout font fit:',scenes,'scenes;',labels,'text rectangles;',len(issues),'possible overflow candidates')
for issue in issues[:18]:print(issue)
