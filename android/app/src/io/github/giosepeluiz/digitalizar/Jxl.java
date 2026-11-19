// Codificador JPEG XL sem perdas (modo modular, preditor gradiente, códigos de prefixo e repetição de zeros).
// Escrito só com Java, sem biblioteca externa. A imagem sai idêntica à original (1 canal cinza ou 3 canais RGB com YCoCg).
// Mesmo algoritmo da versão do PC (src/JpegXl.cs): os dois geram o mesmo arquivo.
package io.github.giosepeluiz.digitalizar;

import android.graphics.Bitmap;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.List;

final class Jxl {
    private Jxl() {}

    static final int GRUPO = 256;
    static final int NRAW = 19;
    static final int NLZ = 33;
    static final int MINLZ = 7;

    // ------------------------------------------------------------ bits (LSB primeiro)
    static final class Bits {
        final ByteArrayOutputStream ms = new ByteArrayOutputStream();
        long buf;
        int n;
        void write(int count, long bits) {
            if (count == 0) return;
            buf |= (bits & ((1L << count) - 1)) << n;
            n += count;
            while (n >= 8) { ms.write((int) (buf & 0xFF)); buf >>>= 8; n -= 8; }
        }
        void padByte() { if (n > 0) write(8 - n, 0); }
        byte[] toBytes() { padByte(); return ms.toByteArray(); }
    }

    // ------------------------------------------------------------ códigos de prefixo
    static int floorLog2(long v) { int r = 0; while ((v >>>= 1) != 0) r++; return r; }

    // retorna {token, nbits, bits}
    static long[] tokenRaw(long v) {
        if (v == 0) return new long[]{0, 0, 0};
        int n = floorLog2(v);
        return new long[]{n + 1, n, v - (1L << n)};
    }

    static long[] tokenLz(long v) {
        if (v < 16) return new long[]{v, 0, 0};
        int n = floorLog2(v);
        return new long[]{16 + n - 4, n, v - (1L << n)};
    }

    static void comprimentos(long[] freqs, int n, int maxLen, byte[] nbits) {
        List<Integer> idx = new ArrayList<Integer>();
        for (int i = 0; i < n; i++) if (freqs[i] > 0) idx.add(i);
        for (int i = 0; i < n; i++) nbits[i] = 0;
        int ni = idx.size();
        if (ni == 0) return;
        if (ni == 1) { nbits[idx.get(0)] = 1; return; }
        int precision = maxLen;
        int W = (1 << precision) + 1;
        long inf = Long.MAX_VALUE / 4;
        long[] d = new long[W * (ni + 1)];
        java.util.Arrays.fill(d, inf);
        d[0] = 0;
        for (int s = 0; s < ni; s++) {
            long f = freqs[idx.get(s)];
            for (int bits = 1; bits <= maxLen; bits++) {
                int delta = 1 << (precision - bits);
                for (int off = 0; off + delta <= (1 << precision); off++) {
                    long a = d[s * W + off];
                    if (a >= inf) continue;
                    long c = a + f * bits;
                    if (c < d[(s + 1) * W + off + delta]) d[(s + 1) * W + off + delta] = c;
                }
            }
        }
        int o = 1 << precision;
        for (int s = ni - 1; s >= 0; s--) {
            long f = freqs[idx.get(s)];
            for (int bits = 1; bits <= maxLen; bits++) {
                int delta = 1 << (precision - bits);
                if (delta <= o && d[s * W + o - delta] < inf && d[(s + 1) * W + o] == d[s * W + o - delta] + f * bits) {
                    o -= delta; nbits[idx.get(s)] = (byte) bits; break;
                }
            }
        }
    }

    static int rev(int bits, int nbits) {
        int r = 0;
        for (int i = 0; i < nbits; i++) { r = (r << 1) | (bits & 1); bits >>= 1; }
        return r;
    }

    static void canonico(byte[] lens, int[] outBits) {
        int[] count = new int[17];
        for (int i = 0; i < lens.length; i++) if (lens[i] > 0) count[lens[i]]++;
        int[] next = new int[17];
        int code = 0;
        for (int l = 1; l <= 16; l++) { code = (code + count[l - 1]) << 1; next[l] = code; }
        for (int i = 0; i < lens.length; i++)
            if (lens[i] > 0) outBits[i] = rev(next[lens[i]]++, lens[i]);
    }

    static final class Prefixo {
        final byte[] rawN = new byte[NRAW];
        final int[] rawB = new int[NRAW];
        final byte[] lzN = new byte[NLZ];
        final int[] lzB = new int[NLZ];

