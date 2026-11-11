#!/usr/bin/env bash
# Compila o Digitalizar para Android sem Gradle nem Android Studio.
# Precisa de: JDK 17+, aapt2, d8.jar, android.jar (API 35) e apksigner.jar em $ASDK.
set -euo pipefail
cd "$(dirname "$0")"
ASDK=${ASDK:-/opt/asdk}
VERSAO=$(grep -o 'versionName="[^"]*"' app/AndroidManifest.xml | cut -d'"' -f2)
OUT=build
rm -rf $OUT && mkdir -p $OUT/res $OUT/gen $OUT/classes $OUT/dex

echo "» recursos"
"$ASDK/aapt2" compile --dir app/res -o $OUT/res/res.zip
"$ASDK/aapt2" link -I "$ASDK/android-res-34.jar" --manifest app/AndroidManifest.xml \
  -A app/assets --java $OUT/gen --min-sdk-version 29 --target-sdk-version 35 \
  -o $OUT/base.apk $OUT/res/res.zip

echo "» java"
find app/src $OUT/gen -name '*.java' > $OUT/fontes.txt
javac -nowarn -Xlint:-options -encoding UTF-8 -source 8 -target 8 -bootclasspath "$ASDK/android.jar:$ASDK/core-lambda-stubs.jar" \
  -d $OUT/classes @$OUT/fontes.txt
(cd $OUT/classes && jar cf ../classes.jar .)

echo "» dex"
java -cp "$ASDK/dx.jar" com.android.dx.command.Main --dex --min-sdk-version=29 --output=$OUT/dex/classes.dex $OUT/classes.jar

echo "» apk"
python3 tools/zipalign.py $OUT/base.apk $OUT/unsigned.apk $OUT/dex/classes*.dex
KS=${KS:-keystore/digitalizar.jks}
java -jar "$ASDK/apksigner.jar" sign --ks "$KS" --ks-pass "pass:${KS_PASS:-digitalizar}" \
  --ks-key-alias digitalizar --v1-signing-enabled true --v2-signing-enabled true \
  --out $OUT/Digitalizar-$VERSAO-android.apk $OUT/unsigned.apk 2>&1 | grep -v JAVA_TOOL || true
java -jar "$ASDK/apksigner.jar" verify --verbose $OUT/Digitalizar-$VERSAO-android.apk 2>&1 | grep -E 'Verifies|v2|ERROR' | grep -v JAVA_TOOL
echo "Pronto: $OUT/Digitalizar-$VERSAO-android.apk"
