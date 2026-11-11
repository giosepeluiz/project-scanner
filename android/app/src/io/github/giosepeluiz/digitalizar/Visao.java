// "Inteligência" de digitalização pela câmera: encontra o documento na foto,
// corrige a perspectiva e transforma a foto num scan (fundo branco, tinta nítida).
// Escrito em Java puro (sem OpenCV nem serviços do Google), para funcionar em qualquer Android
// e poder ser testado fora do telefone.
package io.github.giosepeluiz.digitalizar;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

public final class Visao {
    private Visao() { }

    /** Quadrilátero do documento: cantos em ordem (sup. esq., sup. dir., inf. dir., inf. esq.). */
    public static final class Quad {
        public final float[] x = new float[4], y = new float[4];
        public float confianca;

        public Quad copia() {
            Quad q = new Quad();
            System.arraycopy(x, 0, q.x, 0, 4); System.arraycopy(y, 0, q.y, 0, 4);
            q.confianca = confianca;
            return q;
        }
        public Quad escalar(float sx, float sy) {
            Quad q = copia();
            for (int i = 0; i < 4; i++) { q.x[i] *= sx; q.y[i] *= sy; }
            return q;
        }
        public double area() {
            double a = 0;
            for (int i = 0; i < 4; i++) { int j = (i + 1) & 3; a += x[i] * y[j] - x[j] * y[i]; }
            return Math.abs(a) / 2;
        }
        public static Quad inteiro(float w, float h, float margem) {
            Quad q = new Quad();
            float mx = w * margem, my = h * margem;
            q.x[0] = mx; q.y[0] = my; q.x[1] = w - mx; q.y[1] = my;
            q.x[2] = w - mx; q.y[2] = h - my; q.x[3] = mx; q.y[3] = h - my;
            return q;
        }
        /** Põe os cantos na ordem horária a partir do superior esquerdo. */
        public void ordenar() {
            float cx = 0, cy = 0;
            for (int i = 0; i < 4; i++) { cx += x[i]; cy += y[i]; }
            cx /= 4; cy /= 4;
            final double[] ang = new double[4];
            Integer[] idx = { 0, 1, 2, 3 };
            for (int i = 0; i < 4; i++) ang[i] = Math.atan2(y[i] - cy, x[i] - cx);
            Arrays.sort(idx, new java.util.Comparator<Integer>() {
                public int compare(Integer a, Integer b) { return Double.compare(ang[a], ang[b]); }
            });
            float[] nx = new float[4], ny = new float[4];
            for (int i = 0; i < 4; i++) { nx[i] = x[idx[i]]; ny[i] = y[idx[i]]; }
            // atan2 em coordenadas de ecrã (y para baixo) já dá ordem horária; começa no de menor x+y
            int ini = 0; float melhor = Float.MAX_VALUE;
            for (int i = 0; i < 4; i++) if (nx[i] + ny[i] < melhor) { melhor = nx[i] + ny[i]; ini = i; }
            for (int i = 0; i < 4; i++) { x[i] = nx[(ini + i) & 3]; y[i] = ny[(ini + i) & 3]; }
        }
        public boolean convexo() {
            int sinal = 0;
            for (int i = 0; i < 4; i++) {
                int j = (i + 1) & 3, k = (i + 2) & 3;
                double c = (x[j] - x[i]) * (y[k] - y[j]) - (y[j] - y[i]) * (x[k] - x[j]);
                int s = c > 0 ? 1 : c < 0 ? -1 : 0;
                if (s == 0) return false;
                if (sinal == 0) sinal = s; else if (s != sinal) return false;
            }
            return true;
        }
    }

    // ================================================================ utilitários de imagem
    /** Luminância (0..255) de pixels ARGB, reduzindo para que o lado maior fique ≤ maxLado. */
    public static int[] cinzaReduzido(int[] argb, int w, int h, int maxLado, int[] tamOut) {
        int f = Math.max(1, (int) Math.ceil(Math.max(w, h) / (double) maxLado));
        int nw = w / f, nh = h / f;
        int[] g = new int[nw * nh];
        int n = f * f;
        for (int y = 0; y < nh; y++)
            for (int x = 0; x < nw; x++) {
                int s = 0;
                for (int yy = 0; yy < f; yy++) {
                    int base = (y * f + yy) * w + x * f;
                    for (int xx = 0; xx < f; xx++) {
                        int c = argb[base + xx];
                        s += (((c >> 16) & 255) * 299 + ((c >> 8) & 255) * 587 + (c & 255) * 114) / 1000;
                    }
                }
                g[y * nw + x] = s / n;
            }
        tamOut[0] = nw; tamOut[1] = nh; tamOut[2] = f;
        return g;
    }

