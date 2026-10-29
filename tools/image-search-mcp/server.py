#!/usr/bin/env python3
"""image-search MCP server.

Claude Code가 키워드로 이미지를 검색하고, 로컬에 다운로드된 파일 경로를
받아 Read 도구로 직접 볼 수 있게 해주는 범용 MCP 서버.

검색 백엔드:
  1. BRAVE_API_KEY 환경변수가 있으면 Brave Image Search API (공식, 안정적)
  2. 없으면 ddgs (DuckDuckGo, 키 불필요, 비공식)
"""

from __future__ import annotations

import hashlib
import io
import os
import re
import time
from pathlib import Path
from typing import Any

import requests
from PIL import Image

try:  # mcp 2.x
    from mcp.server.mcpserver import MCPServer as _Server
except ImportError:  # mcp 1.x
    from mcp.server.fastmcp import FastMCP as _Server

mcp = _Server("image-search")

CACHE_DIR = Path(
    os.environ.get("IMAGE_SEARCH_DIR", Path.home() / ".cache" / "image-search-mcp")
).expanduser()
USER_AGENT = (
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/124.0 Safari/537.36"
)
MAX_DOWNLOAD_BYTES = 15 * 1024 * 1024
DOWNLOAD_TIMEOUT = 12


# ---------------------------------------------------------------- backends
def _search_brave(query: str, n: int) -> list[dict[str, Any]]:
    key = os.environ["BRAVE_API_KEY"]
    resp = requests.get(
        "https://api.search.brave.com/res/v1/images/search",
        headers={"X-Subscription-Token": key, "Accept": "application/json"},
        params={"q": query, "count": min(n, 50), "safesearch": "strict"},
        timeout=15,
    )
    resp.raise_for_status()
    out = []
    for r in resp.json().get("results", []):
        props = r.get("properties", {}) or {}
        url = props.get("url") or (r.get("thumbnail") or {}).get("src")
        if not url:
            continue
        out.append(
            {
                "image_url": url,
                "thumbnail_url": (r.get("thumbnail") or {}).get("src"),
                "title": r.get("title", ""),
                "source_page": r.get("url", ""),
            }
        )
    return out


def _search_ddgs(query: str, n: int) -> list[dict[str, Any]]:
    try:
        from ddgs import DDGS  # 신버전 패키지명
    except ImportError:  # 구버전 호환
        from duckduckgo_search import DDGS  # type: ignore

    out = []
    with DDGS() as d:
        for r in d.images(query, max_results=n, safesearch="moderate"):
            url = r.get("image")
            if not url:
                continue
            out.append(
                {
                    "image_url": url,
                    "thumbnail_url": r.get("thumbnail"),
                    "title": r.get("title", ""),
                    "source_page": r.get("url", ""),
                }
            )
    return out


def _search(query: str, n: int) -> tuple[str, list[dict[str, Any]]]:
    """(사용한 백엔드, 결과) 반환. Brave 실패 시 ddgs로 폴백."""
    errors = []
    if os.environ.get("BRAVE_API_KEY"):
        try:
            res = _search_brave(query, n)
            if res:
                return "brave", res
            errors.append("brave: 결과 없음")
        except Exception as e:  # noqa: BLE001
            errors.append(f"brave: {e}")
    try:
        res = _search_ddgs(query, n)
        return "ddgs", res
    except Exception as e:  # noqa: BLE001
        errors.append(f"ddgs: {e}")
    raise RuntimeError("모든 검색 백엔드 실패 → " + " | ".join(errors))


# ---------------------------------------------------------------- download
def _slug(text: str) -> str:
    s = re.sub(r"[^\w가-힣]+", "_", text, flags=re.UNICODE).strip("_")
    return s[:40] or "query"


def _download(url: str, dest_dir: Path, max_size: int) -> Path | None:
    try:
        with requests.get(
            url,
            headers={"User-Agent": USER_AGENT},
            timeout=DOWNLOAD_TIMEOUT,
            stream=True,
        ) as r:
            r.raise_for_status()
            buf = io.BytesIO()
            for chunk in r.iter_content(64 * 1024):
                buf.write(chunk)
                if buf.tell() > MAX_DOWNLOAD_BYTES:
                    return None
        buf.seek(0)
        img = Image.open(buf)
        img.load()
    except Exception:  # noqa: BLE001
        return None

    if max(img.size) > max_size:
        img.thumbnail((max_size, max_size), Image.LANCZOS)
    if img.mode not in ("RGB", "L"):
        img = img.convert("RGB")

    name = hashlib.sha1(url.encode()).hexdigest()[:10] + ".jpg"
    path = dest_dir / name
    img.save(path, "JPEG", quality=88)
    return path


# ---------------------------------------------------------------- tool
@mcp.tool()
def search_images(query: str, count: int = 5, max_size: int = 1024) -> dict:
    """키워드로 웹에서 이미지를 검색해 로컬에 다운로드하고 파일 경로를 반환한다.

    레퍼런스 이미지가 필요할 때(디자인, 게임/앱 아트, 실물 외형 확인, 스타일 참고 등)
    사용한다. 반환된 각 항목의 `path`를 Read 도구로 열면 이미지를 직접 볼 수 있다.

    영어 키워드가 결과 품질이 더 좋은 경우가 많다. 한국어 검색이 부정확하면
    영어나 학명으로 다시 시도할 것.

    Args:
        query: 검색 키워드 (예: "flounder fish", "cyberpunk street concept art")
        count: 받을 이미지 수 (1~10, 기본 5)
        max_size: 긴 변 기준 최대 픽셀 (기본 1024, 256~2048)

    Returns:
        backend, query, images[{path, title, image_url, source_page}], failed
    """
    query = query.strip()
    if not query:
        return {"error": "query가 비어 있습니다."}
    count = max(1, min(int(count), 10))
    max_size = max(256, min(int(max_size), 2048))

    try:
        # 다운로드 실패분을 감안해 넉넉히 요청
        backend, results = _search(query, count * 3)
    except Exception as e:  # noqa: BLE001
        return {"error": str(e)}

    if not results:
        return {"backend": backend, "query": query, "images": [], "failed": 0,
                "note": "검색 결과가 없습니다. 키워드를 바꿔 보세요."}

    dest = CACHE_DIR / f"{_slug(query)}_{int(time.time())}"
    dest.mkdir(parents=True, exist_ok=True)

    images, failed = [], 0
    for r in results:
        if len(images) >= count:
            break
        path = _download(r["image_url"], dest, max_size)
        if path is None and r.get("thumbnail_url"):
            path = _download(r["thumbnail_url"], dest, max_size)
        if path is None:
            failed += 1
            continue
        images.append(
            {
                "path": str(path),
                "title": r["title"],
                "image_url": r["image_url"],
                "source_page": r["source_page"],
            }
        )

    return {
        "backend": backend,
        "query": query,
        "images": images,
        "failed": failed,
        "note": "각 path를 Read 도구로 열어 이미지를 확인하세요. "
                "저작권이 있을 수 있으니 레퍼런스 용도로만 사용하세요.",
    }


if __name__ == "__main__":
    mcp.run()
