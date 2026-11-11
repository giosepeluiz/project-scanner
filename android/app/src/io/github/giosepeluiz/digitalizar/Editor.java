// Edição rápida: girar, recortar, recorte rápido, brilho, contraste, níveis, curvas e limiar,
// com pré-visualização ao vivo. Ao aplicar, tudo é feito na resolução original da página
// (o ficheiro só é gravado quando o utilizador toca em Salvar na tela principal).
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Matrix;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.PointF;
import android.graphics.RectF;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.view.GestureDetector;
import android.view.Gravity;
import android.view.MotionEvent;
import android.view.View;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.ScrollView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Editor extends Activity {
    final Handler ui = new Handler(Looper.getMainLooper());
    Bitmap base, prevBase, mostrada;
    final Imagens.Ajustes aj = new Imagens.Ajustes();
    Imagem img;
    Curva curva;
    Ui.Deslizador dAng, dBrilho, dContr, dInP, dInB, dGama, dOutP, dOutB, dLimiar;
    Ui.Interruptor swLimiar, swRapido;
    LinearLayout painelRapido;
    TextView btnRecortar, status;
    EditText txtPx;
    ProgressBar prog;
    boolean alterado, recortando, ocupado, renderPendente, renderizando;

    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        if (Sessao.imagem == null || Sessao.doc == null) { finish(); return; }
        base = Sessao.imagem;
        prevBase = Imagens.reduzir(base, 1400);

        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Tema.BG);
        Principal.bordas(raiz);

        LinearLayout barra = Ui.linha(this);
        barra.setPadding(S(6), S(6), S(10), S(6));
        TextView fechar = Ui.icone(this, Ui.Ic.FECHAR, 19, Tema.TXT);
        fechar.setMinWidth(S(46)); fechar.setMinHeight(S(46));
        fechar.setBackground(Ui.clicavel(Tema.BG, 6, 0));
        fechar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { onBackPressed(); } });
        barra.addView(fechar);
        barra.addView(Ui.rotulo(this, L.T("Edição rápida"), 18, Tema.TXT, true), new LinearLayout.LayoutParams(0, WC, 1));
        TextView aplicar = Ui.botao(this, Ui.Ic.OK, L.T("Aplicar"), true);
        aplicar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { aplicarEdicao(); } });
        barra.addView(aplicar);
        raiz.addView(barra, Ui.lp(MP, WC));
        prog = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        prog.setIndeterminate(true);
        prog.setIndeterminateTintList(android.content.res.ColorStateList.valueOf(Tema.AZUL));
        prog.setVisibility(View.INVISIBLE);
        raiz.addView(prog, Ui.lp(MP, S(4)));

        img = new Imagem(this);
        img.setBackgroundColor(Tema.PREV);
        raiz.addView(img, new LinearLayout.LayoutParams(MP, 0, 1.05f));

        ScrollView rolo = new ScrollView(this);
        LinearLayout ferr = Ui.coluna(this);
        ferr.setPadding(S(12), S(10), S(12), S(20));
        rolo.addView(ferr);
        raiz.addView(rolo, new LinearLayout.LayoutParams(MP, 0, 1f));
        status = Ui.rotulo(this, "", 12.5f, Tema.TXT2, false);
        status.setPadding(S(14), S(4), S(14), S(6));
        raiz.addView(status, Ui.lp(MP, WC));

        // girar
        LinearLayout cg = secao(ferr, L.T("Girar"));
        dAng = slider(cg, L.T("Ângulo"), -180, 180, 0);
        dAng.sufixo = "°";
        dAng.encaixes = new int[] { -180, -135, -90, -45, 0, 45, 90, 135, 180 };
        nota(cg, L.T("Encaixa automaticamente a cada 45°. Toque duplo repõe."));
        LinearLayout rg = Ui.linha(this);
        TextView esq = Ui.botao(this, Ui.Ic.GIRAR_ESQ, "90°", false), dir = Ui.botao(this, Ui.Ic.GIRAR_DIR, "90°", false);
        esq.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { girar90(-90); } });
        dir.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { girar90(90); } });
        rg.addView(esq, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams ld = new LinearLayout.LayoutParams(0, WC, 1); ld.leftMargin = S(8);
        rg.addView(dir, ld);
        cg.addView(rg, Ui.lpM(MP, WC, 0, 6, 0, 0));

        // recortar
        LinearLayout cr = secao(ferr, L.T("Recortar"));
        LinearLayout rr = Ui.linha(this);
        btnRecortar = Ui.botao(this, Ui.Ic.RECORTAR, L.T("Recortar"), false);
        btnRecortar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { alternarRecorte(); } });
        TextView remover = Ui.botao(this, "", L.T("Remover recorte"), false);
        remover.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) { aj.recorte = null; img.sel = null; if (recortando) alternarRecorte(); else mudou(); }
        });
        rr.addView(btnRecortar, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lr = new LinearLayout.LayoutParams(0, WC, 1); lr.leftMargin = S(8);
        rr.addView(remover, lr);
        cr.addView(rr, Ui.lp(MP, WC));
        nota(cr, L.T("Arraste na imagem para marcar a área."));

        swRapido = new Ui.Interruptor(this, L.T("Recorte rápido"), Prefs.logico("recorte_rapido_on", false));
        cr.addView(swRapido, Ui.lpM(MP, WC, 0, 8, 0, 0));
        painelRapido = Ui.linha(this);
        painelRapido.addView(Ui.rotulo(this, L.T("Cortar em todas as bordas:"), 13.5f, Tema.TXT2, false), new LinearLayout.LayoutParams(0, WC, 1));
        txtPx = new EditText(this);
        txtPx.setInputType(InputType.TYPE_CLASS_NUMBER);
        txtPx.setText(String.valueOf(Prefs.inteiro("recorte_rapido_px", 3)));
        txtPx.setTextColor(Tema.TXT);
        txtPx.setGravity(Gravity.CENTER);
        txtPx.setBackground(Ui.fundo(Tema.ENTRADA, 6, Tema.ENTRADA_BORDA));
        txtPx.setPadding(S(8), S(6), S(8), S(6));
        painelRapido.addView(txtPx, Ui.lpM(S(64), WC, 6, 0, 4, 0));
        painelRapido.addView(Ui.rotulo(this, "px", 13.5f, Tema.TXT2, false), Ui.lpM(WC, WC, 0, 0, 8, 0));
        TextView aplicarR = Ui.botao(this, "", L.T("Aplicar"), true);
        aplicarR.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { recorteRapido(); } });
        painelRapido.addView(aplicarR);
        cr.addView(painelRapido, Ui.lpM(MP, WC, 0, 4, 0, 0));
        painelRapido.setVisibility(swRapido.ligado() ? View.VISIBLE : View.GONE);
        swRapido.aoMudar = new Runnable() {
            public void run() { painelRapido.setVisibility(swRapido.ligado() ? View.VISIBLE : View.GONE); Prefs.definir("recorte_rapido_on", swRapido.ligado()); }
        };

        // luz
        LinearLayout cl = secao(ferr, L.T("Luz"));
        dBrilho = slider(cl, L.T("Brilho"), -100, 100, 0);
        dContr = slider(cl, L.T("Contraste"), -100, 100, 0);

        // níveis
        LinearLayout cn = secao(ferr, L.T("Níveis"));
        dInP = slider(cn, L.T("Entrada – preto"), 0, 254, 0);
        dInB = slider(cn, L.T("Entrada – branco"), 1, 255, 255);
        dGama = slider(cn, L.T("Tons médios (gama)"), 10, 300, 100);
        dOutP = slider(cn, L.T("Saída – preto"), 0, 255, 0);
        dOutB = slider(cn, L.T("Saída – branco"), 0, 255, 255);

        // curvas
        LinearLayout cc = secao(ferr, L.T("Curvas"));
        curva = new Curva(this);
        curva.hist = Imagens.histograma(prevBase);
        cc.addView(curva, Ui.lp(MP, S(240)));
        nota(cc, L.T("Toque para adicionar ponto e arraste. Toque longo remove."));
        TextView reporC = Ui.botao(this, "", L.T("Repor curva"), false);
        reporC.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { curva.repor(); mudou(); } });
        cc.addView(reporC, Ui.lpM(WC, WC, 0, 6, 0, 0));

        // limiar
        LinearLayout ct = secao(ferr, L.T("Limiar"));
        swLimiar = new Ui.Interruptor(this, L.T("Ativar limiar (só preto e branco)"), false);
        swLimiar.aoMudar = new Runnable() { public void run() { Ui.habilitar(dLimiar, swLimiar.ligado()); mudou(); } };
        ct.addView(swLimiar, Ui.lp(MP, WC));
        dLimiar = slider(ct, L.T("Limiar"), 0, 255, 128);
        Ui.habilitar(dLimiar, false);
        dLimiar.setAlpha(1f);

        TextView repor = Ui.botao(this, Ui.Ic.DESFAZER, L.T("Repor tudo"), false);
        repor.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { reporTudo(); } });
        ferr.addView(repor, Ui.lpM(MP, WC, 0, 4, 0, 0));

        setContentView(raiz);
        renderizar();
    }

    LinearLayout secao(LinearLayout pai, String titulo) {
        LinearLayout c = Ui.card(this);
        c.addView(Ui.rotulo(this, titulo, 15.5f, Tema.TXT, true), Ui.lpM(MP, WC, 0, 0, 0, 6));
        pai.addView(c, Ui.lpM(MP, WC, 0, 0, 0, 10));
        return c;
    }

    void nota(LinearLayout c, String t) { c.addView(Ui.rotulo(this, t, 12, Tema.TXT2, false), Ui.lpM(MP, WC, 2, 2, 2, 2)); }

    Ui.Deslizador slider(LinearLayout c, String nome, int min, int max, int padrao) {
        Ui.Deslizador d = new Ui.Deslizador(this, nome, min, max, padrao);
        d.ouvinte = new Ui.Valor() { public void mudou(int v, boolean m) { if (m) Editor.this.mudou(); } };
        c.addView(d, Ui.lp(MP, WC));
        return d;
    }

    // ------------------------------------------------------------- estado dos ajustes
    void lerAjustes() {
        aj.angulo = dAng.valor();
        aj.brilho = dBrilho.valor();
        aj.contraste = dContr.valor();
        aj.inPreto = dInP.valor(); aj.inBranco = dInB.valor();
        aj.gama = dGama.valor() / 100.0;
        aj.outPreto = dOutP.valor(); aj.outBranco = dOutB.valor();
        aj.curva = curva.identidade() ? null : curva.lut();
        aj.limiar = swLimiar.ligado();
        aj.valorLimiar = dLimiar.valor();
    }

    void mudou() {
        alterado = true;
        renderizar();
    }

    /** Recalcula a pré-visualização em segundo plano (só a última pedida conta). */
    void renderizar() {
        if (renderizando) { renderPendente = true; return; }
        renderizando = true;
        lerAjustes();
        final Imagens.Ajustes a = copia(aj);
        if (recortando) a.recorte = null;
        final Bitmap src = prevBase;
        new Thread(new Runnable() {
            public void run() {
                Bitmap r = null;
                try { r = a.aplicar(src); } catch (Throwable ignored) { }
                final Bitmap fr = r;
                ui.post(new Runnable() {
                    public void run() {
                        renderizando = false;
                        if (fr != null) { mostrada = fr; img.invalidate(); }
                        if (renderPendente) { renderPendente = false; renderizar(); }
                    }
                });
            }
        }).start();
    }

    static Imagens.Ajustes copia(Imagens.Ajustes o) {
        Imagens.Ajustes a = new Imagens.Ajustes();
        a.angulo = o.angulo; a.brilho = o.brilho; a.contraste = o.contraste;
        a.inPreto = o.inPreto; a.inBranco = o.inBranco; a.gama = o.gama; a.outPreto = o.outPreto; a.outBranco = o.outBranco;
        a.curva = o.curva; a.limiar = o.limiar; a.valorLimiar = o.valorLimiar;
        a.recorte = o.recorte == null ? null : new RectF(o.recorte);
        return a;
    }

    void girar90(int d) {
        int v = dAng.valor() + d;
        while (v > 180) v -= 360;
        while (v < -180) v += 360;
        dAng.definir(v, false);
        aj.recorte = null;
        mudou();
    }

    void alternarRecorte() {
        recortando = !recortando;
        btnRecortar.setText(recortando ? Ui.comIcone(this, Ui.Ic.OK, L.T("Concluir")) : Ui.comIcone(this, Ui.Ic.RECORTAR, L.T("Recortar")));
        btnRecortar.setBackground(Ui.clicavel(recortando ? Tema.AZUL : Tema.BOTAO, 6, recortando ? 0 : Tema.BOTAO_BORDA));
        btnRecortar.setTextColor(recortando ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        if (recortando) img.sel = aj.recorte == null ? null : new RectF(aj.recorte);
        else if (img.sel != null && img.sel.width() > 0.01f && img.sel.height() > 0.01f) aj.recorte = new RectF(img.sel);
        mudou();
    }

    void recorteRapido() {
        int px;
        try { px = Integer.parseInt(txtPx.getText().toString().trim()); } catch (Exception e) { px = -1; }
        if (px < 1) { Ui.aviso(this, L.T("Digite um número de pixels válido.")); return; }
        if (px * 2 >= Math.min(base.getWidth(), base.getHeight())) { Ui.aviso(this, L.T("O corte é maior que a imagem.")); return; }
        Prefs.definir("recorte_rapido_px", px);
        base = Bitmap.createBitmap(base, px, px, base.getWidth() - 2 * px, base.getHeight() - 2 * px);
        prevBase = Imagens.reduzir(base, 1400);
        curva.hist = Imagens.histograma(prevBase);
        curva.invalidate();
        mudou();
    }

    void reporTudo() {
        dAng.definir(0, false); dBrilho.definir(0, false); dContr.definir(0, false);
        dInP.definir(0, false); dInB.definir(255, false); dGama.definir(100, false); dOutP.definir(0, false); dOutB.definir(255, false);
        curva.repor();
        swLimiar.definir(false); Ui.habilitar(dLimiar, false); dLimiar.definir(128, false);
        aj.recorte = null; img.sel = null;
        if (recortando) alternarRecorte();
        base = Sessao.imagem;
        prevBase = Imagens.reduzir(base, 1400);
        curva.hist = Imagens.histograma(prevBase);
        mudou();
        alterado = false;
    }

    // ------------------------------------------------------------- gravar
    void aplicarEdicao() {
        if (ocupado) return;
        if (recortando) alternarRecorte();
        if (!alterado) { finish(); return; }
        lerAjustes();
        ocupado = true;
        prog.setVisibility(View.VISIBLE);
        status.setText(L.T("Processando…"));
        final Imagens.Ajustes a = copia(aj);
        final Bitmap src = base;
        new Thread(new Runnable() {
            public void run() {
                try {
                    Bitmap r = a.aplicar(src);
                    Documento d = Sessao.doc;
                    d.substituirAtual(Editor.this, r);
                    Sessao.imagem = d.renderizar(d.paginas.get(d.atual));
                    ui.post(new Runnable() {
                        public void run() {
                            Intent i = new Intent();
                            i.putExtra("msg", L.T("Edição aplicada.") + " " + L.T("Toque em Salvar para guardar o arquivo."));
                            setResult(RESULT_OK, i);
                            finish();
                        }
                    });
                } catch (final Throwable t) {
                    ui.post(new Runnable() {
                        public void run() {
                            ocupado = false; prog.setVisibility(View.INVISIBLE);
                            status.setText(L.T("Falhou:") + " " + Principal.msg(t));
                        }
                    });
                }
            }
        }).start();
    }

    @Override public void onBackPressed() {
        if (recortando) { alternarRecorte(); return; }
        if (!alterado) { super.onBackPressed(); return; }
        Ui.confirmar(this, null, L.T("Descartar as alterações?"), "OK", L.T("Cancelar"), new Runnable() { public void run() { finish(); } });
    }

    // ------------------------------------------------------------- área da imagem (com seleção de recorte)
    final class Imagem extends View {
        RectF sel;           // normalizada 0..1
        int modo;            // 0 nada, 1 nova, 2 mover, 3..6 cantos
        PointF ini = new PointF();
        RectF selIni;
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG | Paint.FILTER_BITMAP_FLAG);

        Imagem(Context c) { super(c); }

        RectF destino() {
            Bitmap b = mostrada;
            if (b == null) return new RectF();
            float pad = S(12), w = getWidth() - 2 * pad, h = getHeight() - 2 * pad;
            float s = Math.min(w / b.getWidth(), h / b.getHeight());
            float dw = b.getWidth() * s, dh = b.getHeight() * s;
            return new RectF((getWidth() - dw) / 2, (getHeight() - dh) / 2, (getWidth() + dw) / 2, (getHeight() + dh) / 2);
        }

        @Override protected void onDraw(Canvas c) {
            Bitmap b = mostrada;
            if (b == null) return;
            RectF d = destino();
            c.drawBitmap(b, null, d, p);
            if (!recortando) return;
            p.setStyle(Paint.Style.FILL);
            p.setColor(0x88000000);
            if (sel == null) { c.drawRect(d, p); return; }
            RectF r = paraTela(sel, d);
            Path fora = new Path();
            fora.setFillType(Path.FillType.EVEN_ODD);
            fora.addRect(d, Path.Direction.CW);
            fora.addRect(r, Path.Direction.CW);
            c.drawPath(fora, p);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(S(2));
            p.setColor(Tema.AZUL);
            c.drawRect(r, p);
            p.setStyle(Paint.Style.FILL);
            p.setColor(Color.WHITE);
            float[][] cs = { { r.left, r.top }, { r.right, r.top }, { r.left, r.bottom }, { r.right, r.bottom } };
            for (float[] k : cs) c.drawCircle(k[0], k[1], S(8), p);
        }

        RectF paraTela(RectF n, RectF d) {
            return new RectF(d.left + n.left * d.width(), d.top + n.top * d.height(), d.left + n.right * d.width(), d.top + n.bottom * d.height());
        }

        @Override public boolean onTouchEvent(MotionEvent e) {
            if (!recortando || mostrada == null) return false;
            RectF d = destino();
            float nx = (e.getX() - d.left) / d.width(), ny = (e.getY() - d.top) / d.height();
            nx = Math.max(0, Math.min(1, nx)); ny = Math.max(0, Math.min(1, ny));
            switch (e.getActionMasked()) {
                case MotionEvent.ACTION_DOWN:
                    getParent().requestDisallowInterceptTouchEvent(true);
                    ini.set(nx, ny);
                    modo = 1;
                    if (sel != null) {
                        RectF r = paraTela(sel, d);
                        float[][] cs = { { r.left, r.top }, { r.right, r.top }, { r.left, r.bottom }, { r.right, r.bottom } };
                        for (int i = 0; i < 4; i++) if (Math.hypot(cs[i][0] - e.getX(), cs[i][1] - e.getY()) < S(32)) { modo = 3 + i; break; }
                        if (modo == 1 && r.contains(e.getX(), e.getY())) modo = 2;
                        selIni = new RectF(sel);
                    }
                    return true;
                case MotionEvent.ACTION_MOVE:
                    if (modo == 1) sel = new RectF(Math.min(ini.x, nx), Math.min(ini.y, ny), Math.max(ini.x, nx), Math.max(ini.y, ny));
                    else if (modo == 2) {
                        float dx = nx - ini.x, dy = ny - ini.y;
                        dx = Math.max(-selIni.left, Math.min(1 - selIni.right, dx));
                        dy = Math.max(-selIni.top, Math.min(1 - selIni.bottom, dy));
                        sel = new RectF(selIni.left + dx, selIni.top + dy, selIni.right + dx, selIni.bottom + dy);
                    } else if (modo >= 3) {
                        float l = selIni.left, t = selIni.top, r = selIni.right, b = selIni.bottom;
                        if (modo == 3 || modo == 5) l = nx; else r = nx;
                        if (modo == 3 || modo == 4) t = ny; else b = ny;
                        sel = new RectF(Math.min(l, r), Math.min(t, b), Math.max(l, r), Math.max(t, b));
                    }
                    invalidate();
                    return true;
                case MotionEvent.ACTION_UP:
                case MotionEvent.ACTION_CANCEL:
                    modo = 0;
                    return true;
            }
            return true;
        }
    }

    // ------------------------------------------------------------- editor de curvas
    final class Curva extends View {
        final List<PointF> pts = new ArrayList<>();
        int[] hist;
        int arrastando = -1;
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
        final GestureDetector gd;

        Curva(Context c) {
            super(c);
            repor0();
            gd = new GestureDetector(c, new GestureDetector.SimpleOnGestureListener() {
                @Override public void onLongPress(MotionEvent e) {
                    int i = perto(e.getX(), e.getY());
                    if (i > 0 && i < pts.size() - 1) { pts.remove(i); arrastando = -1; invalidate(); Editor.this.mudou(); }
                }
            });
        }

        void repor0() { pts.clear(); pts.add(new PointF(0, 0)); pts.add(new PointF(255, 255)); }
        void repor() { repor0(); invalidate(); }

        boolean identidade() {
            for (PointF q : pts) if (Math.abs(q.x - q.y) > 0.5f) return false;
            return true;
        }

        RectF area() { float m = S(10); return new RectF(m, m, getWidth() - m, getHeight() - m); }
        float tx(float v) { RectF a = area(); return a.left + v / 255f * a.width(); }
        float ty(float v) { RectF a = area(); return a.bottom - v / 255f * a.height(); }

        int perto(float x, float y) {
            for (int i = 0; i < pts.size(); i++) if (Math.hypot(tx(pts.get(i).x) - x, ty(pts.get(i).y) - y) < S(24)) return i;
            return -1;
        }

        byte[] lut() {
            int n = pts.size();
            double[] x = new double[n], y = new double[n];
            for (int i = 0; i < n; i++) { x[i] = pts.get(i).x; y[i] = pts.get(i).y; }
            double[] d = new double[n - 1], m = new double[n];
            for (int i = 0; i < n - 1; i++) d[i] = (y[i + 1] - y[i]) / Math.max(1e-6, x[i + 1] - x[i]);
            m[0] = d[0]; m[n - 1] = d[n - 2];
            for (int i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;
            for (int i = 0; i < n - 1; i++) {
                if (d[i] == 0) { m[i] = 0; m[i + 1] = 0; continue; }
                double a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
                if (s > 9) { double t = 3 / Math.sqrt(s); m[i] = t * a * d[i]; m[i + 1] = t * b * d[i]; }
            }
            byte[] lut = new byte[256];
            int seg = 0;
            for (int v = 0; v < 256; v++) {
                double r;
                if (v <= x[0]) r = y[0];
                else if (v >= x[n - 1]) r = y[n - 1];
                else {
                    while (v > x[seg + 1]) seg++;
                    double h = x[seg + 1] - x[seg], t = (v - x[seg]) / h, t2 = t * t, t3 = t2 * t;
                    r = (2 * t3 - 3 * t2 + 1) * y[seg] + (t3 - 2 * t2 + t) * h * m[seg] + (-2 * t3 + 3 * t2) * y[seg + 1] + (t3 - t2) * h * m[seg + 1];
                }
                lut[v] = (byte) Math.max(0, Math.min(255, Math.round(r)));
            }
            return lut;
        }

        @Override protected void onDraw(Canvas c) {
            RectF a = area();
            p.setStyle(Paint.Style.FILL);
            p.setColor(Tema.CURVA_BG);
            c.drawRoundRect(a, S(6), S(6), p);
            if (hist != null) {
                int max = 1;
                for (int v : hist) max = Math.max(max, v);
                p.setColor(Tema.CURVA_HIST);
                float bw = a.width() / 256f;
                for (int i = 0; i < 256; i++) {
                    float hh = (float) Math.sqrt(hist[i] / (double) max) * a.height() * 0.9f;
                    c.drawRect(a.left + i * bw, a.bottom - hh, a.left + (i + 1) * bw + 0.5f, a.bottom, p);
                }
            }
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(1);
            p.setColor(Tema.CURVA_GRADE);
            for (int i = 1; i < 4; i++) {
                c.drawLine(a.left + a.width() * i / 4, a.top, a.left + a.width() * i / 4, a.bottom, p);
                c.drawLine(a.left, a.top + a.height() * i / 4, a.right, a.top + a.height() * i / 4, p);
            }
            c.drawLine(a.left, a.bottom, a.right, a.top, p);
            byte[] l = lut();
            Path path = new Path();
            for (int v = 0; v < 256; v++) { float X = tx(v), Y = ty(l[v] & 255); if (v == 0) path.moveTo(X, Y); else path.lineTo(X, Y); }
            p.setStrokeWidth(S(2));
            p.setColor(Tema.AZUL);
            c.drawPath(path, p);
            p.setStyle(Paint.Style.FILL);
            for (int i = 0; i < pts.size(); i++) {
                p.setColor(Tema.TXT);
                c.drawCircle(tx(pts.get(i).x), ty(pts.get(i).y), S(i == arrastando ? 8 : 6), p);
                p.setColor(Tema.AZUL);
                c.drawCircle(tx(pts.get(i).x), ty(pts.get(i).y), S(i == arrastando ? 5 : 3.5f), p);
            }
        }

        @Override public boolean onTouchEvent(MotionEvent e) {
            gd.onTouchEvent(e);
            RectF a = area();
            float vx = Math.max(0, Math.min(255, (e.getX() - a.left) / a.width() * 255));
            float vy = Math.max(0, Math.min(255, (a.bottom - e.getY()) / a.height() * 255));
            switch (e.getActionMasked()) {
                case MotionEvent.ACTION_DOWN:
                    getParent().requestDisallowInterceptTouchEvent(true);
                    arrastando = perto(e.getX(), e.getY());
                    if (arrastando < 0) {
                        pts.add(new PointF(vx, vy));
                        Collections.sort(pts, new Comparator<PointF>() { public int compare(PointF p1, PointF p2) { return Float.compare(p1.x, p2.x); } });
                        for (int i = 0; i < pts.size(); i++) if (pts.get(i).x == vx && pts.get(i).y == vy) arrastando = i;
                        Editor.this.mudou();
                    }
                    invalidate();
                    return true;
                case MotionEvent.ACTION_MOVE:
                    if (arrastando < 0 || arrastando >= pts.size()) return true;
                    PointF q = pts.get(arrastando);
                    float minX = arrastando == 0 ? 0 : pts.get(arrastando - 1).x + 1;
                    float maxX = arrastando == pts.size() - 1 ? 255 : pts.get(arrastando + 1).x - 1;
                    if (arrastando == 0) maxX = Math.min(maxX, 254);
                    q.x = Math.max(minX, Math.min(maxX, vx));
                    q.y = vy;
                    invalidate();
                    Editor.this.mudou();
                    return true;
                case MotionEvent.ACTION_UP:
                case MotionEvent.ACTION_CANCEL:
                    arrastando = -1;
                    getParent().requestDisallowInterceptTouchEvent(false);
                    invalidate();
                    return true;
            }
            return true;
        }
    }
}
