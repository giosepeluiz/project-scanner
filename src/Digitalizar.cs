// Digitalizar - software de digitalização para Windows (WIA)
// Autor: Giosepe Luiz
// Executável portátil: não precisa instalar nada (usa o .NET Framework do Windows).
// Para recompilar no Windows sem instalar nada: execute compilar.bat

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Digitalizar")]
[assembly: AssemblyProduct("Digitalizar")]
[assembly: AssemblyVersion("1.0.0.0")]

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

    // ============================================================ Interface
    class Janela : Form
    {
        static readonly Color BG = Color.FromArgb(0x1f, 0x1f, 0x1f);
        static readonly Color CARD = Color.FromArgb(0x2b, 0x2b, 0x2b);
        static readonly Color PREV = Color.FromArgb(0x26, 0x26, 0x26);
        static readonly Color TXT2 = Color.FromArgb(0xb5, 0xb5, 0xb5);
        static readonly Color AZUL = Color.FromArgb(0x4c, 0x9c, 0xff);

        float escala = 1f;
        int S(int v) { return (int)(v * escala); }

        ComboBox cbScanner, cbFonte, cbTipo, cbModo, cbDpi;
        LinkLabel lnkPasta;
        Button btnScan, btnCopiar, btnAbrir, btnPasta;
        ProgressBar barra;
        Label lblStatus;
        PictureBox preview;
        Label lblVazio;

        string pasta = Config.PastaPadrao;
        Bitmap ultimaImagem;
        string ultimoArquivo;

        public Janela()
        {
            using (var g = CreateGraphics()) escala = g.DpiX / 96f;
            Text = "Digitalizar";
            BackColor = BG;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(S(1000), S(720));
            MinimumSize = new Size(S(820), S(640));
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            Montar();
            Shown += (s, e) => Detectar();
        }

        Panel Card(Control pai, string titulo, ref int y)
        {
            var p = new Panel { BackColor = CARD, Left = S(16), Top = y, Width = S(328), Height = S(66) };
            p.Controls.Add(new Label { Text = titulo, ForeColor = TXT2, Left = S(12), Top = S(6), AutoSize = true, Font = new Font("Segoe UI", 9f) });
            pai.Controls.Add(p);
            y += p.Height + S(10);
            return p;
        }

        ComboBox Combo(Panel card, string[] valores, string padrao, int largura)
        {
            var c = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
                BackColor = CARD, ForeColor = Color.White, Font = new Font("Segoe UI", 11f),
                Left = S(10), Top = S(28), Width = largura > 0 ? largura : S(306)
            };
            if (valores != null) { c.Items.AddRange(valores); c.SelectedItem = padrao; }
            card.Controls.Add(c);
            return c;
        }

        Button Botao(string texto, bool primario)
        {
            var b = new Button
            {
                Text = texto, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                BackColor = primario ? AZUL : CARD, ForeColor = primario ? Color.Black : Color.White,
                Font = new Font("Segoe UI", 10.5f, primario ? FontStyle.Bold : FontStyle.Regular),
                Height = S(40), AutoSize = !primario, Padding = new Padding(S(8), 0, S(8), 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = primario ? Color.FromArgb(0x76, 0xb3, 0xff) : Color.FromArgb(0x3a, 0x3a, 0x3a);
            return b;
        }

        void Montar()
        {
            var esquerda = new Panel { Dock = DockStyle.Left, Width = S(360), BackColor = BG, AutoScroll = true };
            var direita = new Panel { Dock = DockStyle.Fill, BackColor = PREV, Padding = new Padding(S(20)) };
            Controls.Add(direita);
            Controls.Add(esquerda);

            int y = S(16);

            var card = Card(esquerda, "Scanner", ref y);
            cbScanner = Combo(card, null, null, S(220));
            var btnDetectar = new LinkLabel
            {
                Text = "Detectar", LinkColor = AZUL, ActiveLinkColor = Color.White, LinkBehavior = LinkBehavior.HoverUnderline,
                Left = S(240), Top = S(32), AutoSize = true, Font = new Font("Segoe UI", 10f)
            };
            btnDetectar.LinkClicked += (s, e) => Detectar();
            card.Controls.Add(btnDetectar);

            cbFonte = Combo(Card(esquerda, "Fonte", ref y), Config.Fontes, Config.Fontes[0], 0);
            cbTipo = Combo(Card(esquerda, "Tipo de arquivo", ref y), Config.Tipos, Config.TipoPadrao, 0);
            cbModo = Combo(Card(esquerda, "Modo de cor", ref y), Config.Modos, Config.ModoPadrao, 0);
            cbDpi = Combo(Card(esquerda, "Resolução (DPI)", ref y), Config.Resolucoes, Config.ResolucaoPadrao, 0);

            card = Card(esquerda, "Salvar arquivo em", ref y);
            card.Height = S(96); y += S(30);
            lnkPasta = new LinkLabel
            {
                LinkColor = AZUL, ActiveLinkColor = Color.White, Left = S(12), Top = S(28),
                Width = S(306), Height = S(40), Font = new Font("Segoe UI", 9.5f)
            };
            lnkPasta.LinkClicked += (s, e) => EscolherPasta();
            card.Controls.Add(lnkPasta);
            var lnkPadrao = new LinkLabel
            {
                Text = "Restaurar pasta padrão", LinkColor = TXT2, ActiveLinkColor = Color.White,
                Left = S(12), Top = S(70), AutoSize = true, Font = new Font("Segoe UI", 8.5f)
            };
            lnkPadrao.LinkClicked += (s, e) => { pasta = Config.PastaPadrao; AtualizarPasta(); };
            card.Controls.Add(lnkPadrao);
            AtualizarPasta();

            btnScan = Botao("Digitalizar", true);
            btnScan.Left = S(16); btnScan.Top = y; btnScan.Width = S(328);
            btnScan.Click += (s, e) => IniciarScan();
            esquerda.Controls.Add(btnScan);
            y += btnScan.Height + S(10);

            barra = new ProgressBar { Left = S(16), Top = y, Width = S(328), Height = S(6), Style = ProgressBarStyle.Blocks };
            esquerda.Controls.Add(barra);
            y += S(14);

            lblStatus = new Label { Left = S(16), Top = y, Width = S(328), Height = S(60), ForeColor = TXT2, Font = new Font("Segoe UI", 9f) };
            esquerda.Controls.Add(lblStatus);

            // lado direito
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(50), BackColor = PREV, Padding = new Padding(0, S(6), 0, 0) };
            btnCopiar = Botao("📋  Copiar imagem", false);
            btnAbrir = Botao("🖼  Abrir", false);
            btnPasta = Botao("📁  Abrir pasta", false);
            foreach (var b in new[] { btnCopiar, btnAbrir, btnPasta })
            {
                b.Margin = new Padding(0, 0, S(10), 0);
                acoes.Controls.Add(b);
            }
            btnCopiar.Click += (s, e) => Copiar();
            btnAbrir.Click += (s, e) => Abrir();
            btnPasta.Click += (s, e) => AbrirPasta();

            preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = PREV };
            lblVazio = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = TXT2,
                Text = "A pré-visualização aparece aqui\ndepois de digitalizar.", Font = new Font("Segoe UI", 11f)
            };
            direita.Controls.Add(lblVazio);
            direita.Controls.Add(preview);
            direita.Controls.Add(acoes);
            preview.Visible = false;

            AcoesAtivas(false);
        }

        void AtualizarPasta() { lnkPasta.Text = pasta; }

        void AcoesAtivas(bool ativo)
        {
            btnCopiar.Enabled = btnAbrir.Enabled = btnPasta.Enabled = ativo;
            foreach (var b in new[] { btnCopiar, btnAbrir, btnPasta })
                b.ForeColor = ativo ? Color.White : Color.Gray;
        }

        void Ocupado(bool ocupado, string texto)
        {
            if (texto != null) lblStatus.Text = texto;
            btnScan.Enabled = !ocupado;
            btnScan.BackColor = ocupado ? Color.FromArgb(0x3a, 0x5a, 0x80) : AZUL;
            barra.Style = ocupado ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            barra.MarqueeAnimationSpeed = ocupado ? 30 : 0;
            Cursor = ocupado ? Cursors.AppStarting : Cursors.Default;
        }

        void EmSegundoPlano(ThreadStart acao)
        {
            var t = new Thread(acao) { IsBackground = true };
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        void Detectar()
        {
            Ocupado(true, "Procurando scanners…");
            EmSegundoPlano(() =>
            {
                List<ScannerInfo> lista = null; string erro = null;
                try { lista = Wia.Listar(); }
                catch (Exception e) { erro = Wia.MensagemErro(e); }
                BeginInvoke((Action)(() => Detectado(lista ?? new List<ScannerInfo>(), erro)));
            });
        }

        void Detectado(List<ScannerInfo> lista, string erro)
        {
            Ocupado(false, null);
            cbScanner.Items.Clear();
            foreach (var s in lista) cbScanner.Items.Add(s);
            if (lista.Count > 0)
            {
                var hp = lista.FirstOrDefault(s => s.Nome.ToUpper().Contains("HP") || s.Nome.ToUpper().Contains("DESKJET")) ?? lista[0];
                cbScanner.SelectedItem = hp;
                lblStatus.Text = lista.Count + " scanner(s) encontrado(s). Pronto.";
            }
            else
            {
                lblStatus.Text = erro ?? "Nenhum scanner encontrado. Verifique se a impressora está ligada e conectada (Wi-Fi/USB) e clique em 'Detectar'.";
            }
        }

        void EscolherPasta()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = "Escolher pasta de destino";
                d.SelectedPath = Directory.Exists(pasta) ? pasta : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (d.ShowDialog(this) == DialogResult.OK) { pasta = d.SelectedPath; AtualizarPasta(); }
            }
        }

        void IniciarScan()
        {
            var sc = cbScanner.SelectedItem as ScannerInfo;
            if (sc == null)
            {
                MessageBox.Show(this, "Selecione um scanner (clique em 'Detectar').", "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string modo = (string)cbModo.SelectedItem;
            int dpi = int.Parse((string)cbDpi.SelectedItem);
            string tipo = (string)cbTipo.SelectedItem;
            string destino = pasta;

            if (dpi >= 1200 && modo == "Cor" &&
                MessageBox.Show(this, "1200 DPI em cor gera um arquivo muito grande e demora vários minutos. Continuar?",
                    "Digitalizar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            AcoesAtivas(false);
            Ocupado(true, "Digitalizando (" + modo + ", " + dpi + " DPI)…");
            EmSegundoPlano(() =>
            {
                try
                {
                    Bitmap img = Wia.Digitalizar(sc.Id, modo, dpi);
                    BeginInvoke((Action)(() => lblStatus.Text = "Salvando…"));
                    string caminho = Imagens.Guardar(img, destino, tipo, modo, dpi);
                    BeginInvoke((Action)(() => Concluido(img, caminho)));
                }
                catch (Exception e)
                {
                    string msg = Wia.MensagemErro(e);
                    BeginInvoke((Action)(() => Falhou(msg)));
                }
            });
        }

        void Concluido(Bitmap img, string caminho)
        {
            Ocupado(false, "Salvo: " + Path.GetFileName(caminho));
            var antiga = ultimaImagem;
            ultimaImagem = img;
            ultimoArquivo = caminho;
            preview.Image = img;
            preview.Visible = true;
            lblVazio.Visible = false;
            if (antiga != null) antiga.Dispose();
            AcoesAtivas(true);
        }

        void Falhou(string msg)
        {
            Ocupado(false, "Falhou: " + msg);
            MessageBox.Show(this, msg, "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void Copiar()
        {
            if (ultimaImagem == null) return;
            try { Clipboard.SetImage(ultimaImagem); lblStatus.Text = "Imagem copiada. Cole com Ctrl+V."; }
            catch (Exception e) { MessageBox.Show(this, e.Message, "Copiar imagem", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void Abrir()
        {
            if (ultimoArquivo != null && File.Exists(ultimoArquivo))
                Process.Start(new ProcessStartInfo(ultimoArquivo) { UseShellExecute = true });
        }

        void AbrirPasta()
        {
            if (ultimoArquivo != null && File.Exists(ultimoArquivo))
                Process.Start("explorer.exe", "/select,\"" + ultimoArquivo + "\"");
            else if (Directory.Exists(pasta))
                Process.Start("explorer.exe", "\"" + pasta + "\"");
        }
    }

    static class Programa
    {
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Janela());
        }
    }
}