        Prefixo(long[] raw, long[] lz) {
            int usados = 0; for (int i = 0; i < NLZ; i++) if (lz[i] > 0) usados++;
            long[] lzc = lz.clone();
            if (usados < 2) { if (lzc[0] == 0) lzc[0] = 1; if (usados + (lz[0] == 0 ? 1 : 0) < 2 && lzc[1] == 0) lzc[1] = 1; }
            int numraw = NRAW; while (numraw > 0 && raw[numraw - 1] == 0) numraw--;
            long[] l1 = new long[numraw + 1];
            for (int i = 0; i < numraw; i++) l1[i] = raw[i];
            for (int i = 0; i < NLZ; i++) l1[numraw] += lzc[i];
            byte[] l1n = new byte[numraw + 1];
            comprimentos(l1, numraw + 1, 14, l1n);
            int maxL2 = 15 - l1n[numraw];
            byte[] l2n = new byte[NLZ];
            comprimentos(lzc, NLZ, maxL2, l2n);
            for (int i = 0; i < numraw; i++) rawN[i] = l1n[i];
            for (int i = 0; i < NLZ; i++) lzN[i] = l2n[i] != 0 ? (byte) (l1n[numraw] + l2n[i]) : (byte) 0;
            byte[] todos = new byte[NRAW + NLZ];
            System.arraycopy(rawN, 0, todos, 0, NRAW); System.arraycopy(lzN, 0, todos, NRAW, NLZ);
            int[] b = new int[NRAW + NLZ];
            canonico(todos, b);
            System.arraycopy(b, 0, rawB, 0, NRAW); System.arraycopy(b, NRAW, lzB, 0, NLZ);
        }

        void escrever(Bits w) {
            long[] cnt = new long[18];
            cnt[17] = 3 + 2 * (NLZ - 1);
            for (int i = 0; i < NRAW; i++) cnt[rawN[i]]++;
            for (int i = 0; i < NLZ; i++) cnt[lzN[i]]++;
            byte[] cln = new byte[18];
            comprimentos(cnt, 18, 5, cln);
            w.write(2, 0);
            int[] ordem = {1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15};
            int[] clln = {2, 4, 3, 2, 2, 4};
            int[] cllb = {0, 7, 3, 2, 1, 15};
            int num = 18;
            while (cln[ordem[num - 1]] == 0) num--;
            for (int i = 0; i < num; i++) { int s = cln[ordem[i]]; w.write(clln[s], cllb[s]); }
            int[] clb = new int[18];
            canonico(cln, clb);
            for (int i = 0; i < NRAW; i++) w.write(cln[rawN[i]], clb[rawN[i]]);
            int numlz = NLZ; while (lzN[numlz - 1] == 0) numlz--;
            w.write(cln[17], clb[17]); w.write(3, 2);
            w.write(cln[17], clb[17]); w.write(3, 0);
            w.write(cln[17], clb[17]); w.write(3, 2);
            for (int i = 0; i < numlz; i++) w.write(cln[lzN[i]], clb[lzN[i]]);
        }
    }

    // ------------------------------------------------------------ resíduos
    static void canal(int[] p, int w, int h, long[] raw, long[] lz, Prefixo code, Bits cw) {
        int run = 0;
        for (int y = 0; y < h; y++) {
            int row = y * w, prev = (y - 1) * w;
            for (int x = 0; x < w; x++) {
                int px = p[row + x];
                int left = x > 0 ? p[row + x - 1] : (y > 0 ? p[prev] : 0);
                int top = y > 0 ? p[prev + x] : left;
                int tl = y > 0 ? (x > 0 ? p[prev + x - 1] : p[prev]) : left;
                int ac = left - tl, ab = left - top, bc = top - tl;
                int grad = ac + top;
                int pred = ((ac ^ bc) < 0) ? grad : (((ab ^ bc) < 0) ? top : left);
                int r = px - pred;
                long v = r >= 0 ? 2L * r : -2L * r - 1;
                if (v == 0) { run++; continue; }
                if (run > 0) { rle(run, raw, lz, code, cw); run = 0; }
                simbolo(v, raw, code, cw);
            }
        }
        if (run > 0) rle(run, raw, lz, code, cw);
    }

    static void simbolo(long v, long[] raw, Prefixo code, Bits cw) {
        long[] t = tokenRaw(v);
        int token = (int) t[0], nb = (int) t[1];
        if (cw == null) { raw[token]++; return; }
        cw.write(code.rawN[token] + nb, (long) code.rawB[token] | (t[2] << code.rawN[token]));
    }

