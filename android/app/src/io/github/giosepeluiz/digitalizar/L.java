// Idiomas da interface. Os textos-fonte estão em português do Brasil; as traduções ficam em
// assets/idiomas.tsv (o mesmo ficheiro da versão para Windows e Linux, uma coluna por idioma).
package io.github.giosepeluiz.digitalizar;

import android.content.Context;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

public final class L {
    private L() { }

    public static final String SISTEMA = "auto", SEPARADOR = "-";
    public static final String[] CODIGOS = { SISTEMA, "pt-BR", "pt-PT", "en-US", "en-GB", "es", SEPARADOR,
            "de", "fr", "it", "nl", "pl", "ru", "tr", "uk", "ja", "zh-CN" };

    private static final Map<String, String> NOMES = new HashMap<>();
    static {
        String[][] n = { { "pt-BR", "Português (Brasil)" }, { "pt-PT", "Português (Portugal)" },
                { "en-US", "English (United States)" }, { "en-GB", "English (United Kingdom)" },
                { "es", "Español" }, { "de", "Deutsch" }, { "fr", "Français" }, { "it", "Italiano" },
                { "nl", "Nederlands" }, { "pl", "Polski" }, { "ru", "Русский" }, { "tr", "Türkçe" },
                { "uk", "Українська" }, { "ja", "日本語" }, { "zh-CN", "中文（简体）" } };
        for (String[] p : n) NOMES.put(p[0], p[1]);
    }

    public static String escolhido = SISTEMA, atual = "pt-BR";
    private static Map<String, String> tabela = new HashMap<>();
    private static boolean iniciado;

    public static String nome(String codigo) {
        if (SISTEMA.equals(codigo)) return T("Idioma do sistema") + " – " + nome(detectar());
        String n = NOMES.get(codigo);
        return n != null ? n : codigo;
    }

    /** Idioma do sistema, ou inglês se não houver tradução. */
    public static String detectar() {
        Locale l = Locale.getDefault();
        String lingua = l.getLanguage(), pais = l.getCountry();
        if ("pt".equals(lingua)) return ("PT".equals(pais) || "AO".equals(pais) || "MZ".equals(pais)) ? "pt-PT" : "pt-BR";
        if ("en".equals(lingua)) {
            for (String r : new String[] { "GB", "IE", "AU", "NZ", "ZA", "IN" }) if (r.equals(pais)) return "en-GB";
            return "en-US";
        }
        if ("zh".equals(lingua)) return "zh-CN";
        if ("iw".equals(lingua)) lingua = "he";
        for (String c : CODIGOS) if (c.equals(lingua)) return c;
        return "en-US";
    }

    public static synchronized void iniciar(Context ctx) {
        if (iniciado) return;
        iniciado = true;
        escolhido = Prefs.texto("idioma", SISTEMA);
        atual = SISTEMA.equals(escolhido) || !NOMES.containsKey(escolhido) ? detectar() : escolhido;
        tabela = carregar(ctx, atual);
    }

    /** Volta a ler o idioma das preferências (depois de mudar nas Opções). */
    public static synchronized void reiniciar(Context ctx) { iniciado = false; iniciar(ctx); }

    private static Map<String, String> carregar(Context ctx, String codigo) {
        Map<String, String> t = new HashMap<>();
        try (BufferedReader r = new BufferedReader(new InputStreamReader(ctx.getAssets().open("idiomas.tsv"), StandardCharsets.UTF_8))) {
            String[] cab = r.readLine().split("\t", -1);
            int col = -1, ing = -1;
            for (int i = 0; i < cab.length; i++) { if (cab[i].equals(codigo)) col = i; if (cab[i].equals("en-US")) ing = i; }
            if (col < 1) return t;
            String linha;
            while ((linha = r.readLine()) != null) {
                if (linha.isEmpty() || linha.charAt(0) == '#') continue;
                String[] p = linha.split("\t", -1);
                if (p.length > col && !p[col].isEmpty()) t.put(des(p[0]), des(p[col]));
                else if (!codigo.startsWith("pt") && ing > 0 && p.length > ing && !p[ing].isEmpty()) t.put(des(p[0]), des(p[ing]));
            }
        } catch (Exception ignored) { }
        return t;
    }

    private static String des(String s) { return s.replace("\\n", "\n"); }

    /** Traduz um texto (a chave é o texto em português do Brasil). */
    public static String T(String texto) {
        String r = texto == null ? null : tabela.get(texto);
        return r != null ? r : texto;
    }

    /** Traduz e formata ({0}, {1}…). */
    public static String F(String formato, Object... args) {
        String s = T(formato);
        for (int i = 0; i < args.length; i++) s = s.replace("{" + i + "}", String.valueOf(args[i]));
        return s;
    }
}
