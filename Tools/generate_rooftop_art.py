"""Dinosaur and terrace authoring helpers in the existing warm style.
Production finale character and aircraft PNGs are imported by import_finale_art.sh.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import uuid,re
ROOT=Path(__file__).resolve().parents[1]/'Assets/OneMoreFloor/Resources/Art'
from art_canvas import Canvas
# A friendly upright dinosaur. The ring and tail fit inside the same passenger silhouette.
c=Canvas(164,240);c.e((24,217,140,233),(20,32,45,38))
c.p([(55,176),(14,174),(30,155),(40,126),(62,134)],'#739978')
for x,y in [(35,111),(36,130),(29,149),(18,167)]:c.p([(x,y),(x-13,y-8),(x-7,y+9)],'#c5b178')
c.e((20,129,146,179),'#b86d54');c.e((33,134,133,164),'#e8cba0')
c.r((45,168,74,214),'#779b78',10);c.r((90,168,119,214),'#779b78',10)
for x in (43,89):
 c.r((x,207,x+34,221),'#5c7b67',9)
 for a in (x+7,x+17,x+27):c.e((a,211,a+4,218),'#ede1bd')
c.e((43,93,125,190),'#8caa7d');c.e((60,111,111,178),'#d9d6a3')
c.r((32,108,51,157),'#8caa7d',10);c.r((116,109,136,156),'#8caa7d',10)
c.r((46,25,127,106),'#8caa7d',28);c.r((86,58,147,101),'#9ab788',19)
c.p([(49,42),(38,26),(59,28)],'#c7b57d');c.p([(69,27),(64,12),(83,26)],'#c7b57d')
c.e((69,49,77,58),'#283d36');c.e((70,49,73,52),'#fff5d9');c.e((133,71,138,75),'#587b60')
c.l([(103,86),(115,89),(129,85)],'#52745c',2);c.e((79,73,91,80),'#c1b58b')
for x,y in [(49,98),(113,118),(49,124)]:c.e((x,y,x+8,y+10),'#789b72')
# Front half of the swim ring wraps around the belly; soft cream bands and a valve.
c.l([(23,151),(30,166),(48,176),(79,181),(111,177),(133,167),(142,153)],'#b86d54',21)
c.l([(25,150),(33,162),(51,171),(79,175),(107,171),(130,162),(139,150)],'#d59770',16)
for pts in [[(38,158),(48,172)],[(75,167),(76,181)],[(115,161),(108,176)]]:c.l(pts,'#f2dfba',8)
c.r((132,142,141,150),'#eccea1',3);c.e((135,139,141,145),'#f7e8ca');c.save('person_17')
# Full-width terrace with a real open lift headhouse, garden, brass trim and railings.
c=Canvas(800,160)
c.r((1,128,798,155),'#8a9c8d',4);c.r((1,126,798,141),'#e9d3ac',4);c.r((1,143,798,154),'#59766a',3)
c.r((11,132,789,136),'#fff0d1',1)
for x in (31,115,199,283,367,451,535):c.r((x,78,x+5,127),'#6e8879',2)
c.r((18,77,559,83),'#6e8879',3);c.r((18,77,559,79),'#a8b5a0',2)
for x in (70,145):
 c.p([(x-21,105),(x+22,105),(x+16,132),(x-15,132)],'#bb8262');c.r((x-24,101,x+25,110),'#d09d76',4)
 c.l([(x,103),(x,60)],'#587a60',3)
 for xx,yy in [(x-14,67),(x+12,55),(x-13,87),(x+14,78)]:c.e((xx-10,yy-9,xx+10,yy+9),'#83a47b');c.l([(xx,yy),(x,yy+15)],'#63876a',2)
c.r((528,45,534,128),'#677f70',3);c.r((521,40,542,62),'#ead1a0',5);c.r((525,44,538,58),'#fff0c4',4)
c.r((571,20,798,134),'#657e75',3);c.r((582,26,787,130),'#263f3b',2)
c.r((578,23,587,132),'#b8c5b4',2);c.r((782,23,791,132),'#9fae9f',2)
c.r((573,128,797,136),'#d8c18f',2)
c.p([(568,23),(576,9),(792,9),(799,23)],'#435e52');c.r((568,20,799,28),'#91a58e',3);c.r((572,21,796,24),'#e0c38f',2)
c.r((604,30,757,36),'#e6d9ae',2);c.save('rooftop_landing')
