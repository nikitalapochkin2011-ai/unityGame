"""Validate committed assets without starting Unity (requires Pillow for PNG decoding)."""
from pathlib import Path
from PIL import Image
import re
root=Path(__file__).resolve().parents[1]
art=root/'Assets/OneMoreFloor/Resources/Art'
expected=['panel','circle','triangle','window','plant','skyline','cabin','door','lobby_cabin','gear','hallway']+[f'person_{i}' for i in range(8)]+['roof']
for name in expected:
    p=art/(name+'.png')
    with Image.open(p) as im:
        im.load()
        assert im.mode=='RGBA',p
        assert im.width<=2048 and im.height<=2048,p
        if name!='hallway': assert im.getextrema()[3][0]==0, f'{name} must have a transparent background'
    meta=Path(str(p)+'.meta').read_text()
    assert 'spriteMode: 1' in meta and 'textureType: 8' in meta,p
    assert 'spritePixelsToUnits: 200' in meta,p
assert 'spriteBorder: {x: 48, y: 48, z: 48, w: 48}' in (art/'panel.png.meta').read_text()
guids={}
for p in (root/'Assets').rglob('*.meta'):
    guid=re.search(r'^guid: ([0-9a-f]{32})$',p.read_text(),re.M)
    assert guid,p
    assert guid[1] not in guids,(p,guids.get(guid[1]))
    guids[guid[1]]=p
for p in (root/'Assets').rglob('*'):
    if p.suffix!='.meta': assert Path(str(p)+'.meta').exists(),p
print(f'PASS: {len(expected)} RGBA sprites decode; sprite imports, panel borders, {len(guids)} unique asset GUIDs and metadata checked.')
