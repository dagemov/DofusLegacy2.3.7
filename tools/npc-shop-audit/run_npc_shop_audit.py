#!/usr/bin/env python3
"""
NPC shop audit: extract catalogs from sunshine.sql, categorize, price for 75M economy,
distribute max 100 items per NPC, emit JSON + SQL patches + markdown report.
"""
from __future__ import annotations

import argparse
import csv
import json
import os
import re
import subprocess
import sys
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SQL_PATH = ROOT / "database" / "sunshine.sql"
OUT_DIR = Path(__file__).resolve().parent
PATCH_DIR = ROOT / "database" / "patches"
DOCS_PATH = ROOT / "docs" / "npc-shop-distribution.md"

MAX_ITEMS_PER_NPC = 100
START_KAMAS = 75_000_000
HUB_MAP_ID = 2323
NEW_NPC_ID_START = 9001

# TypeId groups for thematic shops
SHOP_FAMILIES: dict[str, list[int]] = {
    "Armas_melee": [5, 6, 7, 8, 19],  # DAGUE, EPEE, MARTEAU, PELLE, HACHE
    "Armas_distance": [2, 3, 4, 102],  # ARC, BAGUETTE, BATON, ARBALETE
    "Sombreros": [16],
    "Capas": [17],
    "Anillos_amuletos": [1, 9],
    "Cinturones_botas": [10, 11],
    "Escudos": [82],
    "Consumibles": [12, 13, 33, 42, 43, 75, 76, 79],
    "Recursos": [
        15, 34, 35, 36, 37, 38, 39, 40, 41, 46, 47, 48, 49, 50, 51, 52, 53, 54,
        55, 56, 57, 58, 59, 60, 62, 63, 64, 65, 68, 69, 70, 95, 96, 98, 103, 104,
        105, 106, 107, 108, 109, 110, 111, 119, 181, 182, 185,
    ],
    "Dofus_familiers": [18, 23, 72, 77, 90, 91, 97, 116, 121, 122, 123, 124, 196],
    "Divers": [],  # catch-all assigned dynamically
}

UNIFIED9_NPC_START = 9101
UNIFIED9_NPC_END = 9109
LEGACY_VIRTUAL_NPC_MIN = 9001
LEGACY_VIRTUAL_NPC_MAX = 9042

# 9 tiendas fijas alineadas a .tienda 1-9 (sin límite de ítems, sin bandas de nivel)
VIRTUAL_SHOP_SLOTS: list[dict] = [
    {"slot": 1, "npcId": 9101, "label": "Sombrero", "typeIds": [16]},
    {"slot": 2, "npcId": 9102, "label": "Capa", "typeIds": [17]},
    {"slot": 3, "npcId": 9103, "label": "Anillo y amuleto", "typeIds": [1, 9]},
    {"slot": 4, "npcId": 9104, "label": "Cinturon y botas", "typeIds": [10, 11]},
    {"slot": 5, "npcId": 9105, "label": "Escudo", "typeIds": [82]},
    {"slot": 6, "npcId": 9106, "label": "Consumible", "typeIds": [12, 13, 33, 42, 43, 75, 76, 79]},
    {"slot": 7, "npcId": 9107, "label": "Recurso", "typeIds": None},  # filled from SHOP_FAMILIES Recursos
    {"slot": 8, "npcId": 9108, "label": "Dofus y mascota", "typeIds": None},  # filled from Dofus_familiers
    {"slot": 9, "npcId": 9109, "label": "Diverso", "typeIds": []},
]
VIRTUAL_SHOP_SLOTS[6]["typeIds"] = SHOP_FAMILIES["Recursos"]
VIRTUAL_SHOP_SLOTS[7]["typeIds"] = SHOP_FAMILIES["Dofus_familiers"]

LEVEL_BUCKETS = [
    (1, 20, "L1_20"),
    (21, 60, "L21_60"),
    (61, 120, "L61_120"),
    (121, 180, "L121_180"),
    (181, 999, "L181_plus"),
]

# UX: filas del hub 2323 (equipo → utilidades), columnas = progresión de nivel
FAMILY_UX_ORDER = [
    "Sombreros",
    "Capas",
    "Anillos_amuletos",
    "Cinturones_botas",
    "Escudos",
    "Armas_melee",
    "Armas_distance",
    "Consumibles",
    "Recursos",
    "Dofus_familiers",
    "Divers",
]

FAMILY_DISPLAY: dict[str, str] = {
    "Sombreros": "Chapeaux",
    "Capas": "Capes",
    "Anillos_amuletos": "Anneaux & Amulettes",
    "Cinturones_botas": "Ceintures & Bottes",
    "Escudos": "Boucliers",
    "Armas_melee": "Armes corps-a-corps",
    "Armas_distance": "Armes a distance",
    "Consumibles": "Consommables",
    "Recursos": "Ressources",
    "Dofus_familiers": "Dofus & Familiers",
    "Divers": "Divers",
}

LEVEL_BUCKET_ORDER = ["L1_20", "L21_60", "L61_120", "L121_180", "L181_plus"]

LEVEL_DISPLAY: dict[str, str] = {
    "L1_20": "Nv 1-20",
    "L21_60": "Nv 21-60",
    "L61_120": "Nv 61-120",
    "L121_180": "Nv 121-180",
    "L181_plus": "Nv 181+",
}

# NPCs que permanecen en 2323 (dialogo / token) — no reubicar ni borrar spawn
HUB_KEEP_NPC_IDS = {537, 788}

# Grilla hub: una fila por categoría, columnas = bandas de nivel (izq → der)
HUB_ROW_BASE_CELLS = [220, 244, 268, 292, 316, 340, 364, 388, 412]
HUB_COL_STEP = 4
HUB_MAX_CELL = 559

PRICE_BANDS = [
    (1, 20, 25_000, 250_000),
    (21, 60, 250_000, 1_500_000),
    (61, 120, 1_500_000, 8_000_000),
    (121, 180, 8_000_000, 22_000_000),
    (181, 999, 18_000_000, 32_000_000),
]

WEAPON_TYPE_IDS = {2, 3, 4, 5, 6, 7, 8, 19, 102}

CATEGORY_MULTIPLIERS: dict[str, float] = {
    "Sombreros": 1.0,
    "Capas": 1.0,
    "Escudos": 1.0,
    "Armas_melee": 1.0,
    "Armas_distance": 1.0,
    "Dofus_familiers": 1.0,
    "Anillos_amuletos": 0.55,
    "Cinturones_botas": 0.40,
    "Consumibles": 0.04,
    "Recursos": 0.015,
    "Divers": 0.35,
}

SET_PRICE_MULTIPLIER = 1.12

INSERT_RE = re.compile(
    r"INSERT INTO `(?P<table>npcs_items|items|npcs|worlds_npcs)` VALUES \((?P<values>.+)\);?\s*$",
    re.IGNORECASE,
)

