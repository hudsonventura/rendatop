#!/usr/bin/env python3
from __future__ import annotations

import json
import os
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "client" / "public" / "bank-logos"
SOURCE_ROOT = Path(os.getenv("BANCOS_EM_SVG_DIR", "/tmp/Bancos-em-SVG"))
REPO_URL = "https://github.com/Tgentil/Bancos-em-SVG"


def svg(content: str) -> str:
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" fill="none">'
        f"{content}</svg>\n"
    )


def agibank_fallback() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" fill="#F36F21"/>
  <circle cx="64" cy="64" r="14" fill="#ffffff"/>
  <path d="M78 36h14v56H78z" fill="#283B8F"/>
"""
    )


def pan_fallback() -> str:
    return svg(
        """
  <path d="M34 92V36h20c14 0 24 10 24 24S68 84 54 84H46v8Zm12-20h8c7 0 12-5 12-12s-5-12-12-12h-8Z" fill="#111111"/>
  <path d="M78 92V36h14l12 32 12-32h4v56h-10V58l-8 22H94l-8-22v34Z" fill="#F3C317"/>
"""
    )


SOURCES = {
    "001": {"bank": "Banco do Brasil", "source": "Banco do Brasil S.A/banco-do-brasil-sem-fundo.svg"},
    "004": {"bank": "Banco do Nordeste", "source": "Banco do Nordeste do Brasil S.A/Logo_BNB.svg"},
    "021": {"bank": "Banestes", "source": "Banco do Estado do Espirito Santo/banestes.svg"},
    "033": {"bank": "Santander", "source": "Banco Santander Brasil S.A/banco-santander-logo.svg"},
    "041": {"bank": "Banrisul", "source": "Banrisul/banrisul-logo-2023.svg"},
    "070": {"bank": "BRB", "source": "BRB - Banco de Brasilia/brb-logo-abreviado.svg"},
    "077": {"bank": "Banco Inter", "source": "Banco Inter S.A/inter.svg"},
    "104": {"bank": "Caixa Economica Federal", "source": "Caixa Econômica Federal/caixa-economica-federal-X.svg"},
    "121": {"bank": "Agibank", "fallback": agibank_fallback, "source_note": "Fallback custom icon; bank not available in Tgentil/Bancos-em-SVG."},
    "136": {"bank": "Unicred", "source": "Unicred/verde.svg"},
    "140": {"bank": "NuFinanceira", "source": "Nu Pagamentos S.A/nubank-logo-2021.svg"},
    "208": {"bank": "BTG Pactual", "source": "Banco BTG Pacutal/btg-pactual.svg"},
    "212": {"bank": "Banco Original", "source": "Banco Original S.A/banco-original-logo-verde.svg"},
    "218": {"bank": "BS2", "source": "Banco BS2 S.A/Banco_BS2.svg"},
    "237": {"bank": "Bradesco", "source": "Bradesco S.A/bradesco.svg"},
    "260": {"bank": "Nubank", "source": "Nu Pagamentos S.A/nubank-logo-2021.svg"},
    "290": {"bank": "PagBank", "source": "PagSeguro Internet S.A/logo-pagbank.svg"},
    "318": {"bank": "BMG", "source": "Banco BMG/banco-bmg-logo.svg"},
    "323": {"bank": "Mercado Pago", "source": "Mercado Pago/mercado-pago.svg"},
    "336": {"bank": "C6 Bank", "source": "Banco C6 S.A/c6 bank.svg"},
    "341": {"bank": "Itau", "source": "Itaú Unibanco S.A/itau.svg"},
    "386": {"bank": "NuFinanceira", "source": "Nu Pagamentos S.A/nubank-logo-2021.svg"},
    "389": {"bank": "Mercantil do Brasil", "source": "Banco Mercantil do Brasil S.A/banco-mercantil-novo-azul.svg"},
    "422": {"bank": "Safra", "source": "Banco Safra S.A/logo-safra.svg"},
    "623": {"bank": "PAN", "fallback": pan_fallback, "source_note": "Fallback custom icon; bank not available in Tgentil/Bancos-em-SVG."},
    "637": {"bank": "Sofisa", "source": "Banco Sofisa/logo-sofisa.svg"},
    "655": {"bank": "BV", "source": "Banco Votorantim/banco-bv-logo.svg"},
    "707": {"bank": "Daycoval", "source": "Banco Daycoval/logo-Daycoval.svg"},
    "748": {"bank": "Sicredi", "source": "Sicredi/logo-svg2.svg"},
    "756": {"bank": "Sicoob", "source": "Sicoob/sicoob-minimalista-com.svg"},
}


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for old_svg in OUTPUT_DIR.glob("*.svg"):
        old_svg.unlink()

    manifest = []
    missing_sources = []

    for code, item in sorted(SOURCES.items()):
        output_path = OUTPUT_DIR / f"{code}.svg"

        if "source" in item:
            source_path = SOURCE_ROOT / item["source"]
            if not source_path.exists():
                missing_sources.append(str(source_path))
                continue
            output_path.write_text(source_path.read_text(encoding="utf-8"), encoding="utf-8")
            manifest.append(
                {
                    "code": code,
                    "bank": item["bank"],
                    "source_type": "github-repo",
                    "source_repo": REPO_URL,
                    "source_path": item["source"],
                }
            )
            continue

        output_path.write_text(item["fallback"](), encoding="utf-8")
        manifest.append(
            {
                "code": code,
                "bank": item["bank"],
                "source_type": "custom-fallback",
                "source_repo": REPO_URL,
                "source_note": item["source_note"],
            }
        )

    if missing_sources:
        joined = "\n".join(missing_sources)
        raise FileNotFoundError(f"Missing source SVGs:\n{joined}")

    (OUTPUT_DIR / "sources.json").write_text(
        json.dumps(manifest, ensure_ascii=True, indent=2) + "\n",
        encoding="utf-8",
    )

    (OUTPUT_DIR / "README.md").write_text(
        "# Bank Logos\n\n"
        "SVG bank logos keyed by Brazilian bank code.\n\n"
        f"- Main source: `{REPO_URL}`.\n"
        "- When the repository had a version without the bank name, that variant was preferred.\n"
        "- `140.svg`, `260.svg`, and `386.svg` intentionally reuse the Nubank asset because they belong to the same group.\n"
        "- `121.svg` and `623.svg` remain custom fallbacks because Agibank and PAN were not present in the source repository on June 1, 2026.\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
