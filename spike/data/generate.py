"""Generates the spike's resolved quote and invoice YAML.

Stands in for the domain: every amount is computed here, exactly, so the template only lays them out.
Run: python -I spike/data/generate.py
"""
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

CENT = Decimal("0.01")

ISSUER = {
    "name": "Batifolio",
    "activity": "Rénovation intérieure",
    "legal_form": "SAS",
    "capital": "20000",
    "siren": "912345678",
    "rcs": "Agen",
    "vat_number": "FR12912345678",
    "address": ["12 rue des Artisans", "47000 Agen"],
    "phone": "05 00 00 00 00",
    "email": "contact@batifolio.example",
}

CUSTOMER = {
    "name": "Les Toits Rouges SARL",
    "address": ["4 place du Marché", "33000 Bordeaux"],
    "siren": "834567890",
    "vat_number": "FR45834567890",
}

SITE = ["Résidence Les Tilleuls, bât. B", "18 avenue Thiers", "33100 Bordeaux"]

# (designation, quantity, unit, unit price excl. VAT, discount %, VAT rate %)
SECTIONS = [
    ("Salle d'eau — étage", [
        ("Dépose carrelage existant", "8.640", "m²", "18.00", "0", "10"),
        ("Carrelage mural 60×120 grès cérame", "8.640", "m²", "35.00", "10", "10"),
        ("Faïence murs 60×120, finition naturelle", "18.720", "u", "33.33", "10", "10"),
        ("Sol 60×60 grès cérame", "4.320", "m²", "26.58", "10", "10"),
        ("Mosaïque sol de douche 30×30", "11.000", "u", "9.75", "0", "10"),
        ("Receveur de douche extra-plat 120×80", "1.000", "u", "285.00", "30", "10"),
        ("Siphon extra-plat horizontal", "1.000", "u", "50.00", "0", "10"),
        ("Pose carrelage murs et sol", "12.960", "m²", "48.00", "0", "10"),
    ]),
    ("WC", [
        ("Bâti-support autoportant", "1.000", "u", "279.12", "0", "10"),
        ("Plaque de commande blanche", "1.000", "u", "78.09", "0", "10"),
        ("Cuvette suspendue sans bride", "1.000", "u", "190.00", "0", "10"),
        ("Carrelage mural 120×60 blanc", "7.200", "u", "54.00", "25", "10"),
        ("Pose WC suspendu", "1.000", "forfait", "320.00", "0", "10"),
    ]),
    ("Revêtements de sol", [
        ("Panneau de construction 10 mm 250×60", "17.000", "u", "36.40", "20", "10"),
        ("Bande d'armature 25 m", "2.000", "u", "14.20", "0", "10"),
        ("Connecteurs de fixation (sac de 20)", "2.000", "u", "19.00", "0", "10"),
        ("Vinyle chêne miel en lames", "40.494", "m²", "45.83", "20", "10"),
        ("Plinthe bois à peindre 80 mm", "20.000", "ml", "5.90", "0", "10"),
        ("Pose revêtement vinyle", "40.494", "m²", "22.00", "0", "10"),
    ]),
    ("Robinetterie et mobilier", [
        ("Ensemble de douche encastré noir", "1.000", "u", "770.00", "30", "10"),
        ("Meuble sous double vasque L120", "1.000", "u", "516.69", "0", "10"),
        ("Double vasque céramique L120", "1.000", "u", "353.43", "0", "10"),
        ("Miroir 120×80", "1.000", "u", "267.00", "0", "10"),
        ("Mitigeur lavabo noir mat", "2.000", "u", "141.80", "30", "10"),
        ("Paroi de douche fixe L120", "1.000", "u", "375.00", "30", "10"),
        ("Kit profilés mur et sol", "1.000", "u", "107.10", "30", "10"),
    ]),
    ("Prestations annexes", [
        ("Étude et métrés", "1.000", "forfait", "250.00", "0", "20"),
        ("Évacuation des gravats en déchetterie", "1.000", "forfait", "180.00", "0", "20"),
        ("Nettoyage de fin de chantier", "1.000", "forfait", "150.00", "0", "20"),
    ]),
]


def money(value: Decimal) -> str:
    return str(value.quantize(CENT, rounding=ROUND_HALF_UP))


