// Scanner da rede pelo protocolo eSCL (AirScan / Mopria), o mesmo que a HP DeskJet 2700 e
// a maioria das multifuncionais Wi-Fi usam. Deteção automática por mDNS (_uscan._tcp).
package io.github.giosepeluiz.digitalizar;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.nsd.NsdManager;
import android.net.nsd.NsdServiceInfo;
import android.net.wifi.WifiManager;
import android.os.Handler;
import android.os.Looper;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.cert.X509Certificate;
import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import javax.net.ssl.HostnameVerifier;
import javax.net.ssl.HttpsURLConnection;
import javax.net.ssl.SSLContext;
import javax.net.ssl.SSLSession;
import javax.net.ssl.TrustManager;
import javax.net.ssl.X509TrustManager;

public final class Escl {
    private Escl() { }

    public static final class Scanner {
        public String nome, base; // base = http://ip:porta/eSCL
        public boolean manual;
        @Override public String toString() { return nome; }
        public String id() { return base; }
        String guardar() { return nome.replace("\t", " ") + "\t" + base; }
        static Scanner ler(String s) {
            String[] p = s.split("\t");
            if (p.length < 2) return null;
            Scanner x = new Scanner(); x.nome = p[0]; x.base = p[1]; x.manual = true;
            return x;
        }
    }

    public interface Retorno { void pronto(List<Scanner> lista); }

    // ------------------------------------------------------------- scanners adicionados à mão
    public static List<Scanner> manuais() {
        List<Scanner> l = new ArrayList<>();
        for (String s : Prefs.texto("scanners_manuais", "").split("\n")) {
            Scanner x = Scanner.ler(s);
            if (x != null) l.add(x);
        }
        return l;
    }

    public static void adicionarManual(Scanner s) {
        StringBuilder b = new StringBuilder();
        for (Scanner x : manuais()) if (!x.base.equals(s.base)) b.append(x.guardar()).append('\n');
        b.append(s.guardar());
        Prefs.definir("scanners_manuais", b.toString());
    }

