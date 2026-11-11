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
[assembly: AssemblyVersion("3.3.0.0")]
[assembly: AssemblyFileVersion("3.3.0.0")]

namespace Digitalizar
{
    class Janela : FormaModerna
    {
        static int S(int v) { return Tema.S(v); }

        Seletor selScanner, selFonte, selTamanho, selTipo, selModo, selDpi, selPasta;
        Botao btnScan, btnCopiar, btnAbrir, btnPasta, btnEditar, btnPartilhar, btnNovo;
        FaixaPaginas faixa;
        Action ajustarBotoes;
        BotaoIcone btnMenu, btnOpcoes, btnScanCompacto;
        BarraProgresso barra;
        Label lblStatus;
        PictureBox preview;
        Label lblVazio;
        Panel esquerda, conteudo, compacto;

        List<ScannerInfo> scanners = new List<ScannerInfo>();
        string pasta = Prefs.Obter("pasta", Config.PastaPadrao);
        Documento doc;                         // digitalização aberta (uma imagem, ou as páginas do PDF)
        string ultimoArquivo { get { return doc == null ? null : doc.Caminho; } }

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
            RestaurarJanela();
            Shown += (s, e) =>
            {
                if (Prefs.Obter("painel", "expandido") == "recolhido") RecolherSemAnimacao();
                Detectar();
                Atualizador.VerificarAutomatico(this);
            };
            FormClosing += (s, e) => GuardarJanela();
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
            btnMenu = new BotaoIcone("", L.T("Recolher / expandir painel")) { Left = S(6) };
            btnMenu.Top = (Barra.Height - btnMenu.Height) / 2;
            btnMenu.Click += (s, e) => AlternarPainel();
            Barra.Controls.Add(btnMenu);
            Barra.InicioTitulo = btnMenu.Right + S(8);

            btnOpcoes = new BotaoIcone("", L.T("Opções"));
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
            selScanner = NovoSeletor(conteudo, L.T("Scanner"), null, null, ref y);
            selScanner.LinkTexto = L.T("Detectar");
            selScanner.LinkClicado += (s, e) => Detectar();
            selScanner.Alterado += (s, e) => { if (selScanner.Indice >= 0 && selScanner.Indice < scanners.Count) Prefs.Definir("scanner", scanners[selScanner.Indice].Nome); };

            selFonte = NovoSeletor(conteudo, L.T("Fonte"), Config.Fontes.Select(L.T).ToArray(), null, ref y);
            selFonte.Indice = 0;
            selTamanho = NovoSeletor(conteudo, L.T("Tamanho do documento"), Config.Tamanhos.Select(t => t.ToString()).ToArray(),
                                     null, ref y);
            selTamanho.Indice = Math.Min(Config.Tamanhos.Length - 1, Math.Max(0, Prefs.ObterInt("tamanho", Config.TamanhoPadrao)));
            selTipo = NovoSeletor(conteudo, L.T("Tipo de arquivo"), Config.Tipos, Prefs.Obter("tipo", Config.TipoPadrao), ref y);
            if (selTipo.Indice < 0) selTipo.Selecionar(Config.TipoPadrao);
            selModo = NovoSeletor(conteudo, L.T("Modo de cor"), Config.Modos.Select(L.T).ToArray(), null, ref y);
            selModo.Indice = Math.Max(0, Array.IndexOf(Config.Modos, Prefs.Obter("modo", Config.ModoPadrao)));
            selDpi = NovoSeletor(conteudo, L.T("Resolução (DPI)"), Config.Resolucoes, Prefs.Obter("dpi", Config.ResolucaoPadrao), ref y);
            if (selDpi.Indice < 0) selDpi.Selecionar(Config.ResolucaoPadrao);
            selTamanho.Alterado += (s, e) => Prefs.Definir("tamanho", selTamanho.Indice);
            selTipo.Alterado += (s, e) => { Prefs.Definir("tipo", selTipo.Valor); AtualizarEstadoPdf(); };
            selModo.Alterado += (s, e) =>
            {
                if (selModo.Indice < 0) return;
                string m = Config.Modos[selModo.Indice];
                Prefs.Definir("modo", m);
                if (doc != null && doc.Modo != m) MudarModoDocumento(m);   // troca o modo depois de digitalizar
            };
            selDpi.Alterado += (s, e) => Prefs.Definir("dpi", selDpi.Valor);

            selPasta = new Seletor
            {
                Titulo = L.T("Salvar arquivo em"), Seta = false, CorDestaque = true, LinkTexto = L.T("Padrão"),
                Left = S(16), Top = y, Width = S(378), Height = S(86)
            };
            selPasta.Clicado += (s, e) => EscolherPasta();
            selPasta.LinkClicado += (s, e) => { pasta = Config.PastaPadrao; Prefs.Definir("pasta", pasta); AtualizarPasta(); };
            conteudo.Controls.Add(selPasta);
            y += selPasta.Height + S(14);
            AtualizarPasta();

