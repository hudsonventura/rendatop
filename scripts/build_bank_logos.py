#!/usr/bin/env python3
from __future__ import annotations

import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "client" / "public" / "bank-logos"


def svg(content: str) -> str:
    return (
        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" fill="none">'
        f"{content}</svg>\n"
    )


def bb_icon() -> str:
    return svg(
        """
  <path d="M64 20 108 64 64 108 20 64Z" fill="#F7C600"/>
  <path d="M41 47h14l10 10-10 10H41l10-10Z" fill="#173F8A"/>
  <path d="M87 47H73L63 57l10 10h14L77 57Z" fill="#173F8A"/>
  <path d="M47 73h14l10 10-10 10H47l10-10Z" fill="#173F8A"/>
  <path d="M81 73H67L57 83l10 10h14L71 83Z" fill="#173F8A"/>
"""
    )


def bnb_icon() -> str:
    return svg(
        """
  <path d="M64 18 98 64 64 110 30 64Z" fill="#1C6AA8"/>
  <path d="M64 34 82 58 64 82 46 58Z" fill="#F0C53A"/>
  <path d="M64 46 72 58 64 70 56 58Z" fill="#1C6AA8"/>
"""
    )


def banestes_icon() -> str:
    return svg(
        """
  <path d="M26 26h36c22 0 40 18 40 40s-18 36-40 36H26Z" fill="#005CA8"/>
  <path d="M44 42h20c12 0 20 8 20 18s-8 18-20 18H44V66h16c6 0 10-3 10-8s-4-8-10-8H44Z" fill="#ffffff"/>
"""
    )


def santander_icon() -> str:
    return svg(
        """
  <path d="M72 18c7 16 5 30-8 42 17-2 28 12 20 27-6 11-21 16-40 14 12-5 20-12 25-22 5-10 2-19-8-28 10-8 14-19 11-33Z" fill="#EC0000"/>
  <path d="M58 66c-8 9-18 16-30 22 5-10 14-19 25-27Z" fill="#EC0000"/>
"""
    )


def banrisul_icon() -> str:
    return svg(
        """
  <path d="M30 34h18v60H30Z" fill="#184B9B"/>
  <path d="M54 34h18v60H54Z" fill="#184B9B"/>
  <path d="M78 34h20v60H78Z" fill="#184B9B"/>
  <path d="M32 82h68L74 102H32Z" fill="#F4C433"/>
"""
    )


def brb_icon() -> str:
    return svg(
        """
  <path d="M28 34h18l18 22-18 22H28l18-22Z" fill="#0057B8"/>
  <path d="M56 34h18l18 22-18 22H56l18-22Z" fill="#0057B8"/>
  <path d="M88 34h12v44H88Z" fill="#0057B8"/>
"""
    )


def inter_icon() -> str:
    return svg(
        """
  <rect x="24" y="24" width="80" height="80" rx="18" fill="#FF7A00"/>
  <path d="M48 46h12v36H48Z" fill="#ffffff"/>
  <circle cx="54" cy="36" r="8" fill="#ffffff"/>
  <path d="M76 46h12v26c0 10-8 18-18 18H60V78h10c3 0 6-3 6-6Z" fill="#ffffff"/>
"""
    )


def caixa_icon() -> str:
    return svg(
        """
  <path d="M28 38h18l18 18 18-18h18L74 64l26 26H82L64 72 46 90H28l26-26Z" fill="#005CA9"/>
  <path d="M68 38h14L64 56H50Z" fill="#F39200"/>
"""
    )


def agibank_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" fill="#F36F21"/>
  <circle cx="64" cy="64" r="14" fill="#ffffff"/>
  <path d="M78 36h14v56H78z" fill="#283B8F"/>
"""
    )


def unicred_icon() -> str:
    return svg(
        """
  <path d="M40 38a34 34 0 1 0 48 48" stroke="#00A651" stroke-width="12" stroke-linecap="round"/>
  <path d="M76 34a30 30 0 0 1 18 54" stroke="#86C440" stroke-width="12" stroke-linecap="round"/>
