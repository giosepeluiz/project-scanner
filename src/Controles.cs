// Controles com visual Windows 11 (tema escuro)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Digitalizar
{
    static class Tema
    {
        public static readonly Color BG = Color.FromArgb(0x20, 0x20, 0x20);
        public static readonly Color CARD = Color.FromArgb(0x2b, 0x2b, 0x2b);
        public static readonly Color CARD_HOVER = Color.FromArgb(0x32, 0x32, 0x32);
        public static readonly Color BORDA = Color.FromArgb(0x3a, 0x3a, 0x3a);
        public static readonly Color PREV = Color.FromArgb(0x27, 0x27, 0x27);
        public static readonly Color TXT2 = Color.FromArgb(0xb5, 0xb5, 0xb5);
        public static readonly Color AZUL = Color.FromArgb(0x4c, 0xc2, 0xff);
        public static readonly Color AZUL_HOVER = Color.FromArgb(0x7a, 0xd3, 0xff);
        public static readonly Color AZUL_PRESS = Color.FromArgb(0x3a, 0xa8, 0xe0);

        public static float Escala = 1f;
        public static int S(int v) { return (int)Math.Round(v * Escala); }

        static readonly Dictionary<string, Font> fontes = new Dictionary<string, Font>();
        public static Font Fonte(float tam, FontStyle estilo = FontStyle.Regular, string nome = "Segoe UI")
        {
            string k = nome + tam + estilo;
            Font f;
            if (!fontes.TryGetValue(k, out f)) { f = new Font(nome, tam, estilo); fontes[k] = f; }
            return f;
        }

        static Icon icone;
        public static Icon Icone
        {
            get
            {
                if (icone == null)
                {
                    try
                    {
                        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Digitalizar.ico"))
                            if (s != null) icone = new Icon(s);
                    }
                    catch { }
                }
                return icone;
            }
        }

        public static GraphicsPath Arredondado(RectangleF r, float raio)
        {
            var p = new GraphicsPath();
            float d = Math.Max(1f, raio * 2);
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Color FundoPai(Control c)
        {
            return c.Parent != null ? c.Parent.BackColor : BG;
        }

    }

    // ---------------------------------------------------------------- menu escuro
    class CoresEscuras : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Tema.CARD; } }
        public override Color MenuBorder { get { return Color.FromArgb(0x45, 0x45, 0x45); } }
        public override Color MenuItemBorder { get { return Tema.CARD_HOVER; } }
        public override Color MenuItemSelected { get { return Tema.BORDA; } }
        public override Color ImageMarginGradientBegin { get { return Tema.CARD; } }
        public override Color ImageMarginGradientMiddle { get { return Tema.CARD; } }
        public override Color ImageMarginGradientEnd { get { return Tema.CARD; } }
        public override Color CheckBackground { get { return Tema.CARD; } }
        public override Color CheckSelectedBackground { get { return Tema.BORDA; } }
        public override Color CheckPressedBackground { get { return Tema.BORDA; } }
    }

    class RenderEscuro : ToolStripProfessionalRenderer
    {
        public RenderEscuro() : base(new CoresEscuras()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // texto desenhado à mão, centrado na vertical do item
            var r = new Rectangle(Tema.S(22), 0, e.Item.Width - Tema.S(30), e.Item.Height);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, r, e.Item.Enabled ? Color.White : Color.Gray,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            var r = new Rectangle(Point.Empty, e.Item.Size);
            using (var b = new SolidBrush(Tema.CARD)) g.FillRectangle(b, r);
            var item = (ToolStripMenuItem)e.Item;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (e.Item.Selected || item.Checked)
            {
                var c = e.Item.Selected ? Tema.BORDA : Color.FromArgb(0x30, 0x30, 0x30);
                using (var p = Tema.Arredondado(new RectangleF(Tema.S(4), Tema.S(2), r.Width - Tema.S(8), r.Height - Tema.S(4)), Tema.S(4)))
                using (var b = new SolidBrush(c)) g.FillPath(b, p);
            }
            if (item.Checked)
            {
                float h = Tema.S(16);
                var pr = new RectangleF(Tema.S(8), (r.Height - h) / 2f, Tema.S(3), h);
                using (var p = Tema.Arredondado(pr, Tema.S(1) + 0.5f))
                using (var b = new SolidBrush(Tema.AZUL)) g.FillPath(b, p);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e) { }
    }

    // ---------------------------------------------------------------- interruptor (switch)
    class Interruptor : Control
    {
        bool ligado, hover;
        public event EventHandler Alterado;

        public Interruptor(string texto)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = texto;
            Cursor = Cursors.Hand;
            Font = Tema.Fonte(9.5f);
            Height = Tema.S(28);
            Width = Tema.S(56) + TextRenderer.MeasureText(texto, Font).Width;
        }

        public bool Ligado
        {
            get { return ligado; }
            set { if (value == ligado) return; ligado = value; Invalidate(); if (Alterado != null) Alterado(this, EventArgs.Empty); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Tema.S(40), h = Tema.S(20), y = (Height - h) / 2f;
            var r = new RectangleF(0.5f, y, w, h);
            using (var p = Tema.Arredondado(r, h / 2 - 0.01f))
            {
                if (ligado) using (var b = new SolidBrush(hover ? Tema.AZUL_HOVER : Tema.AZUL)) g.FillPath(b, p);
                else
                {
                    using (var b = new SolidBrush(hover ? Color.FromArgb(0x3a, 0x3a, 0x3a) : Color.FromArgb(0x2b, 0x2b, 0x2b))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(0xa0, 0xa0, 0xa0), 1f)) g.DrawPath(pen, p);
                }
            }
            float d = ligado ? Tema.S(12) : Tema.S(10);
            float cx = ligado ? w - h / 2 : h / 2 + 0.5f, cy = y + h / 2;
            using (var b = new SolidBrush(ligado ? Color.Black : Color.FromArgb(0xd0, 0xd0, 0xd0)))
                g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
            TextRenderer.DrawText(g, Text, Font, new Rectangle((int)w + Tema.S(12), 0, Width - (int)w - Tema.S(12), Height),
                Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        protected override void OnClick(EventArgs e) { Ligado = !Ligado; base.OnClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    }

    // ---------------------------------------------------------------- seletor (card clicável)
    class Seletor : Control
    {
        public string Titulo = "";
        public string Subtitulo;
        public string ValorFixo;
        public Color CorValor = Color.White;
        public bool Seta = true;
        public string LinkTexto;
        public event EventHandler LinkClicado;
        public event EventHandler Alterado;
        public event EventHandler Clicado;

        string[] itens = new string[0];
        int indice = -1;
        bool hover, hoverLink;

        public Seletor()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Height = Tema.S(68);
        }

        public string[] Itens
        {
            get { return itens; }
            set { itens = value ?? new string[0]; if (indice >= itens.Length) indice = itens.Length - 1; Invalidate(); }
        }

        public int Indice
        {
            get { return indice; }
            set
            {
                if (value == indice) return;
                indice = value; Invalidate();
                if (Alterado != null) Alterado(this, EventArgs.Empty);
            }
        }

        public string Valor { get { return indice >= 0 && indice < itens.Length ? itens[indice] : null; } }

        public void Selecionar(string v) { Indice = Array.IndexOf(itens, v); }

        Rectangle RetLink()
        {
            if (string.IsNullOrEmpty(LinkTexto)) return Rectangle.Empty;
            var sz = TextRenderer.MeasureText(LinkTexto, Tema.Fonte(9f), Size.Empty, TextFormatFlags.NoPadding);
            return new Rectangle(Width - Tema.S(14) - sz.Width, Tema.S(8), sz.Width, sz.Height + Tema.S(2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = Tema.Arredondado(new RectangleF(0, 0, Width - 1, Height - 1), Tema.S(6)))
            {
                using (var b = new SolidBrush(hover ? Tema.CARD_HOVER : Tema.CARD)) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(0x30, 0x30, 0x30))) g.DrawPath(pen, p);
            }

            var rl = RetLink();
            TextRenderer.DrawText(g, Titulo, Tema.Fonte(9f),
                new Rectangle(Tema.S(14), Tema.S(9), Width - Tema.S(28) - rl.Width, Tema.S(18)), Tema.TXT2,
                TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (rl != Rectangle.Empty)
                TextRenderer.DrawText(g, LinkTexto, Tema.Fonte(9f, hoverLink ? FontStyle.Underline : FontStyle.Regular),
                    rl.Location, hoverLink ? Tema.AZUL_HOVER : Tema.AZUL, TextFormatFlags.NoPadding);

            string valor = ValorFixo ?? Valor ?? "—";
            var rv = new Rectangle(Tema.S(14), Tema.S(30), Width - Tema.S(14) - (Seta ? Tema.S(44) : Tema.S(14)), Tema.S(28));
            TextRenderer.DrawText(g, valor, Tema.Fonte(12.5f), rv, CorValor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (!string.IsNullOrEmpty(Subtitulo))
                TextRenderer.DrawText(g, Subtitulo, Tema.Fonte(8.5f),
                    new Rectangle(Tema.S(14), Tema.S(60), Width - Tema.S(28), Tema.S(18)), Tema.TXT2,
                    TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.PathEllipsis | TextFormatFlags.NoPadding);

            if (Seta)
            {
                float cx = Width - Tema.S(26), cy = rv.Top + rv.Height / 2f;
                float a = Tema.S(5);
                using (var pen = new Pen(Tema.AZUL, Math.Max(1.5f, Tema.Escala * 1.6f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new[] { new PointF(cx - a, cy - a / 2), new PointF(cx, cy + a / 2), new PointF(cx + a, cy - a / 2) });
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; hoverLink = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool h = RetLink().Contains(e.Location);
            if (h != hoverLink) { hoverLink = h; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (RetLink().Contains(e.Location)) { if (LinkClicado != null) LinkClicado(this, EventArgs.Empty); return; }
            if (Seta && itens.Length > 0) AbrirMenu();
            else if (Clicado != null) Clicado(this, EventArgs.Empty);
        }

        void AbrirMenu()
        {
            var m = new ContextMenuStrip
            {
                Renderer = new RenderEscuro(), ShowImageMargin = false, ShowCheckMargin = false,
                BackColor = Tema.CARD, Font = Tema.Fonte(11f), Padding = new Padding(0, Tema.S(4), 0, Tema.S(4))
            };
            for (int i = 0; i < itens.Length; i++)
            {
                int k = i;
                var it = new ToolStripMenuItem(itens[i]) { Checked = i == indice, ForeColor = Color.White, AutoSize = false };
                it.Size = new Size(Width - Tema.S(4), Tema.S(36));
                it.Click += (s, e) => Indice = k;
                m.Items.Add(it);
            }
            m.Closed += (s, e) => { hover = false; Invalidate(); };
            m.Show(this, new Point(0, Height - Tema.S(2)));
        }
    }

    // ---------------------------------------------------------------- botão arredondado
    class Botao : Control
    {
        public bool Primario;
        bool hover, press;

        public Botao(string texto, bool primario)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = texto;
            Primario = primario;
            Cursor = Cursors.Hand;
            Font = Tema.Fonte(10.5f, primario ? FontStyle.Bold : FontStyle.Regular);
            Height = Tema.S(38);
            Width = TextRenderer.MeasureText(texto, Font).Width + Tema.S(30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fundo, txt;
            if (Primario)
            {
                fundo = !Enabled ? Color.FromArgb(0x36, 0x4a, 0x58) : press ? Tema.AZUL_PRESS : hover ? Tema.AZUL_HOVER : Tema.AZUL;
                txt = Enabled ? Color.Black : Color.FromArgb(0x8a, 0x8a, 0x8a);
            }
            else
            {
                fundo = !Enabled ? Color.FromArgb(0x2a, 0x2a, 0x2a) : press ? Color.FromArgb(0x2e, 0x2e, 0x2e) : hover ? Color.FromArgb(0x3c, 0x3c, 0x3c) : Color.FromArgb(0x34, 0x34, 0x34);
                txt = Enabled ? Color.White : Color.FromArgb(0x6a, 0x6a, 0x6a);
            }
            using (var p = Tema.Arredondado(new RectangleF(0, 0, Width - 1, Height - 1), Tema.S(5)))
            {
                using (var b = new SolidBrush(fundo)) g.FillPath(b, p);
                if (!Primario) using (var pen = new Pen(Color.FromArgb(0x44, 0x44, 0x44))) g.DrawPath(pen, p);
            }
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, txt,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; press = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { press = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { press = false; Invalidate(); base.OnMouseUp(e); }
    }

    // ---------------------------------------------------------------- barra de progresso fina
    class BarraProgresso : Control
    {
        readonly Timer timer = new Timer { Interval = 16 };
        float pos;
        bool ativo;

        public BarraProgresso()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Tema.S(4);
            timer.Tick += (s, e) => { pos += 0.012f; if (pos > 1.3f) pos = -0.3f; Invalidate(); };
        }

        public bool Ativo
        {
            get { return ativo; }
            set { ativo = value; pos = -0.3f; timer.Enabled = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            if (!ativo) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float w = Width * 0.3f, x = pos * Width;
            var r = RectangleF.Intersect(new RectangleF(x, 0, w, Height), new RectangleF(0, 0, Width, Height));
            if (r.Width > 1)
                using (var p = Tema.Arredondado(r, Height / 2f - 0.01f))
                using (var b = new SolidBrush(Tema.AZUL)) g.FillPath(b, p);
        }

        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }

    // ---------------------------------------------------------------- deslizador (slider)
    class Deslizador : Control
    {
        public string Titulo = "";
        public int Min = 0, Max = 100, Padrao = 0;
        public Func<int, string> Formato;
        public Func<int, int> Encaixe;
        public event EventHandler Alterado;
        int valor;
        bool arrastando, hover;

        public Deslizador()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Tema.S(48);
            Cursor = Cursors.Hand;
        }

        public int Valor
        {
            get { return valor; }
            set
            {
                int v = Math.Max(Min, Math.Min(Max, value));
                if (v == valor) return;
                valor = v; Invalidate();
                if (Alterado != null) Alterado(this, EventArgs.Empty);
            }
        }

        public void DefinirSemEvento(int v) { valor = Math.Max(Min, Math.Min(Max, v)); Invalidate(); }

        int X0 { get { return Tema.S(9); } }
        int X1 { get { return Width - Tema.S(9); } }
        int YT { get { return Tema.S(33); } }
        float Pos(int v) { return X0 + (v - Min) * (X1 - X0) / (float)Math.Max(1, Max - Min); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            TextRenderer.DrawText(g, Titulo, Tema.Fonte(9.5f), new Point(0, Tema.S(2)), Color.FromArgb(0xd8, 0xd8, 0xd8), TextFormatFlags.NoPadding);
            string txt = Formato != null ? Formato(valor) : valor.ToString();
            var sz = TextRenderer.MeasureText(txt, Tema.Fonte(9.5f), Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, txt, Tema.Fonte(9.5f), new Point(Width - sz.Width, Tema.S(2)),
                valor == Padrao ? Tema.TXT2 : Tema.AZUL, TextFormatFlags.NoPadding);

            float h = Tema.S(4);
            using (var p = Tema.Arredondado(new RectangleF(X0, YT - h / 2, X1 - X0, h), h / 2 - 0.01f))
            using (var b = new SolidBrush(Color.FromArgb(0x60, 0x60, 0x60))) g.FillPath(b, p);

            float zero = (Min < 0 && Max > 0) ? Pos(0) : X0;
            float px = Pos(valor);
            float a = Math.Min(zero, px), w = Math.Abs(px - zero);
            if (w > 0.5f)
                using (var b = new SolidBrush(Tema.AZUL)) g.FillRectangle(b, a, YT - h / 2, w, h);

            float r = Tema.S(10);
            using (var b = new SolidBrush(Color.FromArgb(0x45, 0x45, 0x45))) g.FillEllipse(b, px - r, YT - r, 2 * r, 2 * r);
            using (var pen = new Pen(Color.FromArgb(0x50, 0x50, 0x50))) g.DrawEllipse(pen, px - r, YT - r, 2 * r, 2 * r);
            float ri = arrastando ? Tema.S(4) : hover ? Tema.S(7) : Tema.S(5);
            using (var b = new SolidBrush(Tema.AZUL)) g.FillEllipse(b, px - ri, YT - ri, 2 * ri, 2 * ri);
        }

        void Mover(int x)
        {
            int v = Min + (int)Math.Round((x - X0) / (double)(X1 - X0) * (Max - Min));
            v = Math.Max(Min, Math.Min(Max, v));
            if (Encaixe != null) v = Encaixe(v);
            Valor = v;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { arrastando = true; Mover(e.X); Invalidate(); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e) { if (arrastando) Mover(e.X); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { arrastando = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseDoubleClick(MouseEventArgs e) { Valor = Padrao; base.OnMouseDoubleClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    }

    // ---------------------------------------------------------------- editor de curvas
    class CurvaEditor : Control
    {
        readonly List<Point> pts = new List<Point> { new Point(0, 0), new Point(255, 255) };
        public int[] Histograma;
        public event EventHandler Alterado;
        int arrastando = -1;

        public CurvaEditor() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Cursor = Cursors.Cross; }

        int M { get { return Tema.S(8); } }
        RectangleF Area { get { return new RectangleF(M, M, Width - 2 * M, Height - 2 * M); } }

        PointF ParaTela(double x, double y)
        {
            var a = Area;
            return new PointF((float)(a.X + x / 255.0 * a.Width), (float)(a.Bottom - y / 255.0 * a.Height));
        }

        Point ParaCurva(int x, int y)
        {
            var a = Area;
            int cx = (int)Math.Round((x - a.X) / a.Width * 255), cy = (int)Math.Round((a.Bottom - y) / a.Height * 255);
            return new Point(Math.Max(0, Math.Min(255, cx)), Math.Max(0, Math.Min(255, cy)));
        }

        public bool Identidade
        {
            get { return pts.Count == 2 && pts[0] == new Point(0, 0) && pts[1] == new Point(255, 255); }
        }

        public void Repor(bool evento)
        {
            pts.Clear(); pts.Add(new Point(0, 0)); pts.Add(new Point(255, 255));
            Invalidate();
            if (evento && Alterado != null) Alterado(this, EventArgs.Empty);
        }

        public byte[] Lut()
        {
            int n = pts.Count;
            var x = new double[n]; var y = new double[n];
            for (int i = 0; i < n; i++) { x[i] = pts[i].X; y[i] = pts[i].Y; }
            var d = new double[n - 1]; var m = new double[n];
            for (int i = 0; i < n - 1; i++) d[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
            m[0] = d[0]; m[n - 1] = d[n - 2];
            for (int i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;
            for (int i = 0; i < n - 1; i++)
            {
                if (d[i] == 0) { m[i] = 0; m[i + 1] = 0; continue; }
                double a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
                if (s > 9) { double t = 3 / Math.Sqrt(s); m[i] = t * a * d[i]; m[i + 1] = t * b * d[i]; }
            }
            var lut = new byte[256];
            int seg = 0;
            for (int v = 0; v < 256; v++)
            {
                double r;
                if (v <= x[0]) r = y[0];
                else if (v >= x[n - 1]) r = y[n - 1];
                else
                {
                    while (v > x[seg + 1]) seg++;
                    double h = x[seg + 1] - x[seg], t = (v - x[seg]) / h, t2 = t * t, t3 = t2 * t;
                    r = (2 * t3 - 3 * t2 + 1) * y[seg] + (t3 - 2 * t2 + t) * h * m[seg]
                      + (-2 * t3 + 3 * t2) * y[seg + 1] + (t3 - t2) * h * m[seg + 1];
                }
                lut[v] = (byte)Math.Max(0, Math.Min(255, Math.Round(r)));
            }
            return lut;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var a = Area;
            using (var p = Tema.Arredondado(new RectangleF(0, 0, Width - 1, Height - 1), Tema.S(6)))
            using (var b = new SolidBrush(Color.FromArgb(0x1a, 0x1a, 0x1a))) g.FillPath(b, p);

            if (Histograma != null)
            {
                double max = 0;
                for (int i = 0; i < 256; i++) max = Math.Max(max, Math.Sqrt(Histograma[i]));
                if (max > 0)
                    using (var b = new SolidBrush(Color.FromArgb(0x3a, 0x3a, 0x3a)))
                        for (int i = 0; i < 256; i++)
                        {
                            float hh = (float)(Math.Sqrt(Histograma[i]) / max * a.Height);
                            g.FillRectangle(b, a.X + i / 256f * a.Width, a.Bottom - hh, a.Width / 256f + 0.6f, hh);
                        }
            }

            using (var pen = new Pen(Color.FromArgb(0x33, 0x33, 0x33)))
                for (int i = 1; i < 4; i++)
                {
                    g.DrawLine(pen, a.X + a.Width * i / 4, a.Y, a.X + a.Width * i / 4, a.Bottom);
                    g.DrawLine(pen, a.X, a.Y + a.Height * i / 4, a.Right, a.Y + a.Height * i / 4);
                }
            using (var pen = new Pen(Color.FromArgb(0x50, 0x50, 0x50)) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, ParaTela(0, 0), ParaTela(255, 255));

            var lut = Lut();
            var linha = new PointF[256];
            for (int i = 0; i < 256; i++) linha[i] = ParaTela(i, lut[i]);
            using (var pen = new Pen(Tema.AZUL, Math.Max(1.5f, Tema.Escala * 2))) g.DrawLines(pen, linha);

            float r = Tema.S(5);
            foreach (var p in pts)
            {
                var c = ParaTela(p.X, p.Y);
                g.FillEllipse(Brushes.White, c.X - r, c.Y - r, 2 * r, 2 * r);
                using (var pen = new Pen(Tema.AZUL, 1.5f)) g.DrawEllipse(pen, c.X - r, c.Y - r, 2 * r, 2 * r);
            }
        }

        int Achar(Point m)
        {
            int melhor = -1; double dmin = Tema.S(10);
            for (int i = 0; i < pts.Count; i++)
            {
                var c = ParaTela(pts[i].X, pts[i].Y);
                double d = Math.Sqrt((c.X - m.X) * (c.X - m.X) + (c.Y - m.Y) * (c.Y - m.Y));
                if (d < dmin) { dmin = d; melhor = i; }
            }
            return melhor;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int i = Achar(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                if (i > 0 && i < pts.Count - 1) { pts.RemoveAt(i); Invalidate(); if (Alterado != null) Alterado(this, EventArgs.Empty); }
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (i < 0)
            {
                var c = ParaCurva(e.X, e.Y);
                if (c.X <= 0 || c.X >= 255 || pts.Exists(p => p.X == c.X)) return;
                pts.Add(c);
                pts.Sort((p1, p2) => p1.X.CompareTo(p2.X));
                i = pts.IndexOf(c);
                Invalidate();
                if (Alterado != null) Alterado(this, EventArgs.Empty);
            }
            arrastando = i;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (arrastando < 0) { Cursor = Achar(e.Location) >= 0 ? Cursors.Hand : Cursors.Cross; return; }
            var c = ParaCurva(e.X, e.Y);
            int i = arrastando;
            if (i == 0) c.X = 0;
            else if (i == pts.Count - 1) c.X = 255;
            else c.X = Math.Max(pts[i - 1].X + 1, Math.Min(pts[i + 1].X - 1, c.X));
            if (pts[i] == c) return;
            pts[i] = c;
            Invalidate();
            if (Alterado != null) Alterado(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e) { arrastando = -1; }
    }
}