DB_ITEMS_SQL = (
    "SELECT Id, Name, TypeId, Level, Price, ItemSetId FROM items"
)
DB_NPCS_SQL = "SELECT Id, Name, ActionsIdCSV, Token FROM npcs"
DB_NPC_ITEMS_SQL = "SELECT Id, NpcId, Item, Note, Price, Token, ActionId FROM npcs_items"
DB_SPAWNS_SQL = "SELECT Id, Npc, Map, Cell, Direction, Note FROM worlds_npcs"


def load_dotenv() -> dict[str, str]:
    env: dict[str, str] = {}
    env_path = ROOT / ".env"
    if not env_path.exists():
        return env
    for line in env_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        env[key.strip()] = value.strip()
    return env


def run_mariadb_query(sql: str, env: dict[str, str]) -> str | None:
    host = env.get("MYSQL_PUBLISH_HOST", "127.0.0.1")
    if host in ("0.0.0.0", ""):
        host = "127.0.0.1"
    port = str(env.get("MYSQL_PUBLISH_PORT", "3306"))
    user = env.get("MYSQL_APP_USER", "sunshine")
    password = env.get("MYSQL_APP_PASSWORD", "")
    database = env.get("MYSQL_DATABASE", "sunshine")
    proc_env = {**os.environ, "MYSQL_PWD": password}

    for cli in ("mariadb", "mysql"):
        try:
            result = subprocess.run(
                [cli, "-B", "-N", "-h", host, "-P", port, "-u", user, database, "-e", sql],
                capture_output=True,
                text=True,
                timeout=180,
                env=proc_env,
            )
            if result.returncode == 0:
                return result.stdout
        except FileNotFoundError:
            continue
        except subprocess.TimeoutExpired:
            return None
    return None


def parse_tsv_rows(raw: str) -> list[list[str]]:
    rows: list[list[str]] = []
    for line in raw.splitlines():
        if not line.strip():
            continue
        rows.append(line.split("\t"))
    return rows


def fetch_from_mariadb(env: dict[str, str]) -> tuple[dict, dict, list, list] | None:
    items: dict[int, dict] = {}
    npcs: dict[int, dict] = {}
    npc_items: list[dict] = []
    spawns: list[dict] = []

    raw_items = run_mariadb_query(DB_ITEMS_SQL, env)
    if raw_items is None:
        return None

    for row in parse_tsv_rows(raw_items):
        if len(row) < 6:
            continue
        item_id = int(row[0])
        items[item_id] = {
            "itemId": item_id,
            "name": row[1],
            "typeId": int(row[2]),
            "level": int(row[3]),
            "templatePrice": int(float(row[4])),
            "itemSetId": int(row[5]),
        }

    raw_npcs = run_mariadb_query(DB_NPCS_SQL, env)
    if raw_npcs is None:
        return None
    for row in parse_tsv_rows(raw_npcs):
        if len(row) < 4:
            continue
        npc_id = int(row[0])
        token_raw = row[3]
        npcs[npc_id] = {
            "npcId": npc_id,
            "name": row[1],
            "actionsIdCsv": row[2] or "",
            "token": int(token_raw) if token_raw not in ("", "NULL", None) else 0,
        }

    raw_shop = run_mariadb_query(DB_NPC_ITEMS_SQL, env)
    if raw_shop is None:
        return None
    for row in parse_tsv_rows(raw_shop):
        if len(row) < 7:
            continue
        npc_items.append({
            "rowId": int(row[0]),
            "npcId": int(row[1]),
            "itemId": int(row[2]),
            "note": None if row[3] in ("", "NULL") else row[3],
            "npcOverridePrice": int(row[4] or 0),
            "token": int(row[5] or 0),
            "actionId": int(row[6] or 0),
        })

    raw_spawns = run_mariadb_query(DB_SPAWNS_SQL, env)
    if raw_spawns is None:
        return None
    for row in parse_tsv_rows(raw_spawns):
        if len(row) < 5:
            continue
        spawns.append({
            "spawnId": int(row[0]),
            "npcId": int(row[1]),
            "mapId": int(row[2]),
            "cell": int(row[3]),
            "direction": int(row[4]),
            "note": row[5] if len(row) > 5 and row[5] not in ("", "NULL") else None,
        })

    print(f"  items={len(items)} npcs={len(npcs)} shop_rows={len(npc_items)} spawns={len(spawns)}")
    return items, npcs, npc_items, spawns


def parse_sql_value(raw: str):
    raw = raw.strip()
    if raw.lower() == "null":
        return None
    if raw.startswith("'") and raw.endswith("'"):
        return raw[1:-1].replace("''", "'")
    try:
        if "." in raw:
            return float(raw)
        return int(raw)
    except ValueError:
        return raw


def split_sql_tuple(inner: str) -> list:
    parts: list[str] = []
    current: list[str] = []
    in_str = False
    i = 0
    while i < len(inner):
        ch = inner[i]
        if ch == "'" and not in_str:
            in_str = True
            current.append(ch)
        elif ch == "'" and in_str:
            if i + 1 < len(inner) and inner[i + 1] == "'":
                current.append("''")
                i += 1
            else:
                in_str = False
                current.append(ch)
        elif ch == "," and not in_str:
            parts.append("".join(current).strip())
            current = []
        else:
            current.append(ch)
        i += 1
    if current:
        parts.append("".join(current).strip())
    return [parse_sql_value(p) for p in parts]


def load_type_enum() -> dict[int, str]:
    data = json.loads((OUT_DIR / "item_type_enum.json").read_text(encoding="utf-8"))
    return {int(k): v for k, v in data.items()}


def family_for_type(type_id: int, assigned_families: set[int]) -> str:
    for family, type_ids in SHOP_FAMILIES.items():
        if family == "Divers":
            continue
        if type_id in type_ids:
            return family
    return "Divers"


def level_bucket(level: int) -> str:
    for lo, hi, label in LEVEL_BUCKETS:
        if lo <= level <= hi:
            return label
    return "L181_plus"


def hub_occupied_cells(spawns: list, audit: dict) -> set[int]:
    occupied: set[int] = set()
    for s in spawns:
        if s["mapId"] == HUB_MAP_ID:
            occupied.add(s["cell"])
    return occupied


def hub_reserved_cells(occupied: set[int]) -> set[int]:
    """Buffer alrededor de NPCs fijos y celdas ya ocupadas."""
    reserved = set(occupied)
    for cell in list(occupied):
        for delta in (-1, 1, -14, 14):
            reserved.add(cell + delta)
    return reserved


def iter_hub_candidate_cells(reserved: set[int]) -> list[int]:
    candidates: list[int] = []
    for row_base in HUB_ROW_BASE_CELLS:
        for col in range(0, 13 * HUB_COL_STEP, HUB_COL_STEP):
            cell = row_base + col
            if cell > HUB_MAX_CELL:
                break
            if cell not in reserved:
                candidates.append(cell)
    return candidates


def family_sort_key(family: str) -> tuple[int, str]:
    try:
        return (FAMILY_UX_ORDER.index(family), family)
    except ValueError:
        return (len(FAMILY_UX_ORDER), family)


