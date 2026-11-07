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
[assembly: AssemblyCompany("Giosepe Luiz dos Santos")]
[assembly: AssemblyCopyright("© 2026 Giosepe Luiz dos Santos")]
[assembly: AssemblyVersion("3.1.0.0")]
[assembly: AssemblyFileVersion("3.1.0.0")]

namespace Digitalizar
{
    class Janela : FormaModerna
    {
        static int S(int v) { return Tema.S(v); }

        Seletor selScanner, selFonte, selTamanho, selTipo, selModo, selDpi, selPasta;
        Botao btnScan, btnCopiar, btnAbrir, btnPasta, btnEditar, btnPartilhar;
        BotaoIcone btnMenu, btnOpcoes, btnScanCompacto;
        BarraProgresso barra;
        Label lblStatus;
        PictureBox preview;
        Label lblVazio;
        Panel esquerda, conteudo, compacto;

        List<ScannerInfo> scanners = new List<ScannerInfo>();
        string pasta = Config.PastaPadrao;
        Bitmap ultimaImagem;
        string ultimoArquivo;

        // animação da barra lateral
        readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer { Interval = 10 };
        bool expandida = true;
        int larguraDe, larguraPara;
        DateTime inicioAnim;
        const double DURACAO = 220; // ms
        int LARG_EXP { get { return S(410); } }
        int LARG_COMP { get { return S(64); } }

        public Janela()
        {
            Text = "Digitalizar";
            Barra.Titulo = "Digitalizar";
            Size = new Size(S(1140), S(840));
            MinimumSize = new Size(S(760), S(700));
            StartPosition = FormStartPosition.CenterScreen;

            Montar();
            FinalizarBarra();
            anim.Tick += (s, e) => PassoAnimacao();
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
            // ---- barra de título: menu à esquerda, opções à direita
            btnMenu = new BotaoIcone("", "Recolher / expandir painel") { Left = S(6) };
            btnMenu.Top = (Barra.Height - btnMenu.Height) / 2;
            btnMenu.Click += (s, e) => AlternarPainel();
            Barra.Controls.Add(btnMenu);
            Barra.InicioTitulo = btnMenu.Right + S(8);

            btnOpcoes = new BotaoIcone("", "Opções");
            btnOpcoes.Click += (s, e) => { using (var o = new Opcoes()) o.ShowDialog(this); };
            Barra.AdicionarDireita(btnOpcoes, 0);

            // ---- painéis
            esquerda = new Panel { Dock = DockStyle.Left, Width = LARG_EXP, BackColor = Tema.BG };
            conteudo = new Panel { Left = 0, Top = 0, Width = LARG_EXP, Height = 10, BackColor = Tema.BG, AutoScroll = true, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom };
            compacto = new Panel { Left = 0, Top = 0, Width = LARG_COMP, BackColor = Tema.BG, Visible = false, Dock = DockStyle.Fill };
            esquerda.Controls.Add(conteudo);
            esquerda.Controls.Add(compacto);
            esquerda.Resize += (s, e) => conteudo.Height = esquerda.Height;

            var direita = new Panel { Dock = DockStyle.Fill, BackColor = Tema.PREV, Padding = new Padding(S(20)) };
            Controls.Add(direita);
            Controls.Add(esquerda);

            int y = S(8);
            selScanner = NovoSeletor(conteudo, "Scanner", null, null, ref y);
            selScanner.LinkTexto = "Detectar";
            selScanner.LinkClicado += (s, e) => Detectar();

            selFonte = NovoSeletor(conteudo, "Fonte", Config.Fontes, Config.Fontes[0], ref y);
            selTamanho = NovoSeletor(conteudo, "Tamanho do documento", Config.Tamanhos.Select(t => t.ToString()).ToArray(),
                                     Config.Tamanhos[Config.TamanhoPadrao].ToString(), ref y);
            selTipo = NovoSeletor(conteudo, "Tipo de arquivo", Config.Tipos, Config.TipoPadrao, ref y);
            selModo = NovoSeletor(conteudo, "Modo de cor", Config.Modos, Config.ModoPadrao, ref y);
            selDpi = NovoSeletor(conteudo, "Resolução (DPI)", Config.Resolucoes, Config.ResolucaoPadrao, ref y);

            selPasta = new Seletor
            {
                Titulo = "Salvar arquivo em", Seta = false, CorDestaque = true, LinkTexto = "Padrão",
                Left = S(16), Top = y, Width = S(378), Height = S(86)
            };
            selPasta.Clicado += (s, e) => EscolherPasta();
            selPasta.LinkClicado += (s, e) => { pasta = Config.PastaPadrao; AtualizarPasta(); };
            conteudo.Controls.Add(selPasta);
            y += selPasta.Height + S(14);
            AtualizarPasta();

            btnScan = new Botao("Digitalizar", true) { Left = S(16), Top = y, Width = S(378), Height = S(40) };
            btnScan.Click += (s, e) => IniciarScan();
            conteudo.Controls.Add(btnScan);
            y += btnScan.Height + S(10);

            barra = new BarraProgresso { Left = S(16), Top = y, Width = S(378) };
            conteudo.Controls.Add(barra);
            y += S(12);

            lblStatus = new Label { Left = S(16), Top = y, Width = S(378), Height = S(60), ForeColor = Tema.TXT2, Font = Tema.Fonte(9f), BackColor = Tema.BG };
            conteudo.Controls.Add(lblStatus);

            // ---- painel compacto (barra lateral recolhida)
            btnScanCompacto = new BotaoIcone(null, "Digitalizar") { Left = S(10), Top = S(8), Size = new Size(S(44), S(44)), Imagem = Tema.IconeBmp(32) };
            btnScanCompacto.Click += (s, e) => IniciarScan();
            compacto.Controls.Add(btnScanCompacto);

            // ---- lado direito
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(52), BackColor = Tema.PREV, Padding = new Padding(0, S(8), 0, 0), WrapContents = false, AutoScroll = false };
            btnEditar = new Botao("Edição rápida", false, "");
            btnCopiar = new Botao("Copiar imagem", false, "");
            btnPartilhar = new Botao("Compartilhar", false, "");
            btnAbrir = new Botao("Abrir", false, "");
            btnPasta = new Botao("Abrir pasta", false, "");
            foreach (var b in new[] { btnEditar, btnCopiar, btnPartilhar, btnAbrir, btnPasta })
            {
                b.Margin = new Padding(0, 0, S(10), 0);
                acoes.Controls.Add(b);
            }
            acoes.Resize += (s, e) =>
            {
                var bs = new[] { btnEditar, btnCopiar, btnPartilhar, btnAbrir, btnPasta };
                int total = bs.Sum(b => b.LarguraCompleta + S(10));
                bool so = total + S(24) > acoes.ClientSize.Width;
                foreach (var b in bs) b.SoIcone(so);
            };
            btnEditar.Click += (s, e) => Editar();
            btnCopiar.Click += (s, e) => Copiar();
            btnPartilhar.Click += (s, e) => Partilhar();
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

