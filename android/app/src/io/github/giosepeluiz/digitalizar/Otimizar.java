// Otimização automática da página, toda no aparelho (sem internet e sem IA):
//   1) tira as bordas pretas da tampa do scanner e endireita a página (retângulo de menor área em volta dela);
//   2) sem borda detectada, corrige a inclinação do texto (perfil de projeção);
//   3) acerta a orientação (0, 90, 180 ou 270°) pelas linhas de texto; se não houver certeza, não gira.
// Mesma lógica do PC (src/Otimizar.cs): as duas versões tomam as mesmas decisões.
package io.github.giosepeluiz.digitalizar;

import android.graphics.Bitmap;

import java.util.Arrays;

final class Otimizar {
    private Otimizar() {}

    static final int LADO = 1000;                 // maior lado da imagem reduzida usada na análise
    static final double LIMITE_ASSIM = 0.04;      // assimetria mínima para confiar na orientação
    static final long MAX_PIXELS = 30000000L;     // acima disso não otimiza (memória do celular)

    /** Resultado do núcleo: pixels RGB (3 bytes por ponto) e tamanho. */
    static final class Img {
        byte[] rgb; int w, h; String log = "";
    }

    // ------------------------------------------------------------ bitmap
    /** Devolve um Bitmap novo (recortado / endireitado / girado) ou null se nada precisou mudar. */
    static Bitmap processar(Bitmap origem) {
        int w = origem.getWidth(), h = origem.getHeight();
        if ((long) w * h > MAX_PIXELS || w < 50 || h < 50) return null;
        byte[] rgb = ler(origem);
        Img r = executar(rgb, w, h);
        if (r == null) return null;
        return escrever(r.rgb, r.w, r.h);
    }

