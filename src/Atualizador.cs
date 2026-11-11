// Atualização dentro do programa: consulta as releases do GitHub, avisa de versão nova (com as notas) e se atualiza sozinho
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Digitalizar
{
    // ---------------------------------------------------------------- leitor mínimo de JSON (sem dependências)
    static class Json
    {
        public static object Ler(string s)
        {
            int i = 0;
            return Valor(s, ref i);
        }

        static void Esp(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Valor(string s, ref int i)
        {
            Esp(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON incompleto");
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>();
                i++; Esp(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Esp(s, ref i);
                    string k = Texto(s, ref i);
                    Esp(s, ref i);
                    if (s[i] != ':') throw new FormatException("JSON inválido");
                    i++;
                    d[k] = Valor(s, ref i);
                    Esp(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; break; }
                    throw new FormatException("JSON inválido");
                }
                return d;
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Esp(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Valor(s, ref i));
                    Esp(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; break; }
                    throw new FormatException("JSON inválido");
                }
                return l;
            }
            if (c == '"') return Texto(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int ini = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            double n;
            if (i == ini || !double.TryParse(s.Substring(ini, i - ini), NumberStyles.Float, CultureInfo.InvariantCulture, out n))
                throw new FormatException("JSON inválido");
            return n;
        }

        static string Texto(string s, ref int i)
        {
            if (s[i] != '"') throw new FormatException("JSON inválido");
            var sb = new StringBuilder();
            i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    switch (s[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16)); i += 4; break;
                        default: sb.Append(s[i]); break;
                    }
                }
                else sb.Append(s[i]);
                i++;
            }
            i++;
            return sb.ToString();
        }

        public static string Txt(object o, string chave)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(chave, out v) && v != null ? Convert.ToString(v, CultureInfo.InvariantCulture) : "";
        }

        public static bool Bool(object o, string chave)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(chave, out v) && v is bool && (bool)v;
        }

        public static List<object> Lista(object o, string chave)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(chave, out v) ? v as List<object> : null;
        }
    }

    class InfoAtualizacao
    {
        public string Tag, Versao, Nome, Notas, Pagina, ArquivoNome, ArquivoUrl;
        public long Tamanho;
        public bool Nova;
    }

    // ---------------------------------------------------------------- consulta, download e instalação
    static class Atualizador
    {
        public const string Repo = "giosepeluiz/project-scanner";
        internal static string UrlApi = "https://api.github.com/repos/" + Repo + "/releases?per_page=30";   // trocável nos testes

        public static Version VersaoAtual
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return new Version(v.Major, v.Minor, Math.Max(0, v.Build));
            }
        }

        public static string VersaoTexto { get { return VersaoAtual.ToString(); } }

        /// Verificação automática (a cada 24 h, ao abrir o programa). Fica ligada por padrão.
        public static bool AutoLigado
        {
            get { return Prefs.Obter("atualizacao_auto", "1") != "0"; }
            set { Prefs.Definir("atualizacao_auto", value ? "1" : "0"); }
        }

        static void PrepararRede()
        {
            try { ServicePointManager.SecurityProtocol = (SecurityProtocolType)(3072 | 768 | 192); } catch { }
        }

        static HttpWebRequest Pedido(string url)
        {
            PrepararRede();
            var rq = (HttpWebRequest)WebRequest.Create(url);
            rq.UserAgent = "Digitalizar/" + VersaoTexto + " (+https://github.com/" + Repo + ")";
            rq.Accept = "application/vnd.github+json";
            rq.Timeout = 20000;
            rq.ReadWriteTimeout = 30000;
            return rq;
        }

        /// Consulta as releases do PC (tags "v3.3.0"; as do Android, "android-v1.2.0", são ignoradas). Bloqueia: use em outra thread.
        public static InfoAtualizacao Consultar()
        {
            string txt;
            try
            {
                using (var resp = Pedido(UrlApi).GetResponse())
                using (var r = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    txt = r.ReadToEnd();
            }
            catch (WebException ex)
            {
                var h = ex.Response as HttpWebResponse;
                if (h != null && (h.StatusCode == HttpStatusCode.Forbidden || (int)h.StatusCode == 429))
                    throw new Exception(L.T("O GitHub limitou as consultas desta rede. Tente de novo mais tarde."));
                throw;
            }

            var lista = Json.Ler(txt) as List<object>;
            if (lista == null) throw new FormatException("Resposta inesperada do GitHub.");
            var rx = new Regex(@"^v(\d+(\.\d+){1,3})$");
            object melhor = null; Version melhorV = null;
            foreach (var rel in lista)
            {
                if (Json.Bool(rel, "draft") || Json.Bool(rel, "prerelease")) continue;
                var m = rx.Match(Json.Txt(rel, "tag_name"));
                if (!m.Success) continue;
                Version v;
                try { v = new Version(m.Groups[1].Value); } catch { continue; }
                if (melhorV == null || v > melhorV) { melhorV = v; melhor = rel; }
            }
            var info = new InfoAtualizacao();
            if (melhor == null) return info;

            info.Tag = Json.Txt(melhor, "tag_name");
            info.Versao = melhorV.ToString();
            info.Nome = Json.Txt(melhor, "name");
            info.Notas = LimparNotas(Json.Txt(melhor, "body"));
            info.Pagina = Json.Txt(melhor, "html_url");
            string sufixo = Config.Windows ? "-windows.zip" : "-linux.tar.gz";
            var assets = Json.Lista(melhor, "assets");
            if (assets != null)
                foreach (var a in assets)
                    if (Json.Txt(a, "name").EndsWith(sufixo, StringComparison.OrdinalIgnoreCase))
                    {
                        info.ArquivoNome = Json.Txt(a, "name");
                        info.ArquivoUrl = Json.Txt(a, "browser_download_url");
                        long t; long.TryParse(Json.Txt(a, "size"), out t); info.Tamanho = t;
                        break;
                    }
            info.Nova = melhorV > VersaoAtual;
            return info;
        }

        /// Deixa as notas (Markdown) legíveis numa caixa de texto simples
        static string LimparNotas(string md)
        {
            if (string.IsNullOrEmpty(md)) return "";
            var linhas = md.Replace("\r", "").Split(new[] { '\n' });
            var sb = new StringBuilder();
            foreach (var l0 in linhas)
            {
                string l = l0.TrimEnd();
                if (Regex.IsMatch(l, @"^\s*\|?\s*:?-{2,}")) continue;         // linha separadora de tabela
                l = Regex.Replace(l, @"^\s{0,3}#{1,6}\s*", "");
                l = Regex.Replace(l, @"^(\s*)[-*]\s+", "$1• ");
                l = l.Replace("**", "").Replace("`", "");
                l = Regex.Replace(l, @"\[([^\]]+)\]\([^)]+\)", "$1");
                sb.AppendLine(l);
            }
            return Regex.Replace(sb.ToString().Trim(), @"\n{3,}", "\n\n");
        }

        /// Baixa o arquivo da release para a pasta temporária
        public static string Baixar(InfoAtualizacao info, Action<long, long> progresso)
        {
            string destino = Path.Combine(Path.GetTempPath(), info.ArquivoNome);
            var rq = Pedido(info.ArquivoUrl);
            rq.Accept = "application/octet-stream";
            rq.Timeout = 30000;
            using (var resp = rq.GetResponse())
            {
                long total = resp.ContentLength > 0 ? resp.ContentLength : info.Tamanho;
                using (var ent = resp.GetResponseStream())
                using (var sai = new FileStream(destino, FileMode.Create))
                {
                    var buf = new byte[81920];
                    long lido = 0; int n;
                    while ((n = ent.Read(buf, 0, buf.Length)) > 0)
                    {
                        sai.Write(buf, 0, n);
                        lido += n;
                        if (progresso != null) progresso(lido, total);
                    }
                }
            }
            return destino;
        }

        /// Troca o executável pelo novo (o antigo fica como .antiga até o próximo início). Lança exceção se não conseguir.
        public static void Instalar(string arquivo)
        {
            string exe = Assembly.GetExecutingAssembly().Location;
            string novo = exe + ".novo";
            if (File.Exists(novo)) File.Delete(novo);

            if (Config.Windows)
            {
                using (var z = ZipFile.OpenRead(arquivo))
                {
                    var ent = z.Entries.FirstOrDefault(x => x.Name.Equals("Digitalizar.exe", StringComparison.OrdinalIgnoreCase));
                    if (ent == null) throw new Exception("Digitalizar.exe não está no pacote.");
                    ent.ExtractToFile(novo, true);
                }
            }
            else
            {
                string pasta = Path.Combine(Path.GetTempPath(), "digitalizar_atualizacao_" + Process.GetCurrentProcess().Id);
                if (Directory.Exists(pasta)) Directory.Delete(pasta, true);
                Directory.CreateDirectory(pasta);
                var psi = new ProcessStartInfo("tar", "-xzf \"" + arquivo + "\" -C \"" + pasta + "\"") { UseShellExecute = false, CreateNoWindow = true };
                using (var p = Process.Start(psi)) { p.WaitForExit(); if (p.ExitCode != 0) throw new Exception("tar falhou (código " + p.ExitCode + ")."); }
                string achado = Directory.GetFiles(pasta, "Digitalizar.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (achado == null) throw new Exception("Digitalizar.exe não está no pacote.");
                File.Copy(achado, novo, true);
                try { Directory.Delete(pasta, true); } catch { }
            }

            string antiga = exe + ".antiga";
            if (File.Exists(antiga)) File.Delete(antiga);
            File.Move(exe, antiga);                       // um programa em execução pode ser renomeado, mas não sobrescrito
            try { File.Move(novo, exe); }
            catch { File.Move(antiga, exe); throw; }
            try { File.Delete(arquivo); } catch { }
        }

        /// Apaga o executável antigo deixado pela última atualização
        public static void LimparAntigos()
        {
            try
            {
                string antiga = Assembly.GetExecutingAssembly().Location + ".antiga";
                if (File.Exists(antiga)) File.Delete(antiga);
            }
            catch { }
        }

        public static void AbrirPagina(string url)
        {
            if (string.IsNullOrEmpty(url)) url = "https://github.com/" + Repo + "/releases";
            try
            {
                if (Config.Windows) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                else Process.Start("xdg-open", "\"" + url + "\"");
            }
            catch { }
        }

        /// Ao abrir o programa: verifica no máximo a cada 24 h e só avisa se houver versão nova (e que não foi pulada)
        public static void VerificarAutomatico(Form dono)
        {
            if (!AutoLigado) return;
            long ult;
            if (long.TryParse(Prefs.Obter("atualizacao_ultima", "0"), out ult) && ult > 0 &&
                (DateTime.UtcNow - new DateTime(ult, DateTimeKind.Utc)).TotalHours < 24) return;
            var t = new Thread(() =>
            {
                try
                {
                    var info = Consultar();
                    dono.BeginInvoke((Action)(() =>
                    {
                        Prefs.Definir("atualizacao_ultima", DateTime.UtcNow.Ticks.ToString());
                        if (info.Nova && info.Tag != Prefs.Obter("atualizacao_ignorada", "")) Mostrar(dono, info);
                    }));
                }
                catch { }     // sem internet ou GitHub fora do ar: tenta de novo na próxima vez
            }) { IsBackground = true };
            t.Start();
        }

        public static void Mostrar(IWin32Window dono, InfoAtualizacao info)
        {
            using (var f = new JanelaAtualizacao(info)) f.ShowDialog(dono);
        }
    }

    // ---------------------------------------------------------------- janela "Nova versão disponível"
    class JanelaAtualizacao : FormaModerna
    {
        readonly InfoAtualizacao info;
        Botao btnAtualizar, btnDepois, btnPular;
        BarraProgresso barra;
        Label lblStatus;
        bool baixando;
        static int S(int v) { return Tema.S(v); }

        public JanelaAtualizacao(InfoAtualizacao info)
        {
            this.info = info;
            Text = L.T("Nova versão disponível");
            Barra.Titulo = Text;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(S(640), S(640));
            MinimumSize = new Size(S(520), S(480));

            var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.BG, Padding = new Padding(S(24), S(8), S(24), S(16)) };
            Controls.Add(corpo);

            var rodape = new Panel { Dock = DockStyle.Bottom, Height = S(110), BackColor = Tema.BG };
            var notas = new Panel { Dock = DockStyle.Fill, BackColor = Tema.BG };
            var topo = new Panel { Dock = DockStyle.Top, Height = S(84), BackColor = Tema.BG };
            corpo.Controls.Add(notas);
            corpo.Controls.Add(topo);
            corpo.Controls.Add(rodape);

            topo.Controls.Add(new Label
            {
                Text = L.F("Versão {0}", info.Versao), Font = Tema.Fonte(16f, FontStyle.Bold, "Segoe UI Semibold"),
                ForeColor = Tema.TXT, BackColor = Tema.BG, Left = 0, Top = S(4), AutoSize = true
            });
            topo.Controls.Add(new Label
            {
                Text = L.F("A versão {0} está disponível (você usa a {1}).", info.Versao, Atualizador.VersaoTexto),
                Font = Tema.Fonte(10f), ForeColor = Tema.TXT2, BackColor = Tema.BG, Left = 0, Top = S(42), AutoSize = true
            });

            var titNotas = new Label
            {
                Text = L.T("Novidades desta versão"), Font = Tema.Fonte(11f, FontStyle.Bold, "Segoe UI Semibold"),
                ForeColor = Tema.TXT, BackColor = Tema.BG, Dock = DockStyle.Top, Height = S(30)
            };
            var caixa = new TextBox
            {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None,
                BackColor = Tema.CARD, ForeColor = Tema.TXT, Font = Tema.Fonte(10f), Dock = DockStyle.Fill,
                Text = string.IsNullOrEmpty(info.Notas) ? L.T("Sem notas para esta versão.") : info.Notas.Replace("\n", "\r\n")
            };
            var moldura = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CARD, Padding = new Padding(S(12)) };
            moldura.Controls.Add(caixa);
            notas.Controls.Add(moldura);
            notas.Controls.Add(titNotas);

            barra = new BarraProgresso { Left = 0, Top = S(6), Width = S(560), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblStatus = new Label { Left = 0, Top = S(16), Width = S(560), Height = S(20), ForeColor = Tema.TXT2, BackColor = Tema.BG, Font = Tema.Fonte(9f), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            rodape.Controls.Add(barra);
            rodape.Controls.Add(lblStatus);

            var botoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom, Height = S(52), FlowDirection = FlowDirection.RightToLeft, BackColor = Tema.BG,
                Padding = new Padding(0, S(10), 0, 0), WrapContents = false
            };
            btnAtualizar = new Botao(L.T("Atualizar agora"), true) { Margin = new Padding(S(8), 0, 0, 0) };
            btnDepois = new Botao(L.T("Depois"), false) { Margin = new Padding(S(8), 0, 0, 0) };
            btnPular = new Botao(L.T("Pular esta versão"), false) { Margin = new Padding(S(8), 0, 0, 0) };
            botoes.Controls.Add(btnAtualizar);
            botoes.Controls.Add(btnDepois);
            botoes.Controls.Add(btnPular);
            rodape.Controls.Add(botoes);

            Shown += (s, e) => { caixa.SelectionStart = 0; caixa.SelectionLength = 0; btnAtualizar.Focus(); };
            btnAtualizar.Click += (s, e) => Atualizar();
            btnDepois.Click += (s, e) => Close();
            btnPular.Click += (s, e) => { Prefs.Definir("atualizacao_ignorada", info.Tag); Close(); };

            FinalizarBarra();
        }

        void Atualizar()
        {
            if (string.IsNullOrEmpty(info.ArquivoUrl))
            {
                Atualizador.AbrirPagina(info.Pagina);
                lblStatus.Text = L.T("Esta versão não tem pacote para o seu sistema. A página dela foi aberta no navegador.");
                return;
            }
            baixando = true;
            btnAtualizar.Enabled = btnDepois.Enabled = btnPular.Enabled = false;
            barra.Ativo = true;
            lblStatus.Text = L.T("Baixando…");
            var t = new Thread(() =>
            {
                try
                {
                    string arq = Atualizador.Baixar(info, (lido, total) =>
                    {
                        try
                        {
                            BeginInvoke((Action)(() => lblStatus.Text = total > 0
                                ? L.F("Baixando… {0} de {1} MB", (lido / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture), (total / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture))
                                : L.F("Baixando… {0} MB", (lido / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture))));
                        }
                        catch { }
                    });
                    BeginInvoke((Action)(() => lblStatus.Text = L.T("Instalando…")));
                    Atualizador.Instalar(arq);
                    BeginInvoke((Action)(() => { lblStatus.Text = L.T("Atualizado! Reiniciando…"); Programa.Reiniciar(); }));
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    BeginInvoke((Action)(() => Falhou(msg)));
                }
            }) { IsBackground = true };
            t.Start();
        }

        void Falhou(string msg)
        {
            baixando = false;
            barra.Ativo = false;
            btnAtualizar.Enabled = btnDepois.Enabled = btnPular.Enabled = true;
            lblStatus.Text = L.T("Não foi possível atualizar sozinho:") + " " + msg;
            MessageBox.Show(this, L.T("Não foi possível atualizar sozinho. A página da versão vai abrir no navegador, para você baixar à mão.") + "\n\n" + msg,
                L.T("Atualizações"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Atualizador.AbrirPagina(info.Pagina);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (baixando) e.Cancel = true;      // não fecha no meio da instalação
            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && !baixando) { Close(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
