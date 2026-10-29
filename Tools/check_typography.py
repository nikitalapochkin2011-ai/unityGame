"""Check shipped font data and glyph coverage (authoring dependency: fonttools)."""
from pathlib import Path
from fontTools.ttLib import TTFont
root=Path(__file__).resolve().parents[1]
required=set('0123456789АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя№×→←↑↓✓…–—•')
for p in (root/'Assets/OneMoreFloor').rglob('*.cs'):
    required.update(c for c in p.read_text() if ord(c)>127 and not c.isspace())
for name in ['DejaVuSans','DejaVuSans-Bold']:
    path=root/'Assets/OneMoreFloor/Resources/Fonts'/(name+'.ttf')
    with TTFont(path) as font:
        cmap=font.getBestCmap()
        missing=sorted(c for c in required if ord(c) not in cmap)
        assert not missing,(name,missing)
    assert 'includeFontData: 1' in Path(str(path)+'.meta').read_text()
print(f'PASS: regular/bold bundled fonts contain all {len(required)} required Cyrillic, digit and symbol glyphs; font data is included.')
