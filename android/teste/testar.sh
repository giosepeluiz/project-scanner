#!/usr/bin/env bash
# Testa a deteção do documento no computador (sem telefone): gera fotos sintéticas e mede o erro dos cantos.
set -e
cd "$(dirname "$0")"
python3 gerar.py
mkdir -p cls
javac -nowarn -d cls ../app/src/io/github/giosepeluiz/digitalizar/Visao.java Teste.java
java -cp cls Teste