def resolve_sections():
    sections, bases = [], {}
    for title, rows in SECTIONS:
        lines = []
        for designation, qty, unit, price, discount, rate in rows:
            quantity, unit_price = Decimal(qty), Decimal(price)
            net = unit_price * (1 - Decimal(discount) / 100)
            total = (quantity * net).quantize(CENT, rounding=ROUND_HALF_UP)
            bases[rate] = bases.get(rate, Decimal(0)) + total
            line = {
                "designation": designation,
                "quantity": qty,
                "unit": unit,
                "unit_price_excl_vat": price,
                "net_unit_price_excl_vat": str(net.normalize()),
                "total_excl_vat": money(total),
                "vat_rate": rate,
            }
            if discount != "0":
                line["discount_rate"] = discount
            lines.append(line)
        sections.append({"title": title, "lines": lines})
    return sections, bases


def vat_breakdown(bases):
    breakdown = []
    for rate in sorted(bases, key=Decimal):
        vat = (bases[rate] * Decimal(rate) / 100).quantize(CENT, rounding=ROUND_HALF_UP)
        breakdown.append({"rate": rate, "base_excl_vat": money(bases[rate]), "vat": money(vat)})
    return breakdown


def totals(breakdown):
    excl = sum(Decimal(b["base_excl_vat"]) for b in breakdown)
    vat = sum(Decimal(b["vat"]) for b in breakdown)
    return {"excl_vat": money(excl), "vat": money(vat), "incl_vat": money(excl + vat)}


def scalar(value) -> str:
    text = str(value)
    return '"' + text.replace("\\", "\\\\").replace('"', '\\"') + '"'


def dump(value, indent=0) -> str:
    pad = "  " * indent
    if isinstance(value, dict):
        out = []
        for key, item in value.items():
            if isinstance(item, (dict, list)) and item:
                out.append(f"{pad}{key}:\n{dump(item, indent + 1)}")
            elif isinstance(item, bool):
                out.append(f"{pad}{key}: {'true' if item else 'false'}")
            else:
                out.append(f"{pad}{key}: {scalar(item)}")
        return "\n".join(out)
    out = []
    for item in value:
        if isinstance(item, dict):
            body = dump(item, indent + 1).lstrip()
            out.append(f"{pad}- {body}")
        else:
            out.append(f"{pad}- {scalar(item)}")
    return "\n".join(out)


def document(kind_fields, mentions, repeat=1):
    sections, bases = resolve_sections()
    sections = sections * repeat
    bases = {rate: base * repeat for rate, base in bases.items()}
    breakdown = vat_breakdown(bases)
    return {
        "document": kind_fields,
        "issuer": ISSUER,
        "customer": CUSTOMER,
        "site_address": SITE,
        "sections": sections,
        "vat_breakdown": breakdown,
        "totals": totals(breakdown),
        "mentions": mentions,
    }


QUOTE = document(
    {
        "title": "Devis",
        "number": "D-2026-0042",
        "issued_on": "2026-10-18",
        "subject": "Rénovation salle d'eau, WC et sols",
        "acceptance_box": True,
    },
    [
        "Devis valable 30 jours, jusqu'au 17/11/2026.",
        "Devis gratuit.",
        "Taux de TVA réduit de 10 % appliqué aux travaux d'amélioration d'un logement de plus de deux ans.",
        "Assurance décennale : Mutuelle Exemple, contrat n° 000-1234, couverture France métropolitaine.",
    ],
)

INVOICE = document(
    {
        "title": "Facture",
        "number": "F-2026-0107",
        "issued_on": "2026-11-30",
        "subject": "Rénovation salle d'eau, WC et sols",
        "references": ["D'après le devis n° D-2026-0042 du 18/10/2026"],
        "acceptance_box": False,
    },
    [
        "Date d'exécution des travaux : du 03/11/2026 au 27/11/2026.",
        "Nature des opérations : prestations de services.",
        "Échéance : 30/12/2026, paiement par virement.",
        "Escompte pour paiement anticipé : néant.",
        "Pénalités de retard : trois fois le taux d'intérêt légal. "
        "Indemnité forfaitaire pour frais de recouvrement : 40 €.",
        "Assurance décennale : Mutuelle Exemple, contrat n° 000-1234, couverture France métropolitaine.",
    ],
)

# Pagination stress test: the same quote with its sections repeated three times.
LONG_QUOTE = document(dict(QUOTE["document"], number="D-2026-0043"), QUOTE["mentions"], repeat=3)

if __name__ == "__main__":
    here = Path(__file__).parent
    for name, doc in (("devis.yaml", QUOTE), ("facture.yaml", INVOICE), ("devis-long.yaml", LONG_QUOTE)):
        (here / name).write_text(
            "# Generated by generate.py: a resolved document, amounts already computed.\n" + dump(doc) + "\n",
            encoding="utf-8",
        )
        print("wrote", name)