def bucket_sort_key(bucket: str) -> tuple[int, str]:
    try:
        return (LEVEL_BUCKET_ORDER.index(bucket), bucket)
    except ValueError:
        return (len(LEVEL_BUCKET_ORDER), bucket)


def shop_display_name(family: str, bucket: str, chunk_index: int) -> str:
    label = FAMILY_DISPLAY.get(family, family)
    level = LEVEL_DISPLAY.get(bucket, bucket)
    suffix = f" #{chunk_index + 1}" if chunk_index > 0 else ""
    return f"Vendeur {label} ({level}){suffix}"


def assign_hub_cells(shops: list[dict], spawns: list, audit: dict) -> list[dict]:
    occupied = hub_occupied_cells(spawns, audit)
    reserved = hub_reserved_cells(occupied)

    shops_by_family: dict[str, list[dict]] = defaultdict(list)
    for shop in shops:
        shops_by_family[shop["category"]].append(shop)

    assigned: list[dict] = []
    row_idx = 0
    for family in FAMILY_UX_ORDER:
        family_shops = shops_by_family.get(family)
        if not family_shops:
            continue

        family_shops.sort(
            key=lambda s: (bucket_sort_key(s["levelBucket"]), s["proposedNpcId"])
        )
        if row_idx < len(HUB_ROW_BASE_CELLS):
            row_base = HUB_ROW_BASE_CELLS[row_idx]
        else:
            row_base = HUB_ROW_BASE_CELLS[-1] + (row_idx - len(HUB_ROW_BASE_CELLS) + 1) * 24

        bucket_chunk: dict[str, int] = defaultdict(int)
        for shop in family_shops:
            bucket = shop["levelBucket"]
            chunk_idx = bucket_chunk[bucket]
            bucket_chunk[bucket] += 1

            try:
                col = LEVEL_BUCKET_ORDER.index(bucket) + chunk_idx
            except ValueError:
                col = chunk_idx

            cell = row_base + col * HUB_COL_STEP
            while cell in reserved or cell > HUB_MAX_CELL:
                col += 1
                cell = row_base + col * HUB_COL_STEP

            reserved.add(cell)
            reserved.update({cell - 1, cell + 1})

            shop["proposedName"] = shop_display_name(family, bucket, chunk_idx)
            shop["mapPlacements"] = [{"mapId": HUB_MAP_ID, "cell": cell, "direction": 1}]
            shop["layoutRow"] = row_idx
            shop["layoutCol"] = col
            assigned.append(shop)

        row_idx += 1

    return sorted(assigned, key=lambda s: s["proposedNpcId"])

def category_multiplier(type_id: int, item_set_id: int) -> float:
    family = family_for_type(type_id, set())
    if family == "Divers" and type_id in WEAPON_TYPE_IDS:
        mult = 0.90
    else:
        mult = CATEGORY_MULTIPLIERS.get(family, 0.35)
    if item_set_id > 0:
        mult *= SET_PRICE_MULTIPLIER
    return mult


def suggest_price(
    level: int,
    type_id: int,
    effective_price: int,
    payment_token: int,
    item_set_id: int = -1,
) -> int:
    if payment_token > 0:
        return max(1, effective_price)

    if type_id == 23:  # DOFUS
        return min(60_000_000, 48_000_000 + level * 40_000)

    mult = category_multiplier(type_id, item_set_id)

    for lo, hi, pmin, pmax in PRICE_BANDS:
        if lo <= level <= hi:
            if hi == lo:
                base = pmin
            else:
                t = (level - lo) / (hi - lo)
                base = int(pmin + t * (pmax - pmin))
            legacy = max(1, effective_price)
            if legacy > 0:
                blend = int(base * 0.7 + min(legacy, pmax) * 0.3)
                base = max(pmin, min(pmax, blend))
            price = int(base * mult)
            floor = max(1, int(pmin * mult))
            cap = int(pmax * mult)
            return max(floor, min(cap, price))
    return 500_000


def parse_sql_dump(path: Path) -> tuple[dict, dict, dict, list]:
    items: dict[int, dict] = {}
    npcs: dict[int, dict] = {}
    npc_items: list[dict] = []
    spawns: list[dict] = []

    print(f"Parsing {path} ...")
    with path.open("r", encoding="utf-8", errors="replace") as f:
        for line_no, line in enumerate(f, 1):
            m = INSERT_RE.match(line.strip())
            if not m:
                continue
            table = m.group("table")
            vals = split_sql_tuple(m.group("values"))

            if table == "items" and len(vals) >= 7:
                item_id = int(vals[0])
                items[item_id] = {
                    "itemId": item_id,
                    "name": str(vals[2]),
                    "typeId": int(vals[3]),
                    "level": int(vals[6]),
                    "templatePrice": int(float(vals[11])) if len(vals) > 11 else 0,
                    "itemSetId": int(vals[14]) if len(vals) > 14 else -1,
                }
            elif table == "npcs" and len(vals) >= 9:
                npc_id = int(vals[0])
                token_raw = vals[8]
                npcs[npc_id] = {
                    "npcId": npc_id,
                    "name": str(vals[1]),
                    "actionsIdCsv": str(vals[7] or ""),
                    "token": int(token_raw) if token_raw is not None else 0,
                }
            elif table == "npcs_items" and len(vals) >= 7:
                npc_items.append({
                    "rowId": int(vals[0]),
                    "npcId": int(vals[1]),
                    "itemId": int(vals[2]),
                    "note": vals[3],
                    "npcOverridePrice": int(vals[4] or 0),
                    "token": int(vals[5] or 0),
                    "actionId": int(vals[6] or 0),
                })
            elif table == "worlds_npcs" and len(vals) >= 5:
                spawns.append({
                    "spawnId": int(vals[0]),
                    "npcId": int(vals[1]),
                    "mapId": int(vals[2]),
                    "cell": int(vals[3]),
                    "direction": int(vals[4]),
                    "note": vals[5] if len(vals) > 5 else None,
                })

            if line_no % 50000 == 0:
                print(f"  ... line {line_no}")

    print(f"  items={len(items)} npcs={len(npcs)} shop_rows={len(npc_items)} spawns={len(spawns)}")
    return items, npcs, npc_items, spawns


