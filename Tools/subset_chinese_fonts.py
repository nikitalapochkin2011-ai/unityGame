"""Create the shipped OFL font subsets. Optional authoring tool; not used by the game.
Usage: python Tools/subset_chinese_fonts.py /path/to/NotoSansCJKsc-Regular.otf /path/to/NotoSansCJKsc-Bold.otf
Official sources and licence are recorded in Resources/Fonts/CHINESE-FONTS.md.
"""
from pathlib import Path
import sys
from fontTools import subset
from fontTools.ttLib import TTFont
root=Path(__file__).resolve().parents[1]
text=(root/'Assets/OneMoreFloor/Resources/Localization/Strings.txt').read_text()+'简体中文'
characters=''.join(sorted({c for c in text if ord(c)>=0x2e80} | {chr(c) for c in range(32,127)}))
assert len(sys.argv)==3, __doc__
for source,weight in zip(sys.argv[1:],['Regular','Bold']):
    font=TTFont(source)
    missing=[c for c in characters if ord(c) not in font.getBestCmap()]
    assert not missing,missing
    options=subset.Options()
    options.layout_features=['*']; options.name_IDs=['*']; options.name_languages=['*']
    options.name_legacy=True; options.notdef_glyph=True; options.notdef_outline=True
    cutter=subset.Subsetter(options=options); cutter.populate(text=characters); cutter.subset(font)
    # Distinct family name for the subset; original copyright/licence records are retained.
    for record in font['name'].names:
        if record.nameID in (1,2,3,4,6,16,17):
            value={1:'OMF Han',2:weight,3:'OMF Han '+weight,4:'OMF Han '+weight,6:'OMFHan-'+weight,16:'OMF Han',17:weight}[record.nameID]
            record.string=value.encode(record.getEncoding(),errors='strict')
    if 'CFF ' in font:
        font['CFF '].cff.fontNames=['OMFHan-'+weight]
        top=font['CFF '].cff.topDictIndex[0]; top.FamilyName='OMF Han'; top.FullName='OMF Han '+weight
    target=root/'Assets/OneMoreFloor/Resources/Fonts'/('OMFHan-'+weight+'.otf')
    font.save(target)
    print(target.name, len(characters),'covered characters (CJK + ASCII);',target.stat().st_size,'bytes')
