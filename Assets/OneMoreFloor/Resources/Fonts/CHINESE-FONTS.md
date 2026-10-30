# OMF Han: Chinese font subsets

These renamed subsets of **Noto Sans CJK SC** cover all Simplified Chinese characters used by the seven-language catalogue, plus ASCII for font metrics and spacing. They include 437 code points each. The game uses separate regular and bold SDF assets as fallbacks to DejaVu Sans. No font downloads occur in the game.

Original copyright statement retained inside both fonts:

> © 2014-2021 Adobe (http://www.adobe.com/).

Upstream official sources (retrieved 2026-10-30):

- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Bold.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE

The original SIL Open Font License 1.1 is included as Noto-OFL.txt. Font families and PostScript names have been changed to **OMF Han / OMFHan-Regular / OMFHan-Bold** for these subsets. Copyright and licence metadata remain in the font files.

Regenerate after adding Chinese characters: download the two official upstream OTF files, then run `python Tools/subset_chinese_fonts.py /path/to/NotoSansCJKsc-Regular.otf /path/to/NotoSansCJKsc-Bold.otf` (requires fonttools). This changes the source font subsets; recreate the generated GameHan SDF assets in Unity afterwards. `python Tools/check_typography.py` reports missing glyphs before packaging. Preserve the `.meta` GUIDs when updating fonts.

SHA-256 of shipped subsets:

- OMFHan-Regular.otf: `177e406c7159e15b4b4366c4fa57bf5a013c3f171139584bd0847370f3799b7d`
- OMFHan-Bold.otf: `48677c920728a4e5be0eb21c6d155166e87817b9d265367f7821e14dc31ce99b`
