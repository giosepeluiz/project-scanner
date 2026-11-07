// Janela com barra de título integrada (como o Chrome), mantendo encaixe (snap), sombra e animações do Windows
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Digitalizar
{
    class FormaModerna : Form
    {
        const int WM_NCCALCSIZE = 0x83, WM_NCHITTEST = 0x84, WM_NCMOUSEMOVE = 0xA0, WM_NCLBUTTONDOWN = 0xA1,
                  WM_NCLBUTTONUP = 0xA2, WM_NCLBUTTONDBLCLK = 0xA3, WM_NCMOUSELEAVE = 0x2A2;
        const int HTCLIENT = 1, HTCAPTION = 2, HTMAXBUTTON = 9, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14;

        [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)] struct NCCALCSIZE_PARAMS { public RECT r0, r1, r2; public IntPtr pos; }
        [StructLayout(LayoutKind.Sequential)] struct TRACKMOUSEEVENT { public int cbSize; public int dwFlags; public IntPtr hwnd; public int dwHoverTime; }

        [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr h);
        [DllImport("user32.dll")] static extern int GetSystemMetrics(int i);
        [DllImport("user32.dll")] static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT e);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);

        public BarraTitulo Barra;
        static readonly bool Win = Environment.OSVersion.Platform == PlatformID.Win32NT;
        bool pressMax;

        public FormaModerna()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = Tema.BG;
            ForeColor = Tema.TXT;
            Font = Tema.Fonte(10f);
            if (Tema.Icone != null) Icon = Tema.Icone;
            Barra = new BarraTitulo(this) { Dock = DockStyle.Top };
        }

        /// Adiciona a barra por último, para ela ficar no topo de tudo (docking)
        protected void FinalizarBarra()
        {
            Controls.Add(Barra);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Tema.Janela(this);
            if (Win) SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0004 | 0x0020 | 0x0200); // FRAMECHANGED
        }

        protected override void OnActivated(EventArgs e) { base.OnActivated(e); Barra.Ativa = true; }
        protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Barra.Ativa = false; }
        protected override void OnResize(EventArgs e) { base.OnResize(e); if (Barra != null) Barra.Invalidate(); }

        public virtual void TemaMudou() { Barra.Invalidate(); }

        int FrameY() { return GetSystemMetrics(33) + GetSystemMetrics(92); }

        protected override void WndProc(ref Message m)
        {
            if (!Win) { base.WndProc(ref m); return; }
            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    if (m.WParam != IntPtr.Zero)
                    {
                        var p = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
                        int topo = p.r0.T;
                        base.WndProc(ref m);
                        p = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(m.LParam, typeof(NCCALCSIZE_PARAMS));
                        p.r0.T = topo + (IsZoomed(Handle) ? FrameY() : 0);
                        Marshal.StructureToPtr(p, m.LParam, false);
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM_NCHITTEST:
                    base.WndProc(ref m);
                    if ((int)m.Result == HTCLIENT)
                    {
                        var pt = PointToClient(new Point((short)((long)m.LParam & 0xffff), (short)(((long)m.LParam >> 16) & 0xffff)));
                        bool max = WindowState == FormWindowState.Maximized;
                        int borda = Tema.S(5);
                        if (!max && pt.Y < borda && FormBorderStyle == FormBorderStyle.Sizable)
                            m.Result = (IntPtr)(pt.X < Tema.S(14) ? HTTOPLEFT : pt.X > ClientSize.Width - Tema.S(14) ? HTTOPRIGHT : HTTOP);
                        else if (pt.Y < Barra.Height)
                            m.Result = (IntPtr)(Barra.RetMax.Contains(pt) ? HTMAXBUTTON : HTCAPTION);
                    }
                    return;

                case WM_NCMOUSEMOVE:
                    Barra.HoverMax = (int)m.WParam == HTMAXBUTTON;
                    var t = new TRACKMOUSEEVENT { cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT)), dwFlags = 0x02 | 0x10, hwnd = Handle };
                    TrackMouseEvent(ref t);
                    break;

                case WM_NCMOUSELEAVE:
                    Barra.HoverMax = false; pressMax = false; Barra.PressMax = false;
                    break;

                case WM_NCLBUTTONDOWN:
                case WM_NCLBUTTONDBLCLK:
                    if ((int)m.WParam == HTMAXBUTTON) { pressMax = true; Barra.PressMax = true; m.Result = IntPtr.Zero; return; }
                    break;

                case WM_NCLBUTTONUP:
                    if ((int)m.WParam == HTMAXBUTTON)
                    {
                        if (pressMax) AlternarMaximizar();
                        pressMax = false; Barra.PressMax = false;
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        public void AlternarMaximizar()
        {
            if (!MaximizeBox) return;
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }
    }

    // ---------------------------------------------------------------- barra de título
    class BarraTitulo : Control
    {
        const int HTTRANSPARENT = -1;
        readonly FormaModerna janela;
        public string Titulo = "";
        public bool MostrarIcone = true;
        public int InicioTitulo;                // margem à esquerda (ex.: botão de menu)
        bool ativa = true, hoverMax, pressMax;
        int hover = -1, press = -1;             // 0 = minimizar, 2 = fechar

        public BarraTitulo(FormaModerna f)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            janela = f;
            Height = Tema.S(44);
            InicioTitulo = Tema.S(14);
        }

        public bool Ativa { set { ativa = value; Invalidate(); } }
        public bool HoverMax { set { if (hoverMax != value) { hoverMax = value; Invalidate(); } } }
        public bool PressMax { set { if (pressMax != value) { pressMax = value; Invalidate(); } } }

        int LB { get { return Tema.S(46); } }
        int NBotoes { get { return !Config.Windows ? 0 : janela.MaximizeBox ? 3 : (janela.MinimizeBox ? 2 : 1); } }
        public int LarguraBotoes { get { return LB * NBotoes; } }

        Rectangle Ret(int i)   // i: 0 min, 1 max, 2 fechar
        {
            if (!Config.Windows) return Rectangle.Empty;   // no Linux a janela usa a decoração do sistema
            int x = Width;
            if (i == 2) return new Rectangle(x - LB, 0, LB, Height);
            if (i == 1) return janela.MaximizeBox ? new Rectangle(x - 2 * LB, 0, LB, Height) : Rectangle.Empty;
            if (!janela.MinimizeBox) return Rectangle.Empty;
            return new Rectangle(x - (janela.MaximizeBox ? 3 : 2) * LB, 0, LB, Height);
        }

        public Rectangle RetMax { get { return Ret(1); } }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84) // WM_NCHITTEST
            {
                var pt = PointToClient(new Point((short)((long)m.LParam & 0xffff), (short)(((long)m.LParam >> 16) & 0xffff)));
                bool sobreBotao = Ret(0).Contains(pt) || Ret(2).Contains(pt);
                if (!sobreBotao || pt.Y < Tema.S(5) && janela.WindowState != FormWindowState.Maximized)
                {
                    m.Result = (IntPtr)HTTRANSPARENT;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor = Tema.BG);
            Color txt = ativa ? Tema.TXT : Tema.TXT2;

            int x = InicioTitulo;
            if (!Config.Windows) return;   // título e botões já aparecem na decoração do sistema
            if (MostrarIcone)
            {
                int w = Tema.S(16);
                var ic = Tema.IconeBmp(w);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(ic, x, (Height - w) / 2, w, w);
                x += w + Tema.S(12);
            }
            TextRenderer.DrawText(g, Titulo, Tema.Fonte(9f), new Rectangle(x, 0, Width - x - LarguraBotoes, Height), txt,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            // botões da janela
            bool maxim = janela.WindowState == FormWindowState.Maximized;
            string[] glifos = { "", maxim ? "" : "", "" };
            for (int i = 0; i < 3; i++)
            {
                var r = Ret(i);
                if (r.IsEmpty) continue;
                bool h = i == 1 ? hoverMax : hover == i;
                bool p = i == 1 ? pressMax : press == i;
                Color cor = txt;
                if (i == 2 && (h || p))
                {
                    using (var br = new SolidBrush(p ? Color.FromArgb(0xc8, 0x3b, 0x2e) : Color.FromArgb(0xc4, 0x2b, 0x1c))) g.FillRectangle(br, r);
                    cor = Color.White;
                }
                else if (h || p)
                    using (var br = new SolidBrush(p ? Tema.BOTAO_PRESS : Tema.MENU_SEL)) g.FillRectangle(br, r);
                TextRenderer.DrawText(g, glifos[i], Tema.Icones(7.5f), r, cor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
            }
        }

        int Botao(Point p) { if (Ret(0).Contains(p)) return 0; if (Ret(2).Contains(p)) return 2; return -1; }

        protected override void OnMouseMove(MouseEventArgs e) { int b = Botao(e.Location); if (b != hover) { hover = b; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = -1; press = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { press = Botao(e.Location); Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            int b = Botao(e.Location);
            if (e.Button == MouseButtons.Left && b == press)
            {
                if (b == 0) janela.WindowState = FormWindowState.Minimized;
                else if (b == 2) janela.Close();
            }
            press = -1; Invalidate();
            base.OnMouseUp(e);
        }

        /// Coloca um controle (ex.: engrenagem) à esquerda dos botões da janela
        public void AdicionarDireita(Control c, int indice)
        {
            Controls.Add(c);
            Resize += (s, e) => c.Left = Width - LarguraBotoes - (indice + 1) * (c.Width + Tema.S(2)) - Tema.S(4);
            c.Top = (Height - c.Height) / 2;
            c.Left = Width - LarguraBotoes - (indice + 1) * (c.Width + Tema.S(2)) - Tema.S(4);
        }
    }

    // ---------------------------------------------------------------- botão só com ícone (barra de título / barra lateral)
    class BotaoIcone : Control
    {
        public string Glifo;
        public float Tamanho = 11f;
        public Bitmap Imagem;
        public bool Destacado;
        bool hover, press;
        readonly ToolTip dica = new ToolTip();

        public BotaoIcone(string glifo, string dicaTexto)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Glifo = Tema.G(glifo);
            Size = new Size(Tema.S(40), Tema.S(34));
            Cursor = Cursors.Hand;
            if (!string.IsNullOrEmpty(dicaTexto)) dica.SetToolTip(this, dicaTexto);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Tema.FundoPai(this));
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color f = Destacado ? (press ? Tema.AZUL_PRESS : hover ? Tema.AZUL_HOVER : Tema.AZUL)
                                : (press ? Tema.BOTAO_PRESS : hover ? Tema.MENU_SEL : Tema.FundoPai(this));
            if (Destacado || hover || press)
                using (var p = Tema.Arredondado(new RectangleF(0, 0, Width - 1, Height - 1), Tema.S(5)))
                using (var b = new SolidBrush(f)) g.FillPath(b, p);
            if (Imagem != null)
            {
                int w = Tema.S(20);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(Imagem, (Width - w) / 2, (Height - w) / 2, w, w);
            }
            else
                TextRenderer.DrawText(g, Glifo, Tema.Icones(Tamanho), ClientRectangle, Destacado ? Tema.TXT_SOBRE_AZUL : Tema.TXT,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; press = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { press = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { press = false; Invalidate(); base.OnMouseUp(e); }
        protected override void Dispose(bool disposing) { if (disposing) dica.Dispose(); base.Dispose(disposing); }
    }
}
