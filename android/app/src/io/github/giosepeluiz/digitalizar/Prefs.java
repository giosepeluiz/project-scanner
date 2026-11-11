// Preferências guardadas (tudo o que o utilizador escolhe fica lembrado).
package io.github.giosepeluiz.digitalizar;

import android.content.Context;
import android.content.SharedPreferences;

public final class Prefs {
    private Prefs() { }
    private static SharedPreferences sp;

    public static void iniciar(Context ctx) {
        if (sp == null) sp = ctx.getApplicationContext().getSharedPreferences("digitalizar", Context.MODE_PRIVATE);
    }

    public static String texto(String k, String padrao) { return sp == null ? padrao : sp.getString(k, padrao); }
    public static int inteiro(String k, int padrao) { return sp == null ? padrao : sp.getInt(k, padrao); }
    public static boolean logico(String k, boolean padrao) { return sp == null ? padrao : sp.getBoolean(k, padrao); }
    public static void definir(String k, String v) { if (sp != null) sp.edit().putString(k, v).apply(); }
    public static void definir(String k, int v) { if (sp != null) sp.edit().putInt(k, v).apply(); }
    public static void definir(String k, boolean v) { if (sp != null) sp.edit().putBoolean(k, v).apply(); }
    public static void remover(String k) { if (sp != null) sp.edit().remove(k).apply(); }
}
