# OMF Han: Chinese font subsets

These renamed subsets of **Noto Sans CJK SC** cover all Simplified Chinese characters used by the seven-language catalogue, plus ASCII for font metrics and spacing. They include 628 code points each. The game uses separate regular and bold SDF assets as fallbacks to DejaVu Sans. No font downloads occur in the game.

Original copyright statement retained inside both fonts:

> © 2014-2021 Adobe (http://www.adobe.com/).

Upstream official sources (retrieved 2026-11-03):

- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Bold.otf
- https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE

The original SIL Open Font License 1.1 is included as Noto-OFL.txt. Font families and PostScript names have been changed to **OMF Han / OMFHan-Regular / OMFHan-Bold** for these subsets. Copyright and licence metadata remain in the font files.

Regenerate after adding Chinese characters: download the two official upstream OTF files, then run `python Tools/subset_chinese_fonts.py /path/to/NotoSansCJKsc-Regular.otf /path/to/NotoSansCJKsc-Bold.otf` (requires fonttools). This changes the source font subsets; recreate the generated GameHan SDF assets in Unity afterwards. `python Tools/check_typography.py` reports missing glyphs before packaging. Preserve the `.meta` GUIDs when updating fonts.

SHA-256 of shipped subsets:

- OMFHan-Regular.otf: `3fb41fff85fe69dca653d81528c9e1dd2323e578bc76de80846be126f0822014`
- OMFHan-Bold.otf: `1fb24b68351b0dfcab1b019867145896501e4736d9539e4b71c2fdd762f32743`
