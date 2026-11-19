// Codificador JPEG XL sem perdas (modo modular, preditor gradiente, códigos de prefixo e repetição de zeros).
// Escrito só com o .NET, sem DLL. A imagem sai idêntica à original, em 1 canal (cinza) ou 3 canais (RGB, com YCoCg).
// Baseado no formato descrito pela especificação do JPEG XL e na ideia do "fast lossless" do libjxl.
using System;
using System.Collections.Generic;
using System.IO;

namespace Digitalizar
{
    static class JpegXl
    {
        const int GRUPO = 256;
        const int NRAW = 19;          // símbolos "crus" (tokens 0..18)
        const int NLZ = 33;           // símbolos de repetição (224 + token)
        const int MINLZ = 7;          // comprimento mínimo de uma repetição

        // ------------------------------------------------------------ escrita de bits (LSB primeiro)
        sealed class Bits
        {
            readonly MemoryStream ms = new MemoryStream();
            ulong buf;
            int n;
            public void Write(int count, ulong bits)
            {
                if (count == 0) return;
                buf |= (bits & (count == 64 ? ulong.MaxValue : ((1UL << count) - 1))) << n;
                n += count;
                while (n >= 8) { ms.WriteByte((byte)(buf & 0xFF)); buf >>= 8; n -= 8; }
            }
            public void Write(int count, int bits) { Write(count, (ulong)(uint)bits); }
            public void PadByte() { if (n > 0) Write(8 - n, 0UL); }
            public long BitLength { get { return ms.Length * 8 + n; } }
            public void Append(Bits o)       // anexa mantendo os bits contínuos
            {
                byte[] d = o.ms.ToArray();
                for (int i = 0; i < d.Length; i++) Write(8, (ulong)d[i]);
                if (o.n > 0) Write(o.n, o.buf);
            }
            public byte[] ToBytes() { PadByte(); return ms.ToArray(); }
        }

        // ------------------------------------------------------------ códigos de prefixo
        static int FloorLog2(uint v) { int r = 0; while ((v >>= 1) != 0) r++; return r; }

        static void TokenRaw(uint v, out int token, out int nbits, out uint bits)
        {
            if (v == 0) { token = 0; nbits = 0; bits = 0; return; }
            int n = FloorLog2(v);
            token = n + 1; nbits = n; bits = v - (1u << n);
        }

        static void TokenLz(uint v, out int token, out int nbits, out uint bits)
        {
            if (v < 16) { token = (int)v; nbits = 0; bits = 0; return; }
            int n = FloorLog2(v);
            token = 16 + n - 4; nbits = n; bits = v - (1u << n);
        }

        // comprimentos de código ótimos com limite de tamanho (programação dinâmica)
        static void CodeLengths(long[] freqs, int n, int maxLen, byte[] nbits)
        {
            var idx = new List<int>();
            for (int i = 0; i < n; i++) if (freqs[i] > 0) idx.Add(i);
            for (int i = 0; i < n; i++) nbits[i] = 0;
            int ni = idx.Count;
            if (ni == 0) return;
            if (ni == 1) { nbits[idx[0]] = 1; return; }
            int precision = maxLen;
            int W = (1 << precision) + 1;
            long inf = long.MaxValue / 4;
            var d = new long[W * (ni + 1)];
            for (int i = 0; i < d.Length; i++) d[i] = inf;
            d[0] = 0;
            for (int s = 0; s < ni; s++)
            {
                long f = freqs[idx[s]];
                for (int bits = 1; bits <= maxLen; bits++)
                {
                    int delta = 1 << (precision - bits);
                    for (int off = 0; off + delta <= (1 << precision); off++)
                    {
                        long a = d[s * W + off];
                        if (a >= inf) continue;
                        long c = a + f * bits;
                        if (c < d[(s + 1) * W + off + delta]) d[(s + 1) * W + off + delta] = c;
                    }
                }
            }
            int o = 1 << precision;
            for (int s = ni - 1; s >= 0; s--)
            {
                long f = freqs[idx[s]];
                for (int bits = 1; bits <= maxLen; bits++)
                {
                    int delta = 1 << (precision - bits);
                    if (delta <= o && d[s * W + o - delta] < inf && d[(s + 1) * W + o] == d[s * W + o - delta] + f * bits)
                    {
                        o -= delta; nbits[idx[s]] = (byte)bits; break;
                    }
                }
            }
        }