def build_audit(items, npcs, npc_items, spawns, type_enum):
    spawns_by_npc: dict[int, list] = defaultdict(list)
    for s in spawns:
        spawns_by_npc[s["npcId"]].append(s)

    raw_count_by_npc: dict[int, int] = defaultdict(int)
    for row in npc_items:
        raw_count_by_npc[row["npcId"]] += 1

    shops_by_npc: dict[int, list] = defaultdict(list)
    for row in npc_items:
        item = items.get(row["itemId"])
        if not item:
            continue
        npc = npcs.get(row["npcId"], {})
        npc_token = int(npc.get("token") or 0)
        payment_token = row["token"] if row["token"] > 0 else npc_token
        override = row["npcOverridePrice"]
        template_price = item["templatePrice"]
        effective = override if override > 0 else template_price
        type_id = item["typeId"]
        shops_by_npc[row["npcId"]].append({
            "itemId": item["itemId"],
            "name": item["name"],
            "typeId": type_id,
            "category": type_enum.get(type_id, f"TYPE_{type_id}"),
            "level": item["level"],
            "itemSetId": item.get("itemSetId", -1),
            "templatePrice": template_price,
            "npcOverridePrice": override,
            "effectivePrice": effective,
            "paymentToken": payment_token,
        })

    npc_list = []
    orphan_npc_ids: list[int] = []
    for npc_id, shop_rows in sorted(shops_by_npc.items()):
        npc = npcs.get(npc_id, {"npcId": npc_id, "name": f"NPC_{npc_id}"})
        raw_rows = raw_count_by_npc.get(npc_id, 0)
        orphan_rows = max(0, raw_rows - len(shop_rows))
        if orphan_rows > 0:
            orphan_npc_ids.append(npc_id)
        npc_list.append({
            "npcId": npc_id,
            "name": npc.get("name", f"NPC_{npc_id}"),
            "actionsIdCsv": npc.get("actionsIdCsv", ""),
            "token": npc.get("token", 0),
            "itemCount": len(shop_rows),
            "rawRowCount": raw_rows,
            "orphanRowCount": orphan_rows,
            "spawns": spawns_by_npc.get(npc_id, []),
            "items": sorted(shop_rows, key=lambda x: (x["effectivePrice"], x["name"])),
        })

    # NPCs with only orphan rows (no runtime shop items)
    for npc_id, raw_rows in raw_count_by_npc.items():
        if npc_id in shops_by_npc or raw_rows == 0:
            continue
        npc = npcs.get(npc_id, {"npcId": npc_id, "name": f"NPC_{npc_id}"})
        orphan_npc_ids.append(npc_id)
        npc_list.append({
            "npcId": npc_id,
            "name": npc.get("name", f"NPC_{npc_id}"),
            "actionsIdCsv": npc.get("actionsIdCsv", ""),
            "token": npc.get("token", 0),
            "itemCount": 0,
            "rawRowCount": raw_rows,
            "orphanRowCount": raw_rows,
            "spawns": spawns_by_npc.get(npc_id, []),
            "items": [],
        })

    maps_with_vendors = len({s["mapId"] for n in npc_list for s in n["spawns"] if n["itemCount"] > 0})
    total_orphan = sum(n["orphanRowCount"] for n in npc_list)
    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "stats": {
            "npcWithShops": len([n for n in npc_list if n["itemCount"] > 0]),
            "totalShopRows": sum(n["itemCount"] for n in npc_list),
            "rawShopRows": sum(n["rawRowCount"] for n in npc_list),
            "orphanShopRows": total_orphan,
            "npcsWithOrphanRows": len(set(orphan_npc_ids)),
            "mapsWithVendors": maps_with_vendors,
            "uniqueItemsSold": len({i["itemId"] for n in npc_list for i in n["items"]}),
        },
        "npcs": sorted(npc_list, key=lambda x: (-x["itemCount"], x["npcId"])),
    }


def build_by_category(audit, type_enum):
    by_cat: dict[str, dict] = defaultdict(lambda: {"count": 0, "items": []})
    seen: set[int] = set()
    for npc in audit["npcs"]:
        for it in npc["items"]:
            if it["itemId"] in seen:
                continue
            seen.add(it["itemId"])
            cat = it["category"]
            by_cat[cat]["count"] += 1
            by_cat[cat]["items"].append({
                "itemId": it["itemId"],
                "name": it["name"],
                "typeId": it["typeId"],
                "level": it["level"],
                "effectivePrice": it["effectivePrice"],
                "paymentToken": it["paymentToken"],
                "soldByNpcIds": [npc["npcId"]],
            })
    # attach all npc sellers per item
    item_npcs: dict[int, list] = defaultdict(list)
    for npc in audit["npcs"]:
        for it in npc["items"]:
            item_npcs[it["itemId"]].append(npc["npcId"])
    for cat_data in by_cat.values():
        for it in cat_data["items"]:
            it["soldByNpcIds"] = sorted(set(item_npcs[it["itemId"]]))
        cat_data["items"].sort(key=lambda x: (x["level"], x["effectivePrice"], x["name"]))
    return dict(sorted(by_cat.items(), key=lambda kv: -kv[1]["count"]))


def build_lag_report(audit) -> str:
    lines = [
        "# NPC shop lag report",
        "",
        f"Generated: {audit['generatedAt']}",
        "",
        f"- NPCs with shops: **{audit['stats']['npcWithShops']}**",
        f"- Valid shop rows (runtime): **{audit['stats']['totalShopRows']}**",
        f"- Raw `npcs_items` rows: **{audit['stats'].get('rawShopRows', audit['stats']['totalShopRows'])}**",
        f"- Orphan rows (no `items` match): **{audit['stats'].get('orphanShopRows', 0)}**",
        f"- Unique items sold: **{audit['stats']['uniqueItemsSold']}**",
        "",
        "> Runtime lag follows **valid** item count (`ItemManager` skips orphan rows).",
        "",
        "## NPCs over 100 valid items (CRITICAL)",
        "",
        "| NpcId | Name | Valid | Raw | Orphan | Maps |",
        "|-------|------|-------|-----|--------|------|",
    ]
    critical = [n for n in audit["npcs"] if n["itemCount"] > MAX_ITEMS_PER_NPC]
    critical.sort(key=lambda x: -x["itemCount"])
    for n in critical:
        maps = ", ".join(str(s["mapId"]) for s in n["spawns"][:5])
        if len(n["spawns"]) > 5:
            maps += "..."
        lines.append(
            f"| {n['npcId']} | {n['name']} | **{n['itemCount']}** | "
            f"{n.get('rawRowCount', n['itemCount'])} | {n.get('orphanRowCount', 0)} | {maps} |"
        )

    orphan_heavy = sorted(
        [n for n in audit["npcs"] if n.get("orphanRowCount", 0) > 0],
        key=lambda x: -x["orphanRowCount"],
    )[:20]
    if orphan_heavy:
        lines.extend([
            "",
            "## NPCs with orphan rows (DB cleanup recommended)",
            "",
            "| NpcId | Name | Valid | Raw | Orphan |",
            "|-------|------|-------|-----|--------|",
        ])
        for n in orphan_heavy:
            lines.append(
                f"| {n['npcId']} | {n['name']} | {n['itemCount']} | "
                f"{n.get('rawRowCount', 0)} | {n.get('orphanRowCount', 0)} |"
            )

    lines.extend(["", "## All vendors (top 50 by valid items)", "", "| NpcId | Name | Valid | Raw |", "|-------|------|-------|-----|"])
    for n in sorted(audit["npcs"], key=lambda x: -x["itemCount"])[:50]:
        if n["itemCount"] == 0 and n.get("orphanRowCount", 0) == 0:
            continue
        flag = " **CRITICAL**" if n["itemCount"] > MAX_ITEMS_PER_NPC else ""
        lines.append(
            f"| {n['npcId']} | {n['name']} | {n['itemCount']}{flag} | {n.get('rawRowCount', n['itemCount'])} |"
        )
    return "\n".join(lines) + "\n"


