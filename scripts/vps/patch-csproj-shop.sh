#!/bin/bash
set -eu
CSPROJ="/opt/dofus-2.0.0-build/Sunshine net11.0/Sunshine net11.0/Sunshine.csproj"
cp "$CSPROJ" "$CSPROJ.bak-shop-fix"

if git -C /opt/dofus-2.0.0-build rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  git -C /opt/dofus-2.0.0-build checkout -- "Sunshine net11.0/Sunshine net11.0/Sunshine.csproj" || true
fi

add_after() {
  local anchor="$1"
  local needle="$2"
  local line="$3"
  if ! grep -Fq "$needle" "$CSPROJ"; then
    sed -i "/${anchor}/a\\		${line}" "$CSPROJ"
  fi
}

add_after 'XpPanelCommand.cs' 'TiendaCommand.cs' '<Compile Include="Sunshine.WorldServer\\Commands\\Player\\TiendaCommand.cs" />'
add_after 'TiendaCommand.cs' 'TiendasCommand.cs' '<Compile Include="Sunshine.WorldServer\\Commands\\Player\\TiendasCommand.cs" />'
add_after 'Npc.cs' 'VirtualShopRegistry.cs' '<Compile Include="Sunshine.WorldServer\\Game\\Actors\\Npcs\\VirtualShopRegistry.cs" />'
add_after 'VirtualShopRegistry.cs' 'VirtualShopCatalog.cs' '<Compile Include="Sunshine.WorldServer\\Game\\Actors\\Npcs\\VirtualShopCatalog.cs" />'

sed -i '/VirtualShopDirectoryDialog.cs/d' "$CSPROJ"

echo "=== patched entries ==="
grep -E 'TiendaCommand|TiendasCommand|VirtualShopRegistry|VirtualShopCatalog' "$CSPROJ"
