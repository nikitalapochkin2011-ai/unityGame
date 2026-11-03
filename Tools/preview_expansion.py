"""Contact sheets of shipped sprites. These are NOT Unity screenshots."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[1]
a=root/'Assets/OneMoreFloor/Resources/Art';o=root/'Docs';o.mkdir(exist_ok=True)
f=ImageFont.truetype(str(root/'Assets/OneMoreFloor/Resources/Fonts/DejaVuSans.ttf'),18)
im=Image.new('RGB',(1100,310),'#efe4d1');d=ImageDraw.Draw(im)
labels=['Бабушка 1','Бабушка 2','Бабушка 3','Бабушка 4','Дедушка 1','Дедушка 2','Дедушка 3','Дедушка 4','Енот ×2']
for i in range(8,17):
    x=12+(i-8)*121;s=Image.open(a/f'person_{i}.png');s.thumbnail((110,260));im.paste(s,(x,18),s);d.text((x,275),labels[i-8],font=f,fill='#203e4d')
im.save(o/'NewPassengers.png')
im=Image.new('RGB',(1400,15*230),'#eee4d3');d=ImageDraw.Draw(im)
names=['Павлиний шёлк','Коралловая лагуна','Аметист','Розовый кварц','Сапфировый вечер','Новогодний','Оранжерея','Аквариум','Звёздный атлас','Бархатный театр','Эльфийский лес','Обитель дракона','Часовой механизм','Кристальный грот','Небесный дворец']
for i in range(10,25):
    y=(i-10)*230;rarity='Редкая' if i<15 else 'Эпическая' if i<20 else 'Легендарная'
    d.text((12,y+4),f'{rarity}: {names[i-10]} — фон / интерьер / лифт',fill='#203e4d',font=f)
    for k,prefix in enumerate(['background','interior','lift']):
        s=Image.open(a/f'{prefix}_{i}.png');s.thumbnail((445,190));im.paste(s,(12+k*465,y+32),s)
im.save(o/'CosmeticsCatalog.png')
print('Saved two sprite contact sheets (not Play Mode).')
