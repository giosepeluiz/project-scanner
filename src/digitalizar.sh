#!/usr/bin/env bash
# Abre o Digitalizar no Linux (precisa do Mono e do SANE: veja o README).
cd "$(dirname "$(readlink -f "$0")")" || exit 1
if ! command -v mono >/dev/null 2>&1; then
    echo "O Mono não está instalado. No Debian/Ubuntu/Mint: sudo apt install mono-complete sane-utils" >&2
    exit 1
fi
exec mono Digitalizar.exe "$@"