        static int Rev(int bits, int nbits)
        {
            int r = 0;
            for (int i = 0; i < nbits; i++) { r = (r << 1) | (bits & 1); bits >>= 1; }
            return r;
        }

        // códigos canônicos para uma lista de comprimentos (já invertidos para a escrita LSB primeiro)
        static void Canonical(byte[] lens, int[] outBits)
        {
            int[] count = new int[17];
            for (int i = 0; i < lens.Length; i++) if (lens[i] > 0) count[lens[i]]++;
            int[] next = new int[17];
            int code = 0;
            for (int l = 1; l <= 16; l++) { code = (code + count[l - 1]) << 1; next[l] = code; }
            for (int i = 0; i < lens.Length; i++)
                if (lens[i] > 0) outBits[i] = Rev(next[lens[i]]++, lens[i]);
        }

        sealed class Prefixo
        {
            public byte[] rawN = new byte[NRAW];   public int[] rawB = new int[NRAW];
            public byte[] lzN = new byte[NLZ];     public int[] lzB = new int[NLZ];

            public Prefixo(long[] raw, long[] lz)
            {
                // fica no mínimo com 2 símbolos de repetição (o código precisa ser completo)
                int usados = 0; for (int i = 0; i < NLZ; i++) if (lz[i] > 0) usados++;
                long[] lzc = (long[])lz.Clone();
                if (usados < 2) { if (lzc[0] == 0) lzc[0] = 1; if (usados + (lz[0] == 0 ? 1 : 0) < 2 && lzc[1] == 0) lzc[1] = 1; }
                int numraw = NRAW; while (numraw > 0 && raw[numraw - 1] == 0) numraw--;
                long[] l1 = new long[numraw + 1];
                for (int i = 0; i < numraw; i++) l1[i] = raw[i];
                for (int i = 0; i < NLZ; i++) l1[numraw] += lzc[i];
                byte[] l1n = new byte[numraw + 1];
                CodeLengths(l1, numraw + 1, 14, l1n);
                int maxL2 = 15 - l1n[numraw];
                byte[] l2n = new byte[NLZ];
                CodeLengths(lzc, NLZ, maxL2, l2n);
                for (int i = 0; i < numraw; i++) rawN[i] = l1n[i];
                for (int i = 0; i < NLZ; i++) lzN[i] = l2n[i] != 0 ? (byte)(l1n[numraw] + l2n[i]) : (byte)0;
                byte[] todos = new byte[NRAW + NLZ];
                Array.Copy(rawN, 0, todos, 0, NRAW); Array.Copy(lzN, 0, todos, NRAW, NLZ);
                int[] b = new int[NRAW + NLZ];
                Canonical(todos, b);
                Array.Copy(b, 0, rawB, 0, NRAW); Array.Copy(b, NRAW, lzB, 0, NLZ);
            }

            public void Escrever(Bits w)
            {
                long[] cnt = new long[18];
                cnt[17] = 3 + 2 * (NLZ - 1);
                for (int i = 0; i < NRAW; i++) cnt[rawN[i]]++;
                for (int i = 0; i < NLZ; i++) cnt[lzN[i]]++;
                byte[] cln = new byte[18];
                CodeLengths(cnt, 18, 5, cln);
                w.Write(2, 0);   // HSKIP = 0
                int[] ordem = { 1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                int[] clln = { 2, 4, 3, 2, 2, 4 };
                int[] cllb = { 0, 7, 3, 2, 1, 15 };
                int num = 18;
                while (cln[ordem[num - 1]] == 0) num--;
                for (int i = 0; i < num; i++) { int s = cln[ordem[i]]; w.Write(clln[s], cllb[s]); }
                int[] clb = new int[18];
                Canonical(cln, clb);
                for (int i = 0; i < NRAW; i++) w.Write(cln[rawN[i]], clb[rawN[i]]);
                int numlz = NLZ; while (lzN[numlz - 1] == 0) numlz--;
                // 205 zeros até o símbolo 224: três repetições (5, 27, 205)
                w.Write(cln[17], clb[17]); w.Write(3, 2);
                w.Write(cln[17], clb[17]); w.Write(3, 0);
                w.Write(cln[17], clb[17]); w.Write(3, 2);
                for (int i = 0; i < numlz; i++) w.Write(cln[lzN[i]], clb[lzN[i]]);
            }
        }

