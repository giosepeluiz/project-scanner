// Documento em aberto: as páginas digitalizadas ficam aqui (com a imagem original guardada)
// até o utilizador mandar salvar. O modo de cor pode mudar a qualquer momento: cada página é
// sempre recalculada a partir do original.
package io.github.giosepeluiz.digitalizar;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;

public final class Documento {

    public static final class Pagina {
        /** Imagem original (antes do modo de cor), guardada em cache. */
        public File original;
        /** O original tem cor? (digitalizações em cinza não podem voltar a ser coloridas) */
        public boolean cor;
        /** Página vinda da câmera com "Realce automático". */
        public boolean realce;
        public int dpi;
        /** Miniatura já no modo de cor atual. */
        public Bitmap mini;
    }

    public final List<Pagina> paginas = new ArrayList<>();
    public int atual;
    public String modo;
    /** Há mudanças por salvar? */
    public boolean alterado = true;

    // onde foi salvo da última vez
    public String tipoSalvo;
    public final List<Uri> uris = new ArrayList<>();
    public final List<String> nomes = new ArrayList<>();

    public Documento(String modo) { this.modo = modo; }

    public boolean salvo() { return !uris.isEmpty(); }
    public boolean temCor() { for (Pagina p : paginas) if (p.cor) return true; return false; }

    /** Modo realmente aplicado a uma página (um original em cinza não fica colorido). */
    public String modoDe(Pagina p) { return "Cor".equals(modo) && !p.cor ? "Cinza" : modo; }

    // ------------------------------------------------------------- páginas
    static File pasta(Context ctx) {
        File d = new File(ctx.getCacheDir(), "paginas");
        if (!d.exists()) d.mkdirs();
        return d;
    }

    /** Apaga os originais que já não pertencem a nenhum documento aberto. */
    public static void limpar(Context ctx, Documento manter) {
        File[] fs = pasta(ctx).listFiles();
        if (fs == null) return;
        for (File f : fs) {
            boolean usado = false;
            if (manter != null) for (Pagina p : manter.paginas) if (f.equals(p.original)) usado = true;
            if (!usado) f.delete();
        }
    }

    public Pagina adicionar(Context ctx, Bitmap original, boolean cor, boolean realce, int dpi) throws IOException {
        Pagina p = criar(ctx, original, cor, realce, dpi);
        paginas.add(p);
        atual = paginas.size() - 1;
        alterado = true;
        return p;
    }

    /** Troca a página atual pela imagem editada (que passa a ser o novo original). */
    public void substituirAtual(Context ctx, Bitmap editada) throws IOException {
        Pagina velha = paginas.get(atual);
        boolean cor = "Cor".equals(modoDe(velha)) && !Imagens.ehBinaria(editada);
        paginas.set(atual, criar(ctx, editada, cor, false, velha.dpi));
        velha.original.delete();
        alterado = true;
    }

    public void excluir(int i) {
        if (paginas.size() <= 1) return;
        Pagina p = paginas.remove(i);
        p.original.delete();
        if (atual >= paginas.size()) atual = paginas.size() - 1;
        else if (atual > i) atual--;
        alterado = true;
    }

    Pagina criar(Context ctx, Bitmap b, boolean cor, boolean realce, int dpi) throws IOException {
        Pagina p = new Pagina();
        p.cor = cor; p.realce = realce; p.dpi = dpi;
        boolean bin = Imagens.ehBinaria(b);
        p.original = new File(pasta(ctx), "o" + System.nanoTime() + (bin ? ".png" : ".jpg"));
        try (FileOutputStream o = new FileOutputStream(p.original)) {
            b.compress(bin ? Bitmap.CompressFormat.PNG : Bitmap.CompressFormat.JPEG, 95, o);
        }
        p.mini = aplicarModo(Imagens.reduzir(b, 240), p);
        return p;
    }

    // ------------------------------------------------------------- modo de cor
    /** Imagem final da página, no modo de cor atual, em resolução total. */
    public Bitmap renderizar(Pagina p) throws IOException {
        BitmapFactory.Options o = new BitmapFactory.Options();
        o.inMutable = true;
        o.inPreferredConfig = Bitmap.Config.ARGB_8888;
        Bitmap b = BitmapFactory.decodeFile(p.original.getPath(), o);
        if (b == null) throw new IOException(p.original.getName());
        return aplicarModo(b, p);
    }

