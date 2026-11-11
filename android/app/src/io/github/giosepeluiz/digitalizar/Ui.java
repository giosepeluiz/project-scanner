// Controles próprios no estilo da versão para Windows 11: cards, botões, seletores, sliders e switch.
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.app.Dialog;
import android.content.Context;
import android.content.res.ColorStateList;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.graphics.RectF;
import android.graphics.Typeface;
import android.graphics.drawable.ColorDrawable;
import android.graphics.drawable.Drawable;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.RippleDrawable;
import android.text.InputType;
import android.util.TypedValue;
import android.view.GestureDetector;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;
import android.widget.Toast;

import static io.github.giosepeluiz.digitalizar.Tema.S;

public final class Ui {
    private Ui() { }

    /** Ícones do Font Awesome Free (fa-solid-900.ttf em assets/fontawesome). */
    public static final class Ic {
        private Ic() { }
        public static final String MENU = "\uf0c9", INFO = "\uf05a", OPCOES = "\uf013", IMPRESSORA = "\uf02f", CAMERA = "\uf030",
                ATUALIZAR = "\uf021", MAIS = "\uf067", EDITAR = "\uf044", COPIAR = "\uf0c5", PARTILHAR = "\uf1e0", PASTA = "\uf07c",
                ABRIR = "\uf35d", FECHAR = "\uf00d", RAIO = "\uf0e7", IMAGEM = "\uf03e", OK = "\uf00c", GIRAR_ESQ = "\uf2ea",
                GIRAR_DIR = "\uf2f9", RECORTAR = "\uf565", EXPANDIR = "\uf065", MAGIA = "\ue2ca", VOLTAR = "\uf060", SOL = "\uf185",
                LUA = "\uf186", METADE = "\uf042", SETA_BAIXO = "\uf078", LIXO = "\uf1f8", NOVA_PAGINA = "\ue494", PDF = "\uf1c1",
                DESFAZER = "\uf0e2", SALVAR = "\uf0c7", DIGITALIZAR = "\uf15b";
    }

    private static Typeface fa;

    public static Typeface fa(Context c) {
        if (fa == null) {
            try { fa = Typeface.createFromAsset(c.getAssets(), "fontawesome/fa-solid-900.ttf"); } catch (Exception e) { fa = Typeface.DEFAULT; }
        }
        return fa;
    }

    /** Texto com um ícone à frente (o ícone usa a fonte do Font Awesome). */
    public static CharSequence comIcone(Context c, String icone, String texto) {
        if (icone == null || icone.isEmpty()) return texto == null ? "" : texto;
        android.text.SpannableStringBuilder sb = new android.text.SpannableStringBuilder(icone);
        sb.setSpan(new android.text.style.TypefaceSpan(fa(c)), 0, icone.length(), android.text.Spanned.SPAN_EXCLUSIVE_EXCLUSIVE);
        sb.setSpan(new android.text.style.RelativeSizeSpan(0.92f), 0, icone.length(), android.text.Spanned.SPAN_EXCLUSIVE_EXCLUSIVE);
        if (texto != null && !texto.isEmpty()) sb.append("   ").append(texto);
        return sb;
    }

    /** Só o ícone, para botões redondos da barra de título. */
    public static TextView icone(Context c, String icone, float sp, int cor) {
        TextView t = rotulo(c, icone, sp, cor, false);
        t.setTypeface(fa(c));
        t.setGravity(Gravity.CENTER);
        return t;
    }

    public interface Escolha { void escolhido(int indice); }
    public interface Texto { void digitado(String texto); }
    public interface Valor { void mudou(int valor, boolean pelaMao); }

    public static GradientDrawable fundo(int cor, float raio, int borda) {
        GradientDrawable g = new GradientDrawable();
        g.setColor(cor);
        g.setCornerRadius(S(raio));
        if (borda != 0) g.setStroke(Math.max(1, S(1)), borda);
        return g;
    }

    public static Drawable clicavel(int cor, float raio, int borda) {
        int rip = Tema.escuro ? 0x33ffffff : 0x22000000;
        return new RippleDrawable(ColorStateList.valueOf(rip), fundo(cor, raio, borda), fundo(Color.WHITE, raio, 0));
    }

