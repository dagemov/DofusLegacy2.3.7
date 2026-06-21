#!/usr/bin/env python3
"""Patch VPS csproj for chat-based .tienda commands (L0 client, no UI)."""
from pathlib import Path
import sys

CSPROJ = Path("/opt/dofus-2.0.0-build/Sunshine net11.0/Sunshine net11.0/Sunshine.csproj")

ADD_LINES = [
    ('TiendaCommand.cs', '\t\t<Compile Include="Sunshine.WorldServer\\Commands\\Player\\TiendaCommand.cs" />'),
    ('TiendasCommand.cs', '\t\t<Compile Include="Sunshine.WorldServer\\Commands\\Player\\TiendasCommand.cs" />'),
    ('VirtualShopRegistry.cs', '\t\t<Compile Include="Sunshine.WorldServer\\Game\\Actors\\Npcs\\VirtualShopRegistry.cs" />'),
    ('VirtualShopCatalog.cs', '\t\t<Compile Include="Sunshine.WorldServer\\Game\\Actors\\Npcs\\VirtualShopCatalog.cs" />'),
]

REMOVE_NEEDLES = [
    'VirtualShopDirectoryDialog.cs',
]


def main() -> int:
    if not CSPROJ.exists():
        print(f"MISSING {CSPROJ}")
        return 1

    lines = CSPROJ.read_text(encoding="utf-8").splitlines()
    out = []
    for line in lines:
        if any(n in line for n in REMOVE_NEEDLES):
            print(f"Removed csproj line: {line.strip()}")
            continue
        out.append(line)

    text = "\n".join(out)
    if not text.endswith("\n"):
        text += "\n"

    for needle, insert in ADD_LINES:
        if needle in text:
            continue
        anchor = 'XpPanelCommand.cs'
        idx = text.find(anchor)
        if idx == -1:
            print(f"Anchor not found for {needle}")
            return 1
        line_end = text.find("\n", idx)
        text = text[: line_end + 1] + insert + "\n" + text[line_end + 1 :]
        print(f"Added csproj entry: {needle}")

    CSPROJ.write_text(text, encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
