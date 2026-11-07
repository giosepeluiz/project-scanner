// Janela principal
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Digitalizar")]
[assembly: AssemblyProduct("Digitalizar")]
[assembly: AssemblyDescription("Digitalização com edição rápida")]
[assembly: AssemblyVersion("2.0.0.0")]

namespace Digitalizar
{
    class Janela : Form
    {
        static int S(int v) { return Tema.S(v); }

        Seletor selScanner, selFonte, selTipo, selModo, selDpi, selPasta;
        Botao btnScan, btnCopiar, btnAbrir, btnPasta, btnEditar;
        BarraProgresso barra;
        Label lblStatus;
        PictureBox preview;
        Label lblVazio;

        List<ScannerInfo> scanners = new List<ScannerInfo>();
        string pasta = Config.PastaPadrao;
        Bitmap ultimaImagem;
        string ultimoArquivo;

        public Janela()
        {
            Text = "Digitalizar";
            BackColor = Tema.BG;
            ForeColor = Color.White;
            Font = Tema.Fonte(10f);
            ClientSize = new Size(S(1100), S(780));
            MinimumSize = new Size(S(860), S(680));
            StartPosition = FormStartPosition.CenterScreen;
            if (Tema.Icone != null) Icon = Tema.Icone;

            Montar();
            Shown += (s, e) => Detectar();
        }

        Seletor NovoSeletor(Control pai, string titulo, string[] itens, string padrao, ref int y)
        {
            var s = new Seletor { Titulo = titulo, Left = S(16), Top = y, Width = S(378) };
            if (itens != null) { s.Itens = itens; s.Selecionar(padrao); }
            pai.Controls.Add(s);
            y += s.Height + S(8);
            return s;
        }

        void Montar()
        {
            var esquerda = new Panel { Dock = DockStyle.Left, Width = S(410), BackColor = Tema.BG, AutoScroll = true };
            var direita = new Panel { Dock = DockStyle.Fill, BackColor = Tema.PREV, Padding = new Padding(S(20)) };
            Controls.Add(direita);
            Controls.Add(esquerda);

            int y = S(16);
            selScanner = NovoSeletor(esquerda, "Scanner", null, null, ref y);
            selScanner.LinkTexto = "Detectar";
            selScanner.LinkClicado += (s, e) => Detectar();

            selFonte = NovoSeletor(esquerda, "Fonte", Config.Fontes, Config.Fontes[0], ref y);
            selTipo = NovoSeletor(esquerda, "Tipo de arquivo", Config.Tipos, Config.TipoPadrao, ref y);
            selModo = NovoSeletor(esquerda, "Modo de cor", Config.Modos, Config.ModoPadrao, ref y);
            selDpi = NovoSeletor(esquerda, "Resolução (DPI)", Config.Resolucoes, Config.ResolucaoPadrao, ref y);

            selPasta = new Seletor
            {
                Titulo = "Salvar arquivo em", Seta = false, CorValor = Tema.AZUL, LinkTexto = "Padrão",
                Left = S(16), Top = y, Width = S(378), Height = S(86)
            };
            selPasta.Clicado += (s, e) => EscolherPasta();
            selPasta.LinkClicado += (s, e) => { pasta = Config.PastaPadrao; AtualizarPasta(); };
            esquerda.Controls.Add(selPasta);
            y += selPasta.Height + S(14);
            AtualizarPasta();

            btnScan = new Botao("Digitalizar", true) { Left = S(16), Top = y, Width = S(378), Height = S(40) };
            btnScan.Click += (s, e) => IniciarScan();
            esquerda.Controls.Add(btnScan);
            y += btnScan.Height + S(10);

            barra = new BarraProgresso { Left = S(16), Top = y, Width = S(378) };
            esquerda.Controls.Add(barra);
            y += S(12);

            lblStatus = new Label { Left = S(16), Top = y, Width = S(378), Height = S(60), ForeColor = Tema.TXT2, Font = Tema.Fonte(9f), BackColor = Tema.BG };
            esquerda.Controls.Add(lblStatus);

            // lado direito
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(52), BackColor = Tema.PREV, Padding = new Padding(0, S(8), 0, 0), WrapContents = false };
            btnEditar = new Botao("✎  Edição rápida", false);
            btnCopiar = new Botao("📋  Copiar imagem", false);
            btnAbrir = new Botao("🖼  Abrir", false);
            btnPasta = new Botao("📁  Abrir pasta", false);
            foreach (var b in new[] { btnEditar, btnCopiar, btnAbrir, btnPasta })
            {
                b.Margin = new Padding(0, 0, S(10), 0);
                acoes.Controls.Add(b);
            }
            btnEditar.Click += (s, e) => Editar();
            btnCopiar.Click += (s, e) => Copiar();
            btnAbrir.Click += (s, e) => Abrir();
            btnPasta.Click += (s, e) => AbrirPasta();

            preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Tema.PREV, Visible = false };
            preview.DoubleClick += (s, e) => Editar();
            lblVazio = new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Tema.TXT2, BackColor = Tema.PREV,
                Text = "A pré-visualização aparece aqui\ndepois de digitalizar.", Font = Tema.Fonte(11f)
            };
            direita.Controls.Add(lblVazio);
            direita.Controls.Add(preview);
            direita.Controls.Add(acoes);

            AcoesAtivas(false);
        }

        void AtualizarPasta()
        {
            string nome = Path.GetFileName(pasta.TrimEnd('\\'));
            selPasta.ValorFixo = string.IsNullOrEmpty(nome) ? pasta : nome;
            selPasta.Subtitulo = pasta;
            selPasta.Invalidate();
        }

        void AcoesAtivas(bool ativo)
        {
            btnEditar.Enabled = btnCopiar.Enabled = btnAbrir.Enabled = btnPasta.Enabled = ativo;
        }

        void Ocupado(bool ocupado, string texto)
        {
            if (texto != null) lblStatus.Text = texto;
            btnScan.Enabled = !ocupado;
            barra.Ativo = ocupado;
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
            scanners = lista;
            selScanner.Itens = lista.Select(s => s.Nome).ToArray();
            if (lista.Count > 0)
            {
                int i = lista.FindIndex(s => s.Nome.ToUpper().Contains("HP") || s.Nome.ToUpper().Contains("DESKJET"));
                selScanner.Indice = -1;
                selScanner.Indice = i >= 0 ? i : 0;
                lblStatus.Text = lista.Count + " scanner(s) encontrado(s). Pronto.";
            }
            else
            {
                selScanner.Indice = -1;
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
            int idx = selScanner.Indice;
            if (idx < 0 || idx >= scanners.Count)
            {
                MessageBox.Show(this, "Selecione um scanner (clique em 'Detectar').", "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var sc = scanners[idx];
            string modo = selModo.Valor;
            int dpi = int.Parse(selDpi.Valor);
            string tipo = selTipo.Valor;
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
                    BeginInvoke((Action)(() => Concluido(img, caminho, "Salvo: ")));
                }
                catch (Exception e)
                {
                    string msg = Wia.MensagemErro(e);
                    BeginInvoke((Action)(() => Falhou(msg)));
                }
            });
        }

        void Concluido(Bitmap img, string caminho, string prefixo)
        {
            Ocupado(false, prefixo + Path.GetFileName(caminho));
            var antiga = ultimaImagem;
            ultimaImagem = img;
            ultimoArquivo = caminho;
            preview.Image = img;
            preview.Visible = true;
            lblVazio.Visible = false;
            if (antiga != null && antiga != img) antiga.Dispose();
            AcoesAtivas(true);
        }

        void Falhou(string msg)
        {
            Ocupado(false, "Falhou: " + msg);
            MessageBox.Show(this, msg, "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void Editar()
        {
            if (ultimaImagem == null) return;
            using (var ed = new Editor(ultimaImagem, ultimoArquivo))
            {
                if (ed.ShowDialog(this) == DialogResult.OK && ed.ResultadoImagem != null)
                    Concluido(ed.ResultadoImagem, ed.ResultadoCaminho, "Editado e salvo: ");
            }
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
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Tema.Escala = g.DpiX / 96f;
            Application.Run(new Janela());
        }
    }
}