    /** Reduz um plano de luminância (ex.: Y do YUV da câmera) por média de blocos. */
    public static int[] reduzirPlano(byte[] y, int w, int h, int stride, int maxLado, int[] tamOut) {
        int f = Math.max(1, (int) Math.ceil(Math.max(w, h) / (double) maxLado));
        int nw = w / f, nh = h / f, n = f * f;
        int[] g = new int[nw * nh];
        for (int yy = 0; yy < nh; yy++)
            for (int xx = 0; xx < nw; xx++) {
                int s = 0;
                for (int a = 0; a < f; a++) {
                    int base = (yy * f + a) * stride + xx * f;
                    for (int b = 0; b < f; b++) s += y[base + b] & 255;
                }
                g[yy * nw + xx] = s / n;
            }
        tamOut[0] = nw; tamOut[1] = nh; tamOut[2] = f;
        return g;
    }

    static int[] desfocar(int[] src, int w, int h, int r) {
        int[] tmp = new int[w * h], out = new int[w * h];
        for (int y = 0; y < h; y++) {
            int s = 0, base = y * w;
            for (int x = -r; x <= r; x++) s += src[base + clamp(x, 0, w - 1)];
            for (int x = 0; x < w; x++) {
                tmp[base + x] = s / (2 * r + 1);
                s += src[base + clamp(x + r + 1, 0, w - 1)] - src[base + clamp(x - r, 0, w - 1)];
            }
        }
        for (int x = 0; x < w; x++) {
            int s = 0;
            for (int y = -r; y <= r; y++) s += tmp[clamp(y, 0, h - 1) * w + x];
            for (int y = 0; y < h; y++) {
                out[y * w + x] = s / (2 * r + 1);
                s += tmp[clamp(y + r + 1, 0, h - 1) * w + x] - tmp[clamp(y - r, 0, h - 1) * w + x];
            }
        }
        return out;
    }

    static int clamp(int v, int a, int b) { return v < a ? a : v > b ? b : v; }

    static int otsu(int[] v, int max) {
        int[] hist = new int[max + 1];
        for (int a : v) hist[clamp(a, 0, max)]++;
        long total = v.length, soma = 0;
        for (int i = 0; i <= max; i++) soma += (long) i * hist[i];
        long somaB = 0, wB = 0; double melhor = -1; int t = max / 2;
        for (int i = 0; i <= max; i++) {
            wB += hist[i]; if (wB == 0) continue;
            long wF = total - wB; if (wF == 0) break;
            somaB += (long) i * hist[i];
            double mB = somaB / (double) wB, mF = (soma - somaB) / (double) wF;
            double entre = (double) wB * wF * (mB - mF) * (mB - mF);
            if (entre > melhor) { melhor = entre; t = i; }
        }
        return t;
    }

