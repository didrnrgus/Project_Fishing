"""하늘 돔 텍스처(Assets/Resources/Sky)를 Poly Haven CC0 하늘 HDRI(땅 없는 "puresky")로 만든다.

  bpyenv/Scripts/python -I tools/sky/build_sky.py

- Poly Haven이 톤매핑해 둔 JPG(8K 위경도)를 받아(Temp/polyhaven, 한 번만) 줄인다.
- 맑은 날 돔(Fair01~03)은 절차적 하늘(Fishing/Sky) 위에 45%만 덮고 해는 절차적 하늘이 그리므로,
  사진 속 해와 그 둘레 빛무리를 지운다(바깥 고리에서 번져 들어오게 메운다).
- 같은 이름의 PNG를 덮어써서 .meta(GUID·큐브맵 자동 인식 설정)는 그대로 쓴다. 출처는 Assets/Resources/Sky/SOURCES.md.
"""
import json
import math
import os
import urllib.request

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Resources", "Sky")
CACHE = os.path.join(ROOT, "Temp", "polyhaven")

# (출력 이름, Poly Haven id, 가로 크기, 해 지우기)
SKIES = [
    ("Fair01", "kloofendal_48d_partly_cloudy_puresky", 2048, True),
    ("Fair02", "table_mountain_1_puresky", 2048, True),
    ("Fair03", "citrus_orchard_puresky", 2048, True),
    ("Overcast", "overcast_soil_puresky", 4096, False),
]


UA = {"User-Agent": "ProjectFishing-build/1.0"}  # 기본 urllib 요청은 Poly Haven이 막는다(403)


def get(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=UA)) as r:
        return r.read()


def fetch(asset):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, asset + ".jpg")
    if not os.path.exists(path):
        url = json.loads(get(f"https://api.polyhaven.com/files/{asset}"))["tonemapped"]["url"]
        print("받는 중", url)
        with open(path, "wb") as f:
            f.write(get(url))
    return path


def blur(a, r):
    """상자 흐림 세 번 (가우스 근사). a: (h, w) 또는 (h, w, c), 가로는 위경도라 감아 돈다."""
    for _ in range(3):
        for axis, wrap in ((1, True), (0, False)):
            n = a.shape[axis]
            pad = np.take(a, np.arange(-r, n + r) % n if wrap else np.clip(np.arange(-r, n + r), 0, n - 1), axis=axis)
            c = np.cumsum(pad, axis=axis, dtype=np.float64)
            c = np.concatenate([np.zeros_like(np.take(c, [0], axis=axis)), c], axis=axis)
            a = (np.take(c, np.arange(2 * r + 1, n + 2 * r + 1), axis=axis) - np.take(c, np.arange(0, n), axis=axis)) / (2 * r + 1)
    return a


def remove_sun(img):
    h, w, _ = img.shape
    lum = img @ np.array([0.2126, 0.7152, 0.0722])
    upper = lum[: h // 2]
    # 해 = 위쪽 반구에서 흐린 밝기가 가장 큰 곳
    sm = blur(upper, max(2, w // 512))
    y, x = np.unravel_index(np.argmax(sm), sm.shape)
    lon0, lat0 = (x + 0.5) / w * 2 * math.pi, math.pi / 2 - (y + 0.5) / h * math.pi
    ys, xs = np.mgrid[0:h, 0:w]
    lon, lat = (xs + 0.5) / w * 2 * math.pi, math.pi / 2 - (ys + 0.5) / h * math.pi
    cosd = np.sin(lat) * math.sin(lat0) + np.cos(lat) * math.cos(lat0) * np.cos(lon - lon0)
    ang = np.degrees(np.arccos(np.clip(cosd, -1, 1)))
    # 6° 안은 다 지우고 18°까지 서서히, 아래 반구에 비친 해(거울)도 같은 자리라 같이 지운다
    mirror = np.sin(-lat) * math.sin(lat0) + np.cos(lat) * math.cos(lat0) * np.cos(lon - lon0)
    ang = np.minimum(ang, np.degrees(np.arccos(np.clip(mirror, -1, 1))))
    m = np.clip((18.0 - ang) / 12.0, 0, 1)
    m = m * m * (3 - 2 * m)
    keep = 1 - m
    r = max(4, w // 64)
    fill = blur(img * keep[..., None], r) / np.maximum(blur(keep, r), 1e-4)[..., None]
    out = img * keep[..., None] + fill * m[..., None]
    print(f"  해: 경도 {math.degrees(lon0):.0f}° 고도 {math.degrees(lat0):.0f}°")
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, asset, width, sun in SKIES:
        print(name, "<-", asset)
        im = Image.open(fetch(asset)).convert("RGB").resize((width, width // 2), Image.LANCZOS)
        a = np.asarray(im, dtype=np.float64)
        if sun:
            a = remove_sun(a)
        Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).save(os.path.join(OUT, name + ".png"), optimize=True)


if __name__ == "__main__":
    main()
