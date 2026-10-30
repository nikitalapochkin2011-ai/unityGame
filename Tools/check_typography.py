"""Check actual font coverage for all shipped translations (requires fonttools)."""
from pathlib import Path
from fontTools.ttLib import TTFont
import csv
root=Path(__file__).resolve().parents[1]
source=root/'Assets/OneMoreFloor/Resources/Localization/Strings.txt'
rows=list(csv.DictReader(source.open(encoding='utf-8'),delimiter='\t'))
labels='EnglishРусскийTürkçeEspañolDeutschFrançais简体中文'
text=''.join(value for row in rows for key,value in row.items() if key!='key')+labels
required=set(text.replace('\\n','\n'))-set('\n\r\t')
required.update('0123456789№×→←↑↓✓…–—•')
# Developer messages are not translated, but their Cyrillic still uses the bundled Latin font.
required.update(chr(c) for c in range(ord('А'),ord('я')+1)); required.update('Ёё')
latin={c for c in required if ord(c)<0x2e80}
han=required-latin
for name,chars in [('DejaVuSans.ttf',latin),('DejaVuSans-Bold.ttf',latin),('OMFHan-Regular.otf',han),('OMFHan-Bold.otf',han)]:
    path=root/'Assets/OneMoreFloor/Resources/Fonts'/name
    with TTFont(path) as font:
        cmap=font.getBestCmap()
        missing=sorted(c for c in chars if ord(c) not in cmap)
        assert not missing,(name,missing)
        assert font['name'].getDebugName(0),f'Copyright missing: {name}'
    assert 'includeFontData: 1' in Path(str(path)+'.meta').read_text()
print(f'PASS: 4 fonts cover {len(latin)} Latin/Cyrillic/symbol characters and {len(han)} CJK characters; regular and bold, with font data and copyrights.')