        // ------------------------------------------------------------ residuos de um canal de um grupo
        // Percorre os resíduos do preditor gradiente de um plano (w x h). Se 'cw' for nulo, só conta os símbolos.
        static void Canal(int[] p, int w, int h, long[] raw, long[] lz, Prefixo code, Bits cw)
        {
            int run = 0;
            for (int y = 0; y < h; y++)
            {
                int row = y * w, prev = (y - 1) * w;
                for (int x = 0; x < w; x++)
                {
                    int px = p[row + x];
                    int left = x > 0 ? p[row + x - 1] : (y > 0 ? p[prev] : 0);
                    int top = y > 0 ? p[prev + x] : left;
                    int tl = y > 0 ? (x > 0 ? p[prev + x - 1] : p[prev]) : left;
                    int ac = left - tl, ab = left - top, bc = top - tl;
                    int grad = ac + top;
                    int pred = ((ac ^ bc) < 0) ? grad : (((ab ^ bc) < 0) ? top : left);
                    int r = px - pred;
                    uint v = r >= 0 ? (uint)(2 * r) : (uint)(-2 * r - 1);
                    if (v == 0) { run++; continue; }
                    if (run > 0) { Rle(run, raw, lz, code, cw); run = 0; }
                    Simbolo(v, raw, code, cw);
                }
            }
            if (run > 0) Rle(run, raw, lz, code, cw);
        }

        static void Simbolo(uint v, long[] raw, Prefixo code, Bits cw)
        {
            int token, nb; uint bits;
            TokenRaw(v, out token, out nb, out bits);
            if (cw == null) { raw[token]++; return; }
            cw.Write(code.rawN[token] + nb, (ulong)code.rawB[token] | ((ulong)bits << code.rawN[token]));
        }

        // uma sequência de zeros: se for longa, um zero explícito + repetição com distância 1
        static void Rle(int run, long[] raw, long[] lz, Prefixo code, Bits cw)
        {
            if (run <= MINLZ)
            {
                for (int i = 0; i < run; i++) Simbolo(0, raw, code, cw);
                return;
            }
            int token, nb; uint bits;
            TokenLz((uint)(run - (MINLZ + 1)), out token, out nb, out bits);
            if (cw == null) { raw[0]++; lz[token]++; return; }
            Simbolo(0, raw, code, cw);
            cw.Write(code.lzN[token] + nb, (ulong)code.lzB[token] | ((ulong)bits << code.lzN[token]));
        }

        // ------------------------------------------------------------ cabeçalhos
        static void Tamanho(Bits o, int size)
        {
            int s = size - 1;
            if (s < (1 << 9)) { o.Write(2, 0); o.Write(9, s); }
            else if (s < (1 << 13)) { o.Write(2, 1); o.Write(13, s); }
            else if (s < (1 << 18)) { o.Write(2, 2); o.Write(18, s); }
            else { o.Write(2, 3); o.Write(30, s); }
        }

        static readonly int[] OffsetGrupo = { 0, 1024, 17408, 4211712 };
        static readonly int[] BitsToc = { 12, 16, 24, 32 };
        static int Balde(int tam) { int b = 0; while (b < 3 && tam >= OffsetGrupo[b + 1]) b++; return b; }

