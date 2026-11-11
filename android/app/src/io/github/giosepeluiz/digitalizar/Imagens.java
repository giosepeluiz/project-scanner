// Processamento de imagem, gravação (JPEG, PNG, PDF) e pastas de destino.
package io.github.giosepeluiz.digitalizar;

import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Rect;
import android.graphics.RectF;
import android.net.Uri;
import android.os.Environment;
import android.provider.DocumentsContract;
import android.provider.MediaStore;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.zip.Deflater;

public final class Imagens {
    private Imagens() { }

    public static final int LIMIAR_PB = 140;
    public static final String SUBPASTA = "Scanned Documents";

    /** Preto e branco em 1 bit, comprimido (como vai dentro do PDF). */
    public static byte[] bits(Bitmap b) {
        int w = b.getWidth(), h = b.getHeight(), bl = (w + 7) / 8;
        byte[] raw = new byte[bl * h];
        int[] linha = new int[w];
        for (int y = 0; y < h; y++) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            for (int x = 0; x < w; x++) {
                int c = linha[x];
                int l = (((c >> 16) & 255) * 299 + ((c >> 8) & 255) * 587 + (c & 255) * 114) / 1000;
                if (l > 127) raw[y * bl + (x >> 3)] |= (byte) (0x80 >> (x & 7));
            }
        }
        Deflater d = new Deflater(9);
        d.setInput(raw); d.finish();
        ByteArrayOutputStream z = new ByteArrayOutputStream();
        byte[] buf = new byte[65536];
        while (!d.finished()) z.write(buf, 0, d.deflate(buf));
        d.end();
        return z.toByteArray();
    }

    // ------------------------------------------------------------- conversões
    public static void converter(Bitmap b, String modo) {
        if ("Cor".equals(modo)) return;
        int w = b.getWidth(), h = b.getHeight();
        int[] linha = new int[w];
        boolean pb = "Preto e branco".equals(modo);
        for (int y = 0; y < h; y++) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            for (int x = 0; x < w; x++) {
                int c = linha[x];
                int l = (((c >> 16) & 255) * 299 + ((c >> 8) & 255) * 587 + (c & 255) * 114) / 1000;
                if (pb) l = l > LIMIAR_PB ? 255 : 0;
                linha[x] = 0xff000000 | (l << 16) | (l << 8) | l;
            }
            b.setPixels(linha, 0, w, 0, y, w, 1);
        }
    }

    public static Bitmap mutavel(Bitmap b) {
        if (b.isMutable() && b.getConfig() == Bitmap.Config.ARGB_8888) return b;
        return b.copy(Bitmap.Config.ARGB_8888, true);
    }

    public static Bitmap reduzir(Bitmap b, int maxLado) {
        int w = b.getWidth(), h = b.getHeight();
        if (Math.max(w, h) <= maxLado) return b.copy(Bitmap.Config.ARGB_8888, true);
        float f = maxLado / (float) Math.max(w, h);
        return Bitmap.createScaledBitmap(b, Math.max(1, Math.round(w * f)), Math.max(1, Math.round(h * f)), true);
    }

    /** Gira; ângulos que não são múltiplos de 90° aumentam a tela e preenchem com branco. */
    public static Bitmap rotacionar(Bitmap src, int graus) {
        int a = ((graus % 360) + 360) % 360;
        if (a == 0) return src;
        Matrix m = new Matrix();
        m.postRotate(a);
        if (a % 90 == 0) return Bitmap.createBitmap(src, 0, 0, src.getWidth(), src.getHeight(), m, true);
        int w = src.getWidth(), h = src.getHeight();
        double rad = Math.toRadians(a), cs = Math.abs(Math.cos(rad)), sn = Math.abs(Math.sin(rad));
        int nw = (int) Math.ceil(w * cs + h * sn), nh = (int) Math.ceil(w * sn + h * cs);
        Bitmap r = Bitmap.createBitmap(nw, nh, Bitmap.Config.ARGB_8888);
        Canvas c = new Canvas(r);
        c.drawColor(Color.WHITE);
        Matrix t = new Matrix();
        t.postTranslate(-w / 2f, -h / 2f);
        t.postRotate(a);
        t.postTranslate(nw / 2f, nh / 2f);
        c.drawBitmap(src, t, new Paint(Paint.FILTER_BITMAP_FLAG | Paint.ANTI_ALIAS_FLAG));
        return r;
    }

    /** Recorta por um retângulo normalizado (0..1). */
    public static Bitmap recortar(Bitmap b, RectF n) {
        Rect r = new Rect(Math.round(n.left * b.getWidth()), Math.round(n.top * b.getHeight()),
                Math.round(n.right * b.getWidth()), Math.round(n.bottom * b.getHeight()));
        if (!r.intersect(0, 0, b.getWidth(), b.getHeight()) || r.width() < 1 || r.height() < 1) return b;
        return Bitmap.createBitmap(b, r.left, r.top, r.width(), r.height());
    }

    public static void aplicarTom(Bitmap b, byte[] lut, boolean limiar, int valorLimiar) {
        boolean ident = true;
        for (int i = 0; i < 256; i++) if ((lut[i] & 255) != i) { ident = false; break; }
        if (ident && !limiar) return;
        int w = b.getWidth(), h = b.getHeight();
        int[] linha = new int[w];
        for (int y = 0; y < h; y++) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            for (int x = 0; x < w; x++) {
                int c = linha[x];
                int r = lut[(c >> 16) & 255] & 255, g = lut[(c >> 8) & 255] & 255, bl = lut[c & 255] & 255;
                if (limiar) {
                    int l = (r * 299 + g * 587 + bl * 114) / 1000;
                    r = g = bl = l >= valorLimiar ? 255 : 0;
                }
                linha[x] = 0xff000000 | (r << 16) | (g << 8) | bl;
            }
            b.setPixels(linha, 0, w, 0, y, w, 1);
        }
    }

    public static int[] histograma(Bitmap b) {
        int[] hst = new int[256];
        int w = b.getWidth(), h = b.getHeight();
        int passo = Math.max(1, (int) Math.sqrt((double) w * h / 250000));
        int[] linha = new int[w];
        for (int y = 0; y < h; y += passo) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            for (int x = 0; x < w; x += passo) {
                int c = linha[x];
                hst[(((c >> 16) & 255) * 299 + ((c >> 8) & 255) * 587 + (c & 255) * 114) / 1000]++;
            }
        }
        return hst;
    }

    public static boolean ehBinaria(Bitmap b) {
        int w = b.getWidth(), h = b.getHeight();
        int[] linha = new int[w];
        for (int y = 0; y < h; y += Math.max(1, h / 200)) {
            b.getPixels(linha, 0, w, 0, y, w, 1);
            for (int x = 0; x < w; x += 3) {
                int c = linha[x] & 0xffffff;
                if (c != 0 && c != 0xffffff) return false;
            }
        }
        return true;
    }

    // ------------------------------------------------------------- ajustes da edição rápida
    public static final class Ajustes {
        public int angulo, brilho, contraste;
        public int inPreto = 0, inBranco = 255, outPreto = 0, outBranco = 255;
        public double gama = 1.0;
        public byte[] curva;
        public boolean limiar;
        public int valorLimiar = 128;
        public RectF recorte;

        static int B(double v) { return (int) Math.max(0, Math.min(255, Math.round(v))); }

        public byte[] lut() {
            byte[] lut = new byte[256];
            double c = contraste * 2.55;
            double f = (259.0 * (c + 255.0)) / (255.0 * (259.0 - c));
            int ib = Math.min(inPreto, 254), iw = Math.max(inBranco, ib + 1);
            for (int x = 0; x < 256; x++) {
                double v = (x - ib) / (double) (iw - ib);
                v = Math.max(0, Math.min(1, v));
                v = Math.pow(v, 1.0 / gama);
                v = outPreto + v * (outBranco - outPreto);
                if (curva != null) v = curva[B(v)] & 255;
                v = f * (v - 128) + 128 + brilho * 1.28;
                lut[x] = (byte) B(v);
            }
            return lut;
        }

        /** Aplica tudo (girar → recortar → tons) e devolve a nova imagem. */
        public Bitmap aplicar(Bitmap src) {
            Bitmap b = rotacionar(src, angulo);
            if (recorte != null) b = recortar(b, recorte);
            b = b == src ? src.copy(Bitmap.Config.ARGB_8888, true) : mutavel(b);
            aplicarTom(b, lut(), limiar, valorLimiar);
            return b;
        }
    }

    // ------------------------------------------------------------- codificação
    public static String extensao(String tipo) { return "PDF".equals(tipo) ? "pdf" : "PNG".equals(tipo) ? "png" : "jpg"; }
    public static String mime(String tipo) { return "PDF".equals(tipo) ? "application/pdf" : "PNG".equals(tipo) ? "image/png" : "image/jpeg"; }

    public static byte[] codificar(Bitmap b, String tipo, String modo, int dpi) throws IOException {
        if ("PDF".equals(tipo)) return pdf(java.util.Collections.singletonList(paginaPdf(b, modo, dpi)));
        ByteArrayOutputStream os = new ByteArrayOutputStream();
        if ("PNG".equals(tipo)) { b.compress(Bitmap.CompressFormat.PNG, 100, os); return os.toByteArray(); }
        b.compress(Bitmap.CompressFormat.JPEG, 92, os);
        byte[] j = os.toByteArray();
        // grava a resolução (DPI) no cabeçalho JFIF
        if (j.length > 20 && (j[2] & 255) == 0xFF && (j[3] & 255) == 0xE0 && j[6] == 'J' && j[7] == 'F' && j[8] == 'I' && j[9] == 'F') {
            j[13] = 1; j[14] = (byte) (dpi >> 8); j[15] = (byte) dpi; j[16] = (byte) (dpi >> 8); j[17] = (byte) dpi;
        }
        return j;
    }

    /** Uma página pronta para o PDF: JPEG, ou 1 bit comprimido para preto e branco. */
    public static final class PaginaPdf {
        public byte[] dados; public boolean pb; public int w, h, dpi;
    }

    public static PaginaPdf paginaPdf(Bitmap b, String modo, int dpi) {
        PaginaPdf p = new PaginaPdf();
        p.w = b.getWidth(); p.h = b.getHeight(); p.dpi = dpi;
        p.pb = "Preto e branco".equals(modo) || ehBinaria(b);
        if (p.pb) p.dados = bits(b);
        else { ByteArrayOutputStream os = new ByteArrayOutputStream(); b.compress(Bitmap.CompressFormat.JPEG, 90, os); p.dados = os.toByteArray(); }
        return p;
    }

    /** Gerador mínimo de PDF, uma imagem por página (como na versão para Windows). */
    public static byte[] pdf(List<PaginaPdf> paginas) throws IOException {
        int n = paginas.size();
        ByteArrayOutputStream fs = new ByteArrayOutputStream();
        List<Integer> offs = new ArrayList<>();
        escrever(fs, "%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n");
        offs.add(fs.size()); escrever(fs, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        StringBuilder kids = new StringBuilder();
        for (int i = 0; i < n; i++) kids.append(3 + 3 * i).append(" 0 R ");
        offs.add(fs.size()); escrever(fs, "2 0 obj\n<< /Type /Pages /Kids [" + kids.toString().trim() + "] /Count " + n + " >>\nendobj\n");
        for (int i = 0; i < n; i++) {
            PaginaPdf p = paginas.get(i);
            int op = 3 + 3 * i, oi = op + 1, oc = op + 2;
            String W = num(p.w * 72.0 / p.dpi), H = num(p.h * 72.0 / p.dpi);
            String conteudo = "q " + W + " 0 0 " + H + " 0 0 cm /Im0 Do Q";
            offs.add(fs.size());
            escrever(fs, op + " 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + W + " " + H
                    + "] /Resources << /XObject << /Im0 " + oi + " 0 R >> >> /Contents " + oc + " 0 R >>\nendobj\n");
            offs.add(fs.size());
            escrever(fs, oi + " 0 obj\n<< /Type /XObject /Subtype /Image /Width " + p.w + " /Height " + p.h
                    + (p.pb ? " /ColorSpace /DeviceGray /BitsPerComponent 1 /Filter /FlateDecode"
                            : " /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode")
                    + " /Length " + p.dados.length + " >>\nstream\n");
            fs.write(p.dados);
            escrever(fs, "\nendstream\nendobj\n");
            offs.add(fs.size());
            escrever(fs, oc + " 0 obj\n<< /Length " + conteudo.length() + " >>\nstream\n" + conteudo + "\nendstream\nendobj\n");
        }
        int xref = fs.size();
        escrever(fs, "xref\n0 " + (offs.size() + 1) + "\n0000000000 65535 f \n");
        for (int o : offs) escrever(fs, String.format(Locale.ROOT, "%010d 00000 n \n", o));
        escrever(fs, "trailer\n<< /Size " + (offs.size() + 1) + " /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return fs.toByteArray();
    }

    private static void escrever(ByteArrayOutputStream o, String s) throws IOException { o.write(s.getBytes(StandardCharsets.ISO_8859_1)); }
    private static String num(double v) {
        String s = String.format(Locale.ROOT, "%.2f", v);
        while (s.contains(".") && (s.endsWith("0") || s.endsWith("."))) s = s.substring(0, s.length() - 1);
        return s;
    }

    // ------------------------------------------------------------- destino
    public static String nomeNovo(String tipo) {
        return "Scan_" + new SimpleDateFormat("yyyy-MM-dd_HHmmss", Locale.ROOT).format(new Date()) + "." + extensao(tipo);
    }

    /** Nome da pasta para mostrar no card "Salvar arquivo em". */
    public static String nomePasta(Context ctx) {
        String p = Prefs.texto("pasta", "");
        if (p.isEmpty()) return Environment.DIRECTORY_DOCUMENTS + " › " + SUBPASTA;
        try {
            Uri arvore = Uri.parse(p);
            String id = DocumentsContract.getTreeDocumentId(arvore);
            int dp = id.indexOf(':');
            String caminho = dp >= 0 ? id.substring(dp + 1) : id;
            return caminho.isEmpty() ? id : caminho.replace("/", " › ");
        } catch (Exception e) { return p; }
    }

    /** Cria um ficheiro novo na pasta de destino e grava. */
    public static Uri gravarNovo(Context ctx, String nome, String mime, byte[] dados) throws IOException {
        Uri u = criar(ctx, nome, mime);
        escreverUri(ctx, u, dados, "w");
        concluirPendente(ctx, u);
        return u;
    }

    public static void escreverUri(Context ctx, Uri u, byte[] dados, String modo) throws IOException {
        try (OutputStream os = ctx.getContentResolver().openOutputStream(u, modo)) {
            if (os == null) throw new IOException("openOutputStream");
            os.write(dados);
        }
    }

    /** Cópia em cache, entregue a outros apps pelo Provedor. */
    public static File copiaLocal(Context ctx, String nome, byte[] dados) throws IOException {
        File dir = new File(ctx.getCacheDir(), "partilha");
        if (!dir.exists() && !dir.mkdirs()) throw new IOException("cache");
        File[] velhos = dir.listFiles();
        if (velhos != null) for (File f : velhos) if (!f.getName().equals(nome)) f.delete();
        File f = new File(dir, nome);
        try (FileOutputStream o = new FileOutputStream(f)) { o.write(dados); }
        return f;
    }

    private static Uri criar(Context ctx, String nome, String mime) throws IOException {
        ContentResolver cr = ctx.getContentResolver();
        String p = Prefs.texto("pasta", "");
        if (!p.isEmpty()) {
            try {
                Uri arvore = Uri.parse(p);
                Uri dir = DocumentsContract.buildDocumentUriUsingTree(arvore, DocumentsContract.getTreeDocumentId(arvore));
                Uri u = DocumentsContract.createDocument(cr, dir, mime, nome);
                if (u != null) return u;
            } catch (Exception ignored) { }
            // a pasta deixou de estar acessível: volta ao padrão
            Prefs.definir("pasta", "");
        }
        ContentValues v = new ContentValues();
        v.put(MediaStore.MediaColumns.DISPLAY_NAME, nome);
        v.put(MediaStore.MediaColumns.MIME_TYPE, mime);
        v.put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_DOCUMENTS + "/" + SUBPASTA);
        v.put(MediaStore.MediaColumns.IS_PENDING, 1);
        try {
            Uri u = cr.insert(MediaStore.Files.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY), v);
            if (u != null) return u;
        } catch (Exception ignored) { }
        // alguns aparelhos não aceitam imagens em Documentos pela coleção "Files"
        Uri col;
        if (mime.startsWith("image/")) {
            col = MediaStore.Images.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY);
            v.put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_PICTURES + "/" + SUBPASTA);
        } else {
            col = MediaStore.Downloads.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY);
            v.put(MediaStore.MediaColumns.RELATIVE_PATH, Environment.DIRECTORY_DOWNLOADS + "/" + SUBPASTA);
        }
        Uri u = cr.insert(col, v);
        if (u == null) throw new IOException("MediaStore");
        return u;
    }

    private static void concluirPendente(Context ctx, Uri uri) {
        if (!MediaStore.AUTHORITY.equals(uri.getAuthority())) return;
        try {
            ContentValues v = new ContentValues();
            v.put(MediaStore.MediaColumns.IS_PENDING, 0);
            ctx.getContentResolver().update(uri, v, null, null);
        } catch (Exception ignored) { }
    }

    /** Uri da pasta (para o botão "Abrir pasta"). */
    public static Uri uriPasta(Context ctx, Uri arquivo) {
        String p = Prefs.texto("pasta", "");
        if (!p.isEmpty()) {
            Uri arvore = Uri.parse(p);
            return DocumentsContract.buildDocumentUriUsingTree(arvore, DocumentsContract.getTreeDocumentId(arvore));
        }
        String rel = Environment.DIRECTORY_DOCUMENTS + "/" + SUBPASTA;
        if (arquivo != null && MediaStore.AUTHORITY.equals(arquivo.getAuthority())) {
            try (Cursor c = ctx.getContentResolver().query(arquivo, new String[] { MediaStore.MediaColumns.RELATIVE_PATH }, null, null, null)) {
                if (c != null && c.moveToFirst() && c.getString(0) != null) rel = c.getString(0);
            } catch (Exception ignored) { }
        }
        if (rel.endsWith("/")) rel = rel.substring(0, rel.length() - 1);
        return DocumentsContract.buildDocumentUri("com.android.externalstorage.documents", "primary:" + rel);
    }
}