def build_economy_proposal(audit):
    proposals = []
    seen: set[int] = set()
    for npc in audit["npcs"]:
        for it in npc["items"]:
            if it["itemId"] in seen:
                continue
            seen.add(it["itemId"])
            suggested = suggest_price(
                it["level"],
                it["typeId"],
                it["effectivePrice"],
                it["paymentToken"],
                it.get("itemSetId", -1),
            )
            proposals.append({
                "itemId": it["itemId"],
                "name": it["name"],
                "category": it["category"],
                "typeId": it["typeId"],
                "level": it["level"],
                "itemSetId": it.get("itemSetId", -1),
                "legacyEffectivePrice": it["effectivePrice"],
                "suggestedPrice": suggested,
                "paymentToken": it["paymentToken"],
                "priceBand": level_bucket(it["level"]),
            })
    proposals.sort(key=lambda x: (x["paymentToken"], x["level"], x["suggestedPrice"]))
    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "startKamas": START_KAMAS,
        "priceBands": [
            {"levelMin": lo, "levelMax": hi, "priceMin": pmin, "priceMax": pmax}
            for lo, hi, pmin, pmax in PRICE_BANDS
        ],
        "categoryMultipliers": CATEGORY_MULTIPLIERS,
        "setMultiplier": SET_PRICE_MULTIPLIER,
        "weaponTypeIds": sorted(WEAPON_TYPE_IDS),
        "itemCount": len(proposals),
        "items": proposals,
    }


def build_distribution(economy: dict, audit: dict) -> dict:
    price_map = {p["itemId"]: p for p in economy["items"]}

    # Unique kamas items only (token shops stay on original NPCs)
    unique_items: dict[int, dict] = {}
    for npc in audit["npcs"]:
        if npc.get("token", 0) > 0:
            continue
        for it in npc["items"]:
            if it["paymentToken"] > 0:
                continue
            if it["itemId"] not in unique_items:
                unique_items[it["itemId"]] = it

    # Group into family + level bucket
    groups: dict[tuple[str, str], list] = defaultdict(list)
    for item_id, it in unique_items.items():
        family = family_for_type(it["typeId"], set())
        bucket = level_bucket(it["level"])
        key = (family, bucket)
        prop = price_map.get(item_id, {})
        groups[key].append({
            "itemId": item_id,
            "name": it["name"],
            "typeId": it["typeId"],
            "category": it["category"],
            "level": it["level"],
            "price": prop.get("suggestedPrice", it["effectivePrice"]),
            "family": family,
            "levelBucket": bucket,
        })

    shops = []
    next_npc_id = NEW_NPC_ID_START

    token_shops = []
    for npc in audit["npcs"]:
        if npc.get("token", 0) > 0 or any(i["paymentToken"] > 0 for i in npc["items"]):
            token_shops.append({
                "npcId": npc["npcId"],
                "name": npc["name"],
                "token": npc.get("token", 0),
                "itemCount": npc["itemCount"],
                "action": "KEEP_UNCHANGED",
                "spawns": npc["spawns"],
            })

    mega_clear = [n["npcId"] for n in audit["npcs"] if n["itemCount"] > MAX_ITEMS_PER_NPC]
    orphan_only_clear = [
        n["npcId"] for n in audit["npcs"]
        if n["itemCount"] == 0 and n.get("orphanRowCount", 0) > 0
    ]

    for (family, bucket) in sorted(groups.keys()):
        items = sorted(groups[(family, bucket)], key=lambda x: (x["level"], x["price"], x["name"]))
        for chunk_start in range(0, len(items), MAX_ITEMS_PER_NPC):
            chunk = items[chunk_start : chunk_start + MAX_ITEMS_PER_NPC]
            if not chunk:
                continue
            level_min = min(i["level"] for i in chunk)
            level_max = max(i["level"] for i in chunk)
            shops.append({
                "proposedNpcId": next_npc_id,
                "proposedName": f"Vendeur {family} ({bucket})",
                "category": family,
                "levelBucket": bucket,
                "levelMin": level_min,
                "levelMax": level_max,
                "itemCount": len(chunk),
                "mapPlacements": [],
                "items": [{"itemId": i["itemId"], "price": i["price"], "name": i["name"]} for i in chunk],
            })
            next_npc_id += 1

    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "maxItemsPerNpc": MAX_ITEMS_PER_NPC,
        "hubMapId": HUB_MAP_ID,
        "npcIdsToClear": mega_clear,
        "npcIdsOrphanOnly": orphan_only_clear,
        "hubKeepNpcIds": sorted(HUB_KEEP_NPC_IDS),
        "tokenShopsKept": token_shops,
        "proposedShopCount": len(shops),
        "shops": shops,
    }


def finalize_distribution(distribution: dict, spawns: list, audit: dict) -> dict:
    distribution["shops"] = assign_hub_cells(distribution["shops"], spawns, audit)
    distribution["proposedShopCount"] = len(distribution["shops"])
    return distribution


def build_type_id_to_unified_slot() -> dict[int, int]:
    mapping: dict[int, int] = {}
    for entry in VIRTUAL_SHOP_SLOTS:
        if entry["slot"] == 9:
            continue
        for type_id in entry["typeIds"] or []:
            mapping[type_id] = entry["slot"]
    return mapping


def build_unified_distribution(economy: dict, audit: dict) -> dict:
    price_map = {p["itemId"]: p for p in economy["items"]}
    type_to_slot = build_type_id_to_unified_slot()

    unique_items: dict[int, dict] = {}
    for npc in audit["npcs"]:
        if npc.get("token", 0) > 0:
            continue
        for it in npc["items"]:
            if it["paymentToken"] > 0:
                continue
            if it["itemId"] not in unique_items:
                unique_items[it["itemId"]] = it

    slot_items: dict[int, list] = {s["slot"]: [] for s in VIRTUAL_SHOP_SLOTS}
    for item_id, it in unique_items.items():
        slot = type_to_slot.get(it["typeId"], 9)
        prop = price_map.get(item_id, {})
        slot_items[slot].append({
            "itemId": item_id,
            "name": it["name"],
            "typeId": it["typeId"],
            "category": it["category"],
            "level": it["level"],
            "price": prop.get("suggestedPrice", it["effectivePrice"]),
        })

    shops = []
    for entry in VIRTUAL_SHOP_SLOTS:
        items = sorted(
            slot_items[entry["slot"]],
            key=lambda x: (x["level"], x["price"], x["name"]),
        )
        level_min = min((i["level"] for i in items), default=0)
        level_max = max((i["level"] for i in items), default=0)
        shops.append({
            "slot": entry["slot"],
            "proposedNpcId": entry["npcId"],
            "proposedName": f"Tienda {entry['label']}",
            "category": entry["label"],
            "levelMin": level_min,
            "levelMax": level_max,
            "itemCount": len(items),
            "mapPlacements": [],
            "items": [{"itemId": i["itemId"], "price": i["price"], "name": i["name"]} for i in items],
        })

    return {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "mode": "unified9",
        "maxItemsPerNpc": None,
        "shops": shops,
        "proposedShopCount": len(shops),
    }