    static byte[] ler(Bitmap b) {
        int w = b.getWidth(), h = b.getHeight();
        byte[] res = new byte[w * h * 3];
        int[] linha = new int[w];
        for (int y = 0; y < h; y++) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            int o = y * w * 3;
            for (int x = 0; x < w; x++, o += 3) {
                int p = linha[x];
                res[o] = (byte) (p >> 16); res[o + 1] = (byte) (p >> 8); res[o + 2] = (byte) p;
            }
        }
        return res;
    }

    static Bitmap escrever(byte[] rgb, int w, int h) {
        Bitmap b = Bitmap.createBitmap(w, h, Bitmap.Config.ARGB_8888);
        int[] linha = new int[w];
        for (int y = 0; y < h; y++) {
            int o = y * w * 3;
            for (int x = 0; x < w; x++, o += 3)
                linha[x] = 0xFF000000 | ((rgb[o] & 255) << 16) | ((rgb[o + 1] & 255) << 8) | (rgb[o + 2] & 255);
            b.setPixels(linha, 0, w, 0, y, w, 1);
        }
        return b;
    }

    // ------------------------------------------------------------ núcleo (só arrays)
    /** rgb: w*h*3 bytes. Devolve a imagem otimizada ou null se não mudou nada. */
    static Img executar(byte[] rgb, int w, int h) {
        StringBuilder log = new StringBuilder();
        int f = Math.max(1, (Math.max(w, h) + LADO - 1) / LADO);
        int sw = w / f, sh = h / f;
        byte[] g = reduzir(rgb, w, f, sw, sh);

        byte[] cur = rgb; int cw = w, ch = h;
        boolean mudou = false;

        double[] pg = new double[5];   // cx, cy, rw, rh, ang
        if (acharPagina(g, sw, sh, pg)) {
            double fx = (pg[0] + 0.5) * f - 0.5, fy = (pg[1] + 0.5) * f - 0.5;
            int nw = (int) Math.round(pg[2] * f), nh = (int) Math.round(pg[3] * f);
            if (nw >= 16 && nh >= 16) {
                cur = amostrar(rgb, w, h, fx, fy, pg[4], nw, nh);
                cw = nw; ch = nh; mudou = true;
                log.append("recorte ").append(nw).append("x").append(nh).append("; ");
            }
        } else {
            // sem moldura de scanner reconhecível: ainda assim tira faixas pretas coladas nas bordas
            int[] bd = aparaBordas(g, sw, sh);
            if (bd != null) {
                int x0 = bd[0] * f, y0 = bd[1] * f;
                int x1 = bd[2] > 0 ? (sw - bd[2]) * f : w, y1 = bd[3] > 0 ? (sh - bd[3]) * f : h;
                int nw = x1 - x0, nh = y1 - y0;
                if (nw >= 16 && nh >= 16) {
                    cur = amostrar(rgb, w, h, (x0 + x1 - 1) / 2.0, (y0 + y1 - 1) / 2.0, 0, nw, nh);
                    cw = nw; ch = nh; mudou = true;
                    log.append("bordas; ");
                }
            }
            int fa = Math.max(1, (Math.max(cw, ch) + LADO - 1) / LADO);
            int swa = sw, sha = sh;
            byte[] ga = g;
            if (mudou) { swa = cw / fa; sha = ch / fa; ga = reduzir(cur, cw, fa, swa, sha); }
            double d = anguloTexto(ga, swa, sha);
            if (Math.abs(d) >= 0.2) {
                cur = amostrar(cur, cw, ch, (cw - 1) / 2.0, (ch - 1) / 2.0, d * Math.PI / 180, cw, ch);
                mudou = true;
                log.append("texto; ");
            }
        }

        int sw2 = sw, sh2 = sh;
        byte[] g2 = g;
        if (mudou) {
            int f2 = Math.max(1, (Math.max(cw, ch) + LADO - 1) / LADO);
            sw2 = cw / f2; sh2 = ch / f2;
            g2 = reduzir(cur, cw, f2, sw2, sh2);
        }
        int giro = orientacao(g2, sw2, sh2);
        if (giro != 0) {
            cur = girar(cur, cw, ch, giro);
            if (giro != 180) { int t = cw; cw = ch; ch = t; }
            mudou = true;
            log.append("giro ").append(giro).append("; ");
        }
        if (!mudou) return null;
        Img r = new Img();
        r.rgb = cur; r.w = cw; r.h = ch; r.log = log.toString();
        return r;
    }

    /** Reduz por média de blocos f×f para tons de cinza. */
    static byte[] reduzir(byte[] rgb, int w, int f, int sw, int sh) {
        byte[] g = new byte[sw * sh];
        int area = f * f;
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++) {
                int soma = 0;
                for (int j = 0; j < f; j++) {
                    int o = ((y * f + j) * w + x * f) * 3;
                    for (int i = 0; i < f; i++, o += 3)
                        soma += ((rgb[o] & 255) * 77 + (rgb[o + 1] & 255) * 150 + (rgb[o + 2] & 255) * 29) >> 8;
                }
                g[y * sw + x] = (byte) (soma / area);
            }
        return g;
    }

    /** Faixas pretas coladas nas bordas. Devolve {esq, topo, dir, base} em px reduzidos, ou null. */
    static int[] aparaBordas(byte[] g, int sw, int sh) {
        int n = sw * sh;
        int T = Math.min(150, Math.max(40, otsu(g, n)));
        int[] r = { lado(g, sw, sh, T, 0), lado(g, sw, sh, T, 1), lado(g, sw, sh, T, 2), lado(g, sw, sh, T, 3) };
        if (r[0] + r[1] + r[2] + r[3] == 0) return null;
        if (r[0] + r[2] > sw * 0.4 || r[1] + r[3] > sh * 0.4) return null;
        return r;
    }

    /** lado: 0 esquerda, 1 topo, 2 direita, 3 base. Avança enquanto a linha for majoritariamente escura (tolera 3 claras) e soma 2 px. */
    static int lado(byte[] g, int sw, int sh, int T, int lado) {
        boolean vert = lado == 0 || lado == 2;
        int comp = vert ? sh : sw, prof = (int) ((vert ? sw : sh) * 0.15);
        int ultimo = -1, falhas = 0;
        for (int k = 0; k < prof; k++) {
            int dark = 0;
            for (int i = 0; i < comp; i++) {
                int x, y;
                if (lado == 0) { x = k; y = i; } else if (lado == 2) { x = sw - 1 - k; y = i; }
                else if (lado == 1) { x = i; y = k; } else { x = i; y = sh - 1 - k; }
                if ((g[y * sw + x] & 255) < T) dark++;
            }
            if (dark >= comp * 0.4) { ultimo = k; falhas = 0; }
            else if (++falhas > 3) break;
        }
        return ultimo < 0 ? 0 : ultimo + 1 + 2;
    }

    static int otsu(byte[] g, int n) {
        long[] hist = new long[256];
        for (int i = 0; i < n; i++) hist[g[i] & 255]++;
        double total = n, soma = 0;
        for (int i = 0; i < 256; i++) soma += i * (double) hist[i];
        double somaB = 0, wB = 0, melhor = -1; int limiar = 128;
        for (int t = 0; t < 256; t++) {
            wB += hist[t];
            if (wB == 0) continue;
            double wF = total - wB;
            if (wF == 0) break;
            somaB += t * (double) hist[t];
            double mB = somaB / wB, mF = (soma - somaB) / wF;
            double v = wB * wF * (mB - mF) * (mB - mF);
            if (v > melhor) { melhor = v; limiar = t; }
        }
        return limiar;
    }

    // ------------------------------------------------------------ 1) página e bordas pretas
    /** out: {cx, cy, rw, rh, ang(rad)} em px da imagem reduzida. */
    static boolean acharPagina(byte[] g, int sw, int sh, double[] out) {
        int n = sw * sh;
        int T = Math.min(140, Math.max(40, otsu(g, n)));

        int anel = 0, total = 0;
        for (int x = 0; x < sw; x++) { total += 2; if ((g[x] & 255) < T) anel++; if ((g[(sh - 1) * sw + x] & 255) < T) anel++; }
        for (int y = 1; y < sh - 1; y++) { total += 2; if ((g[y * sw] & 255) < T) anel++; if ((g[y * sw + sw - 1] & 255) < T) anel++; }
        if (anel < total * 0.03) return false;

        boolean[] bg = new boolean[n];
        int[] pilha = new int[n];
        int topo = 0;
        for (int x = 0; x < sw; x++) {
            int a = x, b = (sh - 1) * sw + x;
            if ((g[a] & 255) < T && !bg[a]) { bg[a] = true; pilha[topo++] = a; }
            if ((g[b] & 255) < T && !bg[b]) { bg[b] = true; pilha[topo++] = b; }
        }
        for (int y = 0; y < sh; y++) {
            int a = y * sw, b = y * sw + sw - 1;
            if ((g[a] & 255) < T && !bg[a]) { bg[a] = true; pilha[topo++] = a; }
            if ((g[b] & 255) < T && !bg[b]) { bg[b] = true; pilha[topo++] = b; }
        }
        while (topo > 0) {
            int p = pilha[--topo];
            int x = p % sw, y = p / sw;
            if (x > 0 && !bg[p - 1] && (g[p - 1] & 255) < T) { bg[p - 1] = true; pilha[topo++] = p - 1; }
            if (x < sw - 1 && !bg[p + 1] && (g[p + 1] & 255) < T) { bg[p + 1] = true; pilha[topo++] = p + 1; }
            if (y > 0 && !bg[p - sw] && (g[p - sw] & 255) < T) { bg[p - sw] = true; pilha[topo++] = p - sw; }
            if (y < sh - 1 && !bg[p + sw] && (g[p + sw] & 255) < T) { bg[p + sw] = true; pilha[topo++] = p + sw; }
        }
        int nbg = 0;
        for (int i = 0; i < n; i++) if (bg[i]) nbg++;
        if (nbg < n * 0.0005 || nbg > n * 0.85) return false;

        byte[] comp = new byte[n];
        int melhorArea = 0, melhorSemente = -1;
        boolean[] visto = new boolean[n];
        for (int s = 0; s < n; s++) {
            if (bg[s] || visto[s]) continue;
            int area = 0; topo = 0; pilha[topo++] = s; visto[s] = true;
            while (topo > 0) {
                int p = pilha[--topo]; area++;
                int x = p % sw, y = p / sw;
                if (x > 0 && !bg[p - 1] && !visto[p - 1]) { visto[p - 1] = true; pilha[topo++] = p - 1; }
                if (x < sw - 1 && !bg[p + 1] && !visto[p + 1]) { visto[p + 1] = true; pilha[topo++] = p + 1; }
                if (y > 0 && !bg[p - sw] && !visto[p - sw]) { visto[p - sw] = true; pilha[topo++] = p - sw; }
                if (y < sh - 1 && !bg[p + sw] && !visto[p + sw]) { visto[p + sw] = true; pilha[topo++] = p + sw; }
            }
            if (area > melhorArea) { melhorArea = area; melhorSemente = s; }
        }
        if (melhorSemente < 0 || melhorArea < n * 0.10) return false;
        topo = 0; pilha[topo++] = melhorSemente; comp[melhorSemente] = 1;
        while (topo > 0) {
            int p = pilha[--topo];
            int x = p % sw, y = p / sw;
            if (x > 0 && !bg[p - 1] && comp[p - 1] == 0) { comp[p - 1] = 1; pilha[topo++] = p - 1; }
            if (x < sw - 1 && !bg[p + 1] && comp[p + 1] == 0) { comp[p + 1] = 1; pilha[topo++] = p + 1; }
            if (y > 0 && !bg[p - sw] && comp[p - sw] == 0) { comp[p - sw] = 1; pilha[topo++] = p - sw; }
            if (y < sh - 1 && !bg[p + sw] && comp[p + sw] == 0) { comp[p + sw] = 1; pilha[topo++] = p + sw; }
        }

        // pontos do contorno
        int m = 0;
        long[] chaves = new long[n];
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++) {
                int p = y * sw + x;
                if (comp[p] == 0) continue;
                boolean borda = x == 0 || y == 0 || x == sw - 1 || y == sh - 1 ||
                        comp[p - 1] == 0 || comp[p + 1] == 0 || comp[p - sw] == 0 || comp[p + sw] == 0;
                if (borda) chaves[m++] = (long) x * 100000L + y;
            }
        if (m < 8) return false;
        long[] ord = Arrays.copyOf(chaves, m);
        Arrays.sort(ord);
        double[] px = new double[m], py = new double[m];
        for (int i = 0; i < m; i++) { px[i] = ord[i] / 100000L; py[i] = ord[i] % 100000L; }
        double[][] hull = casco(px, py, m);
        double[] ex = hull[0], ey = hull[1];
        int nh = (int) hull[2][0];
        if (nh < 3) return false;

        double melhorA = Double.MAX_VALUE, melhorT = 0;
        for (double t = -45; t < 45; t += 0.25) {
            double a = areaRect(ex, ey, nh, t * Math.PI / 180);
            if (a < melhorA - 1e-9) { melhorA = a; melhorT = t; }
        }
        double t0 = melhorT;
        for (double t = t0 - 0.25; t <= t0 + 0.25 + 1e-9; t += 0.02) {
            double a = areaRect(ex, ey, nh, t * Math.PI / 180);
            if (a < melhorA - 1e-9) { melhorA = a; melhorT = t; }
        }
        double ang = melhorT * Math.PI / 180;
        double c = Math.cos(ang), s2 = Math.sin(ang);
        double minU = Double.MAX_VALUE, maxU = -Double.MAX_VALUE, minV = Double.MAX_VALUE, maxV = -Double.MAX_VALUE;
        for (int i = 0; i < nh; i++) {
            double u = ex[i] * c + ey[i] * s2, v = -ex[i] * s2 + ey[i] * c;
            if (u < minU) minU = u; if (u > maxU) maxU = u;
            if (v < minV) minV = v; if (v > maxV) maxV = v;
        }
        double folga = 0.5 - 1.5;
        minU -= folga; maxU += folga; minV -= folga; maxV += folga;
        double rw = maxU - minU, rh = maxV - minV;
        if (rw < sw * 0.2 || rh < sh * 0.2) return false;

        double areaCasco = 0;
        for (int i = 0; i < nh; i++) { int j = (i + 1) % nh; areaCasco += ex[i] * ey[j] - ex[j] * ey[i]; }
        areaCasco = Math.abs(areaCasco) / 2;
        if (areaCasco < 0.88 * (rw * rh)) return false;

        double uc = (minU + maxU) / 2, vc = (minV + maxV) / 2;
        out[0] = uc * c - vc * s2; out[1] = uc * s2 + vc * c; out[2] = rw; out[3] = rh; out[4] = ang;
        return true;
    }

    static double areaRect(double[] x, double[] y, int n, double a) {
        double c = Math.cos(a), s = Math.sin(a);
        double minU = Double.MAX_VALUE, maxU = -Double.MAX_VALUE, minV = Double.MAX_VALUE, maxV = -Double.MAX_VALUE;
        for (int i = 0; i < n; i++) {
            double u = x[i] * c + y[i] * s, v = -x[i] * s + y[i] * c;
            if (u < minU) minU = u; if (u > maxU) maxU = u;
            if (v < minV) minV = v; if (v > maxV) maxV = v;
        }
        return (maxU - minU) * (maxV - minV);
    }

    /** Casco convexo (cadeia monótona); os pontos já chegam ordenados por x e depois y. Devolve {xs, ys, {n}}. */
    static double[][] casco(double[] px, double[] py, int n) {
        double[] hx = new double[2 * n + 2], hy = new double[2 * n + 2];
        int k = 0;
        for (int i = 0; i < n; i++) {
            while (k >= 2 && cruz(hx[k - 2], hy[k - 2], hx[k - 1], hy[k - 1], px[i], py[i]) <= 0) k--;
            hx[k] = px[i]; hy[k] = py[i]; k++;
        }
        int baixo = k + 1;
        for (int i = n - 2; i >= 0; i--) {
            while (k >= baixo && cruz(hx[k - 2], hy[k - 2], hx[k - 1], hy[k - 1], px[i], py[i]) <= 0) k--;
            hx[k] = px[i]; hy[k] = py[i]; k++;
        }
        return new double[][] { hx, hy, { k - 1 } };
    }

    static double cruz(double ax, double ay, double bx, double by, double cx, double cy) {
        return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
    }

    // ------------------------------------------------------------ 2) inclinação do texto
    static double anguloTexto(byte[] g, int sw, int sh) {
        int n = sw * sh;
        int T = Math.min(170, Math.max(60, otsu(g, n)));
        int total = 0;
        for (int i = 0; i < n; i++) if ((g[i] & 255) < T) total++;
        if (total < 300 || total > n * 0.5) return 0;
        int passo = Math.max(1, total / 60000);
        double[] xs = new double[total / passo + 2], ys = new double[total / passo + 2];
        int m = 0, c = 0;
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
                if ((g[y * sw + x] & 255) < T) { if (c++ % passo == 0) { xs[m] = x; ys[m] = y; m++; } }
        double s0 = pontuacaoLinhas(xs, ys, m, 0, sw, sh);
        double melhorS = s0, melhorT = 0;
        for (double t = -7; t <= 7 + 1e-9; t += 0.25) {
            double s = pontuacaoLinhas(xs, ys, m, t * Math.PI / 180, sw, sh);
            if (s > melhorS * (1 + 1e-12)) { melhorS = s; melhorT = t; }
        }
        double t0 = melhorT;
        for (double t = t0 - 0.25; t <= t0 + 0.25 + 1e-9; t += 0.05) {
            double s = pontuacaoLinhas(xs, ys, m, t * Math.PI / 180, sw, sh);
            if (s > melhorS * (1 + 1e-12)) { melhorS = s; melhorT = t; }
        }
        if (melhorS < s0 * 1.03) return 0;
        return melhorT;
    }

    static double pontuacaoLinhas(double[] xs, double[] ys, int m, double a, int sw, int sh) {
        double c = Math.cos(a), s = Math.sin(a);
        int off = (int) (sw * Math.abs(s)) + 2;
        int tam = sh + 2 * off + 4;
        int[] hist = new int[tam];
        for (int i = 0; i < m; i++) {
            int b = (int) Math.floor(-xs[i] * s + ys[i] * c) + off;
            if (b < 0) b = 0; if (b >= tam) b = tam - 1;
            hist[b]++;
        }
        double soma = 0;
        for (int i = 0; i < tam; i++) soma += (double) hist[i] * hist[i];
        return soma;
    }

    // ------------------------------------------------------------ 3) orientação
    /** Rotação (graus, horário): 0, 90, 180 ou 270. Decide o eixo do texto (retrato/paisagem); o giro de 180° exige certeza. */
    static int orientacao(byte[] g, int sw, int sh) {
        int n = sw * sh;
        int T = Math.min(170, Math.max(60, otsu(g, n)));
        boolean[] tinta = new boolean[n];
        int total = 0;
        for (int i = 0; i < n; i++) if ((g[i] & 255) < T) { tinta[i] = true; total++; }
        if (total < 300 || total > n * 0.5) return 0;
        removerLinhas(tinta, sw, sh);

        double[] R = new double[sh], C = new double[sw];
        int tinta2 = 0;
        for (int y = 0; y < sh; y++)
            for (int x = 0; x < sw; x++)
                if (tinta[y * sw + x]) { R[y]++; C[x]++; tinta2++; }
        if (tinta2 < 300) return 0;
        for (int y = 0; y < sh; y++) R[y] /= sw;
        for (int x = 0; x < sw; x++) C[x] /= sh;

        int eixo = eixoTexto(tinta, sw, sh);
        if (eixo == 0) {
            double er = energia(R, sh), ec = energia(C, sw);
            if (er <= 0 || ec <= 0) return 0;
            double razao = er / ec;
            if (razao > 1.3) eixo = 1; else if (razao < 0.77) eixo = -1;
        }
        if (eixo == 0) return 0;

        if (eixo > 0) {
            double[] v = voto(R, sh);
            if (v[1] >= 3 && v[0] > LIMITE_ASSIM) return 180;
            return 0;
        }
        double[] v = voto(C, sw);
        if (v[1] >= 1 && v[0] > 0) return 270;
        return 90;
    }

    /** Apaga da máscara as retas muito longas (bordas de tabela, moldura): não são texto. */
    static void removerLinhas(boolean[] t, int sw, int sh) {
        int limH = Math.max(20, (int) (sw * 0.12)), limV = Math.max(20, (int) (sh * 0.12));
        for (int y = 0; y < sh; y++) {
            int x = 0;
            while (x < sw) {
                if (!t[y * sw + x]) { x++; continue; }
                int a = x;
                while (x < sw && t[y * sw + x]) x++;
                if (x - a > limH) for (int k = a; k < x; k++) t[y * sw + k] = false;
            }
        }
        for (int x = 0; x < sw; x++) {
            int y = 0;
            while (y < sh) {
                if (!t[y * sw + x]) { y++; continue; }
                int a = y;
                while (y < sh && t[y * sw + x]) y++;
                if (y - a > limV) for (int k = a; k < y; k++) t[k * sw + x] = false;
            }
        }
    }

    /** 1 = texto na horizontal, -1 = na vertical, 0 = sem certeza (manchas alongadas depois de borrar a tinta). */
    static int eixoTexto(boolean[] tinta, int sw, int sh) {
        double sh_ = 0, sv = 0;
        int[] gaps = { 5, 12 };
        for (int k = 0; k < gaps.length; k++) {
            sh_ += alongadas(tinta, sw, sh, gaps[k], true);
            sv += alongadas(tinta, sw, sh, gaps[k], false);
        }
        if (sh_ + sv < 150) return 0;
        if (sh_ > sv * 1.25) return 1;
        if (sv > sh_ * 1.25) return -1;
        return 0;
    }

    static double alongadas(boolean[] tinta, int sw, int sh, int gap, boolean horiz) {
        int n = sw * sh;
        boolean[] m = new boolean[n];
        System.arraycopy(tinta, 0, m, 0, n);
        if (horiz) {
            for (int y = 0; y < sh; y++) {
                int ult = -1;
                for (int x = 0; x < sw; x++)
                    if (tinta[y * sw + x]) {
                        if (ult >= 0 && x - ult > 1 && x - ult - 1 <= gap) for (int k = ult + 1; k < x; k++) m[y * sw + k] = true;
                        ult = x;
                    }
            }
        } else {
            for (int x = 0; x < sw; x++) {
                int ult = -1;
                for (int y = 0; y < sh; y++)
                    if (tinta[y * sw + x]) {
                        if (ult >= 0 && y - ult > 1 && y - ult - 1 <= gap) for (int k = ult + 1; k < y; k++) m[k * sw + x] = true;
                        ult = y;
                    }
            }
        }
        boolean[] visto = new boolean[n];
        int[] pilha = new int[n];
        double score = 0;
        for (int s = 0; s < n; s++) {
            if (!m[s] || visto[s]) continue;
            int topo = 0; pilha[topo++] = s; visto[s] = true;
            int x0 = sw, x1 = -1, y0 = sh, y1 = -1, tint = 0;
            while (topo > 0) {
                int p = pilha[--topo];
                int x = p % sw, y = p / sw;
                if (tinta[p]) tint++;
                if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
                if (x > 0 && m[p - 1] && !visto[p - 1]) { visto[p - 1] = true; pilha[topo++] = p - 1; }
                if (x < sw - 1 && m[p + 1] && !visto[p + 1]) { visto[p + 1] = true; pilha[topo++] = p + 1; }
                if (y > 0 && m[p - sw] && !visto[p - sw]) { visto[p - sw] = true; pilha[topo++] = p - sw; }
                if (y < sh - 1 && m[p + sw] && !visto[p + sw]) { visto[p + sw] = true; pilha[topo++] = p + sw; }
            }
            int bw = x1 - x0 + 1, bh = y1 - y0 + 1;
            int comp = horiz ? bw : bh, larg = horiz ? bh : bw;
            if (comp >= 24 && comp >= 3 * larg && larg <= 80) score += tint;
        }
        return score;
    }

    static double energia(double[] p, int n) {
        double e = 0;
        for (int i = 1; i < n; i++) { double d = p[i] - p[i - 1]; e += d * d; }
        return e / n;
    }

    /** Assimetria média (ponderada pela tinta) do perfil de cada linha de texto. Devolve {voto, linhas}. */
    static double[] voto(double[] p, int n) {
        double[] s = new double[n];
        double max = 0;
        for (int i = 0; i < n; i++) {
            double a = p[i], b = i > 0 ? p[i - 1] : p[i], c = i < n - 1 ? p[i + 1] : p[i];
            s[i] = (a + b + c) / 3;
            if (s[i] > max) max = s[i];
        }
        double th = 0.05 * max;
        double soma = 0, pesos = 0; int linhas = 0;
        int ini = -1;
        for (int i = 0; i <= n; i++) {
            boolean dentro = i < n && s[i] > th;
            if (dentro && ini < 0) ini = i;
            if (!dentro && ini >= 0) {
                int a = ini, b = i - 1; ini = -1;
                int len = b - a + 1;
                if (len < 4 || len > 80) continue;
                double massa = 0, mom = 0;
                for (int k = a; k <= b; k++) { massa += p[k]; mom += p[k] * k; }
                if (massa <= 0) continue;
                double mu = mom / massa, v2 = 0, v3 = 0;
                for (int k = a; k <= b; k++) { double d = k - mu; v2 += p[k] * d * d; v3 += p[k] * d * d * d; }
                v2 /= massa; v3 /= massa;
                if (v2 <= 0) continue;
                double sk = v3 / (v2 * Math.sqrt(v2));
                soma += massa * sk;
                pesos += massa;
                linhas++;
            }
        }
        return new double[] { pesos > 0 ? soma / pesos : 0, linhas };
    }

    // ------------------------------------------------------------ geometria
    static byte[] girar(byte[] s, int w, int h, int graus) {
        if (graus == 180) {
            byte[] d = new byte[s.length];
            int n = w * h;
            for (int i = 0; i < n; i++) {
                int o = i * 3, q = (n - 1 - i) * 3;
                d[q] = s[o]; d[q + 1] = s[o + 1]; d[q + 2] = s[o + 2];
            }
            return d;
        }
        byte[] r = new byte[s.length];
        if (graus == 90) {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    int o = (y * w + x) * 3, q = (x * h + (h - 1 - y)) * 3;
                    r[q] = s[o]; r[q + 1] = s[o + 1]; r[q + 2] = s[o + 2];
                }
        } else {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) {
                    int o = (y * w + x) * 3, q = ((w - 1 - x) * h + y) * 3;
                    r[q] = s[o]; r[q + 1] = s[o + 1]; r[q + 2] = s[o + 2];
                }
        }
        return r;
    }

    /** Recorta e endireita: a saída (ow×oh) tem o centro em (cx, cy) da origem e o eixo x girado de ang (rad). Fora fica branco. */
    static byte[] amostrar(byte[] s, int w, int h, double cx, double cy, double ang, int ow, int oh) {
        byte[] d = new byte[ow * oh * 3];
        double c = Math.cos(ang), sn = Math.sin(ang);
        double mx = (ow - 1) / 2.0, my = (oh - 1) / 2.0;
        if (Math.abs(ang) < 0.0005) {
            int x0 = (int) Math.round(cx - mx), y0 = (int) Math.round(cy - my);
            for (int y = 0; y < oh; y++) {
                int sy = y0 + y;
                for (int x = 0; x < ow; x++) {
                    int sx = x0 + x, q = (y * ow + x) * 3;
                    if (sx < 0 || sy < 0 || sx >= w || sy >= h) { d[q] = d[q + 1] = d[q + 2] = (byte) 255; continue; }
                    int o = (sy * w + sx) * 3;
                    d[q] = s[o]; d[q + 1] = s[o + 1]; d[q + 2] = s[o + 2];
                }
            }
            return d;
        }
        for (int y = 0; y < oh; y++) {
            double v = y - my;
            double bx = cx - v * sn, by = cy + v * c;
            for (int x = 0; x < ow; x++) {
                double u = x - mx;
                double sx = bx + u * c, sy = by + u * sn;
                int q = (y * ow + x) * 3;
                if (sx < -0.5 || sy < -0.5 || sx > w - 0.5 || sy > h - 0.5) { d[q] = d[q + 1] = d[q + 2] = (byte) 255; continue; }
                if (sx < 0) sx = 0; if (sy < 0) sy = 0;
                if (sx > w - 1) sx = w - 1; if (sy > h - 1) sy = h - 1;
                int ix = (int) sx, iy = (int) sy;
                int ix1 = ix + 1 < w ? ix + 1 : ix, iy1 = iy + 1 < h ? iy + 1 : iy;
                double fx = sx - ix, fy = sy - iy;
                int o00 = (iy * w + ix) * 3, o10 = (iy * w + ix1) * 3, o01 = (iy1 * w + ix) * 3, o11 = (iy1 * w + ix1) * 3;
                double w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
                for (int k = 0; k < 3; k++)
                    d[q + k] = (byte) (int) ((s[o00 + k] & 255) * w00 + (s[o10 + k] & 255) * w10 + (s[o01 + k] & 255) * w01 + (s[o11 + k] & 255) * w11 + 0.5);
            }
        }
        return d;
    }
}