    public static TextView rotulo(Context c, String t, float sp, int cor, boolean negrito) {
        TextView v = new TextView(c);
        v.setText(t);
        v.setTextSize(TypedValue.COMPLEX_UNIT_SP, sp);
        v.setTextColor(cor);
        if (negrito) v.setTypeface(Typeface.create("sans-serif-medium", Typeface.NORMAL));
        return v;
    }

    public static LinearLayout.LayoutParams lp(int w, int h) { return new LinearLayout.LayoutParams(w, h); }
    public static LinearLayout.LayoutParams lpM(int w, int h, int l, int t, int r, int b) {
        LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(w, h);
        p.setMargins(S(l), S(t), S(r), S(b));
        return p;
    }
    public static final int MP = ViewGroup.LayoutParams.MATCH_PARENT, WC = ViewGroup.LayoutParams.WRAP_CONTENT;

    public static LinearLayout coluna(Context c) { LinearLayout l = new LinearLayout(c); l.setOrientation(LinearLayout.VERTICAL); return l; }
    public static LinearLayout linha(Context c) { LinearLayout l = new LinearLayout(c); l.setOrientation(LinearLayout.HORIZONTAL); l.setGravity(Gravity.CENTER_VERTICAL); return l; }

    public static LinearLayout card(Context c) {
        LinearLayout l = coluna(c);
        l.setBackground(fundo(Tema.CARD, 8, Tema.CONTORNO));
        l.setPadding(S(14), S(10), S(14), S(12));
        return l;
    }