def write_unified9_sql_patches(distribution: dict):
    PATCH_DIR.mkdir(parents=True, exist_ok=True)
    backup = PATCH_DIR / "npc-shop-unified9-backup.sql"
    apply_sql = PATCH_DIR / "npc-shop-unified9-apply.sql"
    restore = PATCH_DIR / "npc-shop-unified9-restore.sql"

    backup.write_text(
        "-- Snapshot npcs_items before unified9 redistribution\n"
        "DROP TABLE IF EXISTS npcs_items_backup_unified9;\n"
        "CREATE TABLE npcs_items_backup_unified9 AS SELECT * FROM npcs_items;\n",
        encoding="utf-8",
    )
    restore.write_text(
        "-- Restore npcs_items from unified9 backup\n"
        "TRUNCATE TABLE npcs_items;\n"
        "INSERT INTO npcs_items SELECT * FROM npcs_items_backup_unified9;\n",
        encoding="utf-8",
    )

    lines = [
        "-- Apply unified9 virtual shops (.tienda 1-9, one NPC per category, no item cap)",
        f"-- Generated: {distribution['generatedAt']}",
        "",
        "DROP TABLE IF EXISTS npcs_items_backup_unified9;",
        "CREATE TABLE npcs_items_backup_unified9 AS SELECT * FROM npcs_items;",
        "",
        "DELETE ni FROM npcs_items ni LEFT JOIN items i ON i.Id = ni.Item WHERE i.Id IS NULL;",
        "",
        f"DELETE FROM npcs_items WHERE NpcId BETWEEN {LEGACY_VIRTUAL_NPC_MIN} AND {LEGACY_VIRTUAL_NPC_MAX};",
        f"DELETE FROM npcs_items WHERE NpcId BETWEEN {UNIFIED9_NPC_START} AND {UNIFIED9_NPC_END};",
        "",
    ]

    look = "{1|1||100}"
    for shop in distribution["shops"]:
        nid = shop["proposedNpcId"]
        name = shop["proposedName"].replace("'", "''")
        lines.append(f"-- Slot {shop['slot']}: {name} ({shop['itemCount']} items)")
        lines.append(
            f"INSERT INTO npcs (Id, Name, EntityLook, Gender, HasQuest, DialogMessagesIdCSV, DialogRepliesIdCSV, ActionsIdCSV, Token)"
            f" VALUES ({nid}, '{name}', '{look}', 0, 0, '', '', '1', 0)"
            f" ON DUPLICATE KEY UPDATE Name=VALUES(Name), ActionsIdCSV='1';"
        )
        for it in shop["items"]:
            lines.append(
                f"INSERT INTO npcs_items (NpcId, Item, Note, Price, Token, ActionId)"
                f" VALUES ({nid}, {it['itemId']}, NULL, {it['price']}, 0, 0);"
            )
        lines.append("")

    apply_sql.write_text("\n".join(lines), encoding="utf-8")


def write_unified9_docs(distribution: dict, economy: dict | None = None):
    lines = [
        "# NPC shop distribution (unified9)",
        "",
        f"Generated: {distribution['generatedAt']}",
        "",
        "## Objetivo",
        "",
        "- **9 tiendas fijas** alineadas a `.tienda 1` … `.tienda 9`.",
        "- **Sin límite** de ítems por NPC.",
        "- **Sin filtro por nivel** del personaje.",
        f"- Economía alineada a **{START_KAMAS:,} kamas** iniciales.",
        "",
        "## Sincronización de precios (servidor ↔ cliente)",
        "",
        "| Capa | Campo | Rol |",
        "|------|-------|-----|",
        "| DB | `npcs_items.Price` | Precio autoritativo de compra cuando `> 0` |",
        "| DB | `items.Price` | Plantilla D2O; fallback si override=0; base al vender |",
        "| Servidor | `NpcShop.GetPrice()` | `Price > 0 ? Price : templatePrice` |",
        "| Servidor | mensaje 5761 `objectPrice` | Precio enviado al cliente en lista tienda |",
        "| Cliente L0 | `ItemWrapper.price` | Debe usarse en lista **y** panel compra |",
        "",
        "**No sincronizar** precios de tienda vía D2O del cliente. El kit L0 (`TradeCenter.swf`) lee",
        "`param1.price` del ítem de tienda en `ItemNpcStore`, no `dataApi.getItem().price`.",
        "",
        "### Regenerar precios",
        "",
        "```powershell",
        "python tools/npc-shop-audit/run_npc_shop_audit.py --mode unified9 --source sql",
        "```",
        "",
        "Salida: `database/patches/npc-shop-unified9-apply.sql` + `economy-proposal.json`.",
        "",
        "Aplicar en VPS:",
        "",
        "```powershell",
        ".\\scripts\\vps\\apply-npc-shop-unified9.ps1",
        "```",
        "",
        "## Economía v2 (multiplicadores por categoría)",
        "",
        "Bandas base por nivel + multiplicador por familia (sombrero/capa/arma/dofus premium;",
        "consumibles/recursos baratos). Piezas de set: ×1.12 adicional.",
        "",
    ]
    if economy:
        lines.append("| Nivel | Min | Max |")
        lines.append("|-------|-----|-----|")
        for band in economy.get("priceBands", []):
            lines.append(
                f"| {band['levelMin']}-{band['levelMax']} | {band['priceMin']:,} | {band['priceMax']:,} |"
            )
        lines.append("")
        lines.append("| Familia | Multiplicador |")
        lines.append("|---------|---------------|")
        for family, mult in sorted(economy.get("categoryMultipliers", {}).items()):
            lines.append(f"| {family} | {mult} |")
        lines.append(f"| Set bonus | ×{economy.get('setMultiplier', SET_PRICE_MULTIPLIER)} |")
        lines.append("")
    lines.extend([
        "## Tiendas",
        "",
        "| Slot | Comando | NpcId | Categoría | Items | Nivel min-max |",
        "|------|---------|-------|-----------|-------|---------------|",
    ])
    for shop in distribution["shops"]:
        lines.append(
            f"| {shop['slot']} | .tienda {shop['slot']} | {shop['proposedNpcId']} | "
            f"{shop['category']} | {shop['itemCount']} | {shop['levelMin']}-{shop['levelMax']} |"
        )
    lines.extend([
        "",
        "## Aplicar en VPS",
        "",
        "1. Revisar `tools/npc-shop-audit/virtual-shops-unified9.json`.",
        "2. `docker exec -i sunshine-db mariadb -uroot -p... sunshine < database/patches/npc-shop-unified9-apply.sql`",
        "3. Sync `VirtualShopCatalog.cs` + rebuild `sunshine-server`.",
        "",
    ])
    DOCS_PATH.parent.mkdir(parents=True, exist_ok=True)
    DOCS_PATH.write_text("\n".join(lines), encoding="utf-8")