"""
    )


def nubank_icon() -> str:
    return svg(
        """
  <path d="M30 80V50c0-8 6-14 14-14 6 0 10 2 14 8l12 18c2 3 4 4 7 4 4 0 7-3 7-8V36h14v30c0 15-9 26-22 26-8 0-14-3-20-11L44 63v17Z" fill="#8A05BE"/>
"""
    )


def btg_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" stroke="#0057B8" stroke-width="10"/>
  <path d="M50 46h17c9 0 15 5 15 13 0 5-3 9-8 11 6 2 10 7 10 14 0 10-8 16-19 16H50V46Zm14 20c4 0 6-2 6-5s-2-5-6-5h-4v10Zm2 24c4 0 7-3 7-7s-3-7-7-7h-6v14Z" fill="#0057B8"/>
"""
    )


def original_icon() -> str:
    return svg(
        """
  <path d="M34 64c12-18 24-27 36-27 11 0 19 7 24 18l-11 6c-4-8-9-12-15-12-8 0-17 7-27 23 10 16 19 23 27 23 6 0 11-4 15-12l11 6c-5 11-13 18-24 18-12 0-24-9-36-27Z" fill="#00A650"/>
"""
    )


def bs2_icon() -> str:
    return svg(
        """
  <rect x="28" y="34" width="26" height="60" rx="13" fill="#18B7E7"/>
  <path d="M62 40h14c14 0 24 8 24 20 0 8-4 14-12 18l12 16H84L74 80h-6v14H56V46c0-3 3-6 6-6Z" fill="#104A8E"/>
  <path d="M68 52v16h8c7 0 12-3 12-8s-5-8-12-8Z" fill="#18B7E7"/>
"""
    )


def bradesco_icon() -> str:
    return svg(
        """
  <path d="M64 26c14 13 21 26 21 39 0 14-9 27-21 37-12-10-21-23-21-37 0-13 7-26 21-39Z" fill="#CC092F"/>
  <path d="M64 28v62" stroke="#ffffff" stroke-width="8" stroke-linecap="round"/>
  <path d="M64 52c-13 0-24 8-29 21" stroke="#ffffff" stroke-width="8" stroke-linecap="round"/>
  <path d="M64 52c13 0 24 8 29 21" stroke="#ffffff" stroke-width="8" stroke-linecap="round"/>
"""
    )


def pagbank_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="30" fill="none" stroke="#F6C526" stroke-width="14" stroke-dasharray="48 140" stroke-linecap="round" transform="rotate(-40 64 64)"/>
  <circle cx="64" cy="64" r="30" fill="none" stroke="#2C72D6" stroke-width="14" stroke-dasharray="48 140" stroke-linecap="round" transform="rotate(50 64 64)"/>
  <path d="M54 58c5-8 15-8 20 0-5 6-15 6-20 0Z" fill="#35A853"/>
  <path d="M54 70c5 8 15 8 20 0-5-6-15-6-20 0Z" fill="#00BCD4"/>
"""
    )


def bmg_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" fill="#F57C00"/>
  <path d="M50 42h16c10 0 18 6 18 16 0 6-3 10-8 13 6 3 10 8 10 15 0 11-9 18-22 18H50V42Zm13 24c5 0 8-2 8-6s-3-6-8-6h-1v12Zm2 26c5 0 9-3 9-8s-4-8-9-8h-3v16Z" fill="#ffffff"/>
  <circle cx="88" cy="44" r="6" fill="#ffffff"/>
"""
    )


def mercado_pago_icon() -> str:
    return svg(
        """
  <ellipse cx="64" cy="64" rx="42" ry="28" fill="#00B1EA"/>
  <path d="M48 64c4-7 10-10 16-10 5 0 9 2 14 6l4 3 4-3c4-3 8-4 12-4 4 0 8 2 10 6-2 6-7 10-13 10-5 0-9-2-14-6l-3-2-3 2c-5 4-10 6-16 6-7 0-12-3-15-8Z" fill="#ffffff"/>
  <path d="M44 58 54 68M84 58 74 68" stroke="#005B9A" stroke-width="4" stroke-linecap="round"/>
"""
    )


