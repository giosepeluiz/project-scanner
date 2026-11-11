// Estado partilhado entre as telas.
package io.github.giosepeluiz.digitalizar;

import android.graphics.Bitmap;

public final class Sessao {
    private Sessao() { }
    /** Documento em aberto (ainda não salvo ou já salvo). */
    public static Documento doc;
    /** Página atual já no modo de cor escolhido, em resolução total. */
    public static Bitmap imagem;
    /** Foto da câmera já endireitada (ainda sem modo de cor), à espera de entrar no documento. */
    public static Bitmap daCamera;
    public static boolean realceCamera = true;
    public static int dpiCamera = 300;
}
