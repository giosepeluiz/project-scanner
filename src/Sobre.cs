// Janela "Sobre"
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Digitalizar
{
    class Sobre : FormaModerna
    {
        public const string Autor = "Giosepe Luiz dos Santos";
        static int S(int v) { return Tema.S(v); }

        public Sobre()
        {
            Text = "Sobre o Digitalizar";
            Barra.Titulo = Text;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(S(600), S(880));
            MinimumSize = Size;

            var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.BG, AutoScroll = true };
            Controls.Add(corpo);

            var versao = Assembly.GetExecutingAssembly().GetName().Version;
            string ver = versao.Major + "." + versao.Minor + "." + versao.Build;
            DateTime compilado = File.GetLastWriteTime(Application.ExecutablePath);

            // ---- cabeçalho
            int y = S(12);
            var icone = new PictureBox
            {
                Image = Tema.IconeBmp(64), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Tema.BG,
                Left = S(28), Top = y, Width = S(64), Height = S(64)
            };
            corpo.Controls.Add(icone);
            corpo.Controls.Add(Rotulo("Digitalizar", Tema.Fonte(20f, FontStyle.Regular, "Segoe UI Semibold"), Tema.TXT, S(108), y - S(2)));
            corpo.Controls.Add(Rotulo("Versão " + ver, Tema.Fonte(10f), Tema.TXT2, S(110), y + S(38)));
            y += S(84);

            corpo.Controls.Add(Texto(
                "Programa de digitalização para Windows e Linux, com edição rápida da imagem (recorte, rotação, brilho, " +
                "contraste, níveis, curvas e limiar). Funciona como executável portátil, sem instalação.", y, S(66)));
            y += S(74);

            // ---- autoria
            var cartao = Cartao(corpo, y, S(118));
            cartao.Controls.Add(Rotulo("Criado por", Tema.Fonte(9f), Tema.TXT2, S(16), S(12)));
            cartao.Controls.Add(Rotulo(Autor, Tema.Fonte(13f, FontStyle.Regular, "Segoe UI Semibold"), Tema.TXT, S(16), S(30)));
            cartao.Controls.Add(Rotulo("Desenvolvido com a ajuda do Anthropic Claude Opus 5.5", Tema.Fonte(9.5f), Tema.TXT2, S(16), S(62)));
            cartao.Controls.Add(Rotulo("© " + Math.Max(2026, DateTime.Now.Year) + " " + Autor + ". Todos os direitos reservados.", Tema.Fonte(8.5f), Tema.TXT2, S(16), S(88)));
            y += cartao.Height + S(14);

            // ---- redes e contacto
            corpo.Controls.Add(Rotulo("Redes e contacto", Tema.Fonte(11f, FontStyle.Bold, "Segoe UI Semibold"), Tema.TXT, S(28), y));
            y += S(30);
            var redes = Cartao(corpo, y, S(150));
            string[,] links =
            {
                { "Instagram", "instagram.com/giosepeluiz", "https://instagram.com/giosepeluiz" },
                { "GitHub",    "github.com/giosepeluiz",    "https://github.com/giosepeluiz" },
                { "Facebook",  "facebook.com/giosepeluiz",  "https://facebook.com/giosepeluiz" },
                { "E-mail",    "contato@arqueirover.de",    "mailto:contato@arqueirover.de" },
            };
            for (int i = 0; i < links.GetLength(0); i++)
            {
                int ly = S(12) + i * S(33);
                redes.Controls.Add(Rotulo(links[i, 0], Tema.Fonte(9.5f), Tema.TXT2, S(16), ly + S(2)));
                string url = links[i, 2];
                var l = new LinkLabel
                {
                    Text = links[i, 1], Font = Tema.Fonte(10f), LinkColor = Tema.AZUL, ActiveLinkColor = Tema.AZUL_HOVER,
                    VisitedLinkColor = Tema.AZUL, LinkBehavior = LinkBehavior.HoverUnderline, BackColor = Tema.CARD,
                    Left = S(130), Top = ly, AutoSize = true, Cursor = Cursors.Hand
                };
                l.LinkClicked += (s, e) => { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } };
                redes.Controls.Add(l);
            }
            y += redes.Height + S(14);

            // ---- informações técnicas
            corpo.Controls.Add(Rotulo("Informações do software", Tema.Fonte(11f, FontStyle.Bold, "Segoe UI Semibold"), Tema.TXT, S(28), y));
            y += S(30);
            string[,] info =
            {
                { "Versão", ver + " (compilado em " + compilado.ToString("dd/MM/yyyy") + ")" },
                { "Plataforma", Config.Windows ? "Windows 10 / 11 · .NET Framework " + Environment.Version : "Linux · Mono " + Environment.Version },
                { "Sistema", Environment.OSVersion.VersionString + (Environment.Is64BitOperatingSystem ? " · 64 bits" : " · 32 bits") },
                { "Digitalização", Config.Windows ? "WIA (Windows Image Acquisition)" : "SANE (scanimage)" },
                { "Formatos", "JPEG, PNG e PDF" },
                { "Configurações", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Digitalizar") },
            };
            var tec = Cartao(corpo, y, S(12) + info.GetLength(0) * S(28) + S(6));
            for (int i = 0; i < info.GetLength(0); i++)
            {
                int ly = S(12) + i * S(28);
                tec.Controls.Add(Rotulo(info[i, 0], Tema.Fonte(9f), Tema.TXT2, S(16), ly));
                var v = Rotulo(info[i, 1], Tema.Fonte(9f), Tema.TXT, S(130), ly);
                v.AutoSize = false; v.Width = S(380); v.Height = S(22); v.AutoEllipsis = true;
                tec.Controls.Add(v);
            }
            y += tec.Height + S(18);

            // ---- botões
            var copiar = new Botao("Copiar informações", false, "") { Left = S(28), Top = y };
            copiar.Click += (s, e) =>
            {
                var txt = "Digitalizar " + ver + Environment.NewLine + "Criado por " + Autor +
                          " · com a ajuda do Anthropic Claude Opus 5.5" + Environment.NewLine;
                for (int i = 0; i < info.GetLength(0); i++) txt += info[i, 0] + ": " + info[i, 1] + Environment.NewLine;
                try { Clipboard.SetText(txt); copiar.Text = "Copiado ✓"; } catch { }
            };
            corpo.Controls.Add(copiar);
            var fechar = new Botao("Fechar", true) { Width = S(120), Top = y };
            fechar.Left = S(28) + S(528) - fechar.Width;
            fechar.Click += (s, e) => Close();
            corpo.Controls.Add(fechar);
            y += fechar.Height + S(16);
            corpo.Controls.Add(new Label { Left = 0, Top = y, Width = 1, Height = 1, BackColor = Tema.BG });

            FinalizarBarra();
        }

        Panel Cartao(Control pai, int y, int altura)
        {
            var p = new Panel { Left = S(28), Top = y, Width = S(528), Height = altura, BackColor = Tema.CARD };
            pai.Controls.Add(p);
            return p;
        }

        Label Rotulo(string t, Font f, Color cor, int x, int y)
        {
            return new Label { Text = t, Font = f, ForeColor = cor, BackColor = Color.Transparent, Left = x, Top = y, AutoSize = true };
        }

        Label Texto(string t, int y, int altura)
        {
            return new Label { Text = t, Font = Tema.Fonte(9.5f), ForeColor = Tema.TXT2, BackColor = Tema.BG, Left = S(28), Top = y, Width = S(528), Height = altura };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