def c6_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" fill="#111111"/>
  <path d="M82 48c-4-4-9-6-16-6-14 0-24 10-24 22 0 14 10 24 24 24 7 0 12-2 16-6l-8-8c-2 2-5 3-8 3-6 0-11-5-11-13 0-7 5-12 11-12 3 0 6 1 8 3Z" fill="#D1A64C"/>
  <path d="M76 64c7 0 12 5 12 12s-5 12-12 12-12-5-12-12 5-12 12-12Zm0 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Z" fill="#D1A64C"/>
"""
    )


def itau_icon() -> str:
    return svg(
        """
  <rect x="24" y="24" width="80" height="80" rx="20" fill="#EC7000"/>
  <path d="M45 62c0-12 8-20 19-20 10 0 18 7 18 18 0 12-9 20-21 20h-2v10H47V62Zm14 7h2c5 0 8-3 8-8 0-4-3-7-8-7s-8 3-8 8 2 7 6 7Z" fill="#1C3F94"/>
  <circle cx="84" cy="44" r="5" fill="#1C3F94"/>
"""
    )


def mercantil_icon() -> str:
    return svg(
        """
  <path d="M28 92V36h12l24 26 24-26h12v56H86V58L64 82 42 58v34Z" fill="#005DA9"/>
"""
    )


def safra_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="34" stroke="#003C88" stroke-width="10"/>
  <path d="M46 70c8 8 28 8 36-8 6-12-2-24-16-24-9 0-16 3-22 10" fill="none" stroke="#003C88" stroke-width="10" stroke-linecap="round"/>
  <path d="M46 86h36" stroke="#003C88" stroke-width="10" stroke-linecap="round"/>
"""
    )


def pan_icon() -> str:
    return svg(
        """
  <path d="M34 92V36h20c14 0 24 10 24 24S68 84 54 84H46v8Zm12-20h8c7 0 12-5 12-12s-5-12-12-12h-8Z" fill="#111111"/>
  <path d="M78 92V36h14l12 32 12-32h4v56h-10V58l-8 22H94l-8-22v34Z" fill="#F3C317"/>
"""
    )


def sofisa_icon() -> str:
    return svg(
        """
  <circle cx="64" cy="64" r="30" fill="#1DAA59"/>
  <path d="M48 68c4 10 12 16 24 16 8 0 14-3 20-8" fill="none" stroke="#ffffff" stroke-width="10" stroke-linecap="round"/>
  <path d="M80 52c-4-6-10-8-16-8-7 0-13 3-18 8" fill="none" stroke="#ffffff" stroke-width="10" stroke-linecap="round"/>
"""
    )


def bv_icon() -> str:
    return svg(
        """
  <path d="M38 42h18c10 0 18 7 18 16 0 5-2 9-6 12 6 3 10 8 10 15 0 11-9 17-22 17H38V42Zm16 22c5 0 8-2 8-6s-3-6-8-6h-4v12Zm2 26c5 0 8-3 8-8s-3-8-8-8h-6v16Z" fill="#1F4DA0"/>
  <path d="M74 42h16l10 28 10-28h12L98 102H88Z" fill="#2FB36D"/>
"""
    )


def daycoval_icon() -> str:
    return svg(
        """
  <circle cx="54" cy="64" r="24" fill="none" stroke="#0052A5" stroke-width="12"/>
  <path d="M58 40h10c16 0 28 10 28 24S84 88 68 88H58V76h8c9 0 16-5 16-12s-7-12-16-12h-8Z" fill="#0052A5"/>
"""
    )


def sicredi_icon() -> str:
    blades = []
    for angle in range(0, 360, 45):
        blades.append(
            f'<rect x="60" y="20" width="8" height="24" rx="4" fill="#44B549" transform="rotate({angle} 64 64)"/>'
        )
    return svg(
        f"""
  <g>{''.join(blades)}</g>
  <circle cx="64" cy="64" r="18" fill="#44B549"/>
  <circle cx="64" cy="64" r="8" fill="#ffffff"/>
"""
    )


def sicoob_icon() -> str:
    return svg(
        """
  <path d="M64 24 96 80H32Z" fill="#006B3F"/>
  <path d="M64 40 82 72H46Z" fill="#8CC63F"/>
  <circle cx="64" cy="86" r="12" fill="#006B3F"/>
"""
    )