        static void DcGlobal(Bits o, Prefixo[] code, int canais, bool umGrupo)
        {
            o.Write(1, 1);   // fatores DC padrão
            o.Write(1, 1);   // árvore e histogramas globais
            o.Write(1, 0);   // sem lz77 na árvore
            o.Write(1, 1);   // mapa de contextos simples da árvore
            o.Write(2, 0);
            o.Write(1, 1);   // códigos de prefixo para a árvore
            o.Write(4, 0);
            o.Write(6, 0x23);
            o.Write(2, 1); o.Write(2, 3);
            o.Write(2, 0); o.Write(2, 1); o.Write(2, 2); o.Write(2, 3);
            o.Write(1, 0);
            int[] simB = { 0x0, 0x2, 0x1, 0x5, 0x3, 0x7 };
            int[] simN = { 2, 2, 3, 3, 4, 4 };
            int[] arvore = { 1, 2, 1, 4, 1, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0, 0, 5, 0, 0, 0 };
            for (int i = 0; i < arvore.Length; i++) o.Write(simN[arvore[i]], simB[arvore[i]]);

            o.Write(1, 1);   // lz77 ligado no fluxo principal
            o.Write(2, 0);   // deslocamento 224
            o.Write(4, 0xA); // comprimento mínimo 7
            o.Write(4, 4); o.Write(3, 0); o.Write(3, 0);

            o.Write(1, 1);   // mapa de contextos simples
            o.Write(2, 3);
            o.Write(3, 4); o.Write(3, 3); o.Write(3, 2); o.Write(3, 1); o.Write(3, 0);

            o.Write(1, 1);   // códigos de prefixo
            o.Write(4, 0);
            for (int i = 0; i < 4; i++) o.Write(4, 0);
            o.Write(5, 1);   // alfabeto de distâncias = 2
            for (int i = 0; i < 4; i++) { o.Write(1, 1); o.Write(4, 8); o.Write(8, 256); }
            o.Write(2, 1); o.Write(2, 0); o.Write(1, 1);   // distância: um só símbolo
            for (int i = 0; i < 4; i++) code[i].Escrever(o);

            o.Write(1, 1);   // árvore global
            o.Write(1, 1);   // wp padrão
            if (canais > 2) { o.Write(2, 1); o.Write(2, 0); o.Write(5, 0); o.Write(2, 0); }   // RCT YCoCg
            else o.Write(2, 0);
            if (!umGrupo) o.PadByte();
        }

