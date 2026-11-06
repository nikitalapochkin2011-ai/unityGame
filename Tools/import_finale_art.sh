#!/usr/bin/env bash
# Sprite extraction/registration of production illustrations; requires ImageMagick 7.
# Inputs: old-aircraft atlas (1024x1536), new-aircraft atlas (1024x1536),
# new-character atlas and old-character atlas (five equal-width cells each). Illustration sources are NOT redrawn by this tool.
set -euo pipefail
if [[ $# != 4 ]]; then echo "Usage: bash Tools/import_finale_art.sh old-craft.png new-craft.png new-guests.png old-guests.png" >&2; exit 2; fi
cd "$(dirname "$0")/.."
art=Assets/OneMoreFloor/Resources/Art
scratch=$(mktemp -d)
trap 'rm -rf "$scratch"' EXIT
extract_pair() {
 local source=$1 top=$2 bottom=$3 name=$4
 local width=0 height=0
 for col in 0 1; do
  magick "$source" -crop "512x$((bottom-top))+$((col*512))+$top" +repage "$scratch/cell.png"
  local bounds
  bounds=$(magick "$scratch/cell.png" -alpha extract -threshold 3% -format '%@' info:)
  magick "$scratch/cell.png" -crop "$bounds" +repage "$scratch/$col.png"
  read -r w h <<< "$(magick identify -format '%w %h' "$scratch/$col.png")"
  ((w>width)) && width=$w || true
  ((h>height)) && height=$h || true
 done
 # Identical canvases and grounded baselines keep the boarding transition registered.
 magick "$scratch/0.png" -background none -gravity south -extent "$((width+8))x$((height+8))" "PNG32:$art/$name.png"
 magick "$scratch/1.png" -background none -gravity south -extent "$((width+8))x$((height+8))" "PNG32:$art/${name}_boarded.png"
}
old_names=(ufo biplane rocket broom airship)
old_rows=(0 333 599 945 1197 1536)
new_names=(ornithopter balloon gyrocopter sailboat moon)
new_rows=(0 275 652 911 1223 1536)
for i in 0 1 2 3 4; do
 extract_pair "$1" "${old_rows[i]}" "${old_rows[i+1]}" "rooftop_${old_names[i]}"
 extract_pair "$2" "${new_rows[i]}" "${new_rows[i+1]}" "rooftop_${new_names[i]}"
done
for set in 0 1; do
 if ((set==0)); then source=$3; first=23; else source=$4; first=18; fi
 read -r atlas_w atlas_h <<< "$(magick identify -format '%w %h' "$source")"
 for i in 0 1 2 3 4; do
  left=$((i*atlas_w/5)); right=$(((i+1)*atlas_w/5))
  magick "$source" -crop "$((right-left))x$atlas_h+$left+0" +repage "$scratch/character.png"
  bounds=$(magick "$scratch/character.png" -alpha extract -threshold 3% -format '%@' info:)
  magick "$scratch/character.png" -crop "$bounds" +repage -resize 312x464 -background none -gravity south -extent 328x480 "PNG32:$art/person_$((i+first)).png"
 done
done
python - <<'PY'
from pathlib import Path
import re,uuid
art=Path('Assets/OneMoreFloor/Resources/Art')
for p in art.glob('*.png'):
 meta=Path(str(p)+'.meta')
 if meta.exists():continue
 text=(art/'person_0.png.meta').read_text()
 text=re.sub(r'guid: [0-9a-f]{32}','guid: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/rooftop/'+p.stem).hex,text,count=1)
 text=re.sub(r'spriteID: [0-9a-f]+','spriteID: '+uuid.uuid5(uuid.NAMESPACE_URL,'omf/sprite/'+p.stem).hex,text)
 meta.write_text(text)
PY