    static int[] sobel(int[] g, int w, int h) {
        int[] m = new int[w * h];
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++) {
                int i = y * w + x;
                int gx = (g[i - w + 1] + 2 * g[i + 1] + g[i + w + 1]) - (g[i - w - 1] + 2 * g[i - 1] + g[i + w - 1]);
                int gy = (g[i + w - 1] + 2 * g[i + w] + g[i + w + 1]) - (g[i - w - 1] + 2 * g[i - w] + g[i - w + 1]);
                m[i] = Math.abs(gx) + Math.abs(gy);
            }
        return m;
    }

    static boolean[] dilatar(boolean[] b, int w, int h, int r) {
        boolean[] t = new boolean[w * h], o = new boolean[w * h];
        for (int y = 0; y < h; y++) {
            int ult = -1000000;
            for (int x = 0; x < w; x++) { if (b[y * w + x]) ult = x; if (x - ult <= r) t[y * w + x] = true; }
            ult = 1000000;
            for (int x = w - 1; x >= 0; x--) { if (b[y * w + x]) ult = x; if (ult - x <= r) t[y * w + x] = true; }
        }
        for (int x = 0; x < w; x++) {
            int ult = -1000000;
            for (int y = 0; y < h; y++) { if (t[y * w + x]) ult = y; if (y - ult <= r) o[y * w + x] = true; }
            ult = 1000000;
            for (int y = h - 1; y >= 0; y--) { if (t[y * w + x]) ult = y; if (ult - y <= r) o[y * w + x] = true; }
        }
        return o;
    }

    static boolean[] erodir(boolean[] b, int w, int h, int r) {
        boolean[] inv = new boolean[w * h];
        for (int i = 0; i < inv.length; i++) inv[i] = !b[i];
        boolean[] d = dilatar(inv, w, h, r);
        for (int i = 0; i < d.length; i++) d[i] = !d[i];
        return d;
    }

    /** Componentes conexas (4-vizinhança). Devolve o rótulo de cada pixel (0 = fundo) e as áreas. */
    static int rotular(boolean[] m, int w, int h, int[] rot, List<Integer> areas) {
        Arrays.fill(rot, 0);
        int[] fila = new int[w * h];
        int n = 0;
        for (int i = 0; i < m.length; i++) {
            if (!m[i] || rot[i] != 0) continue;
            n++;
            int ini = 0, fim = 0, area = 0;
            fila[fim++] = i; rot[i] = n;
            while (ini < fim) {
                int p = fila[ini++]; area++;
                int px = p % w, py = p / w;
                if (px > 0 && m[p - 1] && rot[p - 1] == 0) { rot[p - 1] = n; fila[fim++] = p - 1; }
                if (px < w - 1 && m[p + 1] && rot[p + 1] == 0) { rot[p + 1] = n; fila[fim++] = p + 1; }
                if (py > 0 && m[p - w] && rot[p - w] == 0) { rot[p - w] = n; fila[fim++] = p - w; }
                if (py < h - 1 && m[p + w] && rot[p + w] == 0) { rot[p + w] = n; fila[fim++] = p + w; }
            }
            areas.add(area);
        }
        return n;
    }

    // ================================================================ geometria
    static double cruz(double ox, double oy, double ax, double ay, double bx, double by) {
        return (ax - ox) * (by - oy) - (ay - oy) * (bx - ox);
    }

    /** Envoltória convexa (monotone chain). Pontos como pares x,y. */
    static double[][] envoltoria(List<double[]> pts) {
        double[][] p = pts.toArray(new double[0][]);
        Arrays.sort(p, new java.util.Comparator<double[]>() {
            public int compare(double[] a, double[] b) { return a[0] != b[0] ? Double.compare(a[0], b[0]) : Double.compare(a[1], b[1]); }
        });
        int n = p.length;
        if (n < 3) return p;
        double[][] h = new double[2 * n][];
        int k = 0;
        for (int i = 0; i < n; i++) {
            while (k >= 2 && cruz(h[k - 2][0], h[k - 2][1], h[k - 1][0], h[k - 1][1], p[i][0], p[i][1]) <= 0) k--;
            h[k++] = p[i];
        }
        for (int i = n - 2, t = k + 1; i >= 0; i--) {
            while (k >= t && cruz(h[k - 2][0], h[k - 2][1], h[k - 1][0], h[k - 1][1], p[i][0], p[i][1]) <= 0) k--;
            h[k++] = p[i];
        }
        return Arrays.copyOf(h, k - 1);
    }

    static double areaPoligono(double[][] p) {
        double a = 0;
        for (int i = 0; i < p.length; i++) { double[] u = p[i], v = p[(i + 1) % p.length]; a += u[0] * v[1] - v[0] * u[1]; }
        return Math.abs(a) / 2;
    }

    /** Remove vértices quase colineares de um polígono convexo, até no máximo 'max' vértices. */
    static double[][] simplificar(double[][] p, int max) {
        List<double[]> l = new ArrayList<>(Arrays.asList(p));
        while (l.size() > max) {
            int pior = -1; double menor = Double.MAX_VALUE;
            for (int i = 0; i < l.size(); i++) {
                double[] a = l.get((i - 1 + l.size()) % l.size()), b = l.get(i), c = l.get((i + 1) % l.size());
                double ar = Math.abs(cruz(a[0], a[1], b[0], b[1], c[0], c[1]));
                if (ar < menor) { menor = ar; pior = i; }
            }
            l.remove(pior);
        }
        return l.toArray(new double[0][]);
    }

    /** Maior quadrilátero com vértices na envoltória convexa. */
    static int[] maiorQuad(double[][] p) {
        int n = p.length;
        if (n < 4) return null;
        double melhor = -1; int[] r = null;
        for (int i = 0; i < n; i++)
            for (int k = i + 2; k < n; k++) {
                if (i == 0 && k == n - 1) continue;
                double mj = -1, ml = -1; int bj = -1, bl = -1;
                for (int j = i + 1; j < k; j++) {
                    double a = Math.abs(cruz(p[i][0], p[i][1], p[j][0], p[j][1], p[k][0], p[k][1]));
                    if (a > mj) { mj = a; bj = j; }
                }
                for (int l = k + 1; l < n + i; l++) {
                    int ll = l % n;
                    double a = Math.abs(cruz(p[i][0], p[i][1], p[k][0], p[k][1], p[ll][0], p[ll][1]));
                    if (a > ml) { ml = a; bl = ll; }
                }
                if (bj < 0 || bl < 0) continue;
                if (mj + ml > melhor) { melhor = mj + ml; r = new int[] { i, bj, k, bl }; }
            }
        return r;
    }

    // ================================================================ deteção
    private static final class Candidato {
        Quad q; double nota;
    }

    /**
     * Procura o documento numa imagem de luminância pequena (lado maior ~250–400 px).
     * Devolve null se não houver nada com cara de folha.
     */
    public static Quad detectar(int[] gray, int w, int h) {
        if (w < 16 || h < 16) return null;
        int[] g = desfocar(gray, w, h, 1);
        int[] mag = sobel(g, w, h);
        int[] magOrd = mag.clone();
        Arrays.sort(magOrd);
        int p90 = magOrd[(int) (magOrd.length * 0.90)];
        int p75 = magOrd[(int) (magOrd.length * 0.75)];
        int tBorda = Math.max(24, p90);
        double areaImg = (double) w * h;
        List<Candidato> cands = new ArrayList<>();

        // 1) Regiões fechadas por bordas (o contorno da folha forma uma "cerca").
        //    Vários limiares: o texto dá bordas fortes, mas a folha sobre fundo claro dá bordas fracas.
        int[] limiares = { tBorda, Math.max(16, magOrd[(int) (magOrd.length * 0.80)]), Math.max(12, magOrd[(int) (magOrd.length * 0.68)]) };
        for (int li = 0; li < limiares.length; li++) {
            if (li > 0 && limiares[li] >= limiares[li - 1]) continue;
            boolean[] borda = new boolean[w * h];
            for (int i = 0; i < borda.length; i++) borda[i] = mag[i] > limiares[li];
            for (int r = 1; r <= 2; r++) {
                boolean[] cerca = dilatar(borda, w, h, r);
                boolean[] fora = inundarDaBorda(cerca, w, h);
                boolean[] dentro = new boolean[w * h];
                for (int i = 0; i < dentro.length; i++) dentro[i] = !fora[i];
                avaliarMascara(dentro, w, h, mag, p75, areaImg, cands);
            }
        }

        // 2) Segmentação por brilho (folha clara sobre fundo escuro, ou o contrário)
        int t = otsu(g, 255);
        for (int pol = 0; pol < 2; pol++) {
            boolean[] m = new boolean[w * h];
            for (int i = 0; i < m.length; i++) m[i] = pol == 0 ? g[i] > t : g[i] <= t;
            m = erodir(dilatar(m, w, h, 3), w, h, 3); // fecha: o texto deixa de furar a folha
            m = dilatar(erodir(m, w, h, 2), w, h, 2); // abre: corta ligações finas ao fundo
            avaliarMascara(m, w, h, mag, p75, areaImg, cands);
        }

        Candidato melhor = null;
        for (Candidato c : cands) if (melhor == null || c.nota > melhor.nota) melhor = c;
        if (melhor == null || melhor.nota < 0.35) return null;
        // o bloco de texto também parece um retângulo: se houver um candidato bom que o contém, é a folha
        boolean trocou = true;
        while (trocou) {
            trocou = false;
            for (Candidato c : cands)
                if (c != melhor && c.nota >= melhor.nota * 0.6 && c.nota >= 0.35 && c.q.area() > melhor.q.area() * 1.08 && contem(c.q, melhor.q)) {
                    melhor = c; trocou = true;
                }
        }
        melhor.q.confianca = (float) Math.min(1, melhor.nota);
        return melhor.q;
    }

    static boolean contem(Quad a, Quad b) {
        for (int i = 0; i < 4; i++) if (!dentroDe(a, b.x[i], b.y[i], 3)) return false;
        return true;
    }

    static boolean dentroDe(Quad q, double px, double py, double folga) {
        for (int i = 0; i < 4; i++) {
            int j = (i + 1) & 3;
            double c = cruz(q.x[i], q.y[i], q.x[j], q.y[j], px, py) / Math.hypot(q.x[j] - q.x[i], q.y[j] - q.y[i]);
            if (c < -folga) return false;
        }
        return true;
    }

    static boolean[] inundarDaBorda(boolean[] parede, int w, int h) {
        boolean[] fora = new boolean[w * h];
        int[] fila = new int[w * h];
        int ini = 0, fim = 0;
        for (int x = 0; x < w; x++) {
            int a = x, b = (h - 1) * w + x;
            if (!parede[a] && !fora[a]) { fora[a] = true; fila[fim++] = a; }
            if (!parede[b] && !fora[b]) { fora[b] = true; fila[fim++] = b; }
        }
        for (int y = 0; y < h; y++) {
            int a = y * w, b = y * w + w - 1;
            if (!parede[a] && !fora[a]) { fora[a] = true; fila[fim++] = a; }
            if (!parede[b] && !fora[b]) { fora[b] = true; fila[fim++] = b; }
        }
        while (ini < fim) {
            int p = fila[ini++], px = p % w, py = p / w;
            if (px > 0 && !parede[p - 1] && !fora[p - 1]) { fora[p - 1] = true; fila[fim++] = p - 1; }
            if (px < w - 1 && !parede[p + 1] && !fora[p + 1]) { fora[p + 1] = true; fila[fim++] = p + 1; }
            if (py > 0 && !parede[p - w] && !fora[p - w]) { fora[p - w] = true; fila[fim++] = p - w; }
            if (py < h - 1 && !parede[p + w] && !fora[p + w]) { fora[p + w] = true; fila[fim++] = p + w; }
        }
        return fora;
    }

    static void avaliarMascara(boolean[] m, int w, int h, int[] mag, int tSuporte, double areaImg, List<Candidato> cands) {
        int[] rot = new int[w * h];
        List<Integer> areas = new ArrayList<>();
        int n = rotular(m, w, h, rot, areas);
        // as 3 maiores componentes
        Integer[] ord = new Integer[n];
        for (int i = 0; i < n; i++) ord[i] = i;
        final List<Integer> ar = areas;
        Arrays.sort(ord, new java.util.Comparator<Integer>() {
            public int compare(Integer a, Integer b) { return ar.get(b) - ar.get(a); }
        });
        for (int c = 0; c < Math.min(3, n); c++) {
            int id = ord[c] + 1, area = areas.get(ord[c]);
            if (area < areaImg * 0.08) break;
            // pontos extremos de cada linha
            List<double[]> pts = new ArrayList<>();
            int cheia = 0;
            for (int y = 0; y < h; y++) {
                int esq = -1, dir = -1;
                for (int x = 0; x < w; x++) if (rot[y * w + x] == id) { if (esq < 0) esq = x; dir = x; }
                if (esq >= 0) { cheia += dir - esq + 1; pts.add(new double[] { esq, y }); pts.add(new double[] { dir + 1, y }); pts.add(new double[] { esq, y + 1 }); pts.add(new double[] { dir + 1, y + 1 }); }
            }
            double[][] env = envoltoria(pts);
            double aEnv = areaPoligono(env);
            if (aEnv < 1) continue;
            double[][] s = simplificar(env, 24);
            int[] qi = maiorQuad(s);
            if (qi == null) continue;
            Quad q = new Quad();
            for (int i = 0; i < 4; i++) { q.x[i] = (float) s[qi[i]][0]; q.y[i] = (float) s[qi[i]][1]; }
            q.ordenar();
            if (!q.convexo()) continue;
            double aq = q.area();
            double quadrado = aq / aEnv;           // quão "quadrilátero" é a forma
            double solidez = cheia / aEnv;         // sem grandes reentrâncias (buracos do texto não contam)
            double fracao = aq / areaImg;
            if (quadrado < 0.80 || solidez < 0.70 || fracao < 0.08) continue;
            // a forma não pode ser a própria moldura da imagem (fundo inteiro)
            int naBorda = 0;
            for (int i = 0; i < 4; i++) if ((q.x[i] < 2 || q.x[i] > w - 3) && (q.y[i] < 2 || q.y[i] > h - 3)) naBorda++;
            if (naBorda >= 3) continue;
            double angOk = angulosOk(q);
            double suporte = suporteBordas(q, mag, w, h, tSuporte);
            double nota = Math.pow(quadrado, 3) * solidez * (0.35 + 0.65 * suporte) * angOk * (0.6 + 0.4 * Math.min(1, fracao / 0.35));
            Candidato cd = new Candidato();
            cd.q = q; cd.nota = nota;
            cands.add(cd);
        }
    }

    static double angulosOk(Quad q) {
        double pior = 1;
        for (int i = 0; i < 4; i++) {
            int a = (i + 3) & 3, b = (i + 1) & 3;
            double ux = q.x[a] - q.x[i], uy = q.y[a] - q.y[i], vx = q.x[b] - q.x[i], vy = q.y[b] - q.y[i];
            double cos = (ux * vx + uy * vy) / (Math.hypot(ux, uy) * Math.hypot(vx, vy) + 1e-9);
            double ang = Math.toDegrees(Math.acos(Math.max(-1, Math.min(1, cos))));
            if (ang < 35 || ang > 145) return 0;
            if (ang < 55) pior = Math.min(pior, (ang - 35) / 20.0);
            if (ang > 125) pior = Math.min(pior, (145 - ang) / 20.0);
        }
        return pior;
    }

    /** Fração dos pontos dos lados do quadrilátero que caem sobre uma borda forte da imagem. */
    static double suporteBordas(Quad q, int[] mag, int w, int h, int t) {
        int ok = 0, tot = 0;
        for (int i = 0; i < 4; i++) {
            int j = (i + 1) & 3;
            for (int s = 1; s < 30; s++) {
                double f = s / 30.0;
                double x = q.x[i] + (q.x[j] - q.x[i]) * f, y = q.y[i] + (q.y[j] - q.y[i]) * f;
                int xi = (int) x, yi = (int) y;
                if (xi < 2 || yi < 2 || xi >= w - 2 || yi >= h - 2) continue; // lado colado à moldura não conta
                tot++;
                boolean achou = false;
                for (int dy = -2; dy <= 2 && !achou; dy++)
                    for (int dx = -2; dx <= 2; dx++) if (mag[(yi + dy) * w + xi + dx] > t) { achou = true; break; }
                if (achou) ok++;
            }
        }
        return tot == 0 ? 0.5 : ok / (double) tot;
    }

    // ================================================================ refinamento dos cantos
    /**
     * Afina os cantos numa imagem de luminância maior: procura a borda real ao longo de cada lado,
     * ajusta uma reta e cruza as retas vizinhas.
     */
    public static Quad refinar(Quad q, int[] gray, int w, int h) {
        int[] g = desfocar(gray, w, h, 1);
        double diag = Math.hypot(w, h);
        int raio = (int) Math.max(4, diag * 0.012);
        double[][] retas = new double[4][];
        for (int i = 0; i < 4; i++) {
            int j = (i + 1) & 3;
            double x0 = q.x[i], y0 = q.y[i], x1 = q.x[j], y1 = q.y[j];
            double dx = x1 - x0, dy = y1 - y0, len = Math.hypot(dx, dy);
            if (len < 10) return q;
            double nx = -dy / len, ny = dx / len;
            List<double[]> pts = new ArrayList<>();
            int amostras = 48;
            for (int s = 3; s < amostras - 2; s++) {
                double f = s / (double) amostras;
                double px = x0 + dx * f, py = y0 + dy * f;
                double melhor = 0; double bx = 0, by = 0;
                for (int d = -raio; d <= raio; d++) {
                    double ax = px + nx * (d - 1), ay = py + ny * (d - 1);
                    double cx = px + nx * (d + 1), cy = py + ny * (d + 1);
                    if (ax < 0 || ay < 0 || cx < 0 || cy < 0 || ax >= w - 1 || ay >= h - 1 || cx >= w - 1 || cy >= h - 1) continue;
                    double gr = Math.abs(amostra(g, w, cx, cy) - amostra(g, w, ax, ay));
                    // prefere bordas próximas da estimativa
                    gr *= 1.0 - 0.3 * Math.abs(d) / raio;
                    if (gr > melhor) { melhor = gr; bx = px + nx * d; by = py + ny * d; }
                }
                if (melhor > 12) pts.add(new double[] { bx, by });
            }
            if (pts.size() < 8) { retas[i] = retaDe(x0, y0, x1, y1); continue; }
            double[] r = ajustarReta(pts);
            for (int it = 0; it < 2; it++) {
                List<double[]> bons = new ArrayList<>();
                for (double[] p : pts) if (Math.abs(r[0] * p[0] + r[1] * p[1] + r[2]) < Math.max(1.5, raio * 0.25)) bons.add(p);
                if (bons.size() < 6) break;
                pts = bons; r = ajustarReta(pts);
            }
            retas[i] = r;
        }
        Quad n = new Quad();
        for (int i = 0; i < 4; i++) {
            double[] a = retas[(i + 3) & 3], b = retas[i];
            double det = a[0] * b[1] - a[1] * b[0];
            double ix, iy;
            if (Math.abs(det) < 1e-9) { ix = q.x[i]; iy = q.y[i]; }
            else { ix = (a[1] * b[2] - b[1] * a[2]) / det; iy = (b[0] * a[2] - a[0] * b[2]) / det; }
            if (Math.hypot(ix - q.x[i], iy - q.y[i]) > raio * 2.5) { ix = q.x[i]; iy = q.y[i]; }
            n.x[i] = (float) Math.max(0, Math.min(w, ix));
            n.y[i] = (float) Math.max(0, Math.min(h, iy));
        }
        n.confianca = q.confianca;
        return n.convexo() ? n : q;
    }

    static double amostra(int[] g, int w, double x, double y) {
        int xi = (int) x, yi = (int) y;
        double fx = x - xi, fy = y - yi;
        int i = yi * w + xi;
        return g[i] * (1 - fx) * (1 - fy) + g[i + 1] * fx * (1 - fy) + g[i + w] * (1 - fx) * fy + g[i + w + 1] * fx * fy;
    }

    static double[] retaDe(double x0, double y0, double x1, double y1) {
        double a = y1 - y0, b = x0 - x1, n = Math.hypot(a, b);
        a /= n; b /= n;
        return new double[] { a, b, -(a * x0 + b * y0) };
    }

    /** Reta ax+by+c=0 por mínimos quadrados totais. */
    static double[] ajustarReta(List<double[]> p) {
        double mx = 0, my = 0;
        for (double[] v : p) { mx += v[0]; my += v[1]; }
        mx /= p.size(); my /= p.size();
        double sxx = 0, syy = 0, sxy = 0;
        for (double[] v : p) { double dx = v[0] - mx, dy = v[1] - my; sxx += dx * dx; syy += dy * dy; sxy += dx * dy; }
        double ang = 0.5 * Math.atan2(2 * sxy, sxx - syy); // direção principal
        double a = -Math.sin(ang), b = Math.cos(ang);
        return new double[] { a, b, -(a * mx + b * my) };
    }

    // ================================================================ tamanho de saída
    /** Largura e altura (px) do documento endireitado, estimadas pelos lados do quadrilátero. */
    public static int[] tamanhoSaida(Quad q) {
        double topo = Math.hypot(q.x[1] - q.x[0], q.y[1] - q.y[0]);
        double base = Math.hypot(q.x[2] - q.x[3], q.y[2] - q.y[3]);
        double esq = Math.hypot(q.x[3] - q.x[0], q.y[3] - q.y[0]);
        double dir = Math.hypot(q.x[2] - q.x[1], q.y[2] - q.y[1]);
        // o lado mais distante da câmera aparece encurtado: usa o maior de cada par
        return new int[] { (int) Math.round(Math.max(topo, base)), (int) Math.round(Math.max(esq, dir)) };
    }

    /**
     * Se a proporção medida estiver perto da do papel escolhido (em qualquer orientação),
     * devolve a proporção exata do papel; senão mantém a medida.
     */
    public static int[] ajustarAoPapel(int[] tam, double papelL, double papelA) {
        if (papelL <= 0 || papelA <= 0) return tam;
        double r = tam[0] / (double) tam[1];
        double rp = papelL / papelA, rpi = papelA / papelL;
        double area = (double) tam[0] * tam[1];
        double alvo = Math.abs(Math.log(r / rp)) < Math.abs(Math.log(r / rpi)) ? rp : rpi;
        if (Math.abs(Math.log(r / alvo)) > 0.12) return tam;
        int nw = (int) Math.round(Math.sqrt(area * alvo));
        return new int[] { nw, (int) Math.round(nw / alvo) };
    }

    // ================================================================ homografia (mapeia saída -> origem)
    public static double[] homografia(double[] sx, double[] sy, double[] dx, double[] dy) {
        double[][] a = new double[8][9];
        for (int i = 0; i < 4; i++) {
            a[2 * i] = new double[] { sx[i], sy[i], 1, 0, 0, 0, -dx[i] * sx[i], -dx[i] * sy[i], dx[i] };
            a[2 * i + 1] = new double[] { 0, 0, 0, sx[i], sy[i], 1, -dy[i] * sx[i], -dy[i] * sy[i], dy[i] };
        }
        for (int c = 0; c < 8; c++) {
            int piv = c;
            for (int r = c + 1; r < 8; r++) if (Math.abs(a[r][c]) > Math.abs(a[piv][c])) piv = r;
            double[] t = a[c]; a[c] = a[piv]; a[piv] = t;
            for (int r = 0; r < 8; r++) {
                if (r == c) continue;
                double f = a[r][c] / a[c][c];
                for (int k = c; k < 9; k++) a[r][k] -= f * a[c][k];
            }
        }
        double[] hm = new double[9];
        for (int i = 0; i < 8; i++) hm[i] = a[i][8] / a[i][i];
        hm[8] = 1;
        return hm;
    }

    /** Endireita o documento (amostragem bilinear). Usado nos testes; no Android usa-se Matrix.setPolyToPoly. */
    public static int[] endireitar(int[] src, int w, int h, Quad q, int ow, int oh) {
        double[] H = homografia(new double[] { 0, ow, ow, 0 }, new double[] { 0, 0, oh, oh },
                new double[] { q.x[0], q.x[1], q.x[2], q.x[3] }, new double[] { q.y[0], q.y[1], q.y[2], q.y[3] });
        int[] out = new int[ow * oh];
        for (int y = 0; y < oh; y++)
            for (int x = 0; x < ow; x++) {
                double d = H[6] * (x + 0.5) + H[7] * (y + 0.5) + H[8];
                double u = (H[0] * (x + 0.5) + H[1] * (y + 0.5) + H[2]) / d - 0.5;
                double v = (H[3] * (x + 0.5) + H[4] * (y + 0.5) + H[5]) / d - 0.5;
                int ui = clamp((int) Math.floor(u), 0, w - 2), vi = clamp((int) Math.floor(v), 0, h - 2);
                double fx = Math.max(0, Math.min(1, u - ui)), fy = Math.max(0, Math.min(1, v - vi));
                int c00 = src[vi * w + ui], c10 = src[vi * w + ui + 1], c01 = src[(vi + 1) * w + ui], c11 = src[(vi + 1) * w + ui + 1];
                int r = 0xff000000;
                for (int sh = 0; sh <= 16; sh += 8) {
                    double val = ((c00 >> sh) & 255) * (1 - fx) * (1 - fy) + ((c10 >> sh) & 255) * fx * (1 - fy)
                            + ((c01 >> sh) & 255) * (1 - fx) * fy + ((c11 >> sh) & 255) * fx * fy;
                    r |= clamp((int) Math.round(val), 0, 255) << sh;
                }
                out[y * ow + x] = r;
            }
        return out;
    }

    // ================================================================ "transformar em scan"
    public static final int COR = 0, CINZA = 1, PB = 2;

    /**
     * Realce de documento: estima a iluminação (o "papel") em cada zona, divide por ela para tirar
     * sombras e o tom amarelado da luz, depois deixa o fundo branco e a tinta nítida.
     * Trabalha no próprio array (ARGB).
     */
    public static void realcar(int[] px, int w, int h, int modo) {
        int cel = Math.max(8, Math.max(w, h) / 40);
        int gw = (w + cel - 1) / cel, gh = (h + cel - 1) / cel;
        float[][] fundo = new float[3][gw * gh];
        int[][] hist = new int[3][256];
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++) {
                for (int c = 0; c < 3; c++) Arrays.fill(hist[c], 0);
                int x0 = gx * cel, y0 = gy * cel, x1 = Math.min(w, x0 + cel), y1 = Math.min(h, y0 + cel), n = 0;
                for (int y = y0; y < y1; y += 2)
                    for (int x = x0; x < x1; x += 2) {
                        int v = px[y * w + x];
                        hist[0][(v >> 16) & 255]++; hist[1][(v >> 8) & 255]++; hist[2][v & 255]++; n++;
                    }
                for (int c = 0; c < 3; c++) {
                    int alvo = (int) (n * 0.92), s = 0, i = 0;
                    for (; i < 255; i++) { s += hist[c][i]; if (s >= alvo) break; }
                    fundo[c][gy * gw + gx] = i;
                }
            }
        for (int c = 0; c < 3; c++) {
            float[] f = fundo[c];
            // zonas cheias de tinta/fotos dão "papel" escuro: o máximo dos vizinhos corrige
            for (int it = 0; it < 3; it++) f = maxGrade(f, gw, gh);
            for (int it = 0; it < 3; it++) f = mediaGrade(f, gw, gh);
            for (int i = 0; i < f.length; i++) f[i] = Math.max(f[i], 40);
            fundo[c] = f;
        }
        float[] linha0 = new float[3], linha1 = new float[3];
        for (int y = 0; y < h; y++) {
            float gyf = (y + 0.5f) / cel - 0.5f;
            int gy0 = clamp((int) Math.floor(gyf), 0, gh - 1), gy1 = Math.min(gh - 1, gy0 + 1);
            float ty = Math.max(0, Math.min(1, gyf - gy0));
            for (int x = 0; x < w; x++) {
                float gxf = (x + 0.5f) / cel - 0.5f;
                int gx0 = clamp((int) Math.floor(gxf), 0, gw - 1), gx1 = Math.min(gw - 1, gx0 + 1);
                float tx = Math.max(0, Math.min(1, gxf - gx0));
                int v = px[y * w + x];
                int[] rgb = { (v >> 16) & 255, (v >> 8) & 255, v & 255 };
                float[] o = linha0;
                for (int c = 0; c < 3; c++) {
                    float[] f = fundo[c];
                    float b = (f[gy0 * gw + gx0] * (1 - tx) + f[gy0 * gw + gx1] * tx) * (1 - ty)
                            + (f[gy1 * gw + gx0] * (1 - tx) + f[gy1 * gw + gx1] * tx) * ty;
                    float n = rgb[c] * 255f / b;               // 255 = papel
                    n = (n - 28f) * 255f / (238f - 28f);        // branco puro e tinta mais escura
                    o[c] = n;
                }
                int r, g, bl;
                if (modo == COR) {
                    float l = 0.299f * o[0] + 0.587f * o[1] + 0.114f * o[2];
                    r = clamp(Math.round(l + (o[0] - l) * 1.25f), 0, 255);
                    g = clamp(Math.round(l + (o[1] - l) * 1.25f), 0, 255);
                    bl = clamp(Math.round(l + (o[2] - l) * 1.25f), 0, 255);
                } else {
                    int l = clamp(Math.round(0.299f * o[0] + 0.587f * o[1] + 0.114f * o[2]), 0, 255);
                    if (modo == PB) l = l > 165 ? 255 : 0;
                    r = g = bl = l;
                }
                px[y * w + x] = 0xff000000 | (r << 16) | (g << 8) | bl;
            }
        }
        linha1[0] = 0;
    }

    static float[] maxGrade(float[] f, int w, int h) {
        float[] o = new float[f.length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) {
                float m = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++) {
                        int xx = clamp(x + dx, 0, w - 1), yy = clamp(y + dy, 0, h - 1);
                        m = Math.max(m, f[yy * w + xx]);
                    }
                o[y * w + x] = m;
            }
        return o;
    }

    static float[] mediaGrade(float[] f, int w, int h) {
        float[] o = new float[f.length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++) {
                float s = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++) s += f[clamp(y + dy, 0, h - 1) * w + clamp(x + dx, 0, w - 1)];
                o[y * w + x] = s / 9;
            }
        return o;
    }

    /** Converte para cinza ou preto e branco sem realce (filtro "Original"). */
    public static void converter(int[] px, int modo) {
        if (modo == COR) return;
        for (int i = 0; i < px.length; i++) {
            int v = px[i];
            int l = (((v >> 16) & 255) * 299 + ((v >> 8) & 255) * 587 + (v & 255) * 114) / 1000;
            if (modo == PB) l = l > 140 ? 255 : 0;
            px[i] = 0xff000000 | (l << 16) | (l << 8) | l;
        }
    }
}
