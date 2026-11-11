// Tema claro / escuro / sistema e cor de destaque (as mesmas cores da versão para Windows).
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.Context;
import android.content.res.Configuration;
import android.graphics.Color;
import android.os.Build;
import android.view.View;
import android.view.Window;
import android.view.WindowInsetsController;

public final class Tema {
    private Tema() { }

    public static final class Destaque {
        public final String id, nome; public final int escuro, claro;
        Destaque(String id, String nome, int escuro, int claro) { this.id = id; this.nome = nome; this.escuro = 0xff000000 | escuro; this.claro = 0xff000000 | claro; }
    }

    public static final Destaque[] PALETA = {
            new Destaque("azul", "Azul", 0x4cc2ff, 0x005fb8),
            new Destaque("turquesa", "Turquesa", 0x3fd6cf, 0x00747a),
            new Destaque("verde", "Verde", 0x6ccb5f, 0x0f7b0f),
            new Destaque("lima", "Lima", 0xb4d455, 0x5a7a00),
            new Destaque("amarelo", "Âmbar", 0xffc83d, 0x9a6400),
            new Destaque("laranja", "Laranja", 0xffa35c, 0xc25700),
            new Destaque("vermelho", "Vermelho", 0xff8a80, 0xc42b1c),
            new Destaque("rosa", "Rosa", 0xff8fc8, 0xbf0077),
            new Destaque("roxo", "Roxo", 0xc29bff, 0x6b3fbf),
            new Destaque("grafite", "Grafite", 0xb8c2cc, 0x4a5560),
    };

    public static int BG, CARD, CARD_HOVER, CONTORNO, BORDA, PREV, TXT, TXT2, DESAB;
    public static int BOTAO, BOTAO_PRESS, BOTAO_BORDA, TRILHO, POLEGAR, ENTRADA, ENTRADA_BORDA;
    public static int CURVA_BG, CURVA_HIST, CURVA_GRADE, MENU_SEL;
    public static int AZUL, AZUL_PRESS, TXT_SOBRE_AZUL;
    public static boolean escuro = true;
    public static String modo = "sistema", destaque = "azul";
    public static float dp = 1f;

    public static int S(float v) { return Math.round(v * dp); }

    public static void aplicar(Activity a) {
        Prefs.iniciar(a);
        L.iniciar(a);
        dp = a.getResources().getDisplayMetrics().density;
        modo = Prefs.texto("tema", "sistema");
        destaque = Prefs.texto("destaque", "azul");
        boolean sistemaEscuro = (a.getResources().getConfiguration().uiMode & Configuration.UI_MODE_NIGHT_MASK) == Configuration.UI_MODE_NIGHT_YES;
        escuro = "sistema".equals(modo) ? sistemaEscuro : "escuro".equals(modo);
        if (escuro) {
            BG = 0xff202020; CARD = 0xff2b2b2b; CARD_HOVER = 0xff323232; CONTORNO = 0xff303030; BORDA = 0xff3a3a3a;
            PREV = 0xff272727; TXT = 0xffffffff; TXT2 = 0xffb5b5b5; DESAB = 0xff6a6a6a;
            BOTAO = 0xff343434; BOTAO_PRESS = 0xff2e2e2e; BOTAO_BORDA = 0xff444444;
            TRILHO = 0xff606060; POLEGAR = 0xff454545; ENTRADA = 0xff2d2d2d; ENTRADA_BORDA = 0xff484848;
            CURVA_BG = 0xff1a1a1a; CURVA_HIST = 0xff3a3a3a; CURVA_GRADE = 0xff333333; MENU_SEL = 0xff3a3a3a;
        } else {
            BG = 0xfff3f3f3; CARD = 0xfffbfbfb; CARD_HOVER = 0xfff5f5f5; CONTORNO = 0xffe5e5e5; BORDA = 0xffe0e0e0;
            PREV = 0xffe9e9e9; TXT = 0xff1b1b1b; TXT2 = 0xff5f5f5f; DESAB = 0xffa0a0a0;
            BOTAO = 0xfffdfdfd; BOTAO_PRESS = 0xfff0f0f0; BOTAO_BORDA = 0xffd8d8d8;
            TRILHO = 0xff8a8a8a; POLEGAR = 0xffffffff; ENTRADA = 0xffffffff; ENTRADA_BORDA = 0xffd0d0d0;
            CURVA_BG = 0xffffffff; CURVA_HIST = 0xffdedede; CURVA_GRADE = 0xffebebeb; MENU_SEL = 0xffeaeaea;
        }
        AZUL = corDestaque(a);
        AZUL_PRESS = escuro ? misturar(AZUL, Color.BLACK, 0.12) : misturar(AZUL, Color.BLACK, 0.15);
        TXT_SOBRE_AZUL = luminancia(AZUL) > 0.5 ? Color.BLACK : Color.WHITE;

        Window w = a.getWindow();
        w.setStatusBarColor(BG);
        w.setNavigationBarColor(BG);
        w.getDecorView().setBackgroundColor(BG);
        if (Build.VERSION.SDK_INT >= 30) {
            WindowInsetsController c = w.getInsetsController();
            if (c != null) {
                int m = WindowInsetsController.APPEARANCE_LIGHT_STATUS_BARS | WindowInsetsController.APPEARANCE_LIGHT_NAVIGATION_BARS;
                c.setSystemBarsAppearance(escuro ? 0 : m, m);
            }
        } else {
            int f = escuro ? 0 : (View.SYSTEM_UI_FLAG_LIGHT_STATUS_BAR | View.SYSTEM_UI_FLAG_LIGHT_NAVIGATION_BAR);
            w.getDecorView().setSystemUiVisibility(f);
        }
    }

    public static int corDestaque(Context c) {
        if ("sistema".equals(destaque)) {
            int s = corSistema(c);
            return escuro ? misturar(s, Color.WHITE, 0.45) : misturar(s, Color.BLACK, 0.12);
        }
        for (Destaque d : PALETA) if (d.id.equals(destaque)) return escuro ? d.escuro : d.claro;
        return escuro ? PALETA[0].escuro : PALETA[0].claro;
    }

    /** Cor de destaque do sistema (Material You no Android 12+). */
    public static int corSistema(Context c) {
        if (Build.VERSION.SDK_INT >= 31) {
            try { return c.getColor(android.R.color.system_accent1_500); } catch (Exception ignored) { }
        }
        return 0xff0078d4;
    }

    public static int misturar(int a, int b, double t) {
        int r = (int) (Color.red(a) + (Color.red(b) - Color.red(a)) * t);
        int g = (int) (Color.green(a) + (Color.green(b) - Color.green(a)) * t);
        int bl = (int) (Color.blue(a) + (Color.blue(b) - Color.blue(a)) * t);
        return Color.rgb(r, g, bl);
    }

    public static double luminancia(int c) {
        return (0.299 * Color.red(c) + 0.587 * Color.green(c) + 0.114 * Color.blue(c)) / 255.0;
    }
}