def write_sql_patches(distribution: dict, audit: dict):
    PATCH_DIR.mkdir(parents=True, exist_ok=True)
    backup = PATCH_DIR / "npc-shop-redistribute-backup.sql"
    apply_sql = PATCH_DIR / "npc-shop-redistribute-apply.sql"
    restore = PATCH_DIR / "npc-shop-redistribute-restore.sql"

    backup.write_text(
        "-- Snapshot npcs_items before redistribution\n"
        "CREATE TABLE IF NOT EXISTS npcs_items_backup_redistribute AS SELECT * FROM npcs_items;\n",
        encoding="utf-8",
    )
    restore.write_text(
        "-- Restore npcs_items from backup\n"
        "TRUNCATE TABLE npcs_items;\n"
        "INSERT INTO npcs_items SELECT * FROM npcs_items_backup_redistribute;\n",
        encoding="utf-8",
    )

    lines = [
        "-- Apply NPC shop redistribution (max 100 items/NPC, hub map 2323)",
        "-- Review npc-distribution-plan.json before running on production.",
        "",
        "CREATE TABLE IF NOT EXISTS npcs_items_backup_redistribute AS SELECT * FROM npcs_items;",
        "",
        "-- Remove orphan shop rows (no matching items template)",
        "DELETE ni FROM npcs_items ni LEFT JOIN items i ON i.Id = ni.Item WHERE i.Id IS NULL;",
        "",
    ]

    clear_ids = sorted(set(distribution["npcIdsToClear"] + distribution.get("npcIdsOrphanOnly", [])))
    hub_remove_ids = sorted(
        set(clear_ids)
        - set(distribution.get("hubKeepNpcIds", []))
        - {t["npcId"] for t in distribution.get("tokenShopsKept", [])}
    )
    if hub_remove_ids:
        id_list = ", ".join(str(i) for i in hub_remove_ids)
        lines.append(f"-- Retirar vendedores obsoletos del hub {HUB_MAP_ID}")
        lines.append(
            f"DELETE FROM worlds_npcs WHERE Map = {HUB_MAP_ID} AND Npc IN ({id_list});"
        )
        lines.append("")

    if clear_ids:
        id_list = ", ".join(str(i) for i in sorted(clear_ids))
        lines.append(f"-- Clear mega-vendors ({len(clear_ids)} NPCs)")
        lines.append(f"DELETE FROM npcs_items WHERE NpcId IN ({id_list});")
        lines.append("")

    for shop in distribution["shops"]:
        nid = shop["proposedNpcId"]
        name = shop["proposedName"].replace("'", "''")
        look = "{1|1||100}"
        lines.append(f"-- {name} ({shop['itemCount']} items)")
        lines.append(
            f"INSERT INTO npcs (Id, Name, EntityLook, Gender, HasQuest, DialogMessagesIdCSV, DialogRepliesIdCSV, ActionsIdCSV, Token)"
            f" VALUES ({nid}, '{name}', '{look}', 0, 0, '', '', '1', 0)"
            f" ON DUPLICATE KEY UPDATE Name=VALUES(Name), ActionsIdCSV='1';"
        )
        for place in shop["mapPlacements"]:
            lines.append(
                f"INSERT INTO worlds_npcs (Npc, Map, Cell, Direction, Note)"
                f" VALUES ({nid}, {place['mapId']}, {place['cell']}, {place.get('direction', 1)}, '{name}');"
            )
        for it in shop["items"]:
            lines.append(
                f"INSERT INTO npcs_items (NpcId, Item, Note, Price, Token, ActionId)"
                f" VALUES ({nid}, {it['itemId']}, NULL, {it['price']}, 0, 0);"
            )
        lines.append("")

    apply_sql.write_text("\n".join(lines), encoding="utf-8")


