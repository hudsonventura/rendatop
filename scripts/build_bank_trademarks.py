#!/usr/bin/env python3
from __future__ import annotations

import json
import os
import time
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "client" / "public" / "trademarks"
DOWNLOAD_CACHE_DIR = Path("/tmp/rendatop-bank-trademarks-commons-cache")
COMMONS_API_URL = "https://commons.wikimedia.org/w/api.php"
COMMONS_CATEGORY_URL = "https://commons.wikimedia.org/wiki/Category:SVG_logos_of_banks_in_Brazil"
REPO_URL = "https://github.com/Tgentil/Bancos-em-SVG"
REPO_ROOT = Path(os.getenv("BANCOS_EM_SVG_DIR", "/tmp/Bancos-em-SVG"))
USER_AGENT = "rendatop-bank-trademarks-builder/1.0"


def svg(content: str) -> str:
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 320 128" fill="none">'
        f"{content}</svg>\n"
    )


def pan_fallback() -> str:
    return svg(
        """
  <path d="M26 96V32h20c16 0 28 11 28 28S62 88 46 88H38v8Zm12-20h8c8 0 14-6 14-16 0-9-6-16-14-16h-8Z" fill="#111111"/>
  <text x="90" y="77" font-family="Arial, Helvetica, sans-serif" font-size="40" font-weight="700" fill="#F3C317">PAN</text>
"""
    )


COMMONS_SOURCES = {
    "001": {"bank": "Banco do Brasil", "title": "File:Banco do Brasil logo.svg"},
    "004": {"bank": "Banco do Nordeste", "title": "File:Logo do Banco do Nordeste.svg"},
    "021": {"bank": "Banestes", "title": "File:Logo do Banestes.svg"},
    "033": {"bank": "Santander", "title": "File:Banco Santander Logotipo.svg"},
    "041": {"bank": "Banrisul", "title": "File:Banrisul logotipo 2022.svg"},
    "077": {"bank": "Banco Inter", "title": "File:Inter&Co logo horizontal.svg"},
    "104": {"bank": "Caixa Economica Federal", "title": "File:Caixa Econômica Federal logo.svg"},
    "121": {"bank": "Agibank", "title": "File:Logo do Agibank.svg"},
    "136": {"bank": "Unicred", "title": "File:Logotipo do Unicred.svg"},
    "140": {"bank": "NuFinanceira", "title": "File:Nubank logo 2021.svg"},
    "208": {"bank": "BTG Pactual", "title": "File:Btg-logo-blue.svg"},
    "212": {"bank": "Banco Original", "title": "File:Logo do Banco Original.svg"},
    "218": {"bank": "BS2", "title": "File:Banco BS2.svg"},
    "237": {"bank": "Bradesco", "title": "File:Banco Bradesco logo.svg"},
    "260": {"bank": "Nubank", "title": "File:Nubank logo 2021.svg"},
    "318": {"bank": "BMG", "title": "File:Banco BMG.svg"},
    "323": {"bank": "Mercado Pago", "title": "File:Mercado Pago.svg"},
    "336": {"bank": "C6 Bank", "title": "File:Logo C6 Bank.svg"},
    "341": {"bank": "Itau", "title": "File:Itaú Unibanco logo 2023.svg"},
    "386": {"bank": "NuFinanceira", "title": "File:Nubank logo 2021.svg"},
    "637": {"bank": "Sofisa", "title": "File:Logo do Banco Sofisa.svg"},
    "655": {"bank": "BV", "title": "File:Banco BV Logo.svg"},
    "756": {"bank": "Sicoob", "title": "File:Logotipo do Sicoob.svg"},
}