    // ------------------------------------------------------------- deteção por mDNS
    public static void detectar(Context ctx, final long tempoMs, final Retorno ret) {
        final Context app = ctx.getApplicationContext();
        final NsdManager nsd = (NsdManager) app.getSystemService(Context.NSD_SERVICE);
        final Handler ui = new Handler(Looper.getMainLooper());
        final List<Scanner> achados = Collections.synchronizedList(new ArrayList<Scanner>());
        for (Scanner m : manuais()) achados.add(m);
        WifiManager wm = (WifiManager) app.getSystemService(Context.WIFI_SERVICE);
        final WifiManager.MulticastLock trava = wm != null ? wm.createMulticastLock("digitalizar") : null;
        if (trava != null) { trava.setReferenceCounted(false); trava.acquire(); }

        final LinkedList<NsdServiceInfo> fila = new LinkedList<>();
        final boolean[] resolvendo = { false };
        final List<NsdManager.DiscoveryListener> ouvintes = new ArrayList<>();

        final Runnable[] proximo = new Runnable[1];
        proximo[0] = new Runnable() {
            public void run() {
                NsdServiceInfo s;
                synchronized (fila) {
                    if (resolvendo[0] || fila.isEmpty()) return;
                    s = fila.removeFirst();
                    resolvendo[0] = true;
                }
                final boolean seguro = s.getServiceType().contains("uscans");
                try {
                    nsd.resolveService(s, new NsdManager.ResolveListener() {
                        public void onResolveFailed(NsdServiceInfo i, int erro) { fim(); }
                        public void onServiceResolved(NsdServiceInfo i) {
                            try {
                                Map<String, byte[]> at = i.getAttributes();
                                String rs = txt(at, "rs", "eSCL");
                                if (rs.startsWith("/")) rs = rs.substring(1);
                                String host = i.getHost().getHostAddress();
                                if (host.contains(":")) host = "[" + host.replaceAll("%.*", "") + "]";
                                Scanner x = new Scanner();
                                String modelo = txt(at, "ty", "");
                                x.nome = modelo.isEmpty() ? i.getServiceName() : modelo;
                                x.base = (seguro ? "https://" : "http://") + host + ":" + i.getPort() + (rs.isEmpty() ? "" : "/" + rs);
                                synchronized (achados) {
                                    boolean repetido = false;
                                    for (Scanner y : achados)
                                        if (y.nome.equals(x.nome) && hostDe(y.base).equals(hostDe(x.base))) {
                                            repetido = true;
                                            if (!seguro && y.base.startsWith("https")) y.base = x.base; // prefere http
                                        }
                                    if (!repetido) achados.add(x);
                                }
                            } catch (Exception ignored) { }
                            fim();
                        }
                        void fim() {
                            synchronized (fila) { resolvendo[0] = false; }
                            ui.post(proximo[0]);
                        }
                    });
                } catch (Exception e) {
                    synchronized (fila) { resolvendo[0] = false; }
                    ui.post(proximo[0]);
                }
            }
        };

        for (String tipo : new String[] { "_uscan._tcp", "_uscans._tcp" }) {
            NsdManager.DiscoveryListener d = new NsdManager.DiscoveryListener() {
                public void onStartDiscoveryFailed(String t, int e) { }
                public void onStopDiscoveryFailed(String t, int e) { }
                public void onDiscoveryStarted(String t) { }
                public void onDiscoveryStopped(String t) { }
                public void onServiceLost(NsdServiceInfo s) { }
                public void onServiceFound(NsdServiceInfo s) {
                    synchronized (fila) { fila.add(s); }
                    ui.post(proximo[0]);
                }
            };
            try { nsd.discoverServices(tipo, NsdManager.PROTOCOL_DNS_SD, d); ouvintes.add(d); } catch (Exception ignored) { }
        }

        ui.postDelayed(new Runnable() {
            public void run() {
                for (NsdManager.DiscoveryListener d : ouvintes) try { nsd.stopServiceDiscovery(d); } catch (Exception ignored) { }
                if (trava != null) try { trava.release(); } catch (Exception ignored) { }
                List<Scanner> l;
                synchronized (achados) { l = new ArrayList<>(achados); }
                ret.pronto(l);
            }
        }, tempoMs);
    }

    static String hostDe(String base) {
        try { return new URL(base).getHost(); } catch (Exception e) { return base; }
    }

    static String txt(Map<String, byte[]> at, String k, String padrao) {
        if (at == null) return padrao;
        byte[] v = at.get(k);
        return v == null ? padrao : new String(v, StandardCharsets.UTF_8).trim();
    }

    /** Tenta encontrar um scanner eSCL num endereço IP (portas e caminhos mais comuns). */
    public static Scanner provarIp(String ip) {
        ip = ip.trim();
        String[] bases = { "http://" + ip + "/eSCL", "http://" + ip + ":8080/eSCL", "https://" + ip + "/eSCL",
                "https://" + ip + ":443/eSCL", "http://" + ip + ":80/eSCL", "http://" + ip + ":9095/eSCL" };
        for (String b : bases) {
            try {
                String xml = texto(b + "/ScannerCapabilities", 4000);
                if (xml.contains("ScannerCapabilities")) {
                    Scanner s = new Scanner();
                    String nome = tag(xml, "MakeAndModel");
                    s.nome = nome == null ? ip : nome;
                    s.base = b; s.manual = true;
                    return s;
                }
            } catch (Exception ignored) { }
        }
        return null;
    }

    // ------------------------------------------------------------- digitalização
    public static final class Capacidades {
        public List<Integer> resolucoes = new ArrayList<>();
        public int largMax = 2550, altMax = 3508; // em 1/300 de polegada
        public boolean jpeg = true, cinza = true;
    }

