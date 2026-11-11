#!/usr/bin/env python3
"""Junta o APK do aapt2 com os classes*.dex e alinha as entradas não comprimidas
(equivalente ao 'zipalign -p 4'). Uso: zipalign.py base.apk saida.apk dex1 [dex2...]"""
import sys, zipfile, os

base, saida, dexes = sys.argv[1], sys.argv[2], sys.argv[3:]

def escrever(zout, info, dados, offset_atual):
    zi = zipfile.ZipInfo(info.filename, date_time=(2026, 1, 1, 0, 0, 0))
    zi.compress_type = info.compress_type
    zi.external_attr = info.external_attr
    if zi.compress_type == zipfile.ZIP_STORED:
        alinh = 16384 if zi.filename.endswith('.so') else 4
        cabec = 30 + len(zi.filename.encode('utf-8'))
        pad = (-(offset_atual + cabec)) % alinh
        zi.extra = b'\0' * pad
    zout.writestr(zi, dados)

with zipfile.ZipFile(base) as zin, zipfile.ZipFile(saida, 'w') as zout:
    entradas = [(i, zin.read(i.filename)) for i in zin.infolist()]
    for d in dexes:
        i = zipfile.ZipInfo(os.path.basename(d))
        i.compress_type = zipfile.ZIP_DEFLATED
        entradas.append((i, open(d, 'rb').read()))
    for info, dados in entradas:
        escrever(zout, info, dados, zout.fp.tell())
print('ok', saida)
