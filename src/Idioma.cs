// Idiomas da interface. Os textos-fonte estão em português do Brasil;
// as traduções ficam no recurso embutido "idiomas.tsv" (uma coluna por idioma).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Digitalizar
{
    static class L
    {
        public const string Sistema = "auto";
        public const string Separador = "-";

        // Ordem do seletor: os principais primeiro, depois um traço e os demais
        public static readonly string[] Codigos =
            { Sistema, "pt-BR", "pt-PT", "en-US", "en-GB", "es", Separador, "de", "fr", "it", "nl", "pl", "ru", "tr", "uk", "ja", "zh-CN" };

        static readonly Dictionary<string, string> nomes = new Dictionary<string, string>
        {
            { "pt-BR", "Português (Brasil)" }, { "pt-PT", "Português (Portugal)" },
            { "en-US", "English (United States)" }, { "en-GB", "English (United Kingdom)" },
            { "es", "Español" }, { "de", "Deutsch" }, { "fr", "Français" }, { "it", "Italiano" },
            { "nl", "Nederlands" }, { "pl", "Polski" }, { "ru", "Русский" }, { "tr", "Türkçe" },
            { "uk", "Українська" }, { "ja", "日本語" }, { "zh-CN", "中文（简体）" },
        };

        public static string Escolhido = Sistema;   // o que está guardado nas preferências
        public static string Atual = "pt-BR";      // o idioma efetivamente em uso
        static Dictionary<string, string> tabela;

        public static string Nome(string codigo)
        {
            if (codigo == Sistema) return T("Idioma do sistema") + " – " + Nome(Detectar());
            if (codigo == Separador) return Separador;
            string n;
            return nomes.TryGetValue(codigo, out n) ? n : codigo;
        }

        /// Idioma do sistema operativo, ou inglês se não houver tradução
        static CultureInfo culturaSistema;

        public static string Detectar()
        {
            var c = culturaSistema ?? CultureInfo.CurrentUICulture;
            string nome = c.Name, lingua = c.TwoLetterISOLanguageName;
            if (nome == "pt-PT" || nome == "pt-AO" || nome == "pt-MZ") return "pt-PT";
            if (lingua == "pt") return "pt-BR";
            if (lingua == "en")
            {
                foreach (var r in new[] { "GB", "IE", "AU", "NZ", "ZA", "IN" })
                    if (nome.EndsWith("-" + r)) return "en-GB";
                return "en-US";
            }
            if (lingua == "zh") return "zh-CN";
            foreach (var cod in Codigos)
                if (cod == lingua) return cod;
            return "en-US";
        }

        public static void Iniciar()
        {
            if (culturaSistema == null) culturaSistema = CultureInfo.CurrentUICulture;
            Escolhido = Prefs.Obter("idioma", Sistema);
            Atual = Escolhido == Sistema || !nomes.ContainsKey(Escolhido) ? Detectar() : Escolhido;
            tabela = Carregar(Atual);
            try { System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(Atual); } catch { }
        }

        static Dictionary<string, string> Carregar(string codigo)
        {
            var t = new Dictionary<string, string>();
            try
            {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("idiomas.tsv"))
                using (var r = new StreamReader(s, Encoding.UTF8))
                {
                    string[] cab = r.ReadLine().Split('\t');
                    int col = Array.IndexOf(cab, codigo);
                    int ing = Array.IndexOf(cab, "en-US");   // reserva para traduções em falta
                    if (col < 1) return t;
                    string linha;
                    while ((linha = r.ReadLine()) != null)
                    {
                        if (linha.Length == 0 || linha[0] == '#') continue;
                        var p = linha.Split('\t');
                        if (p.Length > col && p[col].Length > 0) t[Des(p[0])] = Des(p[col]);
                        else if (!codigo.StartsWith("pt") && ing > 0 && p.Length > ing && p[ing].Length > 0) t[Des(p[0])] = Des(p[ing]);
                    }
                }
            }
            catch { }
            return t;
        }

        static string Des(string s) { return s.Replace("\\n", "\n"); }

        /// Traduz um texto (a chave é o texto em português do Brasil)
        public static string T(string texto)
        {
            string r;
            return tabela != null && texto != null && tabela.TryGetValue(texto, out r) ? r : texto;
        }

        /// Traduz e formata ({0}, {1}…)
        public static string F(string formato, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, T(formato), args);
        }
    }
}
