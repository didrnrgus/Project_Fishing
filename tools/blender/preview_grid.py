"""미리보기 렌더 여러 장을 한 장으로 (2열): bpyenv/Scripts/python -I tools/blender/preview_grid.py out.png <폴더> carp_side carp_head ..."""
import sys

from PIL import Image

out, d, names = sys.argv[1], sys.argv[2], sys.argv[3:]
ims = [Image.open(f'{d}/{n}.png').convert('RGB') for n in names]
w, h = ims[0].size
s = Image.new('RGB', (w * 2, h * ((len(ims) + 1) // 2)))
for i, im in enumerate(ims):
    s.paste(im.resize((w, h)), ((i % 2) * w, (i // 2) * h))
s.thumbnail((1600, 1800))
s.save(out)
