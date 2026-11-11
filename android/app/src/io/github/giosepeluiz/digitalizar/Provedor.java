// Entrega o último ficheiro gravado a outros apps (Copiar imagem, Compartilhar, Abrir),
// sem precisar da biblioteca AndroidX.
package io.github.giosepeluiz.digitalizar;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.content.Context;
import android.database.Cursor;
import android.database.MatrixCursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import android.provider.OpenableColumns;

import java.io.File;
import java.io.FileNotFoundException;

public class Provedor extends ContentProvider {
    public static final String AUTORIDADE = "io.github.giosepeluiz.digitalizar.arquivos";

    public static Uri uri(File f) { return Uri.parse("content://" + AUTORIDADE + "/" + Uri.encode(f.getName())); }

    private File arquivo(Uri u) throws FileNotFoundException {
        Context c = getContext();
        String nome = u.getLastPathSegment();
        if (c == null || nome == null || nome.contains("/") || nome.contains("..")) throw new FileNotFoundException();
        File f = new File(new File(c.getCacheDir(), "partilha"), nome);
        if (!f.exists()) throw new FileNotFoundException(nome);
        return f;
    }

    @Override public boolean onCreate() { return true; }

    @Override public String getType(Uri u) {
        String n = u.getLastPathSegment() == null ? "" : u.getLastPathSegment().toLowerCase();
        return n.endsWith(".pdf") ? "application/pdf" : n.endsWith(".png") ? "image/png" : "image/jpeg";
    }

    @Override public ParcelFileDescriptor openFile(Uri u, String modo) throws FileNotFoundException {
        return ParcelFileDescriptor.open(arquivo(u), ParcelFileDescriptor.MODE_READ_ONLY);
    }

    @Override public Cursor query(Uri u, String[] proj, String sel, String[] args, String ord) {
        File f;
        try { f = arquivo(u); } catch (FileNotFoundException e) { return null; }
        String[] cols = proj != null ? proj : new String[] { OpenableColumns.DISPLAY_NAME, OpenableColumns.SIZE };
        MatrixCursor c = new MatrixCursor(cols);
        Object[] linha = new Object[cols.length];
        for (int i = 0; i < cols.length; i++)
            linha[i] = OpenableColumns.DISPLAY_NAME.equals(cols[i]) ? f.getName() : OpenableColumns.SIZE.equals(cols[i]) ? f.length() : null;
        c.addRow(linha);
        return c;
    }

    @Override public Uri insert(Uri u, ContentValues v) { return null; }
    @Override public int delete(Uri u, String s, String[] a) { return 0; }
    @Override public int update(Uri u, ContentValues v, String s, String[] a) { return 0; }
}
