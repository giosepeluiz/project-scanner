// Revisão da foto: a inteligência marca os cantos do documento; o utilizador pode ajustá-los
// (com lupa) e escolher o filtro. "Aplicar" endireita a folha e transforma a foto num scan.
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.RectF;
import android.media.ExifInterface;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import java.io.File;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Revisao extends Activity {
    final Handler ui = new Handler(Looper.getMainLooper());
    Bitmap foto;
    Cantos cantos;
    TextView dica, btnRealce, btnOriginal, btnAplicar;
    ProgressBar prog;
    boolean realce = true, ocupado;

    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        realce = Prefs.logico("filtro_realce", true);
        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Tema.BG);
        Principal.bordas(raiz);

        dica = Ui.rotulo(this, L.T("Processando…"), 14, Tema.TXT2, false);
        dica.setGravity(Gravity.CENTER);
        dica.setPadding(S(16), S(12), S(16), S(8));
        raiz.addView(dica, Ui.lp(MP, WC));
        prog = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        prog.setIndeterminate(true);
        prog.setIndeterminateTintList(android.content.res.ColorStateList.valueOf(Tema.AZUL));
        raiz.addView(prog, Ui.lp(MP, S(6)));

        cantos = new Cantos(this);
        raiz.addView(cantos, new LinearLayout.LayoutParams(MP, 0, 1));

        LinearLayout filtros = Ui.linha(this);
        filtros.setPadding(S(14), S(10), S(14), S(4));
        btnRealce = Ui.botao(this, Ui.Ic.MAGIA, L.T("Realce automático"), false);
        btnOriginal = Ui.botao(this, Ui.Ic.IMAGEM, L.T("Original"), false);
        btnRealce.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { realce = true; Prefs.definir("filtro_realce", true); pintar(); } });
        btnOriginal.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { realce = false; Prefs.definir("filtro_realce", false); pintar(); } });
        filtros.addView(btnRealce, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lo = new LinearLayout.LayoutParams(0, WC, 1);
        lo.leftMargin = S(8);
        filtros.addView(btnOriginal, lo);
        raiz.addView(filtros, Ui.lp(MP, WC));

        LinearLayout bts = Ui.linha(this);
        bts.setPadding(S(14), S(6), S(14), S(14));
        TextView refazer = Ui.botao(this, Ui.Ic.GIRAR_ESQ, L.T("Refazer"), false);
        refazer.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { setResult(RESULT_CANCELED); finish(); } });
        TextView inteira = Ui.botao(this, Ui.Ic.EXPANDIR, L.T("Imagem inteira"), false);
        inteira.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) { if (foto != null) { cantos.q = Visao.Quad.inteiro(foto.getWidth(), foto.getHeight(), 0); cantos.invalidate(); } }
        });
        btnAplicar = Ui.botao(this, Ui.Ic.OK, L.T("Aplicar"), true);
        btnAplicar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { aplicar(); } });
        bts.addView(refazer, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams l2 = new LinearLayout.LayoutParams(0, WC, 1); l2.leftMargin = S(8);
        bts.addView(inteira, l2);
        LinearLayout.LayoutParams l3 = new LinearLayout.LayoutParams(0, WC, 1); l3.leftMargin = S(8);
        bts.addView(btnAplicar, l3);
        raiz.addView(bts, Ui.lp(MP, WC));
        setContentView(raiz);
        pintar();
        Ui.habilitar(btnAplicar, false);

        final float[] dicaCamera = getIntent().getFloatArrayExtra("quad");
        new Thread(new Runnable() { public void run() { carregar(dicaCamera); } }).start();
    }

    void pintar() {
        btnRealce.setBackground(Ui.clicavel(realce ? Tema.AZUL : Tema.BOTAO, 6, realce ? 0 : Tema.BOTAO_BORDA));
        btnRealce.setTextColor(realce ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        btnOriginal.setBackground(Ui.clicavel(!realce ? Tema.AZUL : Tema.BOTAO, 6, !realce ? 0 : Tema.BOTAO_BORDA));
        btnOriginal.setTextColor(!realce ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
    }

    // ------------------------------------------------------------- carregar + detetar
    static Bitmap lerFoto(File f, int maxLado) {
        BitmapFactory.Options o = new BitmapFactory.Options();
        o.inJustDecodeBounds = true;
        BitmapFactory.decodeFile(f.getPath(), o);
        int amostra = 1;
        while (Math.max(o.outWidth, o.outHeight) / amostra > maxLado) amostra *= 2;
        o = new BitmapFactory.Options();
        o.inSampleSize = amostra;
        o.inPreferredConfig = Bitmap.Config.ARGB_8888;
        Bitmap b = BitmapFactory.decodeFile(f.getPath(), o);
        if (b == null) return null;
        int graus = 0;
        try {
            int or = new ExifInterface(f.getPath()).getAttributeInt(ExifInterface.TAG_ORIENTATION, ExifInterface.ORIENTATION_NORMAL);
            graus = or == ExifInterface.ORIENTATION_ROTATE_90 ? 90 : or == ExifInterface.ORIENTATION_ROTATE_180 ? 180 : or == ExifInterface.ORIENTATION_ROTATE_270 ? 270 : 0;
        } catch (Exception ignored) { }
        if (graus != 0) {
            Matrix m = new Matrix();
            m.postRotate(graus);
            Bitmap r = Bitmap.createBitmap(b, 0, 0, b.getWidth(), b.getHeight(), m, true);
            if (r != b) b.recycle();
            b = r;
        }
        return b;
    }

    static int[] cinza(Bitmap b, int maxLado, float[] escala) {
        float f = Math.min(1f, maxLado / (float) Math.max(b.getWidth(), b.getHeight()));
        int w = Math.max(1, Math.round(b.getWidth() * f)), h = Math.max(1, Math.round(b.getHeight() * f));
        Bitmap p = Bitmap.createScaledBitmap(b, w, h, true);
        int[] px = new int[w * h];
        p.getPixels(px, 0, w, 0, 0, w, h);
        if (p != b) p.recycle();
        int[] t = new int[3];
        int[] g = Visao.cinzaReduzido(px, w, h, Math.max(w, h), t);
        escala[0] = w; escala[1] = h; escala[2] = b.getWidth() / (float) w;
        return g;
    }

    void carregar(float[] dicaCamera) {
        String msg;
        Visao.Quad q = null;
        try {
            final Bitmap b = lerFoto(new File(getCacheDir(), "camera.jpg"), 4096);
            if (b == null) throw new Exception("?");
            foto = b;
            float[] e1 = new float[3], e2 = new float[3];
            int[] g1 = cinza(b, 320, e1);
            Visao.Quad d = Visao.detectar(g1, (int) e1[0], (int) e1[1]);
            if (d != null) {
                int[] g2 = cinza(b, 1400, e2);
                float k = e1[2] / e2[2];
                Visao.Quad r = Visao.refinar(d.escalar(k, k), g2, (int) e2[0], (int) e2[1]);
                q = r.escalar(e2[2], e2[2]);
                msg = L.T("Arraste os cantos para ajustar e toque em Aplicar.");
            } else if (dicaCamera != null) {
                q = new Visao.Quad();
                for (int i = 0; i < 4; i++) { q.x[i] = dicaCamera[2 * i] * b.getWidth(); q.y[i] = dicaCamera[2 * i + 1] * b.getHeight(); }
                msg = L.T("Arraste os cantos para ajustar e toque em Aplicar.");
            } else {
                q = Visao.Quad.inteiro(b.getWidth(), b.getHeight(), 0.06f);
                msg = L.T("Nenhum documento detectado: arraste os cantos para marcar a folha.");
            }
        } catch (Throwable t) {
            msg = L.T("Falhou:") + " " + Principal.msg(t);
        }
        final Visao.Quad fq = q;
        final String fm = msg;
        ui.post(new Runnable() {
            public void run() {
                prog.setVisibility(View.INVISIBLE);
                dica.setText(fm);
                if (fq != null) { cantos.q = fq; cantos.invalidate(); Ui.habilitar(btnAplicar, true); }
            }
        });
    }

    // ------------------------------------------------------------- aplicar
    void aplicar() {
        if (ocupado || foto == null || cantos.q == null) return;
        ocupado = true;
        Ui.habilitar(btnAplicar, false);
        prog.setVisibility(View.VISIBLE);
        dica.setText(L.T("Processando…"));
        final Visao.Quad q = cantos.q.copia();
        q.ordenar();
        final boolean comRealce = realce;
        new Thread(new Runnable() {
            public void run() {
                try {
                    Config.Tamanho papel = Config.TAMANHOS[Math.min(Prefs.inteiro("tamanho", 0), Config.TAMANHOS.length - 1)];
                    int dpiEscolhido = Integer.parseInt(Prefs.texto("resolucao", Config.RESOLUCAO_PADRAO));
                    int[] medido = Visao.tamanhoSaida(q);
                    int[] tam = Visao.ajustarAoPapel(medido, papel.l, papel.a);
                    boolean ehPapel = tam != medido; // ajustarAoPapel devolve um array novo quando encaixa no papel
                    tam = tam.clone();
                    float lim = 4200f / Math.max(tam[0], tam[1]);
                    if (lim < 1) { tam[0] = Math.round(tam[0] * lim); tam[1] = Math.round(tam[1] * lim); }
                    int tw = Math.max(16, tam[0]), th = Math.max(16, tam[1]);
                    Bitmap out = Bitmap.createBitmap(tw, th, Bitmap.Config.ARGB_8888);
                    Matrix m = new Matrix();
                    m.setPolyToPoly(new float[] { q.x[0], q.y[0], q.x[1], q.y[1], q.x[2], q.y[2], q.x[3], q.y[3] }, 0,
                            new float[] { 0, 0, tw, 0, tw, th, 0, th }, 0, 4);
                    Canvas c = new Canvas(out);
                    c.drawColor(Color.WHITE);
                    c.drawBitmap(foto, m, new Paint(Paint.FILTER_BITMAP_FLAG | Paint.ANTI_ALIAS_FLAG));
                    // o realce e o modo de cor são aplicados pelo documento (podem mudar depois)
                    Sessao.realceCamera = comRealce;
                    int dpi = dpiEscolhido;
                    if (ehPapel) {
                        double ladoCm = tw >= th ? Math.max(papel.l, papel.a) : Math.min(papel.l, papel.a);
                        dpi = (int) Math.round(tw / (ladoCm / 2.54));
                    }
                    Sessao.dpiCamera = Math.max(72, Math.min(1200, dpi));
                    Sessao.daCamera = out;
                    ui.post(new Runnable() { public void run() { setResult(RESULT_OK); finish(); } });
                } catch (final Throwable t) {
                    ui.post(new Runnable() {
                        public void run() {
                            ocupado = false; prog.setVisibility(View.INVISIBLE); Ui.habilitar(btnAplicar, true);
                            dica.setText(L.T("Falhou:") + " " + Principal.msg(t));
                        }
                    });
                }
            }
        }).start();
    }

    // ------------------------------------------------------------- vista dos cantos com lupa
    final class Cantos extends View {
        Visao.Quad q;
        int arrastando = -1;
        float tx, ty;
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG | Paint.FILTER_BITMAP_FLAG);
        final Matrix mImg = new Matrix(), inv = new Matrix();

        Cantos(Context c) { super(c); }

        RectF destino() {
            float pad = S(24);
            float w = getWidth() - 2 * pad, h = getHeight() - 2 * pad;
            float s = Math.min(w / foto.getWidth(), h / foto.getHeight());
            float dw = foto.getWidth() * s, dh = foto.getHeight() * s;
            return new RectF((getWidth() - dw) / 2, (getHeight() - dh) / 2, (getWidth() + dw) / 2, (getHeight() + dh) / 2);
        }

        @Override protected void onDraw(Canvas c) {
            if (foto == null) return;
            RectF d = destino();
            mImg.setRectToRect(new RectF(0, 0, foto.getWidth(), foto.getHeight()), d, Matrix.ScaleToFit.FILL);
            mImg.invert(inv);
            c.drawBitmap(foto, mImg, p);
            if (q == null) return;
            float[] pts = new float[8];
            for (int i = 0; i < 4; i++) { pts[2 * i] = q.x[i]; pts[2 * i + 1] = q.y[i]; }
            mImg.mapPoints(pts);
            Path path = new Path();
            path.moveTo(pts[0], pts[1]);
            for (int i = 1; i < 4; i++) path.lineTo(pts[2 * i], pts[2 * i + 1]);
            path.close();
            // escurece fora da folha
            Path fora = new Path();
            fora.addRect(d, Path.Direction.CW);
            fora.setFillType(Path.FillType.EVEN_ODD);
            fora.addPath(path);
            p.setStyle(Paint.Style.FILL);
            p.setColor(0x88000000);
            c.drawPath(fora, p);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(S(2.5f));
            p.setColor(Tema.AZUL);
            c.drawPath(path, p);
            for (int i = 0; i < 4; i++) {
                p.setStyle(Paint.Style.FILL);
                p.setColor(Color.WHITE);
                c.drawCircle(pts[2 * i], pts[2 * i + 1], S(i == arrastando ? 13 : 11), p);
                p.setColor(Tema.AZUL);
                c.drawCircle(pts[2 * i], pts[2 * i + 1], S(i == arrastando ? 9 : 7), p);
            }
            if (arrastando >= 0) lupa(c, pts[2 * arrastando], pts[2 * arrastando + 1]);
        }

        void lupa(Canvas c, float sx, float sy) {
            float r = S(58);
            float cx = sx < getWidth() / 2f ? getWidth() - r - S(16) : r + S(16), cy = r + S(16);
            c.save();
            Path circ = new Path();
            circ.addCircle(cx, cy, r, Path.Direction.CW);
            c.clipPath(circ);
            c.drawColor(Color.BLACK);
            Matrix z = new Matrix(mImg);
            z.postScale(3f, 3f, sx, sy);
            z.postTranslate(cx - sx, cy - sy);
            c.drawBitmap(foto, z, p);
            c.restore();
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(S(3));
            p.setColor(Color.WHITE);
            c.drawCircle(cx, cy, r, p);
            p.setStrokeWidth(S(1.5f));
            p.setColor(Tema.AZUL);
            c.drawLine(cx - S(12), cy, cx + S(12), cy, p);
            c.drawLine(cx, cy - S(12), cx, cy + S(12), p);
        }

        @Override public boolean onTouchEvent(MotionEvent e) {
            if (q == null || foto == null || ocupado) return false;
            float[] pt = { e.getX(), e.getY() };
            switch (e.getActionMasked()) {
                case MotionEvent.ACTION_DOWN: {
                    float[] pts = new float[8];
                    for (int i = 0; i < 4; i++) { pts[2 * i] = q.x[i]; pts[2 * i + 1] = q.y[i]; }
                    mImg.mapPoints(pts);
                    float melhor = S(48);
                    arrastando = -1;
                    for (int i = 0; i < 4; i++) {
                        float dd = (float) Math.hypot(pts[2 * i] - e.getX(), pts[2 * i + 1] - e.getY());
                        if (dd < melhor) { melhor = dd; arrastando = i; }
                    }
                    if (arrastando < 0) return false;
                    tx = pts[2 * arrastando] - e.getX(); ty = pts[2 * arrastando + 1] - e.getY();
                    getParent().requestDisallowInterceptTouchEvent(true);
                    invalidate();
                    return true;
                }
                case MotionEvent.ACTION_MOVE:
                    if (arrastando < 0) return false;
                    pt[0] += tx; pt[1] += ty;
                    inv.mapPoints(pt);
                    q.x[arrastando] = Math.max(0, Math.min(foto.getWidth(), pt[0]));
                    q.y[arrastando] = Math.max(0, Math.min(foto.getHeight(), pt[1]));
                    invalidate();
                    return true;
                case MotionEvent.ACTION_UP:
                case MotionEvent.ACTION_CANCEL:
                    arrastando = -1;
                    invalidate();
                    return true;
            }
            return true;
        }
    }
}