    public static Capacidades capacidades(Scanner s) throws IOException {
        String xml = texto(s.base + "/ScannerCapabilities", 8000);
        Capacidades c = new Capacidades();
        String plat = entre(xml, "Platen");
        if (plat == null) plat = xml;
        Matcher m = Pattern.compile("<[^>]*XResolution>(\\d+)<").matcher(plat);
        while (m.find()) { int r = Integer.parseInt(m.group(1)); if (!c.resolucoes.contains(r)) c.resolucoes.add(r); }
        Collections.sort(c.resolucoes);
        String w = tag(plat, "MaxWidth"), h = tag(plat, "MaxHeight");
        if (w != null) c.largMax = Integer.parseInt(w.trim());
        if (h != null) c.altMax = Integer.parseInt(h.trim());
        c.jpeg = plat.contains("image/jpeg") || !plat.contains("DocumentFormat");
        c.cinza = plat.contains("Grayscale8");
        return c;
    }

    public interface Progresso { void passo(String texto); }

    public static Bitmap digitalizar(Scanner s, String modo, int dpi, double largCm, double altCm, Progresso p) throws IOException {
        Capacidades c;
        try { c = capacidades(s); } catch (IOException e) { throw new IOException(L.T("O scanner não respondeu. Verifique a rede Wi-Fi."), e); }
        int res = dpi;
        if (!c.resolucoes.isEmpty()) {
            res = c.resolucoes.get(0);
            for (int r : c.resolucoes) if (Math.abs(r - dpi) < Math.abs(res - dpi)) res = r;
        }
        int w = c.largMax, h = c.altMax;
        if (largCm > 0 && altCm > 0) {
            w = Math.min(c.largMax, (int) Math.round(largCm / 2.54 * 300));
            h = Math.min(c.altMax, (int) Math.round(altCm / 2.54 * 300));
        }
        String cor = "Cor".equals(modo) || !c.cinza ? "RGB24" : "Grayscale8";
        String fmt = c.jpeg ? "image/jpeg" : "image/png";
        String xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n"
                + "<scan:ScanSettings xmlns:scan=\"http://schemas.hp.com/imaging/escl/2011/05/03\" xmlns:pwg=\"http://www.pwg.org/schemas/2010/12/sm\">\n"
                + "<pwg:Version>2.0</pwg:Version>\n"
                + "<pwg:ScanRegions><pwg:ScanRegion>"
                + "<pwg:ContentRegionUnits>escl:ThreeHundredthsOfInches</pwg:ContentRegionUnits>"
                + "<pwg:XOffset>0</pwg:XOffset><pwg:YOffset>0</pwg:YOffset>"
                + "<pwg:Width>" + w + "</pwg:Width><pwg:Height>" + h + "</pwg:Height>"
                + "</pwg:ScanRegion></pwg:ScanRegions>\n"
                + "<pwg:InputSource>Platen</pwg:InputSource>\n"
                + "<scan:ColorMode>" + cor + "</scan:ColorMode>\n"
                + "<scan:XResolution>" + res + "</scan:XResolution><scan:YResolution>" + res + "</scan:YResolution>\n"
                + "<pwg:DocumentFormat>" + fmt + "</pwg:DocumentFormat>\n"
                + "<scan:DocumentFormatExt>" + fmt + "</scan:DocumentFormatExt>\n"
                + "</scan:ScanSettings>";

        HttpURLConnection con = abrir(s.base + "/ScanJobs", 15000);
        con.setRequestMethod("POST");
        con.setDoOutput(true);
        con.setRequestProperty("Content-Type", "text/xml");
        try (OutputStream os = con.getOutputStream()) { os.write(xml.getBytes(StandardCharsets.UTF_8)); }
        int cod = con.getResponseCode();
        if (cod == 503) throw new IOException(L.T("O scanner está ocupado. Aguarde e tente de novo."));
        if (cod != 201 && cod != 200) throw new IOException(L.F("Falha ao comunicar com o scanner (código {0}).", cod));
        String local = con.getHeaderField("Location");
        con.disconnect();
        if (local == null) throw new IOException(L.F("Falha ao comunicar com o scanner (código {0}).", cod));
        URL trabalho = new URL(new URL(s.base + "/ScanJobs"), local);
        String alvo = trabalho.toString();
        if (alvo.endsWith("/")) alvo = alvo.substring(0, alvo.length() - 1);

        // o scanner pode precisar de aquecer: tenta durante até 3 minutos
        long limite = System.currentTimeMillis() + 180000;
        byte[] img = null;
        while (System.currentTimeMillis() < limite) {
            HttpURLConnection d = abrir(alvo + "/NextDocument", 180000);
            int r = d.getResponseCode();
            if (r == 200) { img = ler(d.getInputStream()); d.disconnect(); break; }
            d.disconnect();
            if (r == 503 || r == 404 && img == null && System.currentTimeMillis() < limite - 170000) {
                try { Thread.sleep(1500); } catch (InterruptedException e) { throw new IOException(L.T("Digitalização cancelada.")); }
                continue;
            }
            throw new IOException(L.F("Falha ao comunicar com o scanner (código {0}).", r));
        }
        try { HttpURLConnection del = abrir(alvo, 3000); del.setRequestMethod("DELETE"); del.getResponseCode(); del.disconnect(); } catch (Exception ignored) { }
        if (img == null || img.length == 0) throw new IOException(L.T("O scanner não devolveu nenhuma imagem."));
        BitmapFactory.Options o = new BitmapFactory.Options();
        o.inMutable = true;
        o.inPreferredConfig = Bitmap.Config.ARGB_8888;
        Bitmap b = BitmapFactory.decodeByteArray(img, 0, img.length, o);
        if (b == null) throw new IOException(L.T("O scanner não devolveu nenhuma imagem."));
        return b;
    }