    static void rle(int run, long[] raw, long[] lz, Prefixo code, Bits cw) {
        if (run <= MINLZ) {
            for (int i = 0; i < run; i++) simbolo(0, raw, code, cw);
            return;
        }
        long[] t = tokenLz(run - (MINLZ + 1));
        int token = (int) t[0], nb = (int) t[1];
        if (cw == null) { raw[0]++; lz[token]++; return; }
        simbolo(0, raw, code, cw);
        cw.write(code.lzN[token] + nb, (long) code.lzB[token] | (t[2] << code.lzN[token]));
    }

    // ------------------------------------------------------------ cabeçalhos
    static void tamanho(Bits o, int size) {
        int s = size - 1;
        if (s < (1 << 9)) { o.write(2, 0); o.write(9, s); }
        else if (s < (1 << 13)) { o.write(2, 1); o.write(13, s); }
        else if (s < (1 << 18)) { o.write(2, 2); o.write(18, s); }
        else { o.write(2, 3); o.write(30, s); }
    }

    static final int[] OFFSET_GRUPO = {0, 1024, 17408, 4211712};
    static final int[] BITS_TOC = {12, 16, 24, 32};
    static int balde(int tam) { int b = 0; while (b < 3 && tam >= OFFSET_GRUPO[b + 1]) b++; return b; }

    static void dcGlobal(Bits o, Prefixo[] code, int canais, boolean umGrupo) {
        o.write(1, 1);
        o.write(1, 1);
        o.write(1, 0);
        o.write(1, 1);
        o.write(2, 0);
        o.write(1, 1);
        o.write(4, 0);
        o.write(6, 0x23);
        o.write(2, 1); o.write(2, 3);
        o.write(2, 0); o.write(2, 1); o.write(2, 2); o.write(2, 3);
        o.write(1, 0);
        int[] simB = {0x0, 0x2, 0x1, 0x5, 0x3, 0x7};
        int[] simN = {2, 2, 3, 3, 4, 4};
        int[] arvore = {1, 2, 1, 4, 1, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0};
        for (int i = 0; i < arvore.length; i++) o.write(simN[arvore[i]], simB[arvore[i]]);

        o.write(1, 1);
        o.write(2, 0);
        o.write(4, 0xA);
        o.write(4, 4); o.write(3, 0); o.write(3, 0);

        o.write(1, 1);
        o.write(2, 3);
        o.write(3, 4); o.write(3, 3); o.write(3, 2); o.write(3, 1); o.write(3, 0);

        o.write(1, 1);
        o.write(4, 0);
        for (int i = 0; i < 4; i++) o.write(4, 0);
        o.write(5, 1);
        for (int i = 0; i < 4; i++) { o.write(1, 1); o.write(4, 8); o.write(8, 256); }
        o.write(2, 1); o.write(2, 0); o.write(1, 1);
        for (int i = 0; i < 4; i++) code[i].escrever(o);

        o.write(1, 1);
        o.write(1, 1);
        if (canais > 2) { o.write(2, 1); o.write(2, 0); o.write(5, 0); o.write(2, 0); }
        else o.write(2, 0);
        if (!umGrupo) o.padByte();
    }

    // ------------------------------------------------------------ API
    /** Codifica um Bitmap: 1 canal quando a imagem é cinza (R=G=B), 3 canais em cores. */
    static byte[] codificar(Bitmap b) {
        int w = b.getWidth(), h = b.getHeight();
        int[] px = new int[w];
        byte[] rgb = new byte[w * h * 3];
        boolean cinza = true;
        for (int y = 0; y < h; y++) {
            b.getPixels(px, 0, w, 0, y, w, 1);
            int o = y * w * 3;
            for (int x = 0; x < w; x++) {
                int p = px[x], r = (p >> 16) & 255, g = (p >> 8) & 255, bl = p & 255;
                if (r != g || g != bl) cinza = false;
                rgb[o + 3 * x] = (byte) r; rgb[o + 3 * x + 1] = (byte) g; rgb[o + 3 * x + 2] = (byte) bl;
            }
        }
        if (!cinza) return codificar(w, h, 3, rgb);
        byte[] g1 = new byte[w * h];
        for (int i = 0; i < g1.length; i++) g1[i] = rgb[3 * i];
        return codificar(w, h, 1, g1);
    }

