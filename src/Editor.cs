// Edição rápida: recortar, girar, brilho, contraste, níveis, curvas e limiar
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace Digitalizar
{
    // ---------------------------------------------------------------- área da imagem com recorte
    class Tela : Control
    {
        Bitmap img;
        bool modoRecorte;
        RectangleF rec = RectangleF.Empty;
        public event EventHandler RecorteAlterado;

        enum Acao { Nada, Novo, Mover, C0, C1, C2, C3 }
        Acao acao = Acao.Nada;
        PointF iniN;
        RectangleF recIni;

        public Tela()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.PREV;
        }

        public void DefinirImagem(Bitmap b)
        {
            var antiga = img; img = b; Invalidate();
            if (antiga != null) antiga.Dispose();
        }

        public RectangleF Recorte { get { return rec; } set { rec = value; Invalidate(); } }
        public bool ModoRecorte
        {
            get { return modoRecorte; }
            set { modoRecorte = value; Cursor = value ? Cursors.Cross : Cursors.Default; Invalidate(); }
        }

        RectangleF Ajustar(float iw, float ih)
        {
            float m = Tema.S(24);
            float aw = Math.Max(10, Width - 2 * m), ah = Math.Max(10, Height - 2 * m);
            float s = Math.Min(aw / iw, ah / ih);
            float w = iw * s, h = ih * s;
            return new RectangleF((Width - w) / 2, (Height - h) / 2, w, h);
        }

        RectangleF DestinoTotal() { return Ajustar(img.Width, img.Height); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor = Tema.PREV);
            if (img == null) return;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (modoRecorte || rec.IsEmpty)
            {
                var d = DestinoTotal();
                Sombra(g, d);
                g.DrawImage(img, d);
                if (!modoRecorte) return;

                var r = new RectangleF(d.X + rec.X * d.Width, d.Y + rec.Y * d.Height, rec.Width * d.Width, rec.Height * d.Height);
                using (var fora = new Region(d))
                {
                    fora.Exclude(r);
                    using (var b = new SolidBrush(Color.FromArgb(150, 0, 0, 0))) g.FillRegion(b, fora);
                }
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                    for (int i = 1; i < 3; i++)
                    {
                        g.DrawLine(pen, r.X + r.Width * i / 3, r.Y, r.X + r.Width * i / 3, r.Bottom);
                        g.DrawLine(pen, r.X, r.Y + r.Height * i / 3, r.Right, r.Y + r.Height * i / 3);
                    }
                using (var pen = new Pen(Color.White, 1.5f)) g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
                float L = Tema.S(18), T = Tema.S(4);
                using (var pen = new Pen(Color.White, T) { StartCap = LineCap.Square, EndCap = LineCap.Square })
                {
                    float o = T / 2;
                    g.DrawLines(pen, new[] { new PointF(r.X - o, r.Y + L), new PointF(r.X - o, r.Y - o), new PointF(r.X + L, r.Y - o) });
                    g.DrawLines(pen, new[] { new PointF(r.Right - L, r.Y - o), new PointF(r.Right + o, r.Y - o), new PointF(r.Right + o, r.Y + L) });
                    g.DrawLines(pen, new[] { new PointF(r.Right + o, r.Bottom - L), new PointF(r.Right + o, r.Bottom + o), new PointF(r.Right - L, r.Bottom + o) });
                    g.DrawLines(pen, new[] { new PointF(r.X + L, r.Bottom + o), new PointF(r.X - o, r.Bottom + o), new PointF(r.X - o, r.Bottom - L) });
                }
                string info = Math.Round(rec.Width * 100) + "% × " + Math.Round(rec.Height * 100) + "%";
                TextRenderer.DrawText(g, info, Tema.Fonte(9f), new Point((int)r.X + Tema.S(6), (int)r.Bottom + Tema.S(8)), Tema.TXT2);
            }
            else
            {
                var src = new RectangleF(rec.X * img.Width, rec.Y * img.Height, rec.Width * img.Width, rec.Height * img.Height);
                var d = Ajustar(src.Width, src.Height);
                Sombra(g, d);
                g.DrawImage(img, d, src, GraphicsUnit.Pixel);
            }
        }

        static void Sombra(Graphics g, RectangleF d)
        {
            for (int i = 1; i <= 6; i++)
                using (var b = new SolidBrush(Color.FromArgb(14, 0, 0, 0)))
                    g.FillRectangle(b, d.X - i + 3, d.Y - i + 4, d.Width + 2 * i, d.Height + 2 * i);
        }

        PointF Norm(Point p, RectangleF d)
        {
            return new PointF(Math.Max(0, Math.Min(1, (p.X - d.X) / d.Width)), Math.Max(0, Math.Min(1, (p.Y - d.Y) / d.Height)));
        }

        Acao Testar(Point p, RectangleF d)
        {
            var r = new RectangleF(d.X + rec.X * d.Width, d.Y + rec.Y * d.Height, rec.Width * d.Width, rec.Height * d.Height);
            float t = Tema.S(14);
            Func<float, float, bool> perto = (x, y) => Math.Abs(p.X - x) <= t && Math.Abs(p.Y - y) <= t;
            if (perto(r.X, r.Y)) return Acao.C0;
            if (perto(r.Right, r.Y)) return Acao.C1;
            if (perto(r.Right, r.Bottom)) return Acao.C2;
            if (perto(r.X, r.Bottom)) return Acao.C3;
            if (r.Contains(p)) return Acao.Mover;
            return Acao.Novo;
        }

        static RectangleF DePontos(PointF a, PointF b)
        {
            return RectangleF.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!modoRecorte || img == null || e.Button != MouseButtons.Left) return;
            var d = DestinoTotal();
            acao = Testar(e.Location, d);
            iniN = Norm(e.Location, d);
            recIni = rec;
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!modoRecorte || img == null) return;
            var d = DestinoTotal();
            if (acao == Acao.Nada)
            {
                var t = Testar(e.Location, d);
                Cursor = t == Acao.C0 || t == Acao.C2 ? Cursors.SizeNWSE : t == Acao.C1 || t == Acao.C3 ? Cursors.SizeNESW :
                         t == Acao.Mover ? Cursors.SizeAll : Cursors.Cross;
                return;
            }
            var p = Norm(e.Location, d);
            switch (acao)
            {
                case Acao.Novo: rec = DePontos(iniN, p); break;
                case Acao.Mover:
                    float x = Math.Max(0, Math.Min(1 - recIni.Width, recIni.X + p.X - iniN.X));
                    float y = Math.Max(0, Math.Min(1 - recIni.Height, recIni.Y + p.Y - iniN.Y));
                    rec = new RectangleF(x, y, recIni.Width, recIni.Height); break;
                case Acao.C0: rec = DePontos(new PointF(recIni.Right, recIni.Bottom), p); break;
                case Acao.C1: rec = DePontos(new PointF(recIni.X, recIni.Bottom), p); break;
                case Acao.C2: rec = DePontos(new PointF(recIni.X, recIni.Y), p); break;
                case Acao.C3: rec = DePontos(new PointF(recIni.Right, recIni.Y), p); break;
            }
            Invalidate();
            if (RecorteAlterado != null) RecorteAlterado(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (acao == Acao.Nada) return;
            acao = Acao.Nada;
            Capture = false;
            if (rec.Width < 0.01f || rec.Height < 0.01f) rec = recIni.IsEmpty ? new RectangleF(0, 0, 1, 1) : recIni;
            Invalidate();
            if (RecorteAlterado != null) RecorteAlterado(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && img != null) { img.Dispose(); img = null; }
            base.Dispose(disposing);
        }
    }

    // ---------------------------------------------------------------- janela do editor
    class Editor : FormaModerna
    {
        readonly Bitmap original;
        readonly string arquivo;
        readonly int dpi;
        Bitmap proxy, proxyRot;
        int anguloCache = int.MinValue;
        bool alterado, salvando;
        readonly bool soImagem;
        readonly string modo;            // modo do documento: o editor ajusta o original em cor e converte no fim

        public Bitmap ResultadoImagem;
        public string ResultadoCaminho;

        Tela tela;
        Panel lat;
        int y;
        Deslizador dlGirar, dlBrilho, dlContraste, dlInPreto, dlInBranco, dlGama, dlOutPreto, dlOutBranco, dlLimiar;
        CurvaEditor curva;
        Interruptor chkLimiar, swRapido;
        Panel painelRapido;
        TextBox txtPx;
        Botao btnRecortar, btnSemRecorte;
        readonly Timer timer = new Timer { Interval = 35 };

        static readonly CultureInfo INV = CultureInfo.InvariantCulture;
        static int S(int v) { return Tema.S(v); }

        /// soImagem: só devolve a imagem editada (usado nas páginas do PDF, que o programa grava depois)
        public Editor(Bitmap img, string modo, string arquivo, bool soImagem = false)
        {
            this.soImagem = soImagem;
            this.modo = modo;
            original = img;
            this.arquivo = arquivo;
            dpi = (int)Math.Round(img.HorizontalResolution);
            if (dpi < 30) dpi = 300;

            Text = L.T("Edição rápida") + " – " + Path.GetFileName(arquivo);
            Barra.Titulo = Text;
            Font = Tema.Fonte(10f);
            Size = new Size(S(1260), S(880));
            MinimumSize = new Size(S(900), S(600));
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;

            proxy = Imagens.Reduzir(img, 1600);
            Montar();
            FinalizarBarra();
            timer.Tick += (s, e) => { timer.Stop(); Renderizar(); };
            Renderizar();
            alterado = false;
        }

        // ------------------------------------------------ montagem da interface
        void Secao(string titulo)
        {
            y += S(10);
            lat.Controls.Add(new Label
            {
                Text = titulo, Font = Tema.Fonte(11f, FontStyle.Bold, "Segoe UI Semibold"), ForeColor = Tema.TXT,
                Left = S(18), Top = y, AutoSize = true, BackColor = Tema.BG
            });
            y += S(30);
        }

        Deslizador Desl(string titulo, int min, int max, int padrao, Func<int, string> fmt)
        {
            var d = new Deslizador { Titulo = titulo, Min = min, Max = max, Padrao = padrao, Formato = fmt, Left = S(18), Top = y, Width = S(346) };
            d.DefinirSemEvento(padrao);
            d.Alterado += (s, e) => Mudou();
            lat.Controls.Add(d);
            y += d.Height + S(2);
            return d;
        }

        Botao Btn(string texto, bool primario, int x, int largura)
        {
            var b = new Botao(texto, primario) { Left = x, Top = y };
            if (largura > 0) b.Width = largura;
            lat.Controls.Add(b);
            return b;
        }

        static string Sinal(int v) { return v > 0 ? "+" + v : v.ToString(); }

        void Montar()
        {
            tela = new Tela { Dock = DockStyle.Fill };
            tela.RecorteAlterado += (s, e) => { alterado = true; };

            lat = new Panel { Dock = DockStyle.Right, Width = S(400), BackColor = Tema.BG, AutoScroll = true };
            var rodape = new Panel { Dock = DockStyle.Bottom, Height = S(66), BackColor = Tema.BG };
            Controls.Add(tela);
            Controls.Add(lat);
            Controls.Add(rodape);

            y = S(6);

            // Girar
            Secao(L.T("Girar"));
            dlGirar = Desl(L.T("Ângulo"), -180, 180, 0, v => Sinal(v) + "°");
            dlGirar.Encaixe = v => { int r = (int)Math.Round(v / 45.0) * 45; return Math.Abs(v - r) <= 4 ? r : v; };
            var b90a = Btn("↺  90°", false, S(18), S(169));
            b90a.Click += (s, e) => dlGirar.Valor = Normalizar(dlGirar.Valor - 90);
            var b90h = Btn("↻  90°", false, S(195), S(169));
            b90h.Click += (s, e) => dlGirar.Valor = Normalizar(dlGirar.Valor + 90);
            y += b90a.Height + S(4);
            Dica(L.T("Encaixa automaticamente a cada 45°. Duplo clique repõe."));

            // Recortar
            Secao(L.T("Recortar"));
            btnRecortar = Btn("✂  " + L.T("Recortar"), false, S(18), S(169));
            btnRecortar.Click += (s, e) => AlternarRecorte();
            btnSemRecorte = Btn(L.T("Remover recorte"), false, S(195), S(169));
            btnSemRecorte.Click += (s, e) => { tela.Recorte = tela.ModoRecorte ? new RectangleF(0, 0, 1, 1) : RectangleF.Empty; alterado = true; };
            y += btnRecortar.Height + S(4);
            Dica(L.T("Arraste na imagem para marcar a área. Enter conclui."));

            swRapido = new Interruptor(L.T("Recorte rápido")) { Left = S(18), Top = y };
            lat.Controls.Add(swRapido);
            y += swRapido.Height + S(6);

            painelRapido = new Panel { Left = S(18), Top = y, Width = S(346), Height = S(76), BackColor = Tema.BG, Visible = false };
            painelRapido.Controls.Add(new Label
            {
                Text = L.T("Cortar em todas as bordas:"), ForeColor = Tema.TXT2, Font = Tema.Fonte(9f), BackColor = Tema.BG,
                Left = 0, Top = S(2), AutoSize = true
            });
            var caixa = new Panel { Left = 0, Top = S(26), Width = S(96), Height = S(38), BackColor = Tema.ENTRADA };
            caixa.Paint += (s2, e2) =>
            {
                using (var pen = new Pen(Tema.ENTRADA_BORDA)) e2.Graphics.DrawRectangle(pen, 0, 0, caixa.Width - 1, caixa.Height - 1);
                using (var pen = new Pen(txtPx.Focused ? Tema.AZUL : Tema.TXT2, txtPx.Focused ? 2 : 1))
                    e2.Graphics.DrawLine(pen, 1, caixa.Height - 1, caixa.Width - 2, caixa.Height - 1);
            };
            txtPx = new TextBox
            {
                Text = Prefs.ObterInt("recorte_rapido_px", 3).ToString(), BorderStyle = BorderStyle.None, BackColor = Tema.ENTRADA, ForeColor = Tema.TXT,
                Font = Tema.Fonte(11f), Left = S(10), Top = S(8), Width = S(50), TextAlign = HorizontalAlignment.Right, MaxLength = 4
            };
            txtPx.KeyPress += (s2, e2) => { if (!char.IsDigit(e2.KeyChar) && !char.IsControl(e2.KeyChar)) e2.Handled = true; };
            txtPx.KeyDown += (s2, e2) => { if (e2.KeyCode == Keys.Enter) { e2.SuppressKeyPress = true; RecorteRapido(); } };
            txtPx.GotFocus += (s2, e2) => caixa.Invalidate();
            txtPx.LostFocus += (s2, e2) => caixa.Invalidate();
            caixa.Controls.Add(txtPx);
            caixa.Controls.Add(new Label { Text = "px", ForeColor = Tema.TXT2, BackColor = Tema.ENTRADA, Font = Tema.Fonte(10f), Left = S(64), Top = S(9), AutoSize = true });
            painelRapido.Controls.Add(caixa);
            var bAplicar = new Botao(L.T("Aplicar"), true) { Left = S(108), Top = S(26), Width = S(110) };
            bAplicar.Click += (s2, e2) => RecorteRapido();
            painelRapido.Controls.Add(bAplicar);
            lat.Controls.Add(painelRapido);
            swRapido.Alterado += (s2, e2) => MostrarRapido(swRapido.Ligado);

            // Luz
            Secao(L.T("Luz"));
            dlBrilho = Desl(L.T("Brilho"), -100, 100, 0, Sinal);
            dlContraste = Desl(L.T("Contraste"), -100, 100, 0, Sinal);

            // Níveis
            Secao(L.T("Níveis"));
            dlInPreto = Desl(L.T("Entrada – preto"), 0, 254, 0, null);
            dlInBranco = Desl(L.T("Entrada – branco"), 1, 255, 255, null);
            dlGama = Desl(L.T("Tons médios (gama)"), 10, 300, 100, v => (v / 100.0).ToString("0.00", INV));
            dlOutPreto = Desl(L.T("Saída – preto"), 0, 255, 0, null);
            dlOutBranco = Desl(L.T("Saída – branco"), 0, 255, 255, null);

            // Curvas
            Secao(L.T("Curvas"));
            curva = new CurvaEditor { Left = S(18), Top = y, Width = S(346), Height = S(346), Histograma = Imagens.Histograma(proxy) };
            curva.Alterado += (s, e) => Mudou();
            lat.Controls.Add(curva);
            y += curva.Height + S(8);
            Dica(L.T("Clique para adicionar ponto e arraste. Botão direito remove."));
            var bCurva = Btn(L.T("Repor curva"), false, S(18), S(169));
            bCurva.Click += (s, e) => curva.Repor(true);
            y += bCurva.Height + S(4);

            // Limiar
            Secao(L.T("Limiar"));
            chkLimiar = new Interruptor(L.T("Ativar limiar (só preto e branco)")) { Left = S(18), Top = y };
            chkLimiar.Alterado += (s, e) => { dlLimiar.Enabled = chkLimiar.Ligado; Mudou(); };
            lat.Controls.Add(chkLimiar);
            y += S(36);
            dlLimiar = Desl(L.T("Limiar"), 1, 254, 128, null);
            dlLimiar.Enabled = false;

            y += S(16);
            lat.Controls.Add(new Label { Left = 0, Top = y, Height = 1, Width = 1 });

            // Rodapé
            var bRepor = new Botao(L.T("Repor tudo"), false) { Left = S(18), Top = S(14) };
            bRepor.Click += (s, e) => ReporTudo();
            rodape.Controls.Add(bRepor);

            var direita = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, FlowDirection = FlowDirection.RightToLeft, BackColor = Tema.BG,
                Width = S(560), Padding = new Padding(0, S(14), S(12), 0), WrapContents = false
            };
            var bSalvar = new Botao(L.T(soImagem ? "Aplicar" : "Salvar"), true) { Width = S(120), Margin = new Padding(S(8), 0, 0, 0) };
            var bComo = new Botao(L.T("Salvar como…"), false) { Width = S(150), Margin = new Padding(S(8), 0, 0, 0) };
            var bCancelar = new Botao(L.T("Cancelar"), false) { Width = S(120), Margin = new Padding(S(8), 0, 0, 0) };
            bSalvar.Click += (s, e) => Salvar(arquivo);
            bComo.Click += (s, e) => SalvarComo();
            bCancelar.Click += (s, e) => Close();
            direita.Controls.Add(bSalvar);
            if (!soImagem) direita.Controls.Add(bComo);
            direita.Controls.Add(bCancelar);
            rodape.Controls.Add(direita);
        }

        void Dica(string texto)
        {
            lat.Controls.Add(new Label
            {
                Text = texto, ForeColor = Tema.TXT2, Font = Tema.Fonte(8.5f), Left = S(18), Top = y,
                Width = S(346), Height = S(34), BackColor = Tema.BG
            });
            y += S(30);
        }

        static int Normalizar(int v)
        {
            v = ((v + 180) % 360 + 360) % 360 - 180;
            return v;
        }

        // ------------------------------------------------ lógica
        void AlternarRecorte()
        {
            if (!tela.ModoRecorte)
            {
                if (tela.Recorte.IsEmpty) tela.Recorte = new RectangleF(0, 0, 1, 1);
                tela.ModoRecorte = true;
                btnRecortar.Text = "✓  " + L.T("Concluir");
                btnRecortar.Primario = true;
            }
            else
            {
                var r = tela.Recorte;
                if (r.X < 0.001f && r.Y < 0.001f && r.Right > 0.999f && r.Bottom > 0.999f) tela.Recorte = RectangleF.Empty;
                tela.ModoRecorte = false;
                btnRecortar.Text = "✂  " + L.T("Recortar");
                btnRecortar.Primario = false;
            }
            btnRecortar.Invalidate();
        }

        void MostrarRapido(bool mostrar)
        {
            if (painelRapido.Visible == mostrar) return;
            int delta = (painelRapido.Height + S(6)) * (mostrar ? 1 : -1);
            int limite = painelRapido.Top;
            lat.SuspendLayout();
            foreach (Control c in lat.Controls)
                if (c != painelRapido && c.Top > limite) c.Top += delta;
            painelRapido.Visible = mostrar;
            lat.ResumeLayout();
            if (mostrar) txtPx.Focus();
        }

        void RecorteRapido()
        {
            int px;
            if (int.TryParse(txtPx.Text, out px) && px >= 0) Prefs.Definir("recorte_rapido_px", px);
            if (!int.TryParse(txtPx.Text, out px) || px < 0)
            {
                MessageBox.Show(this, L.T("Digite um número de pixels válido."), L.T("Recorte rápido"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // tamanho real (resolução original) da imagem já girada
            double escala = original.Width / (double)proxy.Width;
            double w = proxyRot.Width * escala, h = proxyRot.Height * escala;
            var r = tela.Recorte.IsEmpty ? new RectangleF(0, 0, 1, 1) : tela.Recorte;
            float dx = (float)(px / w), dy = (float)(px / h);
            if (r.Width - 2 * dx < 0.02f || r.Height - 2 * dy < 0.02f)
            {
                MessageBox.Show(this, L.T("O corte é maior que a imagem."), L.T("Recorte rápido"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            tela.Recorte = new RectangleF(r.X + dx, r.Y + dy, r.Width - 2 * dx, r.Height - 2 * dy);
            alterado = true;
        }

        Ajustes Coletar()
        {
            var a = new Ajustes
            {
                Angulo = dlGirar.Valor, Brilho = dlBrilho.Valor, Contraste = dlContraste.Valor,
                InPreto = dlInPreto.Valor, InBranco = dlInBranco.Valor, Gama = dlGama.Valor / 100.0,
                OutPreto = dlOutPreto.Valor, OutBranco = dlOutBranco.Valor,
                Curva = curva.Identidade ? null : curva.Lut(),
                Limiar = chkLimiar.Ligado, ValorLimiar = dlLimiar.Valor
            };
            var r = tela.Recorte;
            if (!r.IsEmpty && !(r.X < 0.001f && r.Y < 0.001f && r.Right > 0.999f && r.Bottom > 0.999f)) a.Recorte = r;
            return a;
        }

        void Mudou()
        {
            alterado = true;
            timer.Stop();
            timer.Start();
        }

        void Renderizar()
        {
            var a = Coletar();
            if (a.Angulo != anguloCache)
            {
                if (proxyRot != null) proxyRot.Dispose();
                proxyRot = Imagens.Rotacionar(proxy, a.Angulo);
                anguloCache = a.Angulo;
            }
            var b = Imagens.Copia24(proxyRot);
            Imagens.AplicarTom(b, a.Lut(), a.Limiar, a.ValorLimiar);
            Imagens.ConverterModo(b, modo);
            tela.DefinirImagem(b);
        }

        Bitmap RenderFinal()
        {
            var a = Coletar();
            Bitmap r = Imagens.Rotacionar(original, a.Angulo);
            if (!a.Recorte.IsEmpty)
            {
                var c = Imagens.Recortar(r, a.Recorte);
                r.Dispose();
                r = c;
            }
            Imagens.AplicarTom(r, a.Lut(), a.Limiar, a.ValorLimiar);
            r.SetResolution(dpi, dpi);
            return r;
        }

        void ReporTudo()
        {
            foreach (var d in new[] { dlGirar, dlBrilho, dlContraste, dlInPreto, dlInBranco, dlGama, dlOutPreto, dlOutBranco, dlLimiar })
                d.DefinirSemEvento(d.Padrao);
            curva.Repor(false);
            chkLimiar.Ligado = false;
            swRapido.Ligado = false;
            txtPx.Text = "3";
            if (tela.ModoRecorte) AlternarRecorte();
            tela.Recorte = RectangleF.Empty;
            Mudou();
        }

        void Salvar(string caminho)
        {
            if (tela.ModoRecorte) AlternarRecorte();
            Cursor = Cursors.WaitCursor;
            try
            {
                Bitmap r = RenderFinal();              // em cor: vira o novo original da página
                if (!soImagem)
                {
                    using (var vista = Imagens.Renderizar(r, modo))   // o arquivo sai no modo do documento
                        Imagens.GuardarEm(vista, caminho, dpi);
                }
                ResultadoImagem = r;
                ResultadoCaminho = soImagem ? null : caminho;
                salvando = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, L.T("Não foi possível salvar:") + "\n" + ex.Message, L.T("Edição rápida"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        void SalvarComo()
        {
            using (var d = new SaveFileDialog())
            {
                d.Title = L.T("Salvar como");
                d.Filter = "JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|PDF (*.pdf)|*.pdf";
                string ext = Path.GetExtension(arquivo).ToLowerInvariant();
                d.FilterIndex = ext == ".png" ? 2 : ext == ".pdf" ? 3 : 1;
                d.InitialDirectory = Path.GetDirectoryName(arquivo);
                d.FileName = Path.GetFileNameWithoutExtension(arquivo) + "_editado";
                d.AddExtension = true;
                d.OverwritePrompt = true;
                if (d.ShowDialog(this) != DialogResult.OK) return;
                string c = d.FileName;
                string e = Path.GetExtension(c).ToLowerInvariant();
                if (e != ".jpg" && e != ".jpeg" && e != ".png" && e != ".pdf")
                    c += d.FilterIndex == 2 ? ".png" : d.FilterIndex == 3 ? ".pdf" : ".jpg";
                Salvar(c);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { Salvar(arquivo); return true; }
            if (keyData == (Keys.Control | Keys.Shift | Keys.S)) { if (!soImagem) SalvarComo(); return true; }
            if (keyData == Keys.Enter && tela.ModoRecorte) { AlternarRecorte(); return true; }
            if (keyData == Keys.Escape)
            {
                if (tela.ModoRecorte) AlternarRecorte(); else Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!salvando && alterado &&
                MessageBox.Show(this, L.T("Descartar as alterações?"), L.T("Edição rápida"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                e.Cancel = true;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                if (proxy != null) proxy.Dispose();
                if (proxyRot != null) proxyRot.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
