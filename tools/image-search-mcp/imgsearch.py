"""물고기 실물 사진 찾기 (MCP 없이 바로): 검색 → 내려받기 → 한 장 모음(contact sheet).

  tools/image-search-mcp/.venv/Scripts/python tools/image-search-mcp/imgsearch.py "Siniperca scherzeri side view" 6 out.jpg

out.jpg를 Read로 열어 본다. 사진은 눈으로 보는 참고용이고 저장소에 넣지 않는다.
"""
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import server  # noqa: E402
from PIL import Image  # noqa: E402

q = sys.argv[1]
n = int(sys.argv[2]) if len(sys.argv) > 2 else 6
out = sys.argv[3] if len(sys.argv) > 3 else 'contact.jpg'
r = server.search_images(q, n, 640)
paths = [i['path'] for i in r.get('images', [])]
print(json.dumps({'backend': r.get('backend'), 'err': r.get('error'), 'n': len(paths)}, ensure_ascii=False))
if paths:
    ims = [Image.open(p).convert('RGB') for p in paths]
    for im in ims:
        im.thumbnail((400, 300))
    cols = 3
    rows = (len(ims) + cols - 1) // cols
    sheet = Image.new('RGB', (400 * cols, 300 * rows), 'white')
    for i, im in enumerate(ims):
        sheet.paste(im, ((i % cols) * 400, (i // cols) * 300))
    sheet.save(out, quality=85)
    print(out)