FALLBACK_SOURCES = {
    "070": {"bank": "BRB", "repo_path": "BRB - Banco de Brasilia/brb-logo-nome.svg", "reason": "No matching SVG trademark found in the referenced Wikimedia Commons category."},
    "290": {"bank": "PagBank", "repo_path": "PagSeguro Internet S.A/logo-pagbank.svg", "reason": "No matching SVG trademark found in the referenced Wikimedia Commons category."},
    "389": {"bank": "Mercantil do Brasil", "repo_path": "Banco Mercantil do Brasil S.A/logo_mercantil-nome-branco.svg", "reason": "No matching SVG trademark found in the referenced Wikimedia Commons category."},
    "422": {"bank": "Safra", "repo_path": "Banco Safra S.A/logo-safra-nome.svg", "reason": "No matching SVG trademark found in the referenced Wikimedia Commons category."},
    "623": {"bank": "PAN", "generator": pan_fallback, "reason": "No matching SVG trademark found in the referenced Wikimedia Commons category or fallback repository."},
    "707": {"bank": "Daycoval", "repo_path": "Banco Daycoval/logo-Daycoval.svg", "reason": "Only PNG/JPG assets were found on Wikimedia Commons for this trademark."},
    "748": {"bank": "Sicredi", "repo_path": "Sicredi/logo-svg2.svg", "reason": "Only JPG assets were found on Wikimedia Commons for this trademark."},
}


def request_json(url: str) -> dict:
    for attempt in range(5):
        request = Request(url, headers={"User-Agent": USER_AGENT})
        try:
            with urlopen(request) as response:
                return json.loads(response.read().decode("utf-8"))
        except HTTPError as error:
            if error.code != 429 or attempt == 4:
                raise
            time.sleep(1.2 * (attempt + 1))
    raise RuntimeError(f"Unable to request {url}")


def download_bytes(url: str) -> bytes:
    for attempt in range(5):
        request = Request(url, headers={"User-Agent": USER_AGENT})
        try:
            with urlopen(request) as response:
                return response.read()
        except HTTPError as error:
            if error.code != 429 or attempt == 4:
                raise
            time.sleep(1.2 * (attempt + 1))
    raise RuntimeError(f"Unable to download {url}")


def resolve_commons_original_urls(titles: list[str]) -> dict[str, str]:
    query = urlencode(
        {
            "action": "query",
            "titles": "|".join(titles),
            "prop": "imageinfo",
            "iiprop": "url",
            "format": "json",
        }
    )
    payload = request_json(f"{COMMONS_API_URL}?{query}")
    resolved: dict[str, str] = {}
    for page in payload["query"]["pages"].values():
        title = page["title"]
        if "imageinfo" in page:
            resolved[title] = page["imageinfo"][0]["url"]
    return resolved


def log(message: str) -> None:
    print(f"[build_bank_trademarks] {message}")


