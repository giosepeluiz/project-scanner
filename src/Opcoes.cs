// Janela de opções: tema e cor de destaque
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Digitalizar
{
    class Opcoes : FormaModerna
    {
        readonly List<Opcao> temas = new List<Opcao>();
        readonly List<Amostra> cores = new List<Amostra>();
        static int S(int v) { return Tema.S(v); }

        public Opcoes()
        {
            Text = L.T("Opções");
            Barra.Titulo = Text;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(S(560), Math.Min(S(900), Screen.PrimaryScreen.WorkingArea.Height - S(30)));
            MinimumSize = Size;

            var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.BG, Padding = new Padding(S(24), S(8), S(24), S(16)), AutoScroll = true };
            Controls.Add(corpo);
            int y = S(8);

            // ---- idioma
            corpo.Controls.Add(Titulo(L.T("Idioma"), y)); y += S(34);
            var selIdioma = new Seletor { Titulo = L.T("Idioma da interface"), Left = S(24), Top = y, Width = S(496) };
            var nomes = new string[L.Codigos.Length];
            for (int i = 0; i < nomes.Length; i++) nomes[i] = L.Nome(L.Codigos[i]);
            selIdioma.Itens = nomes;
            selIdioma.Indice = Math.Max(0, Array.IndexOf(L.Codigos, L.Escolhido));
            corpo.Controls.Add(selIdioma);
            y += selIdioma.Height + S(8);
            var aviso = new Label
            {
                Text = L.T("O novo idioma é aplicado ao reiniciar o programa."), ForeColor = Tema.TXT2, BackColor = Tema.BG,
                Font = Tema.Fonte(9f), Left = S(24), Top = y + S(10), AutoSize = true, Visible = false
            };
            var reiniciar = new Botao(L.T("Reiniciar agora"), true) { Top = y, Visible = false };
            reiniciar.Left = S(520) - reiniciar.Width;
            reiniciar.Click += (s, e) => Programa.Reiniciar();
            corpo.Controls.Add(aviso);
            corpo.Controls.Add(reiniciar);
            selIdioma.Alterado += (s, e) =>
            {
                string cod = L.Codigos[selIdioma.Indice];
                if (cod == L.Separador) { selIdioma.Indice = Math.Max(0, Array.IndexOf(L.Codigos, L.Escolhido)); return; }
                Prefs.Definir("idioma", cod);
                bool muda = cod != L.Escolhido;
                aviso.Visible = reiniciar.Visible = muda;
            };
            y += S(54);

            // ---- tema
            corpo.Controls.Add(Titulo(L.T("Tema"), y)); y += S(34);
            var cartaoTema = new Panel { Left = S(24), Top = y, Width = S(496), Height = S(120), BackColor = Tema.CARD };
            corpo.Controls.Add(cartaoTema);
            string[,] opcoes = { { "claro", L.T("Claro") }, { "escuro", L.T("Escuro") }, { "sistema", L.T(Config.Windows ? "Usar o tema do Windows" : "Usar o tema do sistema") } };
            for (int i = 0; i < 3; i++)
            {
                string id = opcoes[i, 0];
                var o = new Opcao(opcoes[i, 1]) { Left = S(16), Top = S(10) + i * S(34), Tag = id, Marcada = Tema.ModoTema == id };
                o.Escolhida += (s, e) => { Tema.Definir(id, Tema.Destaque); Marcar(); };
                cartaoTema.Controls.Add(o);
                temas.Add(o);
            }
            y += cartaoTema.Height + S(24);

            // ---- cor de destaque
            corpo.Controls.Add(Titulo(L.T("Cor de destaque"), y)); y += S(34);
            var cartaoCor = new Panel { Left = S(24), Top = y, Width = S(496), Height = S(170), BackColor = Tema.CARD };
            corpo.Controls.Add(cartaoCor);
            int col = 0, lin = 0;
            Action<Amostra, string> add = (a, id) =>
            {
                a.Tag = id;
                a.Left = S(14) + col * S(48);
                a.Top = S(14) + lin * S(48);
                a.Click += (s, e) => { Tema.Definir(Tema.ModoTema, id); Marcar(); };
                cartaoCor.Controls.Add(a);
                cores.Add(a);
                if (++col == 9) { col = 0; lin++; }
            };
            foreach (var p in Tema.Paleta)
            {
                var cd = p;
                add(new Amostra(L.T(cd.Nome)) { Cor = () => Tema.Escuro ? cd.Escuro : cd.Claro }, cd.Id);
            }
            add(new Amostra(L.T("Cor de destaque do Windows")) { Cor = () => Tema.DestaqueSistema(), Sistema = true }, "sistema");
            var nota = new Label
            {
                Text = L.T("O tom é ajustado automaticamente para o modo claro ou escuro."),
                ForeColor = Tema.TXT2, BackColor = Tema.CARD, Font = Tema.Fonte(8.5f),
                Left = S(16), Top = S(124), AutoSize = true
            };
            cartaoCor.Controls.Add(nota);
            y += cartaoCor.Height + S(20);

            // ---- atualizações
            corpo.Controls.Add(Titulo(L.T("Atualizações"), y)); y += S(34);
            var cartaoAtu = new Panel { Left = S(24), Top = y, Width = S(496), Height = S(138), BackColor = Tema.CARD };
            corpo.Controls.Add(cartaoAtu);
            var swAuto = new Interruptor(L.T("Verificar automaticamente")) { Left = S(16), Top = S(12), Ligado = Atualizador.AutoLigado };
            swAuto.Alterado += (s, e) => Atualizador.AutoLigado = swAuto.Ligado;
            cartaoAtu.Controls.Add(swAuto);
            var lblAtu = new Label
            {
                Text = L.F("Versão instalada: {0}", Atualizador.VersaoTexto), ForeColor = Tema.TXT2, BackColor = Tema.CARD,
                Font = Tema.Fonte(9.5f), Left = S(16), Top = S(50), Width = S(464), Height = S(40)
            };
            cartaoAtu.Controls.Add(lblAtu);
            var verificar = new Botao(L.T("Verificar agora"), false) { Left = S(16), Top = S(88) };
            verificar.Click += (s, e) =>
            {
                verificar.Enabled = false;
                lblAtu.Text = L.T("Procurando atualizações…");
                var t = new Thread(() =>
                {
                    InfoAtualizacao info = null; string erro = null;
                    try { info = Atualizador.Consultar(); } catch (Exception ex) { erro = ex.Message; }
                    try
                    {
                        BeginInvoke((Action)(() =>
                        {
                            verificar.Enabled = true;
                            if (erro != null) lblAtu.Text = L.T("Não foi possível verificar as atualizações.") + "\n" + erro;
                            else if (info.Nova)
                            {
                                lblAtu.Text = L.F("Nova versão disponível: {0}", info.Versao);
                                Atualizador.Mostrar(this, info);
                            }
                            else lblAtu.Text = L.F("Você já está na versão mais recente ({0}).", Atualizador.VersaoTexto);
                        }));
                    }
                    catch { }
                }) { IsBackground = true };
                t.Start();
            };
            cartaoAtu.Controls.Add(verificar);
            y += cartaoAtu.Height + S(20);

            var fechar = new Botao(L.T("Fechar"), true) { Width = S(120), Top = y, Left = S(400) };
            fechar.Click += (s, e) => Close();
            corpo.Controls.Add(fechar);
            var sobre = new Botao(L.T("Sobre o Digitalizar"), false, "\uE946") { Top = y, Left = S(24) };
            sobre.Click += (s, e) => { using (var f = new Sobre()) f.ShowDialog(this); };
            corpo.Controls.Add(sobre);

            Marcar();
            FinalizarBarra();
        }

        Label Titulo(string t, int y)
        {
            return new Label
            {
                Text = t, Font = Tema.Fonte(11f, FontStyle.Bold, "Segoe UI Semibold"), ForeColor = Tema.TXT, BackColor = Tema.BG,
                Left = S(24), Top = y, AutoSize = true
            };
        }

        void Marcar()
        {
            foreach (var o in temas) o.Marcada = (string)o.Tag == Tema.ModoTema;
            foreach (var a in cores) a.Selecionada = (string)a.Tag == Tema.Destaque;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