    static byte[] codificar(int w, int h, int canais, byte[] dados) {
        int ngx = (w + GRUPO - 1) / GRUPO, ngy = (h + GRUPO - 1) / GRUPO;
        int ng = ngx * ngy;
        boolean umGrupo = ng == 1;
        int ndc = ((w + 2047) / 2048) * ((h + 2047) / 2048);

        long[][] raw = new long[canais][], lz = new long[canais][];
        for (int c = 0; c < canais; c++) { raw[c] = new long[NRAW]; lz[c] = new long[NLZ]; }
        int[][] plano = new int[canais][];
        for (int c = 0; c < canais; c++) plano[c] = new int[GRUPO * GRUPO];
        int[] gwh = new int[2];
        for (int g = 0; g < ng; g++) {
            ladrilho(dados, w, h, canais, g, ngx, plano, gwh);
            for (int c = 0; c < canais; c++) canal(plano[c], gwh[0], gwh[1], raw[c], lz[c], null, null);
        }
        Prefixo[] code = new Prefixo[4];
        for (int c = 0; c < canais; c++) code[c] = new Prefixo(raw[c], lz[c]);
        for (int c = canais; c < 4; c++) code[c] = code[0];

        Bits dc = new Bits();
        dcGlobal(dc, code, canais, umGrupo);

        List<byte[]> secoes = new ArrayList<byte[]>();
        if (umGrupo) {
            ladrilho(dados, w, h, canais, 0, ngx, plano, gwh);
            for (int c = 0; c < canais; c++) canal(plano[c], gwh[0], gwh[1], null, null, code[c], dc);
            secoes.add(dc.toBytes());
        } else {
            secoes.add(dc.toBytes());
            for (int i = 0; i < ndc + 1; i++) secoes.add(new byte[0]);
            for (int g = 0; g < ng; g++) {
                ladrilho(dados, w, h, canais, g, ngx, plano, gwh);
                Bits bw = new Bits();
                bw.write(1, 1); bw.write(1, 1); bw.write(2, 0);
                for (int c = 0; c < canais; c++) canal(plano[c], gwh[0], gwh[1], null, null, code[c], bw);
                secoes.add(bw.toBytes());
            }
        }

        Bits o = new Bits();
        o.write(16, 0x0AFF);
        o.write(1, 0);
        tamanho(o, h);
        o.write(3, 0);
        tamanho(o, w);
        o.write(1, 0);
        o.write(1, 0);
        o.write(1, 0);
        o.write(2, 0);
        o.write(1, 1);
        o.write(2, 0);
        o.write(1, 0);
        if (canais > 2) o.write(1, 1);
        else {
            o.write(1, 0); o.write(1, 0);
            o.write(2, 1);
            o.write(2, 1);
            o.write(1, 0);
            o.write(2, 2); o.write(4, 11);
            o.write(2, 1);
        }
        o.write(2, 0);
        o.write(1, 1);
        o.padByte();
        o.write(1, 0);
        o.write(2, 0);
        o.write(1, 1);
        o.write(2, 0);
        o.write(1, 0);
        o.write(2, 0);
        o.write(2, 1);
        o.write(2, 0);
        o.write(1, 0);
        o.write(2, 0);
        o.write(1, 1);
        o.write(2, 0);
        o.write(1, 0);
        o.write(1, 0);
        o.write(2, 0);
        o.write(2, 0);
        o.write(2, 0);
        o.write(1, 0);
        o.padByte();
        for (int i = 0; i < secoes.size(); i++) {
            int tam = secoes.get(i).length, bk = balde(tam);
            o.write(2, bk);
            o.write(BITS_TOC[bk] - 2, tam - OFFSET_GRUPO[bk]);
        }
        o.padByte();
        ByteArrayOutputStream saida = new ByteArrayOutputStream();
        byte[] cab = o.toBytes();
        saida.write(cab, 0, cab.length);
        for (int i = 0; i < secoes.size(); i++) saida.write(secoes.get(i), 0, secoes.get(i).length);
        return saida.toByteArray();
    }

    static void ladrilho(byte[] d, int w, int h, int canais, int g, int ngx, int[][] plano, int[] gwh) {
        int x0 = (g % ngx) * GRUPO, y0 = (g / ngx) * GRUPO;
        int gw = Math.min(GRUPO, w - x0), gh = Math.min(GRUPO, h - y0);
        gwh[0] = gw; gwh[1] = gh;
        for (int y = 0; y < gh; y++) {
            int src = ((y0 + y) * w + x0) * canais, dst = y * gw;
            if (canais == 1) {
                for (int x = 0; x < gw; x++) plano[0][dst + x] = d[src + x] & 255;
            } else {
                for (int x = 0; x < gw; x++) {
                    int r = d[src + 3 * x] & 255, gr = d[src + 3 * x + 1] & 255, b = d[src + 3 * x + 2] & 255;
                    int co = r - b;
                    int tmp = b + (co >> 1);
                    int cg = gr - tmp;
                    int yy = tmp + (cg >> 1);
                    plano[0][dst + x] = yy; plano[1][dst + x] = co; plano[2][dst + x] = cg;
                }
            }
        }
    }
}
