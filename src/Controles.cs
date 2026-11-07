// Controles com visual Windows 11 (tema claro/escuro dinâmico)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Digitalizar
{
    abstract class ControleDesenhado : Control
    {
        protected ControleDesenhado()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
    }

    // ---------------------------------------------------------------- menu
    class CoresMenu : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Tema.CARD; } }
        public override Color MenuBorder { get { return Tema.BORDA; } }
        public override Color MenuItemBorder { get { return Tema.MENU_SEL; } }
        public override Color MenuItemSelected { get { return Tema.MENU_SEL; } }
        public override Color ImageMarginGradientBegin { get { return Tema.CARD; } }
        public override Color ImageMarginGradientMiddle { get { return Tema.CARD; } }
        public override Color ImageMarginGradientEnd { get { return Tema.CARD; } }
    }

    class RenderMenu : ToolStripProfessionalRenderer
    {
        public RenderMenu() : base(new CoresMenu()) { RoundedEdges = false; }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var r = new Rectangle(Tema.S(22), 0, e.Item.Width - Tema.S(30), e.Item.Height);
            TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, r, e.Item.Enabled ? Tema.TXT : Tema.DESAB,
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
                var c = e.Item.Selected ? Tema.MENU_SEL : Tema.CARD_HOVER;
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

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var b = new SolidBrush(Tema.CARD)) e.Graphics.FillRectangle(b, 0, 0, e.Item.Width, e.Item.Height);
            using (var p = new Pen(Tema.BORDA)) e.Graphics.DrawLine(p, Tema.S(12), e.Item.Height / 2, e.Item.Width - Tema.S(12), e.Item.Height / 2);
        }
    }

    // ---------------------------------------------------------------- seletor (card clicável)
    class Seletor : ControleDesenhado
    {
        public string Titulo = "";
        public string Subtitulo;
        public string ValorFixo;
        public bool CorDestaque;
        public bool Seta = true;
        public string LinkTexto;
        public event EventHandler LinkClicado;
        public event EventHandler Alterado;
        public event EventHandler Clicado;

        string[] itens = new string[0];
        int indice = -1;
        bool hover, hoverLink;

        public Seletor() { Cursor = Cursors.Hand; Height = Tema.S(68); }

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
                using (var pen = new Pen(Tema.CONTORNO)) g.DrawPath(pen, p);
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
            TextRenderer.DrawText(g, valor, Tema.Fonte(12.5f), rv, CorDestaque ? Tema.AZUL : Tema.TXT,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            if (!string.IsNullOrEmpty(Subtitulo))
                TextRenderer.DrawText(g, Subtitulo, Tema.Fonte(8.5f),
                    new Rectangle(Tema.S(14), Tema.S(60), Width - Tema.S(28), Tema.S(18)), Tema.TXT2,
                    TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.PathEllipsis | TextFormatFlags.NoPadding);

            if (Seta)
            {
                float cx = Width - Tema.S(26), cy = rv.Top + rv.Height / 2f, a = Tema.S(5);
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
                Renderer = new RenderMenu(), ShowImageMargin = false, ShowCheckMargin = false,
                BackColor = Tema.CARD, Font = Tema.Fonte(11f), Padding = new Padding(0, Tema.S(4), 0, Tema.S(4))
            };
            for (int i = 0; i < itens.Length; i++)
            {
                if (itens[i] == "-") { m.Items.Add(new ToolStripSeparator()); continue; }
                int k = i;
                var it = new ToolStripMenuItem(itens[i]) { Checked = i == indice, AutoSize = false };
                it.Size = new Size(Width - Tema.S(4), Tema.S(36));
                it.Click += (s, e) => Indice = k;
                m.Items.Add(it);
            }
            m.MaximumSize = new Size(Width + Tema.S(20), Tema.S(460));
            m.Closed += (s, e) => { hover = false; Invalidate(); };
            m.Show(this, new Point(0, Height - Tema.S(2)));
        }
    }

    // ---------------------------------------------------------------- botão arredondado (com ícone opcional)
    class Botao : ControleDesenhado
    {
        public bool Primario;
        public string Glifo;
        readonly string textoCompleto;
        bool hover, press;
        readonly ToolTip dica = new ToolTip();

        public int LarguraCompleta
        {
            get { return TextRenderer.MeasureText(textoCompleto, Font).Width + Tema.S(30) + (Glifo != null ? Tema.S(26) : 0); }
        }

        /// Mostra só o ícone (janela estreita) ou ícone + texto
        public void SoIcone(bool so)
        {
            if (Glifo == null) return;
            Text = so ? "" : textoCompleto;
            Width = so ? Tema.S(44) : LarguraCompleta;
            dica.SetToolTip(this, so ? textoCompleto : null);
        }

        public Botao(string texto, bool primario, string glifo = null)
        {
            Text = texto;
            textoCompleto = texto;
            Primario = primario;
            Glifo = Tema.G(glifo);
            Cursor = Cursors.Hand;
            Font = Tema.Fonte(10.5f, primario ? FontStyle.Bold : FontStyle.Regular);
            Height = Tema.S(38);
            Width = TextRenderer.MeasureText(texto, Font).Width + Tema.S(30) + (glifo != null ? Tema.S(26) : 0);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fundo, txt;
            if (Primario)
            {
                fundo = !Enabled ? Tema.Misturar(Tema.AZUL, Tema.BG, 0.6) : press ? Tema.AZUL_PRESS : hover ? Tema.AZUL_HOVER : Tema.AZUL;
                txt = Enabled ? Tema.TXT_SOBRE_AZUL : Tema.Misturar(Tema.TXT_SOBRE_AZUL, fundo, 0.5);
            }
            else
            {
                fundo = !Enabled ? Tema.CARD : press ? Tema.BOTAO_PRESS : hover ? Tema.BOTAO_HOVER : Tema.BOTAO;
                txt = Enabled ? Tema.TXT : Tema.DESAB;
            }
            using (var p = Tema.Arredondado(new RectangleF(0, 0, Width - 1, Height - 1), Tema.S(5)))
            {
                using (var b = new SolidBrush(fundo)) g.FillPath(b, p);
                if (!Primario) using (var pen = new Pen(Tema.BOTAO_BORDA)) g.DrawPath(pen, p);
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
            var tt = TextRenderer.MeasureText(Text, Font, Size.Empty, flags);
            int gw = Glifo != null ? Tema.S(26) : 0;
            if (Text.Length == 0 && Glifo != null)
            {
                TextRenderer.DrawText(g, Glifo, Tema.Icones(11f), ClientRectangle, txt, flags | TextFormatFlags.HorizontalCenter);
                return;
            }
            int x = Math.Max(Tema.S(8), (Width - tt.Width - gw) / 2);
            if (Glifo != null)
                TextRenderer.DrawText(g, Glifo, Tema.Icones(11f), new Rectangle(x, 0, Tema.S(18), Height), txt, flags | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x + gw, 0, Width - x - gw, Height), txt, flags | TextFormatFlags.EndEllipsis);
        }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Cursor = Enabled ? Cursors.Hand : Cursors.Default; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; press = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { press = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { press = false; Invalidate(); base.OnMouseUp(e); }
        protected override void Dispose(bool disposing) { if (disposing) dica.Dispose(); base.Dispose(disposing); }
    }

    // ---------------------------------------------------------------- barra de progresso
    class BarraProgresso : ControleDesenhado
    {
        readonly Timer timer = new Timer { Interval = 16 };
        float pos;
        bool ativo;

        public BarraProgresso()
        {
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
            var r = RectangleF.Intersect(new RectangleF(pos * Width, 0, Width * 0.3f, Height), new RectangleF(0, 0, Width, Height));
            if (r.Width > 1)
                using (var p = Tema.Arredondado(r, Height / 2f - 0.01f))
                using (var b = new SolidBrush(Tema.AZUL)) g.FillPath(b, p);
        }

        protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
    }

    // ---------------------------------------------------------------- interruptor (switch)
    class Interruptor : ControleDesenhado
    {
        bool ligado, hover;
        public event EventHandler Alterado;

        public Interruptor(string texto)
        {
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
            using (var p = Tema.Arredondado(new RectangleF(0.5f, y, w, h), h / 2 - 0.01f))
            {
                if (ligado) using (var b = new SolidBrush(hover ? Tema.AZUL_HOVER : Tema.AZUL)) g.FillPath(b, p);
                else
                {
                    using (var b = new SolidBrush(hover ? Tema.CARD_HOVER : Tema.ENTRADA)) g.FillPath(b, p);
                    using (var pen = new Pen(Tema.TXT2, 1f)) g.DrawPath(pen, p);
                }
            }
            float d = ligado ? Tema.S(12) : Tema.S(10);
            float cx = ligado ? w - h / 2 : h / 2 + 0.5f, cy = y + h / 2;
            using (var b = new SolidBrush(ligado ? Tema.TXT_SOBRE_AZUL : Tema.TXT2))
                g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
            TextRenderer.DrawText(g, Text, Font, new Rectangle((int)w + Tema.S(12), 0, Width - (int)w - Tema.S(12), Height),
                Enabled ? Tema.TXT : Tema.DESAB, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        protected override void OnClick(EventArgs e) { Ligado = !Ligado; base.OnClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    }

    // ---------------------------------------------------------------- botão de opção (radio)
    class Opcao : ControleDesenhado
    {
        bool marcada, hover;
        public event EventHandler Escolhida;

        public Opcao(string texto)
        {
            Text = texto;
            Cursor = Cursors.Hand;
            Font = Tema.Fonte(10f);
            Height = Tema.S(32);
            Width = Tema.S(34) + TextRenderer.MeasureText(texto, Font).Width;
        }

        public bool Marcada { get { return marcada; } set { marcada = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float d = Tema.S(20), y = (Height - d) / 2f;
            if (marcada)
            {
                using (var b = new SolidBrush(Tema.AZUL)) g.FillEllipse(b, 0.5f, y, d, d);
                float i = hover ? Tema.S(10) : Tema.S(8);
                using (var b = new SolidBrush(Tema.TXT_SOBRE_AZUL)) g.FillEllipse(b, 0.5f + (d - i) / 2, y + (d - i) / 2, i, i);
            }
            else
            {
                using (var b = new SolidBrush(hover ? Tema.CARD_HOVER : Tema.ENTRADA)) g.FillEllipse(b, 0.5f, y, d, d);
                using (var pen = new Pen(Tema.TXT2)) g.DrawEllipse(pen, 0.5f, y, d, d);
            }
            TextRenderer.DrawText(g, Text, Font, new Rectangle(Tema.S(30), 0, Width - Tema.S(30), Height), Tema.TXT,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        protected override void OnClick(EventArgs e) { if (Escolhida != null) Escolhida(this, EventArgs.Empty); base.OnClick(e); }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
    }

    // ---------------------------------------------------------------- amostra de cor (paleta)
    class Amostra : ControleDesenhado
    {
        public Func<Color> Cor;
        public bool Sistema;
        bool selecionada, hover;
        readonly ToolTip dica = new ToolTip();

        public Amostra(string nome)
        {
            Size = new Size(Tema.S(40), Tema.S(40));
            Cursor = Cursors.Hand;
            dica.SetToolTip(this, nome);
        }

        public bool Selecionada { get { return selecionada; } set { selecionada = value; Invalidate(); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float m = Tema.S(5), d = Width - 2 * m;
            Color c = Cor();
            if (selecionada || hover)
                using (var pen = new Pen(selecionada ? Tema.TXT : Tema.TXT2, Tema.S(2))) g.DrawEllipse(pen, Tema.S(1), Tema.S(1), Width - Tema.S(3), Height - Tema.S(3));
            using (var b = new SolidBrush(c)) g.FillEllipse(b, m, m, d, d);
            if (Sistema)
            {
                double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
                TextRenderer.DrawText(g, Tema.G("\uE770"), Tema.Icones(9f), new Rectangle(0, 0, Width, Height), lum > 0.55 ? Color.Black : Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            else if (selecionada)
            {
                double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
                TextRenderer.DrawText(g, Tema.G("\uE73E"), Tema.Icones(10f), new Rectangle(0, 0, Width, Height), lum > 0.55 ? Color.Black : Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void Dispose(bool disposing) { if (disposing) dica.Dispose(); base.Dispose(disposing); }
    }

    // ---------------------------------------------------------------- deslizador (slider)
    class Deslizador : ControleDesenhado
    {
        public string Titulo = "";
        public int Min = 0, Max = 100, Padrao = 0;
        public Func<int, string> Formato;
        public Func<int, int> Encaixe;
        public event EventHandler Alterado;
        int valor;
        bool arrastando, hover;

        public Deslizador() { Height = Tema.S(48); Cursor = Cursors.Hand; }

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
            Color acento = Enabled ? Tema.AZUL : Tema.DESAB;

            TextRenderer.DrawText(g, Titulo, Tema.Fonte(9.5f), new Point(0, Tema.S(2)), Enabled ? Tema.TXT : Tema.DESAB, TextFormatFlags.NoPadding);
            string txt = Formato != null ? Formato(valor) : valor.ToString();
            var sz = TextRenderer.MeasureText(txt, Tema.Fonte(9.5f), Size.Empty, TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, txt, Tema.Fonte(9.5f), new Point(Width - sz.Width, Tema.S(2)),
                valor == Padrao || !Enabled ? Tema.TXT2 : Tema.AZUL, TextFormatFlags.NoPadding);

            float h = Tema.S(4);
            using (var p = Tema.Arredondado(new RectangleF(X0, YT - h / 2, X1 - X0, h), h / 2 - 0.01f))
            using (var b = new SolidBrush(Tema.TRILHO)) g.FillPath(b, p);

            float zero = (Min < 0 && Max > 0) ? Pos(0) : X0;
            float px = Pos(valor);
            float a = Math.Min(zero, px), w = Math.Abs(px - zero);
            if (w > 0.5f) using (var b = new SolidBrush(acento)) g.FillRectangle(b, a, YT - h / 2, w, h);

            float r = Tema.S(10);
            using (var b = new SolidBrush(Tema.POLEGAR)) g.FillEllipse(b, px - r, YT - r, 2 * r, 2 * r);
            using (var pen = new Pen(Tema.BOTAO_BORDA)) g.DrawEllipse(pen, px - r, YT - r, 2 * r, 2 * r);
            float ri = arrastando ? Tema.S(4) : hover ? Tema.S(7) : Tema.S(5);
            using (var b = new SolidBrush(acento)) g.FillEllipse(b, px - ri, YT - ri, 2 * ri, 2 * ri);
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
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    }

    // ---------------------------------------------------------------- editor de curvas
    class CurvaEditor : ControleDesenhado
    {
        readonly List<Point> pts = new List<Point> { new Point(0, 0), new Point(255, 255) };
        public int[] Histograma;
        public event EventHandler Alterado;
        int arrastando = -1;

        public CurvaEditor() { Cursor = Cursors.Cross; }

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

        public bool Identidade { get { return pts.Count == 2 && pts[0] == new Point(0, 0) && pts[1] == new Point(255, 255); } }

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
            {
                using (var b = new SolidBrush(Tema.CURVA_BG)) g.FillPath(b, p);
                using (var pen = new Pen(Tema.CONTORNO)) g.DrawPath(pen, p);
            }

            if (Histograma != null)
            {
                double max = 0;
                for (int i = 0; i < 256; i++) max = Math.Max(max, Math.Sqrt(Histograma[i]));
                if (max > 0)
                    using (var b = new SolidBrush(Tema.CURVA_HIST))
                        for (int i = 0; i < 256; i++)
                        {
                            float hh = (float)(Math.Sqrt(Histograma[i]) / max * a.Height);
                            g.FillRectangle(b, a.X + i / 256f * a.Width, a.Bottom - hh, a.Width / 256f + 0.6f, hh);
                        }
            }

            using (var pen = new Pen(Tema.CURVA_GRADE))
                for (int i = 1; i < 4; i++)
                {
                    g.DrawLine(pen, a.X + a.Width * i / 4, a.Y, a.X + a.Width * i / 4, a.Bottom);
                    g.DrawLine(pen, a.X, a.Y + a.Height * i / 4, a.Right, a.Y + a.Height * i / 4);
                }
            using (var pen = new Pen(Tema.TRILHO) { DashStyle = DashStyle.Dash })
                g.DrawLine(pen, ParaTela(0, 0), ParaTela(255, 255));

            var lut = Lut();
            var linha = new PointF[256];
            for (int i = 0; i < 256; i++) linha[i] = ParaTela(i, lut[i]);
            using (var pen = new Pen(Tema.AZUL, Math.Max(1.5f, Tema.Escala * 2))) g.DrawLines(pen, linha);

            float r = Tema.S(5);
            foreach (var p in pts)
            {
                var c = ParaTela(p.X, p.Y);
                using (var b = new SolidBrush(Tema.CARD)) g.FillEllipse(b, c.X - r, c.Y - r, 2 * r, 2 * r);
                using (var pen = new Pen(Tema.AZUL, 2f)) g.DrawEllipse(pen, c.X - r, c.Y - r, 2 * r, 2 * r);
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
