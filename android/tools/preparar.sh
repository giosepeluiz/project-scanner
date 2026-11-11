#!/usr/bin/env bash
# Baixa as ferramentas para compilar o Digitalizar para Android sem o Android Studio
# (aapt2, dx, android.jar e apksigner) para a pasta $ASDK (padrão: /opt/asdk).
# Precisa de: JDK 17+, python3 + pip, npm, git, curl e unzip.
set -euo pipefail
ASDK=${ASDK:-/opt/asdk}
TMP=$(mktemp -d)
mkdir -p "$ASDK"
cd "$TMP"

echo "» aapt2 (pacote aapt2 do PyPI)"
pip download aapt2==0.2.1 --no-deps -q -d aapt2 && (cd aapt2 && unzip -q -o ./*.whl)
cp aapt2/aapt2/bin/Linux/aapt2 "$ASDK/aapt2" && chmod +x "$ASDK/aapt2"

echo "» dx (vem com o dex2jar)"
curl -sSL -o d2j.zip https://github.com/pxb1988/dex2jar/releases/download/v2.4/dex-tools-v2.4.zip
unzip -q -o d2j.zip -d d2j
cp d2j/dex-tools-*/lib/dx-*.jar "$ASDK/dx.jar"

echo "» android.jar (API 35 para o Java, API 34 para os recursos)"
git clone -q --depth 1 --filter=blob:none --sparse https://github.com/Sable/android-platforms
(cd android-platforms && git sparse-checkout set android-34 android-35)
cp android-platforms/android-35/android.jar "$ASDK/android.jar"
cp android-platforms/android-34/android.jar "$ASDK/android-res-34.jar"

echo "» apksigner"
npm pack @postar/apktool-node --silent >/dev/null && tar xzf postar-apktool-node-*.tgz
cp package/lib/apksigner.jar "$ASDK/apksigner.jar"

echo "» stub do LambdaMetafactory (só para o javac)"
mkdir -p stub/java/lang/invoke
cat > stub/java/lang/invoke/LambdaMetafactory.java <<'EOF'
package java.lang.invoke;
public final class LambdaMetafactory {
  public static CallSite metafactory(MethodHandles.Lookup c, String n, MethodType t, MethodType s, MethodHandle i, MethodType d) { return null; }
}
EOF
javac -source 8 -target 8 -bootclasspath "$ASDK/android.jar" -d stub/out stub/java/lang/invoke/*.java 2>/dev/null
(cd stub/out && jar cf "$ASDK/core-lambda-stubs.jar" .)

rm -rf "$TMP"
echo "Pronto: ferramentas em $ASDK"
