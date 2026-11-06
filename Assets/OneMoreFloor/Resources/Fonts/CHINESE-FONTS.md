# OMF Han: Chinese font subsets

These renamed subsets of **Noto Sans CJK SC** cover all Simplified Chinese characters used by the seven-language catalogue, plus ASCII for font metrics and spacing. They include 625 code points each. The game uses separate regular and bold SDF assets as fallbacks to DejaVu Sans. No font downloads occur in the game.

Original copyright statement retained inside both fonts:

> © 2014-2021 Adobe (http://www.adobe.com/).

Upstream official sources (retrieved 2026-11-03):

- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Bold.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE

The original SIL Open Font License 1.1 is included as Noto-OFL.txt. Font families and PostScript names have been changed to **OMF Han / OMFHan-Regular / OMFHan-Bold** for these subsets. Copyright and licence metadata remain in the font files.

Regenerate after adding Chinese characters: download the two official upstream OTF files, then run `python Tools/subset_chinese_fonts.py /path/to/NotoSansCJKsc-Regular.otf /path/to/NotoSansCJKsc-Bold.otf` (requires fonttools). This changes the source font subsets; recreate the generated GameHan SDF assets in Unity afterwards. `python Tools/check_typography.py` reports missing glyphs before packaging. Preserve the `.meta` GUIDs when updating fonts.

SHA-256 of shipped subsets:

- OMFHan-Regular.otf: `f6d50eafa795105602ce5e424529647f3ec6f6ea3a00e3a1ee4b7ef196e63c34`
- OMFHan-Bold.otf: `100b706816e02370ffba73856d3a7debef9721fd43d13caae5f1c4a0dcc566b3`
