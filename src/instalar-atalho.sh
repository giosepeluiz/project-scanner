#!/usr/bin/env bash
# Cria o atalho do Digitalizar no menu de aplicativos do usuário.
set -e
pasta="$(dirname "$(readlink -f "$0")")"
destino="${XDG_DATA_HOME:-$HOME/.local/share}"
mkdir -p "$destino/applications" "$destino/icons/hicolor/64x64/apps"
[ -f "$pasta/icones/Digitalizar_64.png" ] && cp "$pasta/icones/Digitalizar_64.png" "$destino/icons/hicolor/64x64/apps/digitalizar.png"
cat > "$destino/applications/digitalizar.desktop" <<EOT
[Desktop Entry]
Type=Application
Name=Digitalizar
Comment=Digitalize documentos com o seu scanner
Exec="$pasta/digitalizar.sh"
Path=$pasta
Icon=digitalizar
Terminal=false
Categories=Graphics;Scanning;Office;
EOT
chmod +x "$pasta/digitalizar.sh"
command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$destino/applications" || true
echo "Atalho criado: procure por Digitalizar no menu de aplicativos."
