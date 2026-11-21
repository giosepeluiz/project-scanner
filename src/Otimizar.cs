// Otimização automática da página digitalizada, toda no aparelho (sem internet e sem IA):
//   1) tira as bordas pretas da tampa do scanner e endireita a página (retângulo de menor área em volta dela);
//   2) sem borda detectada, corrige a inclinação do texto (perfil de projeção);
//   3) acerta a orientação (0, 90, 180 ou 270°) pelas linhas de texto; se não houver certeza, não gira.
// A mesma lógica existe no Android (Otimizar.java) e as duas dão as mesmas decisões.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Digitalizar
{
    static class Otimizar
    {
        const int LADO = 1000;               // maior lado da imagem reduzida usada na análise
        const double LIMITE_ASSIM = 0.04;    // assimetria mínima para confiar na orientação
        const long MAX_PIXELS = 110000000L;  // acima disso (1200 DPI em A4) não otimiza, por causa da memória

        // ------------------------------------------------------------ bitmap
        /// Devolve uma imagem nova (recortada / endireitada / girada) ou null se nada precisou mudar
        public static Bitmap Processar(Bitmap origem)
        {
            int w = origem.Width, h = origem.Height;
            if ((long)w * h > MAX_PIXELS || w < 50 || h < 50) return null;
            byte[] bgr = Ler(origem);
            int ow, oh; string log;
            byte[] r = Executar(bgr, w, h, out ow, out oh, out log);
            if (r == null) return null;
            var bmp = Escrever(r, ow, oh);
            bmp.SetResolution(origem.HorizontalResolution, origem.VerticalResolution);
            return bmp;
        }

        static byte[] Ler(Bitmap b)
        {
            int w = b.Width, h = b.Height;
            var res = new byte[w * h * 3];
            BitmapData d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                for (int y = 0; y < h; y++) Marshal.Copy(d.Scan0 + y * d.Stride, res, y * w * 3, w * 3);
            }
            finally { b.UnlockBits(d); }
            return res;
        }

        static Bitmap Escrever(byte[] bgr, int w, int h)
        {
            var b = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            BitmapData d = b.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                for (int y = 0; y < h; y++) Marshal.Copy(bgr, y * w * 3, d.Scan0 + y * d.Stride, w * 3);
            }
            finally { b.UnlockBits(d); }
            return b;
        }

        // ------------------------------------------------------------ núcleo (só arrays)
        /// bgr: w*h*3 bytes. Devolve a imagem otimizada (bgr) ou null se não mudou nada.
        public static byte[] Executar(byte[] bgr, int w, int h, out int ow, out int oh, out string log)
        {
            ow = w; oh = h; log = "";
            int f = Math.Max(1, (Math.Max(w, h) + LADO - 1) / LADO);
            int sw = w / f, sh = h / f;
            byte[] g = Reduzir(bgr, w, h, f, sw, sh);

            byte[] cur = bgr; int cw = w, ch = h;
            bool mudou = false;

            double cx, cy, rw, rh, ang;
            if (AcharPagina(g, sw, sh, out cx, out cy, out rw, out rh, out ang))
            {
                // coordenadas da imagem reduzida -> imagem inteira (centro do pixel)
                double fx = (cx + 0.5) * f - 0.5, fy = (cy + 0.5) * f - 0.5;
                int nw = (int)Math.Round(rw * f), nh = (int)Math.Round(rh * f);
                if (nw >= 16 && nh >= 16)
                {
                    cur = Amostrar(bgr, w, h, fx, fy, ang, nw, nh);
                    cw = nw; ch = nh; mudou = true;
                    log += "recorte " + nw + "x" + nh + " ang=" + (ang * 180 / Math.PI).ToString("0.00") + "; ";
                }
            }
            else
            {
                // sem moldura de scanner reconhecível: ainda assim tira faixas pretas coladas nas bordas (tampa aberta, sombra)
                int bl, bt, br, bb;
                if (AparaBordas(g, sw, sh, out bl, out bt, out br, out bb))
                {
                    int x0 = bl * f, y0 = bt * f;
                    int x1 = br > 0 ? (sw - br) * f : w, y1 = bb > 0 ? (sh - bb) * f : h;
                    int nw = x1 - x0, nh = y1 - y0;
                    if (nw >= 16 && nh >= 16)
                    {
                        cur = Amostrar(bgr, w, h, (x0 + x1 - 1) / 2.0, (y0 + y1 - 1) / 2.0, 0, nw, nh);
                        cw = nw; ch = nh; mudou = true;
                        log += "bordas " + bl + "," + bt + "," + br + "," + bb + "; ";
                    }
                }
                int fa = Math.Max(1, (Math.Max(cw, ch) + LADO - 1) / LADO);
                int swa = cw / fa, sha = ch / fa;
                byte[] ga = mudou ? Reduzir(cur, cw, ch, fa, swa, sha) : g;
                if (!mudou) { swa = sw; sha = sh; }
                double d = AnguloTexto(ga, swa, sha);
                if (Math.Abs(d) >= 0.2)
                {
                    cur = Amostrar(cur, cw, ch, (cw - 1) / 2.0, (ch - 1) / 2.0, d * Math.PI / 180, cw, ch);
                    mudou = true;
                    log += "texto " + d.ToString("0.00") + "; ";
                }
            }

            // orientação, sobre o resultado
            int sw2 = sw, sh2 = sh;
            byte[] g2 = g;
            if (mudou)
            {
                int f2 = Math.Max(1, (Math.Max(cw, ch) + LADO - 1) / LADO);
                sw2 = cw / f2; sh2 = ch / f2;
                g2 = Reduzir(cur, cw, ch, f2, sw2, sh2);
            }
            int giro = Orientacao(g2, sw2, sh2);
            if (giro != 0)
            {
                cur = Girar(cur, cw, ch, giro);
                if (giro != 180) { int t = cw; cw = ch; ch = t; }
                mudou = true;
                log += "giro " + giro + "; ";
            }
            if (!mudou) return null;
            ow = cw; oh = ch;
            return cur;
        }

        /// Reduz por média de blocos f×f para tons de cinza (0.114 B + 0.587 G + 0.299 R)
        static byte[] Reduzir(byte[] bgr, int w, int h, int f, int sw, int sh)
        {
            var g = new byte[sw * sh];
            int area = f * f;
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    int soma = 0;
                    for (int j = 0; j < f; j++)
                    {
                        int o = ((y * f + j) * w + x * f) * 3;
                        for (int i = 0; i < f; i++, o += 3)
                            soma += (bgr[o] * 29 + bgr[o + 1] * 150 + bgr[o + 2] * 77) >> 8;
                    }
                    g[y * sw + x] = (byte)(soma / area);
                }
            return g;
        }

        /// Faixas pretas coladas nas bordas (linhas/colunas quase todas escuras). Devolve quanto cortar de cada lado, em px reduzidos.
        static bool AparaBordas(byte[] g, int sw, int sh, out int l, out int t, out int r, out int b)
        {
            int n = sw * sh;
            int T = Math.Min(150, Math.Max(40, Otsu(g, n)));
            l = Lado(g, sw, sh, T, 0); t = Lado(g, sw, sh, T, 1); r = Lado(g, sw, sh, T, 2); b = Lado(g, sw, sh, T, 3);
            if (l + t + r + b == 0) return false;
            if (l + r > sw * 0.4 || t + b > sh * 0.4) { l = t = r = b = 0; return false; }
            return true;
        }

        /// lado: 0 esquerda, 1 topo, 2 direita, 3 base. Avança para dentro enquanto a linha for majoritariamente escura
        /// (tolera 3 linhas claras seguidas) e soma 2 px de folga.
        static int Lado(byte[] g, int sw, int sh, int T, int lado)
        {
            bool vert = lado == 0 || lado == 2;           // esquerda/direita: percorre colunas
            int comp = vert ? sh : sw, prof = (int)((vert ? sw : sh) * 0.15);
            int ultimo = -1, falhas = 0;
            for (int k = 0; k < prof; k++)
            {
                int dark = 0;
                for (int i = 0; i < comp; i++)
                {
                    int x, y;
                    if (lado == 0) { x = k; y = i; } else if (lado == 2) { x = sw - 1 - k; y = i; }
                    else if (lado == 1) { x = i; y = k; } else { x = i; y = sh - 1 - k; }
                    if (g[y * sw + x] < T) dark++;
                }
                if (dark >= comp * 0.4) { ultimo = k; falhas = 0; }
                else if (++falhas > 3) break;
            }
            return ultimo < 0 ? 0 : ultimo + 1 + 2;
        }

        static int Otsu(byte[] g, int n)
        {
            var hist = new long[256];
            for (int i = 0; i < n; i++) hist[g[i]]++;
            double total = n, soma = 0;
            for (int i = 0; i < 256; i++) soma += i * (double)hist[i];
            double somaB = 0, wB = 0, melhor = -1; int limiar = 128;
            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];
                if (wB == 0) continue;
                double wF = total - wB;
                if (wF == 0) break;
                somaB += t * (double)hist[t];
                double mB = somaB / wB, mF = (soma - somaB) / wF;
                double v = wB * wF * (mB - mF) * (mB - mF);
                if (v > melhor) { melhor = v; limiar = t; }
            }
            return limiar;
        }

        // ------------------------------------------------------------ 1) página e bordas pretas
        /// Acha a página (fundo preto em volta) e devolve o retângulo útil: centro, tamanho e ângulo (rad), em px da imagem reduzida
        static bool AcharPagina(byte[] g, int sw, int sh, out double cx, out double cy, out double rw, out double rh, out double ang)
        {
            cx = cy = rw = rh = ang = 0;
            int n = sw * sh;
            int T = Math.Min(140, Math.Max(40, Otsu(g, n)));

            // só procura borda se o contorno da imagem é bastante escuro
            int anel = 0, total = 0;
            for (int x = 0; x < sw; x++) { total += 2; if (g[x] < T) anel++; if (g[(sh - 1) * sw + x] < T) anel++; }
            for (int y = 1; y < sh - 1; y++) { total += 2; if (g[y * sw] < T) anel++; if (g[y * sw + sw - 1] < T) anel++; }
            if (anel < total * 0.03) return false;

            // fundo = escuro ligado ao contorno
            var bg = new bool[n];
            var pilha = new int[n];
            int topo = 0;
            for (int x = 0; x < sw; x++)
            {
                int a = x, b = (sh - 1) * sw + x;
                if (g[a] < T && !bg[a]) { bg[a] = true; pilha[topo++] = a; }
                if (g[b] < T && !bg[b]) { bg[b] = true; pilha[topo++] = b; }
            }
            for (int y = 0; y < sh; y++)
            {
                int a = y * sw, b = y * sw + sw - 1;
                if (g[a] < T && !bg[a]) { bg[a] = true; pilha[topo++] = a; }
                if (g[b] < T && !bg[b]) { bg[b] = true; pilha[topo++] = b; }
            }
            while (topo > 0)
            {
                int p = pilha[--topo];
                int x = p % sw, y = p / sw;
                if (x > 0 && !bg[p - 1] && g[p - 1] < T) { bg[p - 1] = true; pilha[topo++] = p - 1; }
                if (x < sw - 1 && !bg[p + 1] && g[p + 1] < T) { bg[p + 1] = true; pilha[topo++] = p + 1; }
                if (y > 0 && !bg[p - sw] && g[p - sw] < T) { bg[p - sw] = true; pilha[topo++] = p - sw; }
                if (y < sh - 1 && !bg[p + sw] && g[p + sw] < T) { bg[p + sw] = true; pilha[topo++] = p + sw; }
            }
            int nbg = 0;
            for (int i = 0; i < n; i++) if (bg[i]) nbg++;
            if (nbg < n * 0.0005 || nbg > n * 0.85) return false;

            // maior componente do que não é fundo (a página)
            var comp = new int[n];     // 0 = nada, 1 = página
            int melhorArea = 0, melhorSemente = -1;
            var visto = new bool[n];
            for (int s = 0; s < n; s++)
            {
                if (bg[s] || visto[s]) continue;
                int area = 0; topo = 0; pilha[topo++] = s; visto[s] = true;
                while (topo > 0)
                {
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
            // marca a maior componente
            topo = 0; pilha[topo++] = melhorSemente; comp[melhorSemente] = 1;
            while (topo > 0)
            {
                int p = pilha[--topo];
                int x = p % sw, y = p / sw;
                if (x > 0 && !bg[p - 1] && comp[p - 1] == 0) { comp[p - 1] = 1; pilha[topo++] = p - 1; }
                if (x < sw - 1 && !bg[p + 1] && comp[p + 1] == 0) { comp[p + 1] = 1; pilha[topo++] = p + 1; }
                if (y > 0 && !bg[p - sw] && comp[p - sw] == 0) { comp[p - sw] = 1; pilha[topo++] = p - sw; }
                if (y < sh - 1 && !bg[p + sw] && comp[p + sw] == 0) { comp[p + sw] = 1; pilha[topo++] = p + sw; }
            }

            // pontos do contorno
            var px = new System.Collections.Generic.List<int>();
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    int p = y * sw + x;
                    if (comp[p] == 0) continue;
                    bool borda = x == 0 || y == 0 || x == sw - 1 || y == sh - 1 ||
                                 comp[p - 1] == 0 || comp[p + 1] == 0 || comp[p - sw] == 0 || comp[p + sw] == 0;
                    if (borda) px.Add(p);
                }
            int m = px.Count;
            if (m < 8) return false;
            var hx = new double[m]; var hy = new double[m];
            for (int i = 0; i < m; i++) { hx[i] = px[i] % sw; hy[i] = px[i] / sw; }   // já em ordem (y, x)
            int nh;
            double[] ex, ey;
            Casco(hx, hy, m, out ex, out ey, out nh);
            if (nh < 3) return false;

            // retângulo de menor área
            double melhorA = double.MaxValue, melhorT = 0;
            for (double t = -45; t < 45; t += 0.25)
            {
                double a = AreaRect(ex, ey, nh, t * Math.PI / 180);
                if (a < melhorA - 1e-9) { melhorA = a; melhorT = t; }
            }
            double t0 = melhorT;
            for (double t = t0 - 0.25; t <= t0 + 0.25 + 1e-9; t += 0.02)
            {
                double a = AreaRect(ex, ey, nh, t * Math.PI / 180);
                if (a < melhorA - 1e-9) { melhorA = a; melhorT = t; }
            }
            ang = melhorT * Math.PI / 180;
            double c = Math.Cos(ang), s2 = Math.Sin(ang);
            double minU = double.MaxValue, maxU = double.MinValue, minV = double.MaxValue, maxV = double.MinValue;
            for (int i = 0; i < nh; i++)
            {
                double u = ex[i] * c + ey[i] * s2, v = -ex[i] * s2 + ey[i] * c;
                if (u < minU) minU = u; if (u > maxU) maxU = u;
                if (v < minV) minV = v; if (v > maxV) maxV = v;
            }
            // os pontos são centros de pixel: o contorno real fica meio pixel para fora; depois tira 1.5 px para cortar o serrilhado
            double folga = 0.5 - 1.5;
            minU -= folga; maxU += folga; minV -= folga; maxV += folga;
            rw = maxU - minU; rh = maxV - minV;
            if (rw < sw * 0.2 || rh < sh * 0.2) return false;

            // a página precisa ser quase um retângulo (área do casco / área do retângulo)
            double areaCasco = 0;
            for (int i = 0; i < nh; i++) { int j = (i + 1) % nh; areaCasco += ex[i] * ey[j] - ex[j] * ey[i]; }
            areaCasco = Math.Abs(areaCasco) / 2;
            if (areaCasco < 0.88 * (rw * rh)) return false;

            double uc = (minU + maxU) / 2, vc = (minV + maxV) / 2;
            cx = uc * c - vc * s2; cy = uc * s2 + vc * c;
            return true;
        }

        static double AreaRect(double[] x, double[] y, int n, double a)
        {
            double c = Math.Cos(a), s = Math.Sin(a);
            double minU = double.MaxValue, maxU = double.MinValue, minV = double.MaxValue, maxV = double.MinValue;
            for (int i = 0; i < n; i++)
            {
                double u = x[i] * c + y[i] * s, v = -x[i] * s + y[i] * c;
                if (u < minU) minU = u; if (u > maxU) maxU = u;
                if (v < minV) minV = v; if (v > maxV) maxV = v;
            }
            return (maxU - minU) * (maxV - minV);
        }

        /// Casco convexo (cadeia monótona). Os pontos chegam ordenados por y e depois x; reordena por x e depois y.
        static void Casco(double[] px, double[] py, int n, out double[] ox, out double[] oy, out int nh)
        {
            var idx = new int[n];
            for (int i = 0; i < n; i++) idx[i] = i;
            Array.Sort(idx, (a, b) => px[a] != px[b] ? px[a].CompareTo(px[b]) : py[a].CompareTo(py[b]));
            var hx = new double[2 * n + 2]; var hy = new double[2 * n + 2];
            int k = 0;
            for (int ii = 0; ii < n; ii++)
            {
                int i = idx[ii];
                while (k >= 2 && Cruz(hx[k - 2], hy[k - 2], hx[k - 1], hy[k - 1], px[i], py[i]) <= 0) k--;
                hx[k] = px[i]; hy[k] = py[i]; k++;
            }
            int baixo = k + 1;
            for (int ii = n - 2; ii >= 0; ii--)
            {
                int i = idx[ii];
                while (k >= baixo && Cruz(hx[k - 2], hy[k - 2], hx[k - 1], hy[k - 1], px[i], py[i]) <= 0) k--;
                hx[k] = px[i]; hy[k] = py[i]; k++;
            }
            nh = k - 1;
            ox = hx; oy = hy;
        }

        static double Cruz(double ax, double ay, double bx, double by, double cx, double cy)
        {
            return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        }

        // ------------------------------------------------------------ 2) inclinação do texto
        /// Ângulo (graus) em que as linhas de texto ficam mais "afiadas"; 0 se não há certeza
        static double AnguloTexto(byte[] g, int sw, int sh)
        {
            int n = sw * sh;
            int T = Math.Min(170, Math.Max(60, Otsu(g, n)));
            int total = 0;
            for (int i = 0; i < n; i++) if (g[i] < T) total++;
            if (total < 300 || total > n * 0.5) return 0;
            int passo = Math.Max(1, total / 60000);
            var xs = new double[total / passo + 2]; var ys = new double[total / passo + 2];
            int m = 0, c = 0;
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                    if (g[y * sw + x] < T) { if (c++ % passo == 0) { xs[m] = x; ys[m] = y; m++; } }
            double s0 = PontuacaoLinhas(xs, ys, m, 0, sw, sh);
            double melhorS = s0, melhorT = 0;
            for (double t = -7; t <= 7 + 1e-9; t += 0.25)
            {
                double s = PontuacaoLinhas(xs, ys, m, t * Math.PI / 180, sw, sh);
                if (s > melhorS * (1 + 1e-12)) { melhorS = s; melhorT = t; }
            }
            double t0 = melhorT;
            for (double t = t0 - 0.25; t <= t0 + 0.25 + 1e-9; t += 0.05)
            {
                double s = PontuacaoLinhas(xs, ys, m, t * Math.PI / 180, sw, sh);
                if (s > melhorS * (1 + 1e-12)) { melhorS = s; melhorT = t; }
            }
            if (melhorS < s0 * 1.03) return 0;
            return melhorT;
        }

        static double PontuacaoLinhas(double[] xs, double[] ys, int m, double a, int sw, int sh)
        {
            double c = Math.Cos(a), s = Math.Sin(a);
            int off = (int)(sw * Math.Abs(s)) + 2;
            int tam = sh + 2 * off + 4;
            var hist = new int[tam];
            for (int i = 0; i < m; i++)
            {
                int b = (int)Math.Floor(-xs[i] * s + ys[i] * c) + off;
                if (b < 0) b = 0; if (b >= tam) b = tam - 1;
                hist[b]++;
            }
            double soma = 0;
            for (int i = 0; i < tam; i++) soma += (double)hist[i] * hist[i];
            return soma;
        }

        // ------------------------------------------------------------ 3) orientação
        /// Rotação (graus, horário) a aplicar: 0, 90, 180 ou 270.
        /// Primeiro decide o EIXO do texto (linhas na horizontal = retrato/paisagem certo; na vertical = precisa girar 90°);
        /// só o giro de 180° exige certeza, porque a página só fica de cabeça para baixo se a assimetria for clara.
        static int Orientacao(byte[] g, int sw, int sh)
        {
            int n = sw * sh;
            int T = Math.Min(170, Math.Max(60, Otsu(g, n)));
            var tinta = new bool[n];
            int total = 0;
            for (int i = 0; i < n; i++) if (g[i] < T) { tinta[i] = true; total++; }
            if (total < 300 || total > n * 0.5) return 0;
            RemoverLinhas(tinta, sw, sh);

            var R = new double[sh]; var C = new double[sw];
            int tinta2 = 0;
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                    if (tinta[y * sw + x]) { R[y]++; C[x]++; tinta2++; }
            if (tinta2 < 300) return 0;
            for (int y = 0; y < sh; y++) R[y] /= sw;
            for (int x = 0; x < sw; x++) C[x] /= sh;

            int eixo = EixoTexto(tinta, sw, sh);          // 1 = linhas na horizontal, -1 = na vertical, 0 = sem certeza
            if (eixo == 0)
            {
                double er = Energia(R, sh), ec = Energia(C, sw);
                if (er <= 0 || ec <= 0) return 0;
                double razao = er / ec;
                if (razao > 1.3) eixo = 1; else if (razao < 0.77) eixo = -1;
            }
            if (eixo == 0) return 0;

            double v; int linhas;
            if (eixo > 0)
            {
                Voto(R, sh, out v, out linhas);
                if (linhas >= 3 && v > LIMITE_ASSIM) return 180;
                return 0;
            }
            Voto(C, sw, out v, out linhas);
            if (linhas >= 1 && v > 0) return 270;        // topo das letras para a direita: gira 90° anti-horário
            return 90;                                    // topo para a esquerda (ou sem pista): gira 90° horário
        }

        /// Apaga da máscara de tinta as retas muito longas (bordas de tabela, moldura, réguas): não são texto
        static void RemoverLinhas(bool[] t, int sw, int sh)
        {
            int limH = Math.Max(20, (int)(sw * 0.12)), limV = Math.Max(20, (int)(sh * 0.12));
            for (int y = 0; y < sh; y++)
            {
                int x = 0;
                while (x < sw)
                {
                    if (!t[y * sw + x]) { x++; continue; }
                    int a = x;
                    while (x < sw && t[y * sw + x]) x++;
                    if (x - a > limH) for (int k = a; k < x; k++) t[y * sw + k] = false;
                }
            }
            for (int x = 0; x < sw; x++)
            {
                int y = 0;
                while (y < sh)
                {
                    if (!t[y * sw + x]) { y++; continue; }
                    int a = y;
                    while (y < sh && t[y * sw + x]) y++;
                    if (y - a > limV) for (int k = a; k < y; k++) t[k * sw + x] = false;
                }
            }
        }

        /// 1 se o texto corre na horizontal, -1 se na vertical, 0 se não dá para dizer.
        /// Borra a tinta ao longo de cada direção (junta letras e palavras): linhas de texto viram manchas compridas e finas
        /// na direção do texto e blocos quadrados na outra. Vence a direção com mais tinta em manchas alongadas.
        static int EixoTexto(bool[] tinta, int sw, int sh)
        {
            double sh_ = 0, sv = 0;
            int[] gaps = { 5, 12 };
            for (int k = 0; k < gaps.Length; k++)
            {
                sh_ += Alongadas(tinta, sw, sh, gaps[k], true);
                sv += Alongadas(tinta, sw, sh, gaps[k], false);
            }
            if (sh_ + sv < 150) return 0;
            if (sh_ > sv * 1.25) return 1;
            if (sv > sh_ * 1.25) return -1;
            return 0;
        }

        static double Alongadas(bool[] tinta, int sw, int sh, int gap, bool horiz)
        {
            int n = sw * sh;
            var m = new bool[n];
            Array.Copy(tinta, m, n);
            if (horiz)
            {
                for (int y = 0; y < sh; y++)
                {
                    int ult = -1;
                    for (int x = 0; x < sw; x++)
                        if (tinta[y * sw + x])
                        {
                            if (ult >= 0 && x - ult > 1 && x - ult - 1 <= gap) for (int k = ult + 1; k < x; k++) m[y * sw + k] = true;
                            ult = x;
                        }
                }
            }
            else
            {
                for (int x = 0; x < sw; x++)
                {
                    int ult = -1;
                    for (int y = 0; y < sh; y++)
                        if (tinta[y * sw + x])
                        {
                            if (ult >= 0 && y - ult > 1 && y - ult - 1 <= gap) for (int k = ult + 1; k < y; k++) m[k * sw + x] = true;
                            ult = y;
                        }
                }
            }
            var visto = new bool[n];
            var pilha = new int[n];
            double score = 0;
            for (int s = 0; s < n; s++)
            {
                if (!m[s] || visto[s]) continue;
                int topo = 0; pilha[topo++] = s; visto[s] = true;
                int x0 = sw, x1 = -1, y0 = sh, y1 = -1, tint = 0;
                while (topo > 0)
                {
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

        static double Energia(double[] p, int n)
        {
            double e = 0;
            for (int i = 1; i < n; i++) { double d = p[i] - p[i - 1]; e += d * d; }
            return e / n;
        }

        /// Separa as linhas de texto no perfil p e devolve a assimetria (skewness) média, ponderada pela tinta, do perfil de cada linha.
        /// Em texto latino as letras altas (b d f h k l t, maiúsculas) são bem mais comuns que as que descem (g j p q y), então a
        /// cauda do perfil aponta para o TOPO das letras: em texto em pé, medido do topo para a base, a assimetria é negativa.
        static void Voto(double[] p, int n, out double voto, out int linhas)
        {
            var s = new double[n];
            double max = 0;
            for (int i = 0; i < n; i++)
            {
                double a = p[i], b = i > 0 ? p[i - 1] : p[i], c = i < n - 1 ? p[i + 1] : p[i];
                s[i] = (a + b + c) / 3;
                if (s[i] > max) max = s[i];
            }
            double th = 0.05 * max;
            double soma = 0, pesos = 0; linhas = 0;
            int ini = -1;
            for (int i = 0; i <= n; i++)
            {
                bool dentro = i < n && s[i] > th;
                if (dentro && ini < 0) ini = i;
                if (!dentro && ini >= 0)
                {
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
                    double sk = v3 / (v2 * Math.Sqrt(v2));
                    soma += massa * sk;
                    pesos += massa;
                    linhas++;
                }
            }
            voto = pesos > 0 ? soma / pesos : 0;
        }

        // ------------------------------------------------------------ geometria
        /// Gira 90 (horário), 180 ou 270 graus
        static byte[] Girar(byte[] s, int w, int h, int graus)
        {
            if (graus == 180)
            {
                var d = new byte[s.Length];
                int n = w * h;
                for (int i = 0; i < n; i++)
                {
                    int o = i * 3, q = (n - 1 - i) * 3;
                    d[q] = s[o]; d[q + 1] = s[o + 1]; d[q + 2] = s[o + 2];
                }
                return d;
            }
            var r = new byte[s.Length];
            if (graus == 90)
            {
                // (x, y) -> (h-1-y, x); nova largura = h
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int o = (y * w + x) * 3, q = (x * h + (h - 1 - y)) * 3;
                        r[q] = s[o]; r[q + 1] = s[o + 1]; r[q + 2] = s[o + 2];
                    }
            }
            else
            {
                // 270: (x, y) -> (y, w-1-x)
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int o = (y * w + x) * 3, q = ((w - 1 - x) * h + y) * 3;
                        r[q] = s[o]; r[q + 1] = s[o + 1]; r[q + 2] = s[o + 2];
                    }
            }
            return r;
        }

        /// Recorta e endireita: a saída (ow×oh) tem o centro em (cx, cy) da origem e o eixo x girado de ang (rad).
        /// Fora da origem fica branco. Com ângulo quase zero copia os pixels sem interpolar.
        static byte[] Amostrar(byte[] s, int w, int h, double cx, double cy, double ang, int ow, int oh)
        {
            var d = new byte[ow * oh * 3];
            double c = Math.Cos(ang), sn = Math.Sin(ang);
            double mx = (ow - 1) / 2.0, my = (oh - 1) / 2.0;
            if (Math.Abs(ang) < 0.0005)
            {
                int x0 = (int)Math.Round(cx - mx), y0 = (int)Math.Round(cy - my);
                for (int y = 0; y < oh; y++)
                {
                    int sy = y0 + y;
                    for (int x = 0; x < ow; x++)
                    {
                        int sx = x0 + x, q = (y * ow + x) * 3;
                        if (sx < 0 || sy < 0 || sx >= w || sy >= h) { d[q] = d[q + 1] = d[q + 2] = 255; continue; }
                        int o = (sy * w + sx) * 3;
                        d[q] = s[o]; d[q + 1] = s[o + 1]; d[q + 2] = s[o + 2];
                    }
                }
                return d;
            }
            for (int y = 0; y < oh; y++)
            {
                double v = y - my;
                double bx = cx - v * sn, by = cy + v * c;
                for (int x = 0; x < ow; x++)
                {
                    double u = x - mx;
                    double sx = bx + u * c, sy = by + u * sn;
                    int q = (y * ow + x) * 3;
                    if (sx < -0.5 || sy < -0.5 || sx > w - 0.5 || sy > h - 0.5) { d[q] = d[q + 1] = d[q + 2] = 255; continue; }
                    if (sx < 0) sx = 0; if (sy < 0) sy = 0;
                    if (sx > w - 1) sx = w - 1; if (sy > h - 1) sy = h - 1;
                    int ix = (int)sx, iy = (int)sy;
                    int ix1 = ix + 1 < w ? ix + 1 : ix, iy1 = iy + 1 < h ? iy + 1 : iy;
                    double fx = sx - ix, fy = sy - iy;
                    int o00 = (iy * w + ix) * 3, o10 = (iy * w + ix1) * 3, o01 = (iy1 * w + ix) * 3, o11 = (iy1 * w + ix1) * 3;
                    double w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
                    d[q] = (byte)(s[o00] * w00 + s[o10] * w10 + s[o01] * w01 + s[o11] * w11 + 0.5);
                    d[q + 1] = (byte)(s[o00 + 1] * w00 + s[o10 + 1] * w10 + s[o01 + 1] * w01 + s[o11 + 1] * w11 + 0.5);
                    d[q + 2] = (byte)(s[o00 + 2] * w00 + s[o10 + 2] * w10 + s[o01 + 2] * w01 + s[o11 + 2] * w11 + 0.5);
                }
            }
            return d;
        }
    }
}
