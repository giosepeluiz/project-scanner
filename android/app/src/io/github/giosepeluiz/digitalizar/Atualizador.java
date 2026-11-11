// Atualização dentro do app: consulta as releases do GitHub (tags "android-vX.Y.Z"), avisa de versão nova
// com as notas, baixa o APK e abre o instalador do Android. Sem bibliotecas externas.
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.app.Dialog;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.provider.Settings;
import android.view.Gravity;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public final class Atualizador {
    private Atualizador() { }

    public static final String REPO = "giosepeluiz/project-scanner";
    /** Trocável nos testes. */
    public static String urlApi = "https://api.github.com/repos/" + REPO + "/releases?per_page=30";

    private static final Pattern TAG = Pattern.compile("^android-v(\\d+(?:\\.\\d+){1,3})$");

    public static final class Info {
        public String tag = "", versao = "", nome = "", notas = "", pagina = "", apkNome = "", apkUrl = "";
        public long tamanho;
        public boolean nova;
    }

    // ------------------------------------------------------------- preferências
    public static boolean autoLigado() { return Prefs.logico("atualizacao_auto", true); }
    public static void definirAuto(boolean v) { Prefs.definir("atualizacao_auto", v); }

    // ------------------------------------------------------------- consulta
    /** Bloqueia: use fora da thread da interface. */
    public static Info consultar() throws Exception {
        HttpURLConnection c = abrir(urlApi, "application/vnd.github+json");
        String txt;
        try {
            int cod = c.getResponseCode();
            if (cod == 403 || cod == 429) throw new Exception(L.T("O GitHub limitou as consultas desta rede. Tente de novo mais tarde."));
            if (cod != 200) throw new Exception("GitHub: HTTP " + cod);
            txt = ler(c.getInputStream());
        } finally { c.disconnect(); }

        JSONArray lista = new JSONArray(txt);
        Info info = new Info();
        int[] melhor = null;
        JSONObject escolhida = null;
        for (int i = 0; i < lista.length(); i++) {
            JSONObject r = lista.getJSONObject(i);
            if (r.optBoolean("draft") || r.optBoolean("prerelease")) continue;
            Matcher m = TAG.matcher(r.optString("tag_name"));
            if (!m.matches()) continue;
            int[] v = partes(m.group(1));
            if (melhor == null || comparar(v, melhor) > 0) { melhor = v; escolhida = r; }
        }
        if (escolhida == null) return info;

        info.tag = escolhida.optString("tag_name");
        info.versao = escolhida.optString("tag_name").substring("android-v".length());
        info.nome = escolhida.optString("name");
        info.notas = limparNotas(escolhida.optString("body"));
        info.pagina = escolhida.optString("html_url");
        JSONArray assets = escolhida.optJSONArray("assets");
        if (assets != null)
            for (int i = 0; i < assets.length(); i++) {
                JSONObject a = assets.getJSONObject(i);
                if (a.optString("name").toLowerCase(Locale.ROOT).endsWith("-android.apk")) {
                    info.apkNome = a.optString("name");
                    info.apkUrl = a.optString("browser_download_url");
                    info.tamanho = a.optLong("size");
                    break;
                }
            }
        info.nova = comparar(melhor, partes(Config.VERSAO)) > 0;
        return info;
    }

    static int[] partes(String v) {
        String[] p = v.split("\\.");
        int[] r = new int[4];
        for (int i = 0; i < p.length && i < 4; i++) {
            try { r[i] = Integer.parseInt(p[i].trim()); } catch (NumberFormatException e) { r[i] = 0; }
        }
        return r;
    }

    static int comparar(int[] a, int[] b) {
        for (int i = 0; i < 4; i++) if (a[i] != b[i]) return a[i] > b[i] ? 1 : -1;
        return 0;
    }

    static HttpURLConnection abrir(String url, String aceita) throws Exception {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setConnectTimeout(15000);
        c.setReadTimeout(30000);
        c.setRequestProperty("User-Agent", "Digitalizar-Android/" + Config.VERSAO + " (+https://github.com/" + REPO + ")");
        c.setRequestProperty("Accept", aceita);
        return c;
    }

    static String ler(InputStream in) throws Exception {
        BufferedReader r = new BufferedReader(new InputStreamReader(in, "UTF-8"));
        StringBuilder sb = new StringBuilder();
        char[] buf = new char[8192];
        int n;
        while ((n = r.read(buf)) > 0) sb.append(buf, 0, n);
        r.close();
        return sb.toString();
    }

    /** Deixa as notas (Markdown) legíveis em texto simples. */
    static String limparNotas(String md) {
        if (md == null || md.length() == 0) return "";
        StringBuilder sb = new StringBuilder();
        for (String l0 : md.replace("\r", "").split("\n")) {
            String l = l0.replaceAll("\\s+$", "");
            if (l.matches("^\\s*\\|?\\s*:?-{2,}.*")) continue;
            l = l.replaceAll("^\\s{0,3}#{1,6}\\s*", "");
            l = l.replaceAll("^(\\s*)[-*]\\s+", "$1• ");
            l = l.replace("**", "").replace("`", "");
            l = l.replaceAll("\\[([^\\]]+)\\]\\([^)]+\\)", "$1");
            sb.append(l).append('\n');
        }
        return sb.toString().trim().replaceAll("\n{3,}", "\n\n");
    }

    // ------------------------------------------------------------- aviso automático
    /** Ao abrir o app: no máximo uma vez a cada 24 h; só avisa se houver versão nova e que não foi pulada. */
    public static void verificarAuto(final Activity a) {
        if (!autoLigado()) return;
        long ult = Prefs.texto("atualizacao_ultima", "0").length() > 0 ? parseLong(Prefs.texto("atualizacao_ultima", "0")) : 0;
        if (ult > 0 && System.currentTimeMillis() - ult < 24L * 3600 * 1000) return;
        new Thread(new Runnable() {
            public void run() {
                try {
                    final Info info = consultar();
                    a.runOnUiThread(new Runnable() {
                        public void run() {
                            if (a.isFinishing()) return;
                            Prefs.definir("atualizacao_ultima", String.valueOf(System.currentTimeMillis()));
                            if (info.nova && !info.tag.equals(Prefs.texto("atualizacao_ignorada", ""))) mostrar(a, info);
                        }
                    });
                } catch (Throwable e) { /* sem internet: tenta de novo na próxima vez */ }
            }
        }).start();
    }

    static long parseLong(String s) {
        try { return Long.parseLong(s); } catch (NumberFormatException e) { return 0; }
    }

    // ------------------------------------------------------------- janela "Nova versão disponível"
    public static void mostrar(final Activity a, final Info info) {
        LinearLayout col = Ui.coluna(a);
        col.setPadding(Tema.S(22), Tema.S(20), Tema.S(22), Tema.S(16));
        col.addView(Ui.rotulo(a, L.T("Nova versão disponível"), 18, Tema.TXT, true), Ui.lpM(Ui.MP, Ui.WC, 0, 0, 0, 4));
        col.addView(Ui.rotulo(a, L.F("A versão {0} está disponível (você usa a {1}).", info.versao, Config.VERSAO), 14, Tema.TXT2, false),
                Ui.lpM(Ui.MP, Ui.WC, 0, 0, 0, 14));
        col.addView(Ui.rotulo(a, L.T("Novidades desta versão"), 15, Tema.TXT, true), Ui.lpM(Ui.MP, Ui.WC, 0, 0, 0, 6));

        ScrollView sv = new ScrollView(a);
        sv.setBackground(Ui.fundo(Tema.BG, 8, Tema.BORDA));
        TextView notas = Ui.rotulo(a, info.notas.length() == 0 ? L.T("Sem notas para esta versão.") : info.notas, 14, Tema.TXT, false);
        notas.setPadding(Tema.S(12), Tema.S(10), Tema.S(12), Tema.S(10));
        notas.setTextIsSelectable(false);
        sv.addView(notas);
        int alturaMax = Math.min(Tema.S(260), a.getResources().getDisplayMetrics().heightPixels / 3);
        col.addView(sv, new LinearLayout.LayoutParams(Ui.MP, alturaMax));

        final TextView status = Ui.rotulo(a, "", 13, Tema.TXT2, false);
        col.addView(status, Ui.lpM(Ui.MP, Ui.WC, 0, 10, 0, 6));

        final TextView atualizar = Ui.botao(a, Ui.Ic.ATUALIZAR, L.T("Atualizar agora"), true);
        final TextView depois = Ui.botao(a, null, L.T("Depois"), false);
        final TextView pular = Ui.botao(a, null, L.T("Pular esta versão"), false);
        col.addView(atualizar, Ui.lpM(Ui.MP, Ui.WC, 0, 4, 0, 8));
        LinearLayout linha = Ui.linha(a);
        linha.setGravity(Gravity.CENTER);
        linha.addView(depois, Ui.lpM(0, Ui.WC, 0, 0, 4, 0));
        ((LinearLayout.LayoutParams) depois.getLayoutParams()).weight = 1;
        linha.addView(pular, Ui.lpM(0, Ui.WC, 4, 0, 0, 0));
        ((LinearLayout.LayoutParams) pular.getLayoutParams()).weight = 1;
        col.addView(linha, Ui.lp(Ui.MP, Ui.WC));

        final Dialog d = Ui.dialogo(a, col);
        final boolean[] baixando = new boolean[1];
        d.setCancelable(true);
        depois.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { if (!baixando[0]) d.dismiss(); } });
        pular.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                if (baixando[0]) return;
                Prefs.definir("atualizacao_ignorada", info.tag);
                d.dismiss();
            }
        });
        atualizar.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                if (baixando[0]) return;
                if (info.apkUrl.length() == 0) {
                    abrirPagina(a, info.pagina);
                    status.setText(L.T("Esta versão não tem pacote para o seu sistema. A página dela foi aberta no navegador."));
                    return;
                }
                if (Build.VERSION.SDK_INT >= 26 && !a.getPackageManager().canRequestPackageInstalls()) {
                    status.setText(L.T("Permita a instalação de apps desta fonte para o Digitalizar e toque em Atualizar agora de novo."));
                    try {
                        a.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES, Uri.parse("package:" + a.getPackageName())));
                    } catch (Throwable e) { /* sem a tela de permissão: o texto acima já explica */ }
                    return;
                }
                baixando[0] = true;
                Ui.habilitar(atualizar, false);
                Ui.habilitar(depois, false);
                Ui.habilitar(pular, false);
                status.setText(L.T("Baixando…"));
                baixar(a, info, status, d, baixando, atualizar, depois, pular);
            }
        });
        d.show();
    }

    static void baixar(final Activity a, final Info info, final TextView status, final Dialog d, final boolean[] baixando,
                       final TextView atualizar, final TextView depois, final TextView pular) {
        new Thread(new Runnable() {
            public void run() {
                try {
                    File dir = new File(a.getCacheDir(), "partilha");
                    dir.mkdirs();
                    final File apk = new File(dir, info.apkNome);
                    HttpURLConnection c = abrir(info.apkUrl, "application/octet-stream");
                    try {
                        if (c.getResponseCode() != 200) throw new Exception("GitHub: HTTP " + c.getResponseCode());
                        final long total = c.getContentLength() > 0 ? c.getContentLength() : info.tamanho;
                        InputStream in = c.getInputStream();
                        OutputStream out = new FileOutputStream(apk);
                        byte[] buf = new byte[65536];
                        long lido = 0, ultimo = 0;
                        int n;
                        while ((n = in.read(buf)) > 0) {
                            out.write(buf, 0, n);
                            lido += n;
                            if (lido - ultimo > 131072) {
                                ultimo = lido;
                                final long l = lido;
                                a.runOnUiThread(new Runnable() {
                                    public void run() {
                                        status.setText(total > 0 ? L.F("Baixando… {0} de {1} MB", mb(l), mb(total)) : L.F("Baixando… {0} MB", mb(l)));
                                    }
                                });
                            }
                        }
                        out.close();
                        in.close();
                    } finally { c.disconnect(); }
                    a.runOnUiThread(new Runnable() {
                        public void run() {
                            baixando[0] = false;
                            Ui.habilitar(atualizar, true);
                            Ui.habilitar(depois, true);
                            Ui.habilitar(pular, true);
                            status.setText(L.T("Baixado. Toque em Instalar na tela do Android para concluir."));
                            instalar(a, apk);
                        }
                    });
                } catch (final Throwable e) {
                    a.runOnUiThread(new Runnable() {
                        public void run() {
                            baixando[0] = false;
                            Ui.habilitar(atualizar, true);
                            Ui.habilitar(depois, true);
                            Ui.habilitar(pular, true);
                            status.setText(L.T("Não foi possível atualizar sozinho:") + " " + (e.getMessage() == null ? e.getClass().getSimpleName() : e.getMessage()));
                            abrirPagina(a, info.pagina);
                        }
                    });
                }
            }
        }).start();
    }

    static String mb(long b) { return String.format(Locale.ROOT, "%.1f", b / 1048576.0); }

    /** Abre o instalador do Android com o APK baixado (o app é substituído mantendo os dados, pois a assinatura é a mesma). */
    public static void instalar(Activity a, File apk) {
        Intent i = new Intent(Intent.ACTION_VIEW);
        i.setDataAndType(Provedor.uri(apk), "application/vnd.android.package-archive");
        i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_ACTIVITY_NEW_TASK);
        a.startActivity(i);
    }

    public static void abrirPagina(Context c, String url) {
        if (url == null || url.length() == 0) url = "https://github.com/" + REPO + "/releases";
        try {
            Intent i = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
            i.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            c.startActivity(i);
        } catch (Throwable e) { /* sem navegador */ }
    }

    /** Apaga APKs baixados em atualizações anteriores. */
    public static void limpar(Context c) {
        try {
            File[] fs = new File(c.getCacheDir(), "partilha").listFiles();
            if (fs != null) for (File f : fs) if (f.getName().toLowerCase(Locale.ROOT).endsWith(".apk")) f.delete();
        } catch (Throwable e) { /* ignora */ }
    }
}