def main() -> None:
    log(f"Output directory: {OUTPUT_DIR}")
    log(f"Fallback repo directory: {REPO_ROOT}")
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    DOWNLOAD_CACHE_DIR.mkdir(parents=True, exist_ok=True)

    removed_count = 0
    for old_svg in OUTPUT_DIR.glob("*.svg"):
        old_svg.unlink()
        removed_count += 1
    log(f"Removed {removed_count} old SVG file(s)")

    manifest = []

    titles = sorted({item["title"] for item in COMMONS_SOURCES.values()})
    title_to_cache_path = {
        title: DOWNLOAD_CACHE_DIR / f"{title.removeprefix('File:').replace('/', '_')}"
        for title in titles
    }
    missing_cached_titles = [
        title for title, cache_path in title_to_cache_path.items()
        if not cache_path.exists()
    ]
    title_urls = {}
    if missing_cached_titles:
        log(f"Resolving {len(missing_cached_titles)} Wikimedia Commons file(s)")
        query_titles = sorted(set(missing_cached_titles))
        if "File:Itaú Unibanco logo 2023.svg" in query_titles:
            query_titles.append("File:Banco Itaú logo.svg")
        if "File:Banrisul logotipo 2022.svg" in query_titles:
            query_titles.append("File:Banrisul Logo (2022).svg")
        if "File:Inter&Co logo horizontal.svg" in query_titles:
            query_titles.append("File:Logo do banco Inter (2023).svg")
        titles = sorted(set(query_titles))
        title_urls = resolve_commons_original_urls(titles)
        if "File:Itaú Unibanco logo 2023.svg" not in title_urls and "File:Banco Itaú logo.svg" in title_urls:
            title_urls["File:Itaú Unibanco logo 2023.svg"] = title_urls["File:Banco Itaú logo.svg"]
        if "File:Banrisul logotipo 2022.svg" not in title_urls and "File:Banrisul Logo (2022).svg" in title_urls:
            title_urls["File:Banrisul logotipo 2022.svg"] = title_urls["File:Banrisul Logo (2022).svg"]
        if "File:Inter&Co logo horizontal.svg" not in title_urls and "File:Logo do banco Inter (2023).svg" in title_urls:
            title_urls["File:Inter&Co logo horizontal.svg"] = title_urls["File:Logo do banco Inter (2023).svg"]
        time.sleep(1.0)
    else:
        log("Using cached Wikimedia Commons files")

    generated_from_commons = 0
    for code, item in sorted(COMMONS_SOURCES.items()):
        title = item["title"]
        cache_path = title_to_cache_path[title]
        if not cache_path.exists():
            if title not in title_urls:
                raise KeyError(f"Could not resolve Wikimedia Commons title: {title}")
            cache_path.write_bytes(download_bytes(title_urls[title]))
            time.sleep(0.8)
        (OUTPUT_DIR / f"{code}.svg").write_bytes(cache_path.read_bytes())
        generated_from_commons += 1
        manifest.append(
            {
                "code": code,
                "bank": item["bank"],
                "source_type": "wikimedia-commons",
                "source_category": COMMONS_CATEGORY_URL,
                "source_title": title,
                "source_page": f"https://commons.wikimedia.org/wiki/{title.replace(' ', '_')}",
                "cache_from": str(cache_path),
            }
        )

    generated_from_fallback = 0
    for code, item in sorted(FALLBACK_SOURCES.items()):
        output_path = OUTPUT_DIR / f"{code}.svg"
        if "repo_path" in item:
            source_path = REPO_ROOT / item["repo_path"]
            output_path.write_text(source_path.read_text(encoding="utf-8"), encoding="utf-8")
            generated_from_fallback += 1
            manifest.append(
                {
                    "code": code,
                    "bank": item["bank"],
                    "source_type": "github-repo-fallback",
                    "source_category": COMMONS_CATEGORY_URL,
                    "source_repo": REPO_URL,
                    "source_path": item["repo_path"],
                    "fallback_reason": item["reason"],
                }
            )
            continue

        output_path.write_text(item["generator"](), encoding="utf-8")
        generated_from_fallback += 1
        manifest.append(
            {
                "code": code,
                "bank": item["bank"],
                "source_type": "custom-fallback",
                "source_category": COMMONS_CATEGORY_URL,
                "fallback_reason": item["reason"],
            }
        )

    manifest.sort(key=lambda entry: entry["code"])
    (OUTPUT_DIR / "sources.json").write_text(
        json.dumps(manifest, ensure_ascii=True, indent=2) + "\n",
        encoding="utf-8",
    )

    (OUTPUT_DIR / "README.md").write_text(
        "# Trademarks\n\n"
        "SVG bank trademarks keyed by Brazilian bank code.\n\n"
        f"- Main source: `{COMMONS_CATEGORY_URL}`.\n"
        "- Files from Wikimedia Commons were downloaded from the original SVG linked by each file page.\n"
        "- `140.svg`, `260.svg`, and `386.svg` intentionally reuse the Nubank trademark because they belong to the same group.\n"
        "- Banks without a suitable SVG trademark in that Commons category use a repo or custom fallback documented in `sources.json`.\n",
        encoding="utf-8",
    )

    log(
        "Generated "
        f"{generated_from_commons} Commons SVG(s) and {generated_from_fallback} fallback SVG(s)"
    )
    log(f"Wrote manifest: {OUTPUT_DIR / 'sources.json'}")
    log("Done")


if __name__ == "__main__":
    main()