    /** Botão arredondado; primário = cor de destaque. */
    public static TextView botao(Context c, String icone, String texto, boolean primario) {
        TextView b = new TextView(c);
        b.setText(comIcone(c, icone, texto));
        b.setTextSize(TypedValue.COMPLEX_UNIT_SP, 14);
        b.setGravity(Gravity.CENTER);
        b.setTextColor(primario ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        b.setTypeface(Typeface.create("sans-serif-medium", Typeface.NORMAL));
        b.setBackground(clicavel(primario ? Tema.AZUL : Tema.BOTAO, 6, primario ? 0 : Tema.BOTAO_BORDA));
        b.setPadding(S(14), S(10), S(14), S(10));
        b.setMinHeight(S(44));
        b.setClickable(true);
        b.setFocusable(true);
        return b;
    }

    public static void habilitar(View v, boolean sim) {
        v.setEnabled(sim);
        v.setAlpha(sim ? 1f : 0.4f);
    }

    public static void aviso(Context c, String msg) { Toast.makeText(c, msg, Toast.LENGTH_LONG).show(); }

    // ------------------------------------------------------------- seletor (card com título e valor)
    public static final class Seletor extends LinearLayout {
        public final TextView titulo, valor;
        public String[] itens = new String[0];
        public int indice = -1;
        public Escolha aoEscolher;
        public String tituloDialogo;

        public Seletor(final Activity a, String t) {
            super(a);
            setOrientation(VERTICAL);
            tituloDialogo = t;
            titulo = rotulo(a, t, 12.5f, Tema.TXT2, false);
            LinearLayout caixa = linha(a);
            caixa.setBackground(clicavel(Tema.ENTRADA, 6, Tema.ENTRADA_BORDA));
            caixa.setPadding(S(12), S(10), S(12), S(10));
            valor = rotulo(a, "", 15, Tema.TXT, false);
            valor.setSingleLine(true);
            valor.setEllipsize(android.text.TextUtils.TruncateAt.END);
            caixa.addView(valor, new LinearLayout.LayoutParams(0, WC, 1));
            TextView seta = icone(a, Ic.SETA_BAIXO, 13, Tema.AZUL);
            seta.setPadding(S(8), 0, 0, 0);
            caixa.addView(seta);
            addView(titulo, lpM(MP, WC, 0, 0, 0, 4));
            addView(caixa, lp(MP, WC));
            caixa.setOnClickListener(new OnClickListener() {
                public void onClick(View v) {
                    if (bloqueado) return;
                    lista(a, tituloDialogo, itens, indice, new Escolha() {
                        public void escolhido(int i) { definir(i); if (aoEscolher != null) aoEscolher.escolhido(i); }
                    });
                }
            });
        }

        boolean bloqueado;
        /** Trava ou destrava a escolha (o valor continua visível, só fica esmaecido) */
        public void bloquear(boolean sim) { bloqueado = sim; setAlpha(sim ? 0.5f : 1f); }
        public void definirItens(String[] i, int sel) { itens = i; definir(sel); }
        public void definir(int i) {
            indice = i;
            valor.setText(i >= 0 && i < itens.length ? itens[i] : "—");
        }
        public String atual() { return indice >= 0 && indice < itens.length ? itens[indice] : null; }
    }

    // ------------------------------------------------------------- diálogos
    public static Dialog dialogo(Activity a, View conteudo) {
        Dialog d = new Dialog(a);
        d.requestWindowFeature(Window.FEATURE_NO_TITLE);
        FrameLayout f = new FrameLayout(a);
        conteudo.setBackground(fundo(Tema.CARD, 12, Tema.BORDA));
        f.addView(conteudo);
        d.setContentView(f);
        final Window w = d.getWindow();
        if (w != null) {
            w.setBackgroundDrawable(new ColorDrawable(Color.TRANSPARENT));
            final int largura = Math.min(a.getResources().getDisplayMetrics().widthPixels - S(32), S(460));
            w.setLayout(largura, WindowManager.LayoutParams.WRAP_CONTENT);
            d.setOnShowListener(new android.content.DialogInterface.OnShowListener() {
                public void onShow(android.content.DialogInterface x) { w.setLayout(largura, WindowManager.LayoutParams.WRAP_CONTENT); }
            });
        }
        return d;
    }

    public static void lista(Activity a, String titulo, String[] itens, int sel, final Escolha e) {
        LinearLayout col = coluna(a);
        col.setPadding(S(8), S(16), S(8), S(8));
        TextView t = rotulo(a, titulo, 18, Tema.TXT, true);
        t.setPadding(S(14), 0, S(14), S(10));
        col.addView(t);
        LinearLayout itensL = coluna(a);
        ScrollView sv = new ScrollView(a);
        sv.addView(itensL);
        col.addView(sv, lp(MP, WC));
        final Dialog d = dialogo(a, col);
        for (int i = 0; i < itens.length; i++) {
            if (L.SEPARADOR.equals(itens[i])) {
                View s = new View(a);
                s.setBackgroundColor(Tema.BORDA);
                itensL.addView(s, lpM(MP, Math.max(1, S(1)), 14, 6, 14, 6));
                continue;
            }
            final int idx = i;
            LinearLayout l = linha(a);
            l.setPadding(S(14), S(12), S(14), S(12));
            l.setBackground(clicavel(i == sel ? Tema.MENU_SEL : Tema.CARD, 6, 0));
            View marca = new View(a);
            marca.setBackground(fundo(i == sel ? Tema.AZUL : Color.TRANSPARENT, 2, 0));
            l.addView(marca, lpM(S(3), S(18), 0, 0, 10, 0));
            l.addView(rotulo(a, itens[i], 15, Tema.TXT, false), new LinearLayout.LayoutParams(0, WC, 1));
            l.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { d.dismiss(); e.escolhido(idx); } });
            itensL.addView(l, lp(MP, WC));
        }
        d.show();
        // limita a altura a 70% do ecrã
        int max = (int) (a.getResources().getDisplayMetrics().heightPixels * 0.7);
        sv.measure(View.MeasureSpec.makeMeasureSpec(S(400), View.MeasureSpec.AT_MOST), View.MeasureSpec.UNSPECIFIED);
        if (sv.getMeasuredHeight() > max) { sv.getLayoutParams().height = max; sv.requestLayout(); }
    }

    public static void confirmar(Activity a, String titulo, String msg, String sim, String nao, final Runnable aoSim) {
        LinearLayout col = coluna(a);
        col.setPadding(S(22), S(20), S(22), S(16));
        if (titulo != null) col.addView(rotulo(a, titulo, 18, Tema.TXT, true), lpM(MP, WC, 0, 0, 0, 10));
        col.addView(rotulo(a, msg, 15, Tema.TXT2, false), lpM(MP, WC, 0, 0, 0, 18));
        LinearLayout bts = linha(a);
        bts.setGravity(Gravity.END);
        final Dialog d = dialogo(a, col);
        TextView bn = botao(a, null, nao, false), bs = botao(a, null, sim, true);
        bts.addView(bn, lpM(WC, WC, 0, 0, 8, 0));
        bts.addView(bs);
        col.addView(bts, lp(MP, WC));
        bn.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { d.dismiss(); } });
        bs.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { d.dismiss(); aoSim.run(); } });
        d.show();
    }

    public static void perguntar(Activity a, String titulo, String dica, String valor, boolean numero, final Texto t) {
        LinearLayout col = coluna(a);
        col.setPadding(S(22), S(20), S(22), S(16));
        col.addView(rotulo(a, titulo, 18, Tema.TXT, true), lpM(MP, WC, 0, 0, 0, 12));
        final EditText e = new EditText(a);
        e.setText(valor);
        e.setHint(dica);
        e.setSingleLine(true);
        e.setTextColor(Tema.TXT);
        e.setHintTextColor(Tema.DESAB);
        e.setInputType(numero ? InputType.TYPE_CLASS_NUMBER : InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_URI);
        e.setBackground(fundo(Tema.ENTRADA, 6, Tema.ENTRADA_BORDA));
        e.setPadding(S(12), S(10), S(12), S(10));
        col.addView(e, lpM(MP, WC, 0, 0, 0, 18));
        LinearLayout bts = linha(a);
        bts.setGravity(Gravity.END);
        final Dialog d = dialogo(a, col);
        TextView bn = botao(a, null, L.T("Cancelar"), false), bs = botao(a, null, "OK", true);
        bts.addView(bn, lpM(WC, WC, 0, 0, 8, 0));
        bts.addView(bs);
        col.addView(bts, lp(MP, WC));
        bn.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { d.dismiss(); } });
        bs.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { d.dismiss(); t.digitado(e.getText().toString()); } });
        if (d.getWindow() != null) d.getWindow().setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE);
        d.show();
        e.requestFocus();
    }

    // ------------------------------------------------------------- slider com rótulo e valor (toque duplo repõe)
    public static final class Deslizador extends View {
        final String nome; final int min, max, padrao;
        int valor; public Valor ouvinte;
        public int[] encaixes; public int tolerancia = 3;
        public String sufixo = "";
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
        final GestureDetector gd;
        boolean arrastando;

        public Deslizador(Context c, String nome, int min, int max, int padrao) {
            super(c);
            this.nome = nome; this.min = min; this.max = max; this.padrao = padrao; this.valor = padrao;
            gd = new GestureDetector(c, new GestureDetector.SimpleOnGestureListener() {
                @Override public boolean onDoubleTap(MotionEvent e) { definir(Deslizador.this.padrao, true); return true; }
            });
        }

        public int valor() { return valor; }
        public void definir(int v, boolean pelaMao) {
            v = Math.max(min, Math.min(max, v));
            if (encaixes != null) for (int e : encaixes) if (Math.abs(v - e) <= tolerancia) v = e;
            if (v == valor && pelaMao) { invalidate(); return; }
            valor = v;
            invalidate();
            if (ouvinte != null) ouvinte.mudou(v, pelaMao);
        }

        @Override protected void onMeasure(int wm, int hm) {
            setMeasuredDimension(MeasureSpec.getSize(wm), S(nome == null ? 36 : 58));
        }

        float x0() { return S(10); }
        float x1() { return getWidth() - S(10); }
        float yTrilho() { return getHeight() - S(18); }

        @Override protected void onDraw(Canvas c) {
            p.setTypeface(Typeface.DEFAULT);
            if (nome != null) {
                p.setTextSize(S(13)); p.setColor(Tema.TXT2); p.setTextAlign(Paint.Align.LEFT);
                c.drawText(nome, x0() - S(4), S(16), p);
                p.setColor(Tema.TXT); p.setTextAlign(Paint.Align.RIGHT);
                c.drawText(valor + sufixo, x1() + S(4), S(16), p);
            }
            float y = yTrilho(), t = (valor - min) / (float) (max - min), xc = x0() + (x1() - x0()) * t;
            p.setStrokeCap(Paint.Cap.ROUND);
            p.setStrokeWidth(S(4));
            p.setColor(Tema.TRILHO); c.drawLine(x0(), y, x1(), y, p);
            p.setColor(isEnabled() ? Tema.AZUL : Tema.DESAB); c.drawLine(x0(), y, xc, y, p);
            p.setStyle(Paint.Style.FILL);
            p.setColor(Tema.POLEGAR); c.drawCircle(xc, y, S(10), p);
            p.setColor(Tema.BOTAO_BORDA); p.setStyle(Paint.Style.STROKE); p.setStrokeWidth(S(1)); c.drawCircle(xc, y, S(10), p);
            p.setStyle(Paint.Style.FILL);
            p.setColor(isEnabled() ? Tema.AZUL : Tema.DESAB); c.drawCircle(xc, y, S(arrastando ? 7 : 5), p);
        }

        @Override public boolean onTouchEvent(MotionEvent e) {
            if (!isEnabled()) return false;
            gd.onTouchEvent(e);
            switch (e.getActionMasked()) {
                case MotionEvent.ACTION_DOWN:
                    getParent().requestDisallowInterceptTouchEvent(true);
                    arrastando = true;
                case MotionEvent.ACTION_MOVE:
                    float t = (e.getX() - x0()) / (x1() - x0());
                    definir(Math.round(min + t * (max - min)), true);
                    return true;
                case MotionEvent.ACTION_UP:
                case MotionEvent.ACTION_CANCEL:
                    arrastando = false;
                    getParent().requestDisallowInterceptTouchEvent(false);
                    invalidate();
                    return true;
            }
            return true;
        }
    }

    // ------------------------------------------------------------- switch no estilo Windows 11
    public static final class Interruptor extends View {
        final String texto; boolean ligado;
        public Runnable aoMudar;
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);

        public Interruptor(Context c, String texto, boolean ligado) {
            super(c);
            this.texto = texto; this.ligado = ligado;
            setClickable(true);
            setOnClickListener(new OnClickListener() { public void onClick(View v) { definir(!Interruptor.this.ligado); if (aoMudar != null) aoMudar.run(); } });
        }
        public boolean ligado() { return ligado; }
        public void definir(boolean v) { ligado = v; invalidate(); }

        @Override protected void onMeasure(int wm, int hm) { setMeasuredDimension(MeasureSpec.getSize(wm), S(40)); }

        @Override protected void onDraw(Canvas c) {
            float h = getHeight(), cy = h / 2, w = S(40), hh = S(20);
            RectF r = new RectF(S(2), cy - hh / 2, S(2) + w, cy + hh / 2);
            p.setStyle(Paint.Style.FILL);
            p.setColor(ligado ? Tema.AZUL : Color.TRANSPARENT);
            c.drawRoundRect(r, hh / 2, hh / 2, p);
            if (!ligado) { p.setStyle(Paint.Style.STROKE); p.setStrokeWidth(S(1)); p.setColor(Tema.TXT2); c.drawRoundRect(r, hh / 2, hh / 2, p); }
            p.setStyle(Paint.Style.FILL);
            p.setColor(ligado ? Tema.TXT_SOBRE_AZUL : Tema.TXT2);
            c.drawCircle(ligado ? r.right - hh / 2 : r.left + hh / 2, cy, S(ligado ? 6 : 5), p);
            p.setColor(Tema.TXT); p.setTextSize(S(14)); p.setTextAlign(Paint.Align.LEFT);
            c.drawText(texto, r.right + S(12), cy + S(5), p);
        }
    }
}