        // ------------------------------------------------ barra lateral recolhível
        void AlternarPainel()
        {
            expandida = !expandida;
            larguraDe = esquerda.Width;
            larguraPara = expandida ? LARG_EXP : LARG_COMP;
            inicioAnim = DateTime.Now;
            if (expandida) { compacto.Visible = false; conteudo.Visible = true; }
            anim.Start();
        }

        void PassoAnimacao()
        {
            double t = Math.Min(1, (DateTime.Now - inicioAnim).TotalMilliseconds / DURACAO);
            double e = 1 - Math.Pow(1 - t, 3);                       // ease-out cúbico
            esquerda.Width = (int)Math.Round(larguraDe + (larguraPara - larguraDe) * e);
            conteudo.Left = expandida ? (int)((e - 1) * S(40)) : (int)(-e * S(40));   // desliza enquanto recolhe/expande
            if (t >= 1)
            {
                anim.Stop();
                conteudo.Left = 0;
                if (!expandida) { conteudo.Visible = false; compacto.Visible = true; }
            }
        }

        // ------------------------------------------------ estado
        void AtualizarPasta()
        {
            string nome = Path.GetFileName(pasta.TrimEnd('\\'));
            selPasta.ValorFixo = string.IsNullOrEmpty(nome) ? pasta : nome;
            selPasta.Subtitulo = pasta;
            selPasta.Invalidate();
        }

        void AcoesAtivas(bool ativo)
        {
            btnEditar.Enabled = btnCopiar.Enabled = btnPartilhar.Enabled = btnAbrir.Enabled = btnPasta.Enabled = ativo;
        }

        void Ocupado(bool ocupado, string texto)
        {
            if (texto != null) lblStatus.Text = texto;
            btnScan.Enabled = !ocupado;
            btnScanCompacto.Enabled = !ocupado;
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
            var tam = Config.Tamanhos[Math.Max(0, selTamanho.Indice)];

            if (dpi >= 1200 && modo == "Cor" &&
                MessageBox.Show(this, "1200 DPI em cor gera um arquivo muito grande e demora vários minutos. Continuar?",
                    "Digitalizar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            AcoesAtivas(false);
            Ocupado(true, "Digitalizando (" + tam.Nome + ", " + modo + ", " + dpi + " DPI)…");
            EmSegundoPlano(() =>
            {
                try
                {
                    Bitmap img = Wia.Digitalizar(sc.Id, modo, dpi, tam.L, tam.A);
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

        // ------------------------------------------------ ações
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

        void Partilhar()
        {
            if (ultimoArquivo == null || !File.Exists(ultimoArquivo)) return;
            if (!Config.Windows)
            {
                AbrirPasta();
                lblStatus.Text = "Pasta aberta: arraste o arquivo para onde quiser compartilhar.";
                return;
            }
            if (!Partilha.Abrir(Handle, ultimoArquivo))
            {
                MessageBox.Show(this, "Não foi possível abrir a janela de compartilhamento do Windows.\n" +
                    "O arquivo vai ser mostrado no Explorador: clique nele com o botão direito → Compartilhar.",
                    "Compartilhar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AbrirPasta();
            }
        }

        void Abrir()
        {
            if (ultimoArquivo == null || !File.Exists(ultimoArquivo)) return;
            if (Config.Windows) Process.Start(new ProcessStartInfo(ultimoArquivo) { UseShellExecute = true });
            else try { Process.Start("xdg-open", "\"" + ultimoArquivo + "\""); } catch { }
        }

        void AbrirPasta()
        {
            if (!Config.Windows)
            {
                string alvo = ultimoArquivo != null && File.Exists(ultimoArquivo) ? Path.GetDirectoryName(ultimoArquivo) : pasta;
                try { Process.Start("xdg-open", "\"" + alvo + "\""); } catch { }
                return;
            }
            if (ultimoArquivo != null && File.Exists(ultimoArquivo))
                Process.Start("explorer.exe", "/select,\"" + ultimoArquivo + "\"");
            else if (Directory.Exists(pasta))
                Process.Start("explorer.exe", "\"" + pasta + "\"");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1) { using (var f = new Sobre()) f.ShowDialog(this); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) anim.Dispose();
            base.Dispose(disposing);
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
            Tema.Carregar();
            Application.Run(new Janela());
        }
    }
}