    Bitmap aplicarModo(Bitmap b, Pagina p) {
        String m = modoDe(p);
        b = Imagens.mutavel(b);
        if (p.realce) {
            int w = b.getWidth(), h = b.getHeight();
            int[] px = new int[w * h];
            b.getPixels(px, 0, w, 0, 0, w, h);
            Visao.realcar(px, w, h, Config.modoVisao(m));
            b.setPixels(px, 0, w, 0, 0, w, h);
        } else Imagens.converter(b, m);
        return b;
    }

    /** Refaz as miniaturas depois de mudar o modo de cor. */
    public void atualizarMiniaturas() throws IOException {
        for (Pagina p : paginas) {
            BitmapFactory.Options o = new BitmapFactory.Options();
            o.inJustDecodeBounds = true;
            BitmapFactory.decodeFile(p.original.getPath(), o);
            int amostra = 1;
            while (Math.max(o.outWidth, o.outHeight) / amostra > 480) amostra *= 2;
            o = new BitmapFactory.Options();
            o.inSampleSize = amostra;
            o.inMutable = true;
            Bitmap b = BitmapFactory.decodeFile(p.original.getPath(), o);
            if (b != null) p.mini = aplicarModo(Imagens.reduzir(b, 240), p);
        }
    }

    // ------------------------------------------------------------- exportar / salvar
    public byte[] pdf() throws IOException {
        List<Imagens.PaginaPdf> l = new ArrayList<>();
        for (Pagina p : paginas) {
            Bitmap b = renderizar(p);
            l.add(Imagens.paginaPdf(b, modoDe(p), p.dpi));
            b.recycle();
        }
        return Imagens.pdf(l);
    }

    public byte[] imagem(int i, String tipo) throws IOException {
        Pagina p = paginas.get(i);
        Bitmap b = renderizar(p);
        byte[] d = Imagens.codificar(b, tipo, modoDe(p), p.dpi);
        b.recycle();
        return d;
    }

    /**
     * Grava na pasta de destino. Em PDF: um ficheiro com todas as páginas. Em JPEG/PNG: um ficheiro
     * por página. Se já foi salvo no mesmo formato, substitui os mesmos ficheiros.
     */
    public void salvar(Context ctx, String tipo) throws IOException {
        boolean pdf = "PDF".equals(tipo);
        int n = 1;                      // PDF: um arquivo com todas as páginas; JPEG/PNG: só a imagem que está na tela
        boolean substituir = tipo.equals(tipoSalvo) && uris.size() == n;
        if (!substituir) { uris.clear(); nomes.clear(); }
        String base = Imagens.nomeNovo(tipo);
        base = base.substring(0, base.lastIndexOf('.'));
        for (int i = 0; i < n; i++) {
            byte[] dados = pdf ? pdf() : imagem(atual, tipo);
            if (substituir) Imagens.escreverUri(ctx, uris.get(i), dados, "wt");
            else {
                String nome = base + (n > 1 ? "_p" + (i + 1) : "") + "." + Imagens.extensao(tipo);
                uris.add(Imagens.gravarNovo(ctx, nome, Imagens.mime(tipo), dados));
                nomes.add(nome);
            }
        }
        tipoSalvo = tipo;
        alterado = false;
    }

    /** Ficheiro temporário para Copiar / Compartilhar / Abrir (o documento todo em PDF, ou a página atual). */
    public File exportar(Context ctx, String tipo, boolean soImagem) throws IOException {
        boolean pdf = "PDF".equals(tipo) && !soImagem;
        String t = pdf ? "PDF" : "PNG".equals(tipo) ? "PNG" : "JPEG";
        byte[] dados = pdf ? pdf() : imagem(atual, t);
        String nome;
        if (salvo() && t.equals(tipoSalvo) && !alterado) nome = nomes.get(pdf ? 0 : Math.min(atual, nomes.size() - 1));
        else nome = "Digitalizacao" + (pdf || paginas.size() == 1 ? "" : "_p" + (atual + 1)) + "." + Imagens.extensao(t);
        return Imagens.copiaLocal(ctx, nome, dados);
    }

    static byte[] jpeg(Bitmap b) {
        ByteArrayOutputStream o = new ByteArrayOutputStream();
        b.compress(Bitmap.CompressFormat.JPEG, 92, o);
        return o.toByteArray();
    }
}