            btnScan = new Botao(L.T("Digitalizar (ação)"), true) { Left = S(16), Top = y, Width = S(378), Height = S(40) };
            btnScan.Click += (s, e) => IniciarScan();
            conteudo.Controls.Add(btnScan);
            y += btnScan.Height + S(10);

            barra = new BarraProgresso { Left = S(16), Top = y, Width = S(378) };
            conteudo.Controls.Add(barra);
            y += S(12);

            lblStatus = new Label { Left = S(16), Top = y, Width = S(378), Height = S(60), ForeColor = Tema.TXT2, Font = Tema.Fonte(9f), BackColor = Tema.BG };
            conteudo.Controls.Add(lblStatus);

            // ---- painel compacto (barra lateral recolhida)
            btnScanCompacto = new BotaoIcone(null, L.T("Digitalizar (ação)")) { Left = S(10), Top = S(8), Size = new Size(S(44), S(44)), Imagem = Tema.IconeBmp(32) };
            btnScanCompacto.Click += (s, e) => IniciarScan();
            compacto.Controls.Add(btnScanCompacto);

            // ---- lado direito
            var acoes = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = S(52), BackColor = Tema.PREV, Padding = new Padding(0, S(8), 0, 0), WrapContents = false, AutoScroll = false };
            btnEditar = new Botao(L.T("Edição rápida"), false, "");
            btnCopiar = new Botao(L.T("Copiar imagem"), false, "");
            btnPartilhar = new Botao(L.T("Compartilhar"), false, "");
            btnAbrir = new Botao(L.T("Abrir"), false, "");
            btnPasta = new Botao(L.T("Abrir pasta"), false, "");
            btnNovo = new Botao(L.T("Novo PDF"), false, "\uE7C3") { Visible = false, Margin = new Padding(0, 0, S(10), 0) };
            btnNovo.Click += (s, e) => NovoPdf();
            acoes.Controls.Add(btnNovo);
            foreach (var b in new[] { btnEditar, btnCopiar, btnPartilhar, btnAbrir, btnPasta })
            {
                b.Margin = new Padding(0, 0, S(10), 0);
                acoes.Controls.Add(b);
            }
            ajustarBotoes = () =>
            {
                var bs = new[] { btnNovo, btnEditar, btnCopiar, btnPartilhar, btnAbrir, btnPasta }.Where(b => b.Visible).ToArray();
                int total = bs.Sum(b => b.LarguraCompleta + S(10));
                bool so = total + S(24) > acoes.ClientSize.Width;
                foreach (var b in bs) b.SoIcone(so);
            };
            acoes.Resize += (s, e) => ajustarBotoes();
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
                Text = L.T("A pré-visualização aparece aqui\ndepois de digitalizar."), Font = Tema.Fonte(11f)
            };
            direita.Controls.Add(lblVazio);
            direita.Controls.Add(preview);
            faixa = new FaixaPaginas { Dock = DockStyle.Bottom, Visible = false };
            faixa.PaginaEscolhida += i => EscolherPagina(i);
            faixa.PaginaExcluir += i => ExcluirPagina(i);
            faixa.Adicionar += (s, e) => { if (btnScan.Enabled) IniciarScan(); };
            direita.Controls.Add(faixa);
            direita.Controls.Add(acoes);

            AcoesAtivas(false);
        }

        // ------------------------------------------------ barra lateral recolhível
        void RecolherSemAnimacao()
        {
            expandida = false;
            esquerda.Width = LARG_COMP;
            conteudo.Visible = false;
            compacto.Visible = true;
        }

        void RestaurarJanela()
        {
            var p = Prefs.Obter("janela", "").Split(',');
            int x, y, w, h;
            if (p.Length < 5 || !int.TryParse(p[0], out x) || !int.TryParse(p[1], out y) || !int.TryParse(p[2], out w) || !int.TryParse(p[3], out h)) return;
            var r = new Rectangle(x, y, Math.Max(w, MinimumSize.Width), Math.Max(h, MinimumSize.Height));
            bool visivel = false;
            foreach (var sc in Screen.AllScreens) if (sc.WorkingArea.IntersectsWith(r)) visivel = true;
            if (!visivel) return;
            StartPosition = FormStartPosition.Manual;
            Bounds = r;
            if (p[4] == "max") WindowState = FormWindowState.Maximized;
        }

        public void GuardarAgora() { GuardarJanela(); }

        void GuardarJanela()
        {
            var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (WindowState == FormWindowState.Minimized) return;
            Prefs.Definir("janela", r.X + "," + r.Y + "," + r.Width + "," + r.Height + "," + (WindowState == FormWindowState.Maximized ? "max" : "normal"));
        }

        void AlternarPainel()
        {
            expandida = !expandida;
            Prefs.Definir("painel", expandida ? "expandido" : "recolhido");
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
            btnEditar.Enabled = btnCopiar.Enabled = btnPartilhar.Enabled = btnAbrir.Enabled = btnPasta.Enabled = btnNovo.Enabled = ativo;
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
            Ocupado(true, L.T("Procurando scanners…"));
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
                string salvo = Prefs.Obter("scanner", "");
                int i = lista.FindIndex(s => s.Nome == salvo);
                if (i < 0) i = lista.FindIndex(s => s.Nome.ToUpper().Contains("HP") || s.Nome.ToUpper().Contains("DESKJET"));
                selScanner.Indice = -1;
                selScanner.Indice = i >= 0 ? i : 0;
                lblStatus.Text = L.F("{0} scanner(s) encontrado(s). Pronto.", lista.Count);
            }
            else
            {
                selScanner.Indice = -1;
                lblStatus.Text = erro ?? L.T("Nenhum scanner encontrado. Verifique se a impressora está ligada e conectada (Wi-Fi/USB) e clique em 'Detectar'.");
            }
        }

        void EscolherPasta()
        {
            using (var d = new FolderBrowserDialog())
            {
                d.Description = L.T("Escolher pasta de destino");
                d.SelectedPath = Directory.Exists(pasta) ? pasta : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (d.ShowDialog(this) == DialogResult.OK) { pasta = d.SelectedPath; Prefs.Definir("pasta", pasta); AtualizarPasta(); }
            }
        }

        string ModoSelecionado() { return Config.Modos[Math.Max(0, selModo.Indice)]; }

        void IniciarScan()
        {
            int idx = selScanner.Indice;
            if (idx < 0 || idx >= scanners.Count)
            {
                MessageBox.Show(this, L.T("Selecione um scanner (clique em 'Detectar')."), "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var sc = scanners[idx];
            string modo = ModoSelecionado();
            int dpi = int.Parse(selDpi.Valor);
            string tipo = selTipo.Valor;
            string destino = pasta;
            var tam = Config.Tamanhos[Math.Max(0, selTamanho.Indice)];

            // o scanner sempre digitaliza em cor: assim dá para trocar para cinza ou preto e branco (e voltar) depois
            if (dpi >= 1200 &&
                MessageBox.Show(this, L.T("1200 DPI gera um arquivo muito grande e demora vários minutos. Continuar?"),
                    "Digitalizar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            AcoesAtivas(false);
            Ocupado(true, L.F("Digitalizando ({0}, {1}, {2} DPI)…", L.T(tam.Nome), L.T(modo), dpi));
            EmSegundoPlano(() =>
            {
                try
                {
                    Bitmap img = Wia.Digitalizar(sc.Id, "Cor", dpi, tam.L, tam.A);
                    var pag = new Pagina(img, dpi, modo);
                    BeginInvoke((Action)(() => PaginaPronta(pag, tipo, destino)));
                }
                catch (Exception e)
                {
                    string msg = Wia.MensagemErro(e);
                    BeginInvoke((Action)(() => Falhou(msg)));
                }
            });
        }

        /// A digitalização terminou: entra no documento (nova página do PDF aberto, ou documento novo) e é salva
        void PaginaPronta(Pagina pag, string tipo, string destino)
        {
            Ocupado(false, null);
            preview.Image = null;
            bool somar = tipo == "PDF" && doc != null && doc.EhPdf;
            if (!somar)
            {
                if (doc != null) doc.Dispose();
                doc = new Documento(tipo, ModoSelecionado());
            }
            doc.Adicionar(pag);
            string erro = Gravar(destino);
            Mostrar();
            if (erro != null) { Falhou(erro); return; }
            lblStatus.Text = L.T("Salvo:") + " " + Path.GetFileName(doc.Caminho) + (doc.EhPdf ? " · " + L.F("Página {0} de {1}", doc.Indice + 1, doc.Paginas.Count) : "")
                + "\n" + L.T("Dica: troque o modo de cor para converter esta imagem.");
        }

        string Gravar() { return Gravar(pasta); }

        string Gravar(string destino)
        {
            try { doc.Salvar(destino); return null; }
            catch (Exception e) { return e.Message; }
        }

        /// Atualiza a pré-visualização, a faixa de páginas e os botões conforme o documento aberto
        void Mostrar()
        {
            var img = doc == null ? null : doc.ImagemAtual;
            preview.Image = img;
            preview.Visible = img != null;
            lblVazio.Visible = img == null;
            faixa.Doc = doc;
            AtualizarEstadoPdf();
            faixa.Atualizar();
            if (faixa.Visible) faixa.GarantirVisivel(doc.Indice);
            AcoesAtivas(img != null);
        }

        /// Páginas só funcionam com PDF: o botão vira "Adicionar página" enquanto há um PDF aberto e o tipo PDF está escolhido
        void AtualizarEstadoPdf()
        {
            bool somar = selTipo.Valor == "PDF" && doc != null && doc.EhPdf;
            btnScan.Text = somar ? L.T("Adicionar página") : L.T("Digitalizar (ação)");
            if (faixa != null) faixa.Visible = somar;
            btnNovo.Visible = somar;
            if (ajustarBotoes != null) ajustarBotoes();
        }

        void MudarModoDocumento(string modo)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                preview.Image = null;
                doc.MudarModo(modo);
                string erro = Gravar();
                Mostrar();
                if (erro != null) Falhou(erro);
                else lblStatus.Text = L.T("Modo de cor alterado e salvo:") + " " + Path.GetFileName(doc.Caminho);
            }
            finally { Cursor = Cursors.Default; }
        }

        void EscolherPagina(int i)
        {
            if (doc == null) return;
            preview.Image = null;
            doc.Selecionar(i);
            Mostrar();
            lblStatus.Text = L.F("Página {0} de {1}", doc.Indice + 1, doc.Paginas.Count);
        }

        void ExcluirPagina(int i)
        {
            if (doc == null || doc.Paginas.Count < 2) return;
            if (MessageBox.Show(this, L.F("Excluir a página {0}?", i + 1), L.T("Excluir página"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            preview.Image = null;
            doc.Excluir(i);
            string erro = Gravar();
            Mostrar();
            if (erro != null) Falhou(erro);
            else lblStatus.Text = L.T("Salvo:") + " " + Path.GetFileName(doc.Caminho) + " · " + L.F("Página {0} de {1}", doc.Indice + 1, doc.Paginas.Count);
        }

        /// Fecha o PDF aberto (o arquivo continua salvo) para começar outro
        void NovoPdf()
        {
            if (doc == null) return;
            preview.Image = null;
            doc.Dispose();
            doc = null;
            Mostrar();
            lblStatus.Text = L.T("Pronto para um novo PDF.");
        }

        void Falhou(string msg)
        {
            Ocupado(false, L.T("Falhou:") + " " + msg);
            MessageBox.Show(this, msg, "Digitalizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // ------------------------------------------------ ações
        void Editar()
        {
            if (doc == null || doc.ImagemAtual == null) return;
            bool pdf = doc.EhPdf;
            using (var ed = new Editor(doc.ImagemAtual, doc.Caminho, pdf))
            {
                if (ed.ShowDialog(this) == DialogResult.OK && ed.ResultadoImagem != null)
                {
                    preview.Image = null;
                    doc.SubstituirAtual(ed.ResultadoImagem);
                    string erro = null;
                    if (pdf) erro = Gravar();
                    else if (ed.ResultadoCaminho != null) doc.Caminho = ed.ResultadoCaminho;
                    Mostrar();
                    if (erro != null) Falhou(erro);
                    else lblStatus.Text = L.T("Editado e salvo:") + " " + Path.GetFileName(doc.Caminho);
                }
            }
        }

        void Copiar()
        {
            if (doc == null || doc.ImagemAtual == null) return;
            try { Clipboard.SetImage(doc.ImagemAtual); lblStatus.Text = L.T("Imagem copiada. Cole com Ctrl+V."); }
            catch (Exception e) { MessageBox.Show(this, e.Message, L.T("Copiar imagem"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        void Partilhar()
        {
            if (ultimoArquivo == null || !File.Exists(ultimoArquivo)) return;
            if (!Config.Windows)
            {
                AbrirPasta();
                lblStatus.Text = L.T("Pasta aberta: arraste o arquivo para onde quiser compartilhar.");
                return;
            }
            if (!Partilha.Abrir(Handle, ultimoArquivo))
            {
                MessageBox.Show(this, L.T("Não foi possível abrir a janela de compartilhamento do Windows.") + "\n" +
                    L.T("O arquivo vai ser mostrado no Explorador: clique nele com o botão direito → Compartilhar."),
                    L.T("Compartilhar"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /// Fecha e abre de novo o programa (usado ao trocar de idioma)
        public static void Reiniciar()
        {
            try
            {
                foreach (Form f in Application.OpenForms) { var jp = f as Janela; if (jp != null) jp.GuardarAgora(); }
                string exe = Assembly.GetExecutingAssembly().Location;
                if (Config.Windows) Process.Start(exe);
                else Process.Start("mono", "\"" + exe + "\"");
            }
            catch { }
            Environment.Exit(0);
        }

        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var g = Graphics.FromHwnd(IntPtr.Zero)) Tema.Escala = g.DpiX / 96f;
            Tema.Carregar();
            L.Iniciar();
            Atualizador.LimparAntigos();
            Application.Run(new Janela());
        }
    }
}
