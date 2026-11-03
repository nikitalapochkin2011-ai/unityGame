"""Capture 21 layouts × seven languages × three portrait viewports. Not Unity screenshots."""
from pathlib import Path
import subprocess,sys
root=Path(__file__).resolve().parents[2]
dest=Path(sys.argv[1]).resolve()
project=root/'Tools/UIReview/Review.csproj'
subprocess.run(['dotnet','build',str(project),'-v:q'],cwd=root,check=True)
dll=project.parent/'bin/Debug/net8.0/Review.dll'
for case,dimensions in [('normal',('600','1060')),('narrow',('360','800')),('notch',('390','844','47','34'))]:
 for lang in ['ru','en','de','fr','es','tr','zh-Hans']:
  subprocess.run(['dotnet',str(dll),str(dest/(case+'-'+lang)),lang,*dimensions],cwd=root,check=True,stdout=subprocess.DEVNULL)
  if '--rooftop' in sys.argv:
   roofdims=(*dimensions[:2],*(dimensions[2:] if len(dimensions)>2 else ('0','0')),'rooftop')
   subprocess.run(['dotnet',str(dll),str(dest/(case+'-'+lang)),lang,*roofdims],cwd=root,check=True,stdout=subprocess.DEVNULL)
 print('Captured',case,flush=True)
