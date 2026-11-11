// Opções de digitalização (as mesmas da versão para Windows e Linux).
package io.github.giosepeluiz.digitalizar;

import java.util.Locale;

public final class Config {
    private Config() { }

    public static final String VERSAO = "1.3.0";
    public static final String AUTOR = "Giosepe Luiz dos Santos";

    public static final String[] TIPOS = { "JPEG", "PNG", "PDF" };
    public static final String TIPO_PADRAO = "JPEG";
    public static final String[] MODOS = { "Cor", "Cinza", "Preto e branco" };
    public static final String MODO_PADRAO = "Preto e branco";
    public static final String[] RESOLUCOES = { "75", "150", "300", "600", "1200" };
    public static final String RESOLUCAO_PADRAO = "300";

    public static final class Tamanho {
        public final String nome; public final double l, a;
        Tamanho(String n, double l, double a) { nome = n; this.l = l; this.a = a; }
        @Override public String toString() {
            if (l <= 0) return L.T(nome);
            return L.T(nome) + " (" + num(l) + " × " + num(a) + " cm)";
        }
        static String num(double v) {
            String s = String.format(Locale.ROOT, "%.2f", v);
            while (s.contains(".") && (s.endsWith("0") || s.endsWith("."))) s = s.substring(0, s.length() - 1);
            return s;
        }
    }

    public static final Tamanho[] TAMANHOS = {
            new Tamanho("A4", 21.0, 29.7), new Tamanho("A5", 14.8, 21.0), new Tamanho("A6", 10.5, 14.8),
            new Tamanho("B5 (ISO)", 17.6, 25.0), new Tamanho("B6 (ISO)", 12.5, 17.6),
            new Tamanho("Carta (EUA)", 21.59, 27.94), new Tamanho("Executivo (EUA)", 18.42, 26.67),
            new Tamanho("Foto 10 × 15", 10.0, 15.0), new Tamanho("Foto 13 × 18", 13.0, 18.0),
            new Tamanho("Cartão de visita", 8.5, 5.5), new Tamanho("Área máxima do vidro", 0, 0),
    };
    public static final int TAMANHO_PADRAO = 0;

    public static int modoVisao(String modo) {
        return "Cor".equals(modo) ? Visao.COR : "Cinza".equals(modo) ? Visao.CINZA : Visao.PB;
    }
}