    // ------------------------------------------------------------- HTTP
    static HttpURLConnection abrir(String url, int tempo) throws IOException {
        HttpURLConnection c = (HttpURLConnection) new URL(url).openConnection();
        c.setConnectTimeout(Math.min(tempo, 6000));
        c.setReadTimeout(tempo);
        c.setInstanceFollowRedirects(true);
        if (c instanceof HttpsURLConnection) confiarLocal((HttpsURLConnection) c);
        return c;
    }

    /** Scanners na rede local usam certificados auto-assinados: aceita-os só nestas ligações. */
    static void confiarLocal(HttpsURLConnection c) {
        try {
            SSLContext ctx = SSLContext.getInstance("TLS");
            ctx.init(null, new TrustManager[] { new X509TrustManager() {
                public void checkClientTrusted(X509Certificate[] a, String b) { }
                public void checkServerTrusted(X509Certificate[] a, String b) { }
                public X509Certificate[] getAcceptedIssuers() { return new X509Certificate[0]; }
            } }, null);
            c.setSSLSocketFactory(ctx.getSocketFactory());
            c.setHostnameVerifier(new HostnameVerifier() { public boolean verify(String h, SSLSession s) { return true; } });
        } catch (Exception ignored) { }
    }

    static String texto(String url, int tempo) throws IOException {
        HttpURLConnection c = abrir(url, tempo);
        try {
            if (c.getResponseCode() != 200) throw new IOException("HTTP " + c.getResponseCode());
            return new String(ler(c.getInputStream()), StandardCharsets.UTF_8);
        } finally { c.disconnect(); }
    }

    static byte[] ler(InputStream in) throws IOException {
        try (InputStream i = in) {
            ByteArrayOutputStream o = new ByteArrayOutputStream();
            byte[] b = new byte[65536];
            int n;
            while ((n = i.read(b)) > 0) o.write(b, 0, n);
            return o.toByteArray();
        }
    }

    static String tag(String xml, String nome) {
        Matcher m = Pattern.compile("<(?:\\w+:)?" + nome + ">([^<]*)</").matcher(xml);
        return m.find() ? m.group(1) : null;
    }

    static String entre(String xml, String nome) {
        Matcher m = Pattern.compile("<(?:\\w+:)?" + nome + ">([\\s\\S]*?)</(?:\\w+:)?" + nome + ">").matcher(xml);
        return m.find() ? m.group(1) : null;
    }
}