ICONS = {
    "001": ("Banco do Brasil", bb_icon, "Custom square icon inspired by the Banco do Brasil symbol."),
    "004": ("Banco do Nordeste", bnb_icon, "Custom square icon inspired by the Banco do Nordeste symbol."),
    "021": ("Banestes", banestes_icon, "Custom square icon inspired by the Banestes symbol."),
    "033": ("Santander", santander_icon, "Custom square icon inspired by the Santander flame symbol."),
    "041": ("Banrisul", banrisul_icon, "Custom square icon inspired by the Banrisul symbol."),
    "070": ("BRB", brb_icon, "Custom square icon inspired by the BRB symbol."),
    "077": ("Banco Inter", inter_icon, "Custom square icon inspired by the Inter app icon."),
    "104": ("Caixa Economica Federal", caixa_icon, "Custom square icon inspired by the Caixa symbol."),
    "121": ("Agibank", agibank_icon, "Custom square icon inspired by the Agibank symbol."),
    "136": ("Unicred", unicred_icon, "Custom square icon inspired by the Unicred symbol."),
    "140": ("NuFinanceira", nubank_icon, "Custom square icon inspired by the Nubank symbol."),
    "208": ("BTG Pactual", btg_icon, "Custom square icon inspired by the BTG Pactual emblem."),
    "212": ("Banco Original", original_icon, "Custom square icon inspired by the Banco Original symbol."),
    "218": ("BS2", bs2_icon, "Custom square icon inspired by the BS2 brand mark."),
    "237": ("Bradesco", bradesco_icon, "Custom square icon inspired by the Bradesco tree symbol."),
    "260": ("Nubank", nubank_icon, "Custom square icon inspired by the Nubank symbol."),
    "290": ("PagBank", pagbank_icon, "Custom square icon inspired by the PagBank symbol."),
    "318": ("BMG", bmg_icon, "Custom square icon inspired by the BMG app icon."),
    "323": ("Mercado Pago", mercado_pago_icon, "Custom square icon inspired by the Mercado Pago handshake symbol."),
    "336": ("C6 Bank", c6_icon, "Custom square icon inspired by the C6 Bank symbol."),
    "341": ("Itau", itau_icon, "Custom square icon inspired by the Itau app icon."),
    "386": ("NuFinanceira", nubank_icon, "Custom square icon inspired by the Nubank symbol."),
    "389": ("Mercantil do Brasil", mercantil_icon, "Custom square icon inspired by the Mercantil do Brasil monogram."),
    "422": ("Safra", safra_icon, "Custom square icon inspired by the Safra symbol."),
    "623": ("PAN", pan_icon, "Custom square icon inspired by the PAN app icon."),
    "637": ("Sofisa", sofisa_icon, "Custom square icon inspired by the Sofisa symbol."),
    "655": ("BV", bv_icon, "Custom square icon inspired by the BV symbol."),
    "707": ("Daycoval", daycoval_icon, "Custom square icon inspired by the Daycoval symbol."),
    "748": ("Sicredi", sicredi_icon, "Custom square icon inspired by the Sicredi pinwheel."),
    "756": ("Sicoob", sicoob_icon, "Custom square icon inspired by the Sicoob triangle mark."),
}


def main() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for old_svg in OUTPUT_DIR.glob("*.svg"):
        old_svg.unlink()

    manifest = []
    for code, (bank, generator, note) in sorted(ICONS.items()):
        (OUTPUT_DIR / f"{code}.svg").write_text(generator(), encoding="utf-8")
        manifest.append(
            {
                "code": code,
                "bank": bank,
                "source_type": "custom",
                "source_note": note,
            }
        )

    (OUTPUT_DIR / "sources.json").write_text(
        json.dumps(manifest, ensure_ascii=True, indent=2) + "\n",
        encoding="utf-8",
    )

    (OUTPUT_DIR / "README.md").write_text(
        "# Bank Logos\n\n"
        "Square SVG bank icons keyed by Brazilian bank code.\n\n"
        "- All files use a `128x128` square canvas.\n"
        "- The set prioritizes the symbol/logo mark and avoids full wordmarks.\n"
        "- `140.svg`, `260.svg`, and `386.svg` intentionally reuse the Nubank icon because they belong to the same group.\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
