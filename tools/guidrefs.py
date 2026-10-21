"""지우려는 폴더 안 에셋을 폴더 밖 에셋(씬·프리팹·머티리얼·.asset·.meta)이 GUID로 참조하는지 찾는다.

  python -I tools/guidrefs.py "Assets/AllSkyFree" "Assets/Fantasy Skybox FREE"

참조가 없으면 "참조 없음". 있으면 (참조하는 파일 → 폴더 안 파일) 목록. 코드(Resources.Load 경로·Shader.Find)는 따로 grep한다.
"""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
TEXT_EXT = {".unity", ".prefab", ".mat", ".asset", ".meta", ".controller", ".anim", ".overrideController", ".playable", ".shadergraph", ".lighting"}


def main(folders):
    folders = [os.path.normpath(os.path.join(ROOT, f)) for f in folders]
    owned = {}
    for folder in folders:
        for dp, _, fs in os.walk(folder):
            for f in fs:
                if not f.endswith(".meta"):
                    continue
                with open(os.path.join(dp, f), encoding="utf-8", errors="ignore") as h:
                    m = re.search(r"^guid: ([0-9a-f]{32})", h.read(), re.M)
                if m:
                    owned[m.group(1)] = os.path.relpath(os.path.join(dp, f[:-5]), ROOT)
    pat = re.compile(r"guid: ([0-9a-f]{32})")
    hits = []
    for dp, dns, fs in os.walk(os.path.join(ROOT, "Assets")):
        if any(os.path.normpath(dp).startswith(f) for f in folders):
            continue
        for f in fs:
            if os.path.splitext(f)[1] not in TEXT_EXT:
                continue
            p = os.path.join(dp, f)
            try:
                with open(p, encoding="utf-8", errors="ignore") as h:
                    text = h.read()
            except OSError:
                continue
            own = re.search(r"^guid: ([0-9a-f]{32})", text, re.M) if f.endswith(".meta") else None
            for g in set(pat.findall(text)):
                if g in owned and not (own and own.group(1) == g):
                    hits.append((os.path.relpath(p, ROOT), owned[g]))
    print(f"폴더 안 에셋 {len(owned)}개")
    if not hits:
        print("참조 없음")
    for a, b in sorted(hits):
        print(f"{a} -> {b}")


if __name__ == "__main__":
    main(sys.argv[1:])