        // ------------------------------------------------------------ API
        /// dados: largura*altura*canais bytes (canais = 1 cinza ou 3 RGB), linha a linha, sem preenchimento
        public static byte[] Codificar(int w, int h, int canais, byte[] dados)
        {
            int ngx = (w + GRUPO - 1) / GRUPO, ngy = (h + GRUPO - 1) / GRUPO;
            int ng = ngx * ngy;
            bool umGrupo = ng == 1;
            int ndc = ((w + 2047) / 2048) * ((h + 2047) / 2048);

            // 1ª passada: histogramas por canal
            long[][] raw = new long[canais][], lz = new long[canais][];
            for (int c = 0; c < canais; c++) { raw[c] = new long[NRAW]; lz[c] = new long[NLZ]; }
            int[][] plano = new int[canais][];
            for (int c = 0; c < canais; c++) plano[c] = new int[GRUPO * GRUPO];
            for (int g = 0; g < ng; g++)
            {
                int gw, gh;
                Ladrilho(dados, w, h, canais, g, ngx, plano, out gw, out gh);
                for (int c = 0; c < canais; c++) Canal(plano[c], gw, gh, raw[c], lz[c], null, null);
            }
            Prefixo[] code = new Prefixo[4];
            for (int c = 0; c < canais; c++) code[c] = new Prefixo(raw[c], lz[c]);
            for (int c = canais; c < 4; c++) code[c] = code[0];

            // seção global
            Bits dc = new Bits();
            DcGlobal(dc, code, canais, umGrupo);

            // 2ª passada: dados dos grupos
            var secoes = new List<byte[]>();
            if (umGrupo)
            {
                int gw, gh;
                Ladrilho(dados, w, h, canais, 0, ngx, plano, out gw, out gh);
                for (int c = 0; c < canais; c++) Canal(plano[c], gw, gh, null, null, code[c], dc);
                secoes.Add(dc.ToBytes());
            }
            else
            {
                secoes.Add(dc.ToBytes());
                for (int i = 0; i < ndc + 1; i++) secoes.Add(new byte[0]);   // grupos DC e AC global: vazios
                for (int g = 0; g < ng; g++)
                {
                    int gw, gh;
                    Ladrilho(dados, w, h, canais, g, ngx, plano, out gw, out gh);
                    Bits bw = new Bits();
                    bw.Write(1, 1); bw.Write(1, 1); bw.Write(2, 0);
                    for (int c = 0; c < canais; c++) Canal(plano[c], gw, gh, null, null, code[c], bw);
                    secoes.Add(bw.ToBytes());
                }
            }

            // cabeçalho da imagem + do quadro + TOC
            Bits o = new Bits();
            o.Write(16, 0x0AFF);
            o.Write(1, 0);
            Tamanho(o, h);
            o.Write(3, 0);
            Tamanho(o, w);
            o.Write(1, 0);   // all_default = falso
            o.Write(1, 0);   // extra_fields
            o.Write(1, 0);   // ponto flutuante
            o.Write(2, 0);   // 8 bits por amostra
            o.Write(1, 1);   // buffer de 16 bits basta
            o.Write(2, 0);   // sem canais extras
            o.Write(1, 0);   // não é XYB
            if (canais > 2) o.Write(1, 1);   // sRGB padrão
            else
            {
                o.Write(1, 0); o.Write(1, 0);
                o.Write(2, 1);      // tons de cinza
                o.Write(2, 1);      // D65
                o.Write(1, 0);      // sem gama
                o.Write(2, 2); o.Write(4, 11);   // função de transferência sRGB
                o.Write(2, 1);      // intenção relativa
            }
            o.Write(2, 0);   // sem extensões
            o.Write(1, 1);   // transformação padrão
            o.PadByte();
            // quadro
            o.Write(1, 0);   // all_default
            o.Write(2, 0);   // quadro normal
            o.Write(1, 1);   // modular
            o.Write(2, 0);   // flags padrão
            o.Write(1, 0);   // não é YCbCr
            o.Write(2, 0);   // sem upsampling
            o.Write(2, 1);   // grupo de 256
            o.Write(2, 0);   // uma passada
            o.Write(1, 0);   // sem tamanho ou origem próprios
            o.Write(2, 0);   // mistura: substituir
            o.Write(1, 1);   // último quadro
            o.Write(2, 0);   // sem nome
            o.Write(1, 0);   // filtro de laço não é o padrão
            o.Write(1, 0);   // sem gaborish
            o.Write(2, 0);   // 0 iterações EPF
            o.Write(2, 0);   // sem extensões do LF
            o.Write(2, 0);   // sem extensões do quadro
            o.Write(1, 0);   // sem permutação do TOC
            o.PadByte();
            for (int i = 0; i < secoes.Count; i++)
            {
                int tam = secoes[i].Length, b = Balde(tam);
                o.Write(2, b);
                o.Write(BitsToc[b] - 2, tam - OffsetGrupo[b]);
            }
            o.PadByte();
            var saida = new MemoryStream();
            byte[] cab = o.ToBytes();
            saida.Write(cab, 0, cab.Length);
            for (int i = 0; i < secoes.Count; i++) saida.Write(secoes[i], 0, secoes[i].Length);
            return saida.ToArray();
        }

        // copia um ladrilho de 256x256 para planos inteiros (em RGB aplica YCoCg)
        static void Ladrilho(byte[] d, int w, int h, int canais, int g, int ngx, int[][] plano, out int gw, out int gh)
        {
            int x0 = (g % ngx) * GRUPO, y0 = (g / ngx) * GRUPO;
            gw = Math.Min(GRUPO, w - x0); gh = Math.Min(GRUPO, h - y0);
            for (int y = 0; y < gh; y++)
            {
                int src = ((y0 + y) * w + x0) * canais, dst = y * gw;
                if (canais == 1)
                {
                    for (int x = 0; x < gw; x++) plano[0][dst + x] = d[src + x];
                }
                else
                {
                    for (int x = 0; x < gw; x++)
                    {
                        int r = d[src + 3 * x], gr = d[src + 3 * x + 1], b = d[src + 3 * x + 2];
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
}
