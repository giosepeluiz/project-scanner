// Digitalizar - software de digitalização para Windows (WIA)
// Autor: Giosepe Luiz
// Executável portátil: não precisa instalar nada (usa o .NET Framework do Windows).
// Para recompilar no Windows sem instalar nada: execute compilar.bat

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace Digitalizar
{
    static class Config
    {
        public const string PastaPadrao = @"C:\Users\giosepeluiz\OneDrive\Documents\Scanned Documents";
        public static readonly string[] Tipos = { "JPEG", "PNG", "PDF" };
        public const string TipoPadrao = "JPEG";
        public static readonly string[] Modos = { "Cor", "Cinza", "Preto e branco" };
        public const string ModoPadrao = "Preto e branco";
        public static readonly string[] Resolucoes = { "75", "150", "300", "600", "1200" };
        public const string ResolucaoPadrao = "300";
        public static readonly string[] Fontes = { "Área do vidro" };
    }

    // ============================================================ WIA
    class ScannerInfo
    {
        public string Nome;
        public string Id;
        public override string ToString() { return Nome; }
    }

    static class Wia
    {
        const int WIA_SCANNER = 1;
        const string FORMATO_BMP = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";

        const int PROP_INTENT = 6146;      // 1 cor, 2 cinza, 4 texto (P&B)
        const int PROP_XRES = 6147;
        const int PROP_YRES = 6148;
        const int PROP_XPOS = 6149;
        const int PROP_YPOS = 6150;
        const int PROP_XEXT = 6151;
        const int PROP_YEXT = 6152;
        const int PROP_DATATYPE = 4103;    // 0 P&B, 2 cinza, 3 cor
        const int PROP_DOC_HANDLING = 3088;// 2 = vidro

        static dynamic NovoGerenciador()
        {
            Type t = Type.GetTypeFromProgID("WIA.DeviceManager");
            if (t == null) throw new Exception("O serviço WIA do Windows não está disponível.");
            return Activator.CreateInstance(t);
        }

        public static List<ScannerInfo> Listar()
        {
            var lista = new List<ScannerInfo>();
            dynamic dm = NovoGerenciador();
            dynamic infos = dm.DeviceInfos;
            int n = infos.Count;
            for (int i = 1; i <= n; i++)
            {
                dynamic info = infos[i];
                if ((int)info.Type != WIA_SCANNER) continue;
                string nome = "Scanner";
                dynamic props = info.Properties;
                int np = props.Count;
                for (int j = 1; j <= np; j++)
                {
                    dynamic p = props[j];
                    if ((string)p.Name == "Name") { nome = Convert.ToString(p.Value); break; }
                }
                lista.Add(new ScannerInfo { Nome = nome, Id = (string)info.DeviceID });
            }
            return lista;
        }

        static dynamic AcharProp(dynamic props, int id)
        {
            int n = props.Count;
            for (int i = 1; i <= n; i++)
            {
                dynamic p = props[i];
                if ((int)p.PropertyID == id) return p;
            }
            return null;
        }

        static void Definir(dynamic props, int id, object valor)
        {
            dynamic p = AcharProp(props, id);
            if (p == null) return;
            try { p.Value = valor; return; }
            catch { }
            try
            {
                object o = p;
                o.GetType().InvokeMember("Value", BindingFlags.SetProperty, null, o, new object[] { valor });
            }
            catch { }
        }

        static void Maximizar(dynamic props, int id)
        {
            dynamic p = AcharProp(props, id);
            if (p == null) return;
            try { object max = p.SubTypeMax; Definir(props, id, max); } catch { }
        }

        public static Bitmap Digitalizar(string deviceId, string modo, int dpi)
        {
            dynamic dm = NovoGerenciador();
            dynamic infos = dm.DeviceInfos;
            dynamic dispositivo = null;
            int n = infos.Count;
            for (int i = 1; i <= n; i++)
            {
                dynamic info = infos[i];
                if ((string)info.DeviceID == deviceId) { dispositivo = info.Connect(); break; }
            }
            if (dispositivo == null)
                throw new Exception("Scanner não encontrado. Verifique se está ligado e clique em 'Detectar'.");

            Definir(dispositivo.Properties, PROP_DOC_HANDLING, 2);

            dynamic item = dispositivo.Items[1];
            dynamic props = item.Properties;

            int intent = modo == "Cor" ? 1 : modo == "Cinza" ? 2 : 4;
            int dataType = modo == "Cor" ? 3 : modo == "Cinza" ? 2 : 0;
            Definir(props, PROP_INTENT, intent);
            Definir(props, PROP_DATATYPE, dataType);
            Definir(props, PROP_XRES, dpi);
            Definir(props, PROP_YRES, dpi);
            Definir(props, PROP_XPOS, 0);
            Definir(props, PROP_YPOS, 0);
            Maximizar(props, PROP_XEXT);
            Maximizar(props, PROP_YEXT);

            dynamic imagem = item.Transfer(FORMATO_BMP);

            string tmp = Path.Combine(Path.GetTempPath(), "digitalizar_tmp_" + Process.GetCurrentProcess().Id + ".bmp");
            if (File.Exists(tmp)) File.Delete(tmp);
            imagem.SaveFile(tmp);

            Bitmap bmp;
            using (var fs = new FileStream(tmp, FileMode.Open, FileAccess.Read))
            using (var original = new Bitmap(fs))
            {
                bmp = Imagens.Para24bpp(original);
            }
            try { File.Delete(tmp); } catch { }

            // garante o modo pedido mesmo que o driver ignore a configuração
            if (modo == "Cinza") Imagens.ConverterCinza(bmp);
            else if (modo == "Preto e branco") Imagens.ConverterPB(bmp);

            bmp.SetResolution(dpi, dpi);
            return bmp;
        }

        static readonly Dictionary<uint, string> Erros = new Dictionary<uint, string>
        {
            { 0x80210006, "O scanner está ocupado. Aguarde e tente de novo." },
            { 0x80210005, "O scanner está desligado ou desconectado." },
            { 0x80210001, "Erro geral do scanner." },
            { 0x8021000C, "O scanner está em modo de aviso (tampa aberta?)." },
            { 0x80210015, "Nenhum scanner encontrado." },
            { 0x80210064, "Digitalização cancelada." },
            { 0x80210009, "O scanner está ocupado por outro programa." },
        };

        public static string MensagemErro(Exception e)
        {
            while (e is TargetInvocationException && e.InnerException != null) e = e.InnerException;
            var com = e as COMException;
            if (com != null)
            {
                uint c = unchecked((uint)com.ErrorCode);
                string m;
                if (Erros.TryGetValue(c, out m)) return m;
                return "Erro do scanner (código 0x" + c.ToString("X8") + ").";
            }
            return e.Message;
        }
    }

    // ============================================================ Imagens
    static class Imagens
    {
        public const int LimiarPB = 140;

        public static Bitmap Para24bpp(Image src)
        {
            var b = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);
            b.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.White);
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
            return b;
        }

        static void Processar(Bitmap b, Func<int, int, int, int> f)
        {
            var r = new Rectangle(0, 0, b.Width, b.Height);
            BitmapData d = b.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                int stride = d.Stride;
                byte[] linha = new byte[stride];
                for (int y = 0; y < b.Height; y++)
                {
                    IntPtr p = d.Scan0 + y * stride;
                    Marshal.Copy(p, linha, 0, stride);
                    for (int x = 0; x < b.Width; x++)
                    {
                        int i = x * 3;
                        byte v = (byte)f(linha[i + 2], linha[i + 1], linha[i]);
                        linha[i] = linha[i + 1] = linha[i + 2] = v;
                    }
                    Marshal.Copy(linha, 0, p, stride);
                }
            }
            finally { b.UnlockBits(d); }
        }

        static int Luma(int r, int g, int b) { return (r * 299 + g * 587 + b * 114) / 1000; }

        public static void ConverterCinza(Bitmap b) { Processar(b, (r, g, bl) => Luma(r, g, bl)); }
        public static void ConverterPB(Bitmap b) { Processar(b, (r, g, bl) => Luma(r, g, bl) > LimiarPB ? 255 : 0); }

        static ImageCodecInfo Codec(string mime)
        {
            return ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == mime);
        }

        public static byte[] Jpeg(Bitmap b, long qualidade)
        {
            var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualidade);
            using (var ms = new MemoryStream())
            {
                b.Save(ms, Codec("image/jpeg"), ep);
                return ms.ToArray();
            }
        }

        public static string Guardar(Bitmap b, string pasta, string tipo, string modo, int dpi)
        {
            Directory.CreateDirectory(pasta);
            string ext = tipo == "JPEG" ? ".jpg" : tipo == "PNG" ? ".png" : ".pdf";
            string nomeBase = "Scan_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string caminho = Path.Combine(pasta, nomeBase + ext);
            int n = 1;
            while (File.Exists(caminho)) { caminho = Path.Combine(pasta, nomeBase + "_" + n + ext); n++; }

            if (tipo == "JPEG")
                File.WriteAllBytes(caminho, Jpeg(b, 92));
            else if (tipo == "PNG")
                b.Save(caminho, ImageFormat.Png);
            else
                Pdf.Escrever(caminho, b, modo, dpi);
            return caminho;
        }

        // ---------------------------------------------------- edição
        public static void GuardarEm(Bitmap b, string caminho, int dpi)
        {
            string ext = Path.GetExtension(caminho).ToLowerInvariant();
            b.SetResolution(dpi, dpi);
            string tmp = caminho + ".tmp";
            if (ext == ".png") b.Save(tmp, ImageFormat.Png);
            else if (ext == ".pdf") Pdf.Escrever(tmp, b, EhBinaria(b) ? "Preto e branco" : "Cor", dpi);
            else File.WriteAllBytes(tmp, Jpeg(b, 92));
            if (File.Exists(caminho)) File.Delete(caminho);
            File.Move(tmp, caminho);
        }

        public static bool EhBinaria(Bitmap b)
        {
            BitmapData d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                byte[] linha = new byte[d.Stride];
                int n = b.Width * 3;
                for (int y = 0; y < b.Height; y++)
                {
                    Marshal.Copy(d.Scan0 + y * d.Stride, linha, 0, d.Stride);
                    for (int i = 0; i < n; i += 3)
                    {
                        byte v = linha[i];
                        if ((v != 0 && v != 255) || linha[i + 1] != v || linha[i + 2] != v) return false;
                    }
                }
                return true;
            }
            finally { b.UnlockBits(d); }
        }

        public static Bitmap Copia24(Bitmap b)
        {
            return b.Clone(new Rectangle(0, 0, b.Width, b.Height), PixelFormat.Format24bppRgb);
        }

        public static Bitmap Reduzir(Bitmap b, int maxLado)
        {
            double s = Math.Min(1.0, maxLado / (double)Math.Max(b.Width, b.Height));
            int w = Math.Max(1, (int)(b.Width * s)), h = Math.Max(1, (int)(b.Height * s));
            var r = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            r.SetResolution(b.HorizontalResolution, b.VerticalResolution);
            using (var g = Graphics.FromImage(r))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(b, 0, 0, w, h);
            }
            return r;
        }

        public static Bitmap Rotacionar(Bitmap src, int graus)
        {
            int a = ((graus % 360) + 360) % 360;
            Bitmap c = Copia24(src);
            if (a == 0) return c;
            if (a == 90) { c.RotateFlip(RotateFlipType.Rotate90FlipNone); return c; }
            if (a == 180) { c.RotateFlip(RotateFlipType.Rotate180FlipNone); return c; }
            if (a == 270) { c.RotateFlip(RotateFlipType.Rotate270FlipNone); return c; }
            c.Dispose();

            int w = src.Width, h = src.Height;
            double rad = a * Math.PI / 180.0;
            double cs = Math.Abs(Math.Cos(rad)), sn = Math.Abs(Math.Sin(rad));
            int nw = (int)Math.Ceiling(w * cs + h * sn), nh = (int)Math.Ceiling(w * sn + h * cs);
            var r = new Bitmap(nw, nh, PixelFormat.Format24bppRgb);
            r.SetResolution(src.HorizontalResolution, src.VerticalResolution);
            using (var g = Graphics.FromImage(r))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TranslateTransform(nw / 2f, nh / 2f);
                g.RotateTransform(a);
                g.TranslateTransform(-w / 2f, -h / 2f);
                g.DrawImage(src, 0, 0, w, h);
            }
            return r;
        }

        public static Bitmap Recortar(Bitmap b, RectangleF n)
        {
            var r = Rectangle.Round(new RectangleF(n.X * b.Width, n.Y * b.Height, n.Width * b.Width, n.Height * b.Height));
            r.Intersect(new Rectangle(0, 0, b.Width, b.Height));
            if (r.Width < 1 || r.Height < 1) return Copia24(b);
            return b.Clone(r, PixelFormat.Format24bppRgb);
        }

        public static void AplicarTom(Bitmap b, byte[] lut, bool limiar, int valorLimiar)
        {
            bool identidade = true;
            for (int i = 0; i < 256; i++) if (lut[i] != i) { identidade = false; break; }
            if (identidade && !limiar) return;

            BitmapData d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                byte[] linha = new byte[d.Stride];
                int n = b.Width * 3;
                for (int y = 0; y < b.Height; y++)
                {
                    IntPtr p = d.Scan0 + y * d.Stride;
                    Marshal.Copy(p, linha, 0, d.Stride);
                    if (limiar)
                    {
                        for (int i = 0; i < n; i += 3)
                        {
                            int l = (lut[linha[i + 2]] * 299 + lut[linha[i + 1]] * 587 + lut[linha[i]] * 114) / 1000;
                            byte v = (byte)(l >= valorLimiar ? 255 : 0);
                            linha[i] = linha[i + 1] = linha[i + 2] = v;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < n; i++) linha[i] = lut[linha[i]];
                    }
                    Marshal.Copy(linha, 0, p, d.Stride);
                }
            }
            finally { b.UnlockBits(d); }
        }

        public static int[] Histograma(Bitmap b)
        {
            var h = new int[256];
            BitmapData d = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                byte[] linha = new byte[d.Stride];
                int n = b.Width * 3;
                for (int y = 0; y < b.Height; y++)
                {
                    Marshal.Copy(d.Scan0 + y * d.Stride, linha, 0, d.Stride);
                    for (int i = 0; i < n; i += 3) h[(linha[i + 2] * 299 + linha[i + 1] * 587 + linha[i] * 114) / 1000]++;
                }
            }
            finally { b.UnlockBits(d); }
            return h;
        }
    }

    // ============================================================ Ajustes da edição rápida
    class Ajustes
    {
        public int Angulo, Brilho, Contraste;
        public int InPreto = 0, InBranco = 255, OutPreto = 0, OutBranco = 255;
        public double Gama = 1.0;
        public byte[] Curva;
        public bool Limiar;
        public int ValorLimiar = 128;
        public RectangleF Recorte = RectangleF.Empty;

        static byte B(double v) { return (byte)Math.Max(0, Math.Min(255, Math.Round(v))); }

        public byte[] Lut()
        {
            var lut = new byte[256];
            double c = Contraste * 2.55;
            double f = (259.0 * (c + 255.0)) / (255.0 * (259.0 - c));
            int ib = Math.Min(InPreto, 254), iw = Math.Max(InBranco, ib + 1);
            for (int x = 0; x < 256; x++)
            {
                double v = (x - ib) / (double)(iw - ib);
                v = Math.Max(0, Math.Min(1, v));
                v = Math.Pow(v, 1.0 / Gama);
                v = OutPreto + v * (OutBranco - OutPreto);
                if (Curva != null) v = Curva[B(v)];
                v = f * (v - 128) + 128 + Brilho * 1.28;
                lut[x] = B(v);
            }
            return lut;
        }
    }

    // ============================================================ PDF (gerador mínimo)
    static class Pdf
    {
        public static void Escrever(string caminho, Bitmap b, string modo, int dpi)
        {
            byte[] dados;
            string filtro, espaco;
            int bits;
            if (modo == "Preto e branco")
            {
                dados = Zlib(BitsPB(b));
                filtro = "/FlateDecode"; espaco = "/DeviceGray"; bits = 1;
            }
            else
            {
                dados = Imagens.Jpeg(b, 90);
                filtro = "/DCTDecode"; espaco = "/DeviceRGB"; bits = 8;
            }

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            double wPt = b.Width * 72.0 / dpi, hPt = b.Height * 72.0 / dpi;
            string W = wPt.ToString("0.##", inv), H = hPt.ToString("0.##", inv);
            string conteudo = "q " + W + " 0 0 " + H + " 0 0 cm /Im0 Do Q";

            var offsets = new List<long>();
            using (var fs = new FileStream(caminho, FileMode.Create))
            {
                Action<string> w = s => { byte[] x = Encoding.ASCII.GetBytes(s); fs.Write(x, 0, x.Length); };
                w("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");
                offsets.Add(fs.Position); w("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
                offsets.Add(fs.Position); w("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
                offsets.Add(fs.Position); w("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + W + " " + H +
                    "] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
                offsets.Add(fs.Position);
                w("4 0 obj\n<< /Type /XObject /Subtype /Image /Width " + b.Width + " /Height " + b.Height +
                  " /ColorSpace " + espaco + " /BitsPerComponent " + bits + " /Filter " + filtro +
                  " /Length " + dados.Length + " >>\nstream\n");
                fs.Write(dados, 0, dados.Length);
                w("\nendstream\nendobj\n");
                offsets.Add(fs.Position);
                w("5 0 obj\n<< /Length " + conteudo.Length + " >>\nstream\n" + conteudo + "\nendstream\nendobj\n");
                long xref = fs.Position;
                w("xref\n0 6\n0000000000 65535 f \n");
                foreach (long o in offsets) w(o.ToString("D10") + " 00000 n \n");
                w("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
            }
        }

        static byte[] BitsPB(Bitmap b)
        {
            int bytesLinha = (b.Width + 7) / 8;
            byte[] saida = new byte[bytesLinha * b.Height];
            var r = new Rectangle(0, 0, b.Width, b.Height);
            BitmapData d = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                byte[] linha = new byte[d.Stride];
                for (int y = 0; y < b.Height; y++)
                {
                    Marshal.Copy(d.Scan0 + y * d.Stride, linha, 0, d.Stride);
                    int baseOut = y * bytesLinha;
                    for (int x = 0; x < b.Width; x++)
                        if (linha[x * 3] > 127) saida[baseOut + (x >> 3)] |= (byte)(0x80 >> (x & 7)); // 1 = branco
                }
            }
            finally { b.UnlockBits(d); }
            return saida;
        }

        static byte[] Zlib(byte[] raw)
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78); ms.WriteByte(0x9C);
                using (var ds = new DeflateStream(ms, CompressionMode.Compress, true))
                    ds.Write(raw, 0, raw.Length);
                uint a = 1, bb = 0;
                foreach (byte x in raw) { a = (a + x) % 65521; bb = (bb + a) % 65521; }
                uint ad = (bb << 16) | a;
                ms.WriteByte((byte)(ad >> 24)); ms.WriteByte((byte)(ad >> 16));
                ms.WriteByte((byte)(ad >> 8)); ms.WriteByte((byte)ad);
                return ms.ToArray();
            }
        }
    }

}