def write_docs(distribution: dict, audit: dict, economy: dict):
    lines = [
        "# NPC shop distribution",
        "",
        f"Generated: {distribution['generatedAt']}",
        "",
        "## Objetivo",
        "",
        f"- Repartir catálogos para **máximo {MAX_ITEMS_PER_NPC} ítems por NPC**.",
        f"- Economía alineada a **{START_KAMAS:,} kamas** iniciales.",
        f"- Hub principal: mapa **{HUB_MAP_ID}**.",
        "",
        "## Resumen",
        "",
        f"| Métrica | Valor |",
        f"|---------|-------|",
        f"| NPC vendedores actuales | {audit['stats']['npcWithShops']} |",
        f"| Filas tienda válidas (runtime) | {audit['stats']['totalShopRows']} |",
        f"| Filas huérfanas en DB | {audit['stats'].get('orphanShopRows', 0)} |",
        f"| NPC CRITICAL (>100 válidos) | {len(distribution['npcIdsToClear'])} |",
        f"| NPC solo huérfanos (limpieza) | {len(distribution.get('npcIdsOrphanOnly', []))} |",
        f"| Tiendas propuestas (nuevas) | {distribution['proposedShopCount']} |",
        f"| Tiendas token sin cambio | {len(distribution['tokenShopsKept'])} |",
        "",
        "## NPCs a vaciar (mega-vendors)",
        "",
        ", ".join(str(i) for i in sorted(distribution["npcIdsToClear"])) or "(ninguno)",
        "",
        "## NPCs solo con filas huérfanas (se limpian en apply)",
        "",
        ", ".join(str(i) for i in sorted(distribution.get("npcIdsOrphanOnly", []))) or "(ninguno)",
        "",
        "## Tiendas token (sin cambios)",
        "",
        "| NpcId | Nombre | Items |",
        "|-------|--------|-------|",
    ]
    for t in distribution["tokenShopsKept"]:
        lines.append(f"| {t['npcId']} | {t['name']} | {t['itemCount']} |")

    lines.extend([
        "",
        "## Layout hub 2323 (UX)",
        "",
        "Filas = categoría de equipo (arriba → abajo). Columnas = progresión de nivel (izquierda → derecha).",
        "",
        "| Fila | Categoría | NPCs |",
        "|------|-----------|------|",
    ])
    rows: dict[int, list[str]] = defaultdict(list)
    for shop in distribution["shops"]:
        rows[shop.get("layoutRow", -1)].append(
            f"{shop['proposedNpcId']} ({shop['levelMin']}-{shop['levelMax']})"
        )
    for row_idx in sorted(rows.keys()):
        sample = distribution["shops"][0]
        family = next(
            (s["category"] for s in distribution["shops"] if s.get("layoutRow") == row_idx),
            "?",
        )
        label = FAMILY_DISPLAY.get(family, family)
        lines.append(f"| {row_idx + 1} | {label} | {', '.join(rows[row_idx])} |")

    lines.extend([
        "",
        "## Tiendas propuestas (kamas)",
        "",
        "| NpcId | Nombre | Categoría | Nivel | Items | Celda 2323 | Precio min | Precio max |",
        "|-------|--------|-----------|-------|-------|------------|------------|------------|",
    ])
    for shop in distribution["shops"]:
        prices = [i["price"] for i in shop["items"]]
        cell = shop["mapPlacements"][0]["cell"] if shop["mapPlacements"] else "-"
        lines.append(
            f"| {shop['proposedNpcId']} | {shop['proposedName']} | {shop['category']} | "
            f"{shop['levelMin']}-{shop['levelMax']} | {shop['itemCount']} | {cell} | "
            f"{min(prices):,} | {max(prices):,} |"
        )

    lines.extend([
        "",
        "## Archivos generados",
        "",
        "- `tools/npc-shop-audit/npc-shops-full.json`",
        "- `tools/npc-shop-audit/items-by-category.json`",
        "- `tools/npc-shop-audit/economy-proposal.json`",
        "- `tools/npc-shop-audit/npc-distribution-plan.json`",
        "- `tools/npc-shop-audit/npc-lag-report.md`",
        "- `database/patches/npc-shop-redistribute-apply.sql`",
        "",
        "## Aplicar en VPS",
        "",
        "1. Revisar `npc-distribution-plan.json`.",
        "2. `mariadb sunshine < database/patches/npc-shop-redistribute-apply.sql`",
        "3. Reiniciar `sunshine-server`.",
        "",
    ])
    DOCS_PATH.parent.mkdir(parents=True, exist_ok=True)
    DOCS_PATH.write_text("\n".join(lines), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="NPC shop audit and redistribution planner")
    parser.add_argument(
        "--mode",
        choices=("legacy", "unified9"),
        default="unified9",
        help="legacy=42 NPCs by level bucket; unified9=9 NPCs by .tienda slot (default)",
    )
    parser.add_argument(
        "--source",
        choices=("auto", "db", "sql"),
        default="auto",
        help="Data source: MariaDB, sunshine.sql, or auto (try DB then SQL)",
    )
    args = parser.parse_args()
    dotenv = load_dotenv()

    type_enum = load_type_enum()
    items = npcs = None
    npc_items = spawns = None
    source_used = "sql"

    if args.source in ("auto", "db"):
        print("Trying MariaDB ...")
        db_data = fetch_from_mariadb(dotenv)
        if db_data is not None:
            items, npcs, npc_items, spawns = db_data
            source_used = "mariadb"
        elif args.source == "db":
            print("MariaDB unavailable.", file=sys.stderr)
            sys.exit(1)
        else:
            print("MariaDB unavailable, falling back to sunshine.sql")

    if items is None:
        if not SQL_PATH.exists():
            print(f"Missing {SQL_PATH}", file=sys.stderr)
            sys.exit(1)
        items, npcs, npc_items, spawns = parse_sql_dump(SQL_PATH)

    audit = build_audit(items, npcs, npc_items, spawns, type_enum)
    audit["dataSource"] = source_used
    by_category = build_by_category(audit, type_enum)
    lag_report = build_lag_report(audit)
    economy = build_economy_proposal(audit)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "npc-shops-full.json").write_text(
        json.dumps(audit, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (OUT_DIR / "items-by-category.json").write_text(
        json.dumps(by_category, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (OUT_DIR / "npc-lag-report.md").write_text(lag_report, encoding="utf-8")
    (OUT_DIR / "economy-proposal.json").write_text(
        json.dumps(economy, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    if args.mode == "unified9":
        distribution = build_unified_distribution(economy, audit)
        (OUT_DIR / "virtual-shops-unified9.json").write_text(
            json.dumps(distribution, ensure_ascii=False, indent=2), encoding="utf-8"
        )
        write_unified9_sql_patches(distribution)
        write_unified9_docs(distribution, economy)
        with (OUT_DIR / "npc-shop-list.csv").open("w", newline="", encoding="utf-8") as f:
            w = csv.writer(f)
            w.writerow(["slot", "npcId", "name", "category", "itemCount", "levelMin", "levelMax", "priceMin", "priceMax"])
            for shop in distribution["shops"]:
                prices = [i["price"] for i in shop["items"]]
                w.writerow([
                    shop["slot"], shop["proposedNpcId"], shop["proposedName"], shop["category"],
                    shop["itemCount"], shop["levelMin"], shop["levelMax"],
                    min(prices) if prices else 0, max(prices) if prices else 0,
                ])
        print("\n=== Done (unified9) ===")
        print(f"  source: {source_used}")
        print(f"  unique items sold (kamas): {sum(s['itemCount'] for s in distribution['shops'])}")
        for shop in distribution["shops"]:
            print(f"  slot {shop['slot']} NPC {shop['proposedNpcId']} {shop['category']}: {shop['itemCount']} items")
        print(f"  SQL: {PATCH_DIR / 'npc-shop-unified9-apply.sql'}")
        print(f"  Docs: {DOCS_PATH}")
        return

    distribution = build_distribution(economy, audit)
    distribution = finalize_distribution(distribution, spawns, audit)

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    (OUT_DIR / "npc-shops-full.json").write_text(
        json.dumps(audit, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (OUT_DIR / "items-by-category.json").write_text(
        json.dumps(by_category, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (OUT_DIR / "npc-lag-report.md").write_text(lag_report, encoding="utf-8")
    (OUT_DIR / "economy-proposal.json").write_text(
        json.dumps(economy, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    (OUT_DIR / "npc-distribution-plan.json").write_text(
        json.dumps(distribution, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    write_sql_patches(distribution, audit)
    write_docs(distribution, audit, economy)

    # Compact NPC list CSV for quick reference
    with (OUT_DIR / "npc-shop-list.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["npcId", "name", "category", "levelMin", "levelMax", "itemCount", "mapId", "cell", "priceMin", "priceMax"])
        for shop in distribution["shops"]:
            prices = [i["price"] for i in shop["items"]]
            place = shop["mapPlacements"][0] if shop["mapPlacements"] else {}
            w.writerow([
                shop["proposedNpcId"], shop["proposedName"], shop["category"],
                shop["levelMin"], shop["levelMax"], shop["itemCount"],
                place.get("mapId", ""), place.get("cell", ""),
                min(prices) if prices else 0, max(prices) if prices else 0,
            ])

    print("\n=== Done ===")
    print(f"  source: {source_used}")
    print(f"  npc-shops-full.json: {audit['stats']['npcWithShops']} NPCs, {audit['stats']['totalShopRows']} valid rows")
    print(f"  orphan rows: {audit['stats'].get('orphanShopRows', 0)}")
    print(f"  CRITICAL NPCs (>100): {len(distribution['npcIdsToClear'])}")
    print(f"  Proposed shops: {distribution['proposedShopCount']}")
    print(f"  Docs: {DOCS_PATH}")


if __name__ == "__main__":
    main()
