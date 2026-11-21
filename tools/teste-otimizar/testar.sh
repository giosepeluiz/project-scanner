#!/usr/bin/env bash
# Testa o "Otimizar" no computador, sem scanner e sem telefone:
#   1) gera páginas sintéticas (texto digitado e croqui) com borda preta, inclinação e giros de 0/90/180/270°;
#   2) roda o C# (src/Otimizar.cs, via Mono) e confere se a página voltou à posição certa;
#   3) roda o Java (android/.../Otimizar.java) e confere que dá byte a byte o mesmo resultado do C#.
# Precisa de: mono-mcs, JDK 17+, python3 com numpy e pillow.  Uso: bash tools/teste-otimizar/testar.sh
set -euo pipefail
AQUI=$(cd "$(dirname "$0")" && pwd)
RAIZ=$(cd "$AQUI/../.." && pwd)
W=${W:-$(mktemp -d)}
echo "pasta de trabalho: $W"
cd "$W"
cp "$AQUI"/*.py .
python3 gerar-texto.py            # c0..c29 + casos.txt (esperado por conferir.py)
python3 gerar-croqui.py           # s0..s6 (croqui: o lado do giro de 90° é um padrão, não uma pista)

mcs -nowarn:0649 -out:T.exe -r:System.Drawing.dll "$AQUI/T.cs" "$RAIZ/src/Otimizar.cs"
mkdir -p jout
javac -nowarn -d jout "$AQUI/java/android/graphics/Bitmap.java" \
  "$RAIZ/android/app/src/io/github/giosepeluiz/digitalizar/Otimizar.java" \
  "$AQUI/java/io/github/giosepeluiz/digitalizar/TO.java" 2>&1 | grep -v "^Picked up" || true

echo "== C#: texto digitado (esperado: todos OK; c0..c3 podem mostrar dif ~13 só pelo recorte da borda; c29 = maiúsculas de cabeça para baixo, limitação conhecida)"
while IFS=, read -r n borda ang rot tipo || [ -n "$n" ]; do mono T.exe c$n.raw c$n.out >/dev/null; done < casos.txt
python3 conferir.py | tail -35

echo "== C#: croquis (giro de 90° horário é o padrão; s3 = 180° não é corrigido, limitação conhecida)"
for i in 0 1 2 3 4 5 6; do echo -n "s$i: "; mono T.exe s$i.raw s$i.out | tail -1; done

echo "== Java x C#: as saídas têm de ser idênticas"
ruim=0
for f in c*.raw s*.raw; do
  n=${f%.raw}; rm -f $n.j.out
  java -cp jout io.github.giosepeluiz.digitalizar.TO $f $n.j.out >/dev/null 2>&1 || true
  if [ -f $n.out ] || [ -f $n.j.out ]; then cmp -s $n.out $n.j.out || { echo "DIFERENTE: $n"; ruim=1; }; fi
done
[ $ruim = 0 ] && echo "Java e C# iguais em todos os casos" || exit 1
