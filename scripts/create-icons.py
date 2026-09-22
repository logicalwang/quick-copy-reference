from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

root = Path(__file__).resolve().parents[1] / 'dev.vicky.copyreference.sdPlugin' / 'imgs' / 'plugin'
root.mkdir(parents=True, exist_ok=True)


def icon(size):
    image = Image.new('RGBA', (size, size), '#101c2c')
    draw = ImageDraw.Draw(image)
    unit = size / 288
    width = max(2, round(6 * unit))
    margin = round(16 * unit)
    draw.rounded_rectangle((margin, margin, size-margin, size-margin), radius=round(30*unit), outline='#62caba', width=width)
    # Stacked document leaves plus a visible Markdown link cue.
    draw.rounded_rectangle((round(79*unit), round(66*unit), round(176*unit), round(190*unit)), radius=round(11*unit), outline='#62caba', width=width)
    draw.rounded_rectangle((round(109*unit), round(89*unit), round(210*unit), round(213*unit)), radius=round(11*unit), fill='#101c2c', outline='#62caba', width=width)
    for y in (119, 144, 169):
        draw.line((round(131*unit),round(y*unit),round(185*unit),round(y*unit)), fill='#dce8ee', width=max(2,round(5*unit)))
    return image


for filename, size in [('category-icon.png', 28), ('category-icon@2x.png', 56), ('marketplace.png', 288), ('marketplace@2x.png', 512)]:
    icon(size).save(root / filename)
