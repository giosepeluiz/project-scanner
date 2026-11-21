#!/usr/bin/env bash
# Monta os arquivos de uma release a partir do código atual (versões lidas de src/Janela.cs e do AndroidManifest):
#   Digitalizar-<v>-windows.zip, Digitalizar-<v>-linux.tar.gz, Digitalizar-<a>-android.apk e SHA256SUMS-<v>.txt
# Uso: bash tools/empacotar.sh [pasta-de-saida]      (padrão: ./entrega)
# Precisa de: mono-mcs, zip, tar, JDK 17+ e o Android SDK enxuto em $ASDK (android/tools/preparar.sh) + keystore em android/keystore/.
# Os arquivos NÃO vão para o git (veja o .gitignore): você os anexa na release do GitHub.
set -euo pipefail
RAIZ=$(cd "$(dirname "$0")/.." && pwd)
SAIDA=$(mkdir -p "${1:-entrega}" && cd "${1:-entrega}" && pwd)
V=$(grep -o 'AssemblyVersion("[0-9.]*")' "$RAIZ/src/Janela.cs" | grep -o '[0-9]*\.[0-9]*\.[0-9]*' | head -1)
A=$(grep -o 'versionName="[^"]*"' "$RAIZ/android/app/AndroidManifest.xml" | cut -d'"' -f2)
echo "PC $V · Android $A → $SAIDA"
TMP=$(mktemp -d)

echo "» PC (Mono)"
cd "$RAIZ/src"
mcs -nowarn:0649 -target:winexe -out:"$TMP/Digitalizar.exe" -win32icon:Digitalizar.ico \
  -resource:Digitalizar.ico,Digitalizar.ico -resource:idiomas.tsv,idiomas.tsv \
  $(for t in 16 20 24 32 40 48 64; do echo -n "-resource:icones/Digitalizar_$t.png,ico_$t.png "; done) \
  -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:Microsoft.CSharp.dll -r:System.Core.dll -r:System.dll \
  -r:System.IO.Compression.dll -r:System.IO.Compression.FileSystem.dll *.cs
mkdir -p "$TMP/win" "$TMP/lin/icones"
cp "$TMP/Digitalizar.exe" "$RAIZ/LICENSE" "$TMP/win/"
cp "$TMP/Digitalizar.exe" "$RAIZ/LICENSE" digitalizar.sh instalar-atalho.sh "$TMP/lin/"
cp icones/Digitalizar_64.png "$TMP/lin/icones/"
rm -f "$SAIDA/Digitalizar-$V-windows.zip" "$SAIDA/Digitalizar-$V-linux.tar.gz"
(cd "$TMP/win" && zip -q -X "$SAIDA/Digitalizar-$V-windows.zip" Digitalizar.exe LICENSE)
(cd "$TMP/lin" && tar czf "$SAIDA/Digitalizar-$V-linux.tar.gz" Digitalizar.exe LICENSE digitalizar.sh instalar-atalho.sh icones)

echo "» Android"
(cd "$RAIZ/android" && ./build.sh)
cp "$RAIZ/android/build/Digitalizar-$A-android.apk" "$SAIDA/"

cd "$SAIDA"
sha256sum "Digitalizar-$V-windows.zip" "Digitalizar-$V-linux.tar.gz" "Digitalizar-$A-android.apk" | tee "SHA256SUMS-$V.txt"
