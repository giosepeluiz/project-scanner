// Tema (claro / escuro / sistema), cor de destaque e configurações guardadas
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Digitalizar
{
    class CorDestaque
    {
        public string Id, Nome;
        public Color Escuro, Claro;
        public CorDestaque(string id, string nome, int escuro, int claro)
        {
            Id = id; Nome = nome;
            Escuro = Color.FromArgb(255, Color.FromArgb(escuro));
            Claro = Color.FromArgb(255, Color.FromArgb(claro));
        }
    }

    static class Tema
    {
        // ---------------------------------------------------- paleta atual
        public static Color BG, CARD, CARD_HOVER, CONTORNO, BORDA, PREV, TXT, TXT2, DESAB;
        public static Color BOTAO, BOTAO_HOVER, BOTAO_PRESS, BOTAO_BORDA;
        public static Color TRILHO, POLEGAR, ENTRADA, ENTRADA_BORDA, CURVA_BG, CURVA_HIST, CURVA_GRADE, MENU_SEL;
        public static Color AZUL, AZUL_HOVER, AZUL_PRESS, TXT_SOBRE_AZUL;
        public static bool Escuro = true;

        public static readonly CorDestaque[] Paleta =
        {
            new CorDestaque("azul",     "Azul",      0x4cc2ff, 0x005fb8),
            new CorDestaque("turquesa", "Turquesa",  0x3fd6cf, 0x00747a),
            new CorDestaque("verde",    "Verde",     0x6ccb5f, 0x0f7b0f),
            new CorDestaque("lima",     "Lima",      0xb4d455, 0x5a7a00),
            new CorDestaque("amarelo",  "Âmbar",     0xffc83d, 0x9a6400),
            new CorDestaque("laranja",  "Laranja",   0xffa35c, 0xc25700),
            new CorDestaque("vermelho", "Vermelho",  0xff8a80, 0xc42b1c),
            new CorDestaque("rosa",     "Rosa",      0xff8fc8, 0xbf0077),
            new CorDestaque("roxo",     "Roxo",      0xc29bff, 0x6b3fbf),
            new CorDestaque("grafite",  "Grafite",   0xb8c2cc, 0x4a5560),
        };

        // ---------------------------------------------------- escala / fontes / ícone
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

        // No Linux não existem as fontes de ícones do Windows: usa símbolos Unicode equivalentes
        public static string G(string g)
        {
            if (g == null || Config.Windows) return g;
            switch (g)
            {
                case "\uE700": return "\u2630"; // menu ☰
                case "\uE713": return "\u2699"; // opções ⚙
                case "\uE70F": return "\u270E"; // editar ✎
                case "\uE8C8": return "\u2750"; // copiar ❐
                case "\uE72D": return "\u2197"; // compartilhar ↗
                case "\uE946": return "\u24D8"; // sobre ⓘ
                case "\uE73E": return "\u2713"; // marcado ✓
                case "\uE770": return "\u25CE"; // sistema ◎
                default: return null;
            }
        }

        static string fonteIcones;
        public static Font Icones(float tam)
        {
            if (!Config.Windows) return Fonte(tam + 1, FontStyle.Regular, "DejaVu Sans");
            if (fonteIcones == null)
            {
                fonteIcones = "Segoe MDL2 Assets";
                using (var col = new System.Drawing.Text.InstalledFontCollection())
                    foreach (var ff in col.Families)
                        if (ff.Name == "Segoe Fluent Icons") { fonteIcones = ff.Name; break; }
            }
            return Fonte(tam, FontStyle.Regular, fonteIcones);
        }

        static Icon icone;
        public static Icon Icone
        {
            get
            {
                if (icone == null)
                    try
                    {
                        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Digitalizar.ico"))
                            if (s != null) icone = new Icon(s);
                    }
                    catch { }
                return icone;
            }
        }

        static readonly Dictionary<int, Bitmap> iconesBmp = new Dictionary<int, Bitmap>();
        static readonly int[] tamanhosIcone = { 16, 20, 24, 32, 40, 48, 64 };
        public static Bitmap IconeBmp(int tam)
        {
            int t = tamanhosIcone[tamanhosIcone.Length - 1];
            foreach (int x in tamanhosIcone) if (x >= tam) { t = x; break; }
            Bitmap b;
            if (!iconesBmp.TryGetValue(t, out b))
            {
                try
                {
                    using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("ico_" + t + ".png"))
                        b = new Bitmap(s);
                }
                catch { b = new Bitmap(t, t); }
                iconesBmp[t] = b;
            }
            return b;
        }

        // ---------------------------------------------------- utilitários de desenho
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

        public static Color FundoPai(Control c) { return c.Parent != null ? c.Parent.BackColor : BG; }

        public static Color Misturar(Color a, Color b, double t)
        {
            return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }

        static Color C(int rgb) { return Color.FromArgb(255, Color.FromArgb(rgb)); }

        // ---------------------------------------------------- configurações
        public static string ModoTema = "escuro";      // claro | escuro | sistema
        public static string Destaque = "azul";        // id da paleta | sistema

        static string ArquivoConfig
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Digitalizar", "config.ini"); }
        }

        public static void Carregar()
        {
            try
            {
                if (File.Exists(ArquivoConfig))
                    foreach (var l in File.ReadAllLines(ArquivoConfig))
                    {
                        int i = l.IndexOf('=');
                        if (i < 0) continue;
                        string k = l.Substring(0, i).Trim(), v = l.Substring(i + 1).Trim();
                        if (k == "tema") ModoTema = v;
                        else if (k == "destaque") Destaque = v;
                    }
            }
            catch { }
            Calcular();
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (ModoTema != "sistema" && Destaque != "sistema") return;
                foreach (Form f in Application.OpenForms) { f.BeginInvoke((Action)Reaplicar); break; }
            };
        }

        public static void Guardar()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArquivoConfig));
                File.WriteAllLines(ArquivoConfig, new[] { "tema=" + ModoTema, "destaque=" + Destaque });
            }
            catch { }
        }

        public static void Definir(string modo, string destaque)
        {
            ModoTema = modo; Destaque = destaque;
            Guardar();
            Reaplicar();
        }

        static bool SistemaEscuro()
        {
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return v is int && (int)v == 0;
            }
            catch { return false; }
        }

        public static Color DestaqueSistema()
        {
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null);
                if (v is int)
                {
                    int abgr = (int)v;
                    return Color.FromArgb(255, abgr & 0xff, (abgr >> 8) & 0xff, (abgr >> 16) & 0xff);
                }
            }
            catch { }
            return C(0x0078d4);
        }

        static void Calcular()
        {
            Escuro = ModoTema == "sistema" ? SistemaEscuro() : ModoTema != "claro";
            if (Escuro)
            {
                BG = C(0x202020); CARD = C(0x2b2b2b); CARD_HOVER = C(0x323232); CONTORNO = C(0x303030); BORDA = C(0x3a3a3a);
                PREV = C(0x272727); TXT = C(0xffffff); TXT2 = C(0xb5b5b5); DESAB = C(0x6a6a6a);
                BOTAO = C(0x343434); BOTAO_HOVER = C(0x3c3c3c); BOTAO_PRESS = C(0x2e2e2e); BOTAO_BORDA = C(0x444444);
                TRILHO = C(0x606060); POLEGAR = C(0x454545); ENTRADA = C(0x2d2d2d); ENTRADA_BORDA = C(0x484848);
                CURVA_BG = C(0x1a1a1a); CURVA_HIST = C(0x3a3a3a); CURVA_GRADE = C(0x333333); MENU_SEL = C(0x3a3a3a);
            }
            else
            {
                BG = C(0xf3f3f3); CARD = C(0xfbfbfb); CARD_HOVER = C(0xf5f5f5); CONTORNO = C(0xe5e5e5); BORDA = C(0xe0e0e0);
                PREV = C(0xe9e9e9); TXT = C(0x1b1b1b); TXT2 = C(0x5f5f5f); DESAB = C(0xa0a0a0);
                BOTAO = C(0xfdfdfd); BOTAO_HOVER = C(0xf6f6f6); BOTAO_PRESS = C(0xf0f0f0); BOTAO_BORDA = C(0xd8d8d8);
                TRILHO = C(0x8a8a8a); POLEGAR = C(0xffffff); ENTRADA = C(0xffffff); ENTRADA_BORDA = C(0xd0d0d0);
                CURVA_BG = C(0xffffff); CURVA_HIST = C(0xdedede); CURVA_GRADE = C(0xebebeb); MENU_SEL = C(0xeaeaea);
            }

            Color a;
            if (Destaque == "sistema")
            {
                Color s = DestaqueSistema();
                a = Escuro ? Misturar(s, Color.White, 0.45) : Misturar(s, Color.Black, 0.12);
            }
            else
            {
                CorDestaque cd = Paleta[0];
                foreach (var p in Paleta) if (p.Id == Destaque) cd = p;
                a = Escuro ? cd.Escuro : cd.Claro;
            }
            AZUL = a;
            AZUL_HOVER = Escuro ? Misturar(a, Color.White, 0.18) : Misturar(a, Color.White, 0.12);
            AZUL_PRESS = Escuro ? Misturar(a, Color.Black, 0.12) : Misturar(a, Color.Black, 0.15);
            double lum = (0.299 * a.R + 0.587 * a.G + 0.114 * a.B) / 255.0;
            TXT_SOBRE_AZUL = lum > 0.55 ? Color.Black : Color.White;
        }

        // ---------------------------------------------------- aplicar a todas as janelas abertas
        public static void Reaplicar()
        {
            var antigas = new[] { BG, CARD, PREV, TXT, TXT2, ENTRADA };
            Calcular();
            var novas = new[] { BG, CARD, PREV, TXT, TXT2, ENTRADA };
            foreach (Form f in Application.OpenForms)
            {
                Trocar(f, antigas, novas);
                Janela(f);
                f.Invalidate(true);
                var fm = f as FormaModerna;
                if (fm != null) fm.TemaMudou();
            }
        }

        static void Trocar(Control c, Color[] antigas, Color[] novas)
        {
            for (int i = 0; i < antigas.Length; i++)
            {
                if (c.BackColor.ToArgb() == antigas[i].ToArgb()) { c.BackColor = novas[i]; break; }
            }
            for (int i = 0; i < antigas.Length; i++)
            {
                if (c.ForeColor.ToArgb() == antigas[i].ToArgb()) { c.ForeColor = novas[i]; break; }
            }
            foreach (Control f in c.Controls) Trocar(f, antigas, novas);
        }

        // ---------------------------------------------------- integração com o Windows
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] static extern int SetWindowTheme(IntPtr hwnd, string app, string id);

        public static void Janela(Form f)
        {
            if (!f.IsHandleCreated || Environment.OSVersion.Platform != PlatformID.Win32NT) return;
            try
            {
                int v = Escuro ? 1 : 0;
                DwmSetWindowAttribute(f.Handle, 20, ref v, 4);     // modo escuro do contorno
                int canto = 2;
                DwmSetWindowAttribute(f.Handle, 33, ref canto, 4); // cantos arredondados (Windows 11)
            }
            catch { }
            BarrasRolagem(f);
        }

        public static void BarrasRolagem(Control c)
        {
            try
            {
                var sc = c as ScrollableControl;
                if (sc != null && sc.AutoScroll && c.IsHandleCreated)
                    SetWindowTheme(c.Handle, Escuro ? "DarkMode_Explorer" : "Explorer", null);
            }
            catch { }
            foreach (Control f in c.Controls) BarrasRolagem(f);
        }
    }
}
