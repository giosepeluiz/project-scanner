// Tela principal: escolher a origem (scanner da rede ou câmera), as opções, digitalizar e agir sobre o resultado.
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.provider.DocumentsContract;
import android.view.Gravity;
import android.view.View;
import android.view.WindowInsets;
import android.widget.FrameLayout;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.ScrollView;
import android.widget.TextView;

import java.io.File;
import java.util.ArrayList;
import java.util.List;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Principal extends Activity {
    static final int REQ_CAMERA = 2, REQ_EDITOR = 3, REQ_OPCOES = 4, REQ_PASTA = 5;

    final Handler ui = new Handler(Looper.getMainLooper());
    List<Escl.Scanner> scanners = new ArrayList<>();
    Ui.Seletor selScanner, selTamanho, selTipo, selModo, selRes;
    TextView txtPasta, status, btnDigitalizar, btnSalvar, btnRede, btnCamera, placeholder;
    LinearLayout painel, cardScanner, acoes;
    ImageView previa;
    ProgressBar progresso;
    View btnDetectar;
    boolean ocupado, acrescentar, paginaCamera;
    String origem;
    LinearLayout faixa, miniaturas;
    TextView btnAbrirPasta;
    TextView tituloPaginas, btnNovaPagina;

    // ------------------------------------------------------------- montagem da tela
    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        origem = Prefs.texto("origem", "rede");

        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Tema.BG);
        bordas(raiz);

        // barra de título
        LinearLayout barra = Ui.linha(this);
        barra.setPadding(S(6), S(6), S(6), S(6));
        TextView menu = icone(Ui.Ic.MENU);
        menu.setContentDescription(L.T("Recolher / expandir painel"));
        menu.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { alternarPainel(); } });
        barra.addView(menu);
        ImageView logo = new ImageView(this);
        logo.setImageResource(R.mipmap.ic_launcher);
        barra.addView(logo, Ui.lpM(S(26), S(26), 4, 0, 10, 0));
        barra.addView(Ui.rotulo(this, "Digitalizar", 19, Tema.TXT, true), new LinearLayout.LayoutParams(0, WC, 1));
        TextView sobre = icone(Ui.Ic.INFO);
        sobre.setContentDescription(L.T("Sobre o Digitalizar"));
        sobre.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { startActivity(new Intent(Principal.this, Sobre.class)); } });
        TextView opcoes = icone(Ui.Ic.OPCOES);
        opcoes.setContentDescription(L.T("Opções"));
        opcoes.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { startActivityForResult(new Intent(Principal.this, Opcoes.class), REQ_OPCOES); } });
        barra.addView(sobre);
        barra.addView(opcoes);
        raiz.addView(barra, Ui.lp(MP, WC));

        ScrollView rolo = new ScrollView(this);
        rolo.setFillViewport(true);
        LinearLayout corpo = Ui.coluna(this);
        corpo.setPadding(S(14), S(4), S(14), S(14));
        rolo.addView(corpo);
        raiz.addView(rolo, new LinearLayout.LayoutParams(MP, 0, 1));

        // painel de opções (recolhível)
        painel = Ui.coluna(this);
        painel.setLayoutTransition(new android.animation.LayoutTransition());
        corpo.addView(painel, Ui.lp(MP, WC));

        painel.addView(Ui.rotulo(this, L.T("Origem"), 12.5f, Tema.TXT2, false), Ui.lpM(MP, WC, 2, 0, 0, 4));
        LinearLayout seg = Ui.linha(this);
        btnRede = Ui.botao(this, Ui.Ic.IMPRESSORA, L.T("Scanner da rede"), false);
        btnCamera = Ui.botao(this, Ui.Ic.CAMERA, L.T("Câmera"), false);
        seg.addView(btnRede, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lc = new LinearLayout.LayoutParams(0, WC, 1);
        lc.leftMargin = S(8);
        seg.addView(btnCamera, lc);
        painel.addView(seg, Ui.lpM(MP, WC, 0, 0, 0, 12));
        btnRede.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { definirOrigem("rede"); } });
        btnCamera.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { definirOrigem("camera"); } });

        cardScanner = Ui.coluna(this);
        selScanner = new Ui.Seletor(this, L.T("Scanner"));
        selScanner.aoEscolher = new Ui.Escolha() { public void escolhido(int i) { Prefs.definir("scanner", scanners.get(i).id()); } };
        cardScanner.addView(selScanner, Ui.lp(MP, WC));
        LinearLayout lsc = Ui.linha(this);
        TextView det = Ui.botao(this, Ui.Ic.ATUALIZAR, L.T("Detectar"), false);
        det.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { detectar(); } });
        btnDetectar = det;
        TextView ip = Ui.botao(this, Ui.Ic.MAIS, L.T("Adicionar por IP"), false);
        ip.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { adicionarIp(); } });
        lsc.addView(det, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams li = new LinearLayout.LayoutParams(0, WC, 1);
        li.leftMargin = S(8);
        lsc.addView(ip, li);
        cardScanner.addView(lsc, Ui.lpM(MP, WC, 0, 8, 0, 0));
        painel.addView(cardScanner, Ui.lpM(MP, WC, 0, 0, 0, 12));

        selTamanho = new Ui.Seletor(this, L.T("Tamanho do documento"));
        String[] tams = new String[Config.TAMANHOS.length];
        for (int i = 0; i < tams.length; i++) tams[i] = Config.TAMANHOS[i].toString();
        selTamanho.definirItens(tams, Math.min(Prefs.inteiro("tamanho", Config.TAMANHO_PADRAO), tams.length - 1));
        selTamanho.aoEscolher = new Ui.Escolha() { public void escolhido(int i) { Prefs.definir("tamanho", i); } };
        painel.addView(selTamanho, Ui.lpM(MP, WC, 0, 0, 0, 12));

        selTipo = new Ui.Seletor(this, L.T("Tipo de arquivo"));
        selTipo.definirItens(Config.TIPOS, indice(Config.TIPOS, Prefs.texto("tipo", Config.TIPO_PADRAO)));
        selTipo.aoEscolher = new Ui.Escolha() { public void escolhido(int i) { Prefs.definir("tipo", Config.TIPOS[i]); mudouTipo(); } };

        selModo = new Ui.Seletor(this, L.T("Modo de cor"));
        String[] modos = new String[Config.MODOS.length];
        for (int i = 0; i < modos.length; i++) modos[i] = L.T(Config.MODOS[i]);
        selModo.definirItens(modos, indice(Config.MODOS, Prefs.texto("modo", Config.MODO_PADRAO)));
        selModo.aoEscolher = new Ui.Escolha() { public void escolhido(int i) { Prefs.definir("modo", Config.MODOS[i]); mudarModo(Config.MODOS[i]); } };

        selRes = new Ui.Seletor(this, L.T("Resolução (DPI)"));
        selRes.definirItens(Config.RESOLUCOES, indice(Config.RESOLUCOES, Prefs.texto("resolucao", Config.RESOLUCAO_PADRAO)));
        selRes.aoEscolher = new Ui.Escolha() { public void escolhido(int i) { Prefs.definir("resolucao", Config.RESOLUCOES[i]); } };

        LinearLayout duas = Ui.linha(this);
        duas.addView(selTipo, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lm = new LinearLayout.LayoutParams(0, WC, 1);
        lm.leftMargin = S(8);
        duas.addView(selModo, lm);
        painel.addView(duas, Ui.lpM(MP, WC, 0, 0, 0, 12));
        painel.addView(selRes, Ui.lpM(MP, WC, 0, 0, 0, 12));

        // pasta de destino
        LinearLayout cp = Ui.linha(this);
        cp.setBackground(Ui.clicavel(Tema.CARD, 8, Tema.CONTORNO));
        cp.setPadding(S(14), S(10), S(10), S(10));
        LinearLayout cpt = Ui.coluna(this);
        cpt.addView(Ui.rotulo(this, L.T("Salvar arquivo em"), 12.5f, Tema.TXT2, false));
        txtPasta = Ui.rotulo(this, "", 14.5f, Tema.TXT, false);
        cpt.addView(txtPasta);
        cp.addView(cpt, new LinearLayout.LayoutParams(0, WC, 1));
        TextView padrao = Ui.rotulo(this, L.T("Padrão"), 14, Tema.AZUL, true);
        padrao.setPadding(S(10), S(8), S(6), S(8));
        padrao.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { Prefs.definir("pasta", ""); txtPasta.setText(Imagens.nomePasta(Principal.this)); } });
        cp.addView(padrao);
        cp.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { escolherPasta(); } });
        painel.addView(cp, Ui.lpM(MP, WC, 0, 0, 0, 12));
        txtPasta.setText(Imagens.nomePasta(this));

        // pré-visualização
        FrameLayout cartao = new FrameLayout(this);
        cartao.setBackground(Ui.fundo(Tema.PREV, 8, Tema.CONTORNO));
        previa = new ImageView(this);
        previa.setScaleType(ImageView.ScaleType.FIT_CENTER);
        previa.setPadding(S(8), S(8), S(8), S(8));
        cartao.addView(previa, new FrameLayout.LayoutParams(MP, MP));
        placeholder = Ui.rotulo(this, L.T("A pré-visualização aparece aqui\ndepois de digitalizar."), 14, Tema.TXT2, false);
        placeholder.setGravity(Gravity.CENTER);
        cartao.addView(placeholder, new FrameLayout.LayoutParams(MP, MP));
        int alt = (int) (getResources().getDisplayMetrics().heightPixels * 0.5);
        corpo.addView(cartao, Ui.lpM(MP, alt, 0, 0, 0, 10));
        previa.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { if (Sessao.imagem != null) abrirEditor(); } });

        // páginas do PDF
        faixa = Ui.card(this);
        LinearLayout cabP = Ui.linha(this);
        tituloPaginas = Ui.rotulo(this, "", 15, Tema.TXT, true);
        cabP.addView(tituloPaginas, new LinearLayout.LayoutParams(0, WC, 1));
        btnNovaPagina = Ui.botao(this, Ui.Ic.NOVA_PAGINA, L.T("Adicionar página"), true);
        btnNovaPagina.setTextSize(13);
        btnNovaPagina.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { adicionarPagina(); } });
        cabP.addView(btnNovaPagina);
        faixa.addView(cabP, Ui.lp(MP, WC));
        android.widget.HorizontalScrollView hs = new android.widget.HorizontalScrollView(this);
        hs.setHorizontalScrollBarEnabled(false);
        miniaturas = Ui.linha(this);
        hs.addView(miniaturas);
        faixa.addView(hs, Ui.lpM(MP, WC, 0, 10, 0, 2));
        faixa.addView(Ui.rotulo(this, L.T("Toque em uma página para ver ou editar."), 12, Tema.TXT2, false), Ui.lpM(MP, WC, 0, 4, 0, 0));
        faixa.setVisibility(View.GONE);
        corpo.addView(faixa, Ui.lpM(MP, WC, 0, 0, 0, 10));

        // ações depois de digitalizar
        acoes = Ui.coluna(this);
        LinearLayout a1 = Ui.linha(this), a2 = Ui.linha(this);
        a1.addView(acao(Ui.Ic.EDITAR, L.T("Edição rápida"), new Runnable() { public void run() { abrirEditor(); } }), new LinearLayout.LayoutParams(0, WC, 1));
        a1.addView(acao(Ui.Ic.COPIAR, L.T("Copiar imagem"), new Runnable() { public void run() { copiar(); } }), pesoMargem());
        a2.addView(acao(Ui.Ic.PARTILHAR, L.T("Compartilhar"), new Runnable() { public void run() { compartilhar(); } }), new LinearLayout.LayoutParams(0, WC, 1));
        a2.addView(acao(Ui.Ic.ABRIR, L.T("Abrir"), new Runnable() { public void run() { abrir(); } }), pesoMargem());
        btnAbrirPasta = acao(Ui.Ic.PASTA, L.T("Abrir pasta"), new Runnable() { public void run() { abrirPasta(); } });
        a2.addView(btnAbrirPasta, pesoMargem());
        acoes.addView(a1, Ui.lp(MP, WC));
        acoes.addView(a2, Ui.lpM(MP, WC, 0, 8, 0, 0));
        corpo.addView(acoes, Ui.lp(MP, WC));

        // rodapé: estado + botão principal
        LinearLayout rodape = Ui.coluna(this);
        rodape.setPadding(S(14), S(4), S(14), S(12));
        progresso = new ProgressBar(this, null, android.R.attr.progressBarStyleHorizontal);
        progresso.setIndeterminate(true);
        progresso.setIndeterminateTintList(android.content.res.ColorStateList.valueOf(Tema.AZUL));
        progresso.setVisibility(View.INVISIBLE);
        rodape.addView(progresso, Ui.lp(MP, S(6)));
        status = Ui.rotulo(this, "", 13, Tema.TXT2, false);
        status.setMaxLines(3);
        rodape.addView(status, Ui.lpM(MP, WC, 2, 2, 2, 8));
        btnDigitalizar = Ui.botao(this, "", L.T("Digitalizar (ação)"), true);
        btnDigitalizar.setTextSize(16);
        btnDigitalizar.setMinHeight(S(52));
        btnDigitalizar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { digitalizar(); } });
        btnSalvar = Ui.botao(this, Ui.Ic.SALVAR, L.T("Salvar"), true);
        btnSalvar.setTextSize(16);
        btnSalvar.setMinHeight(S(52));
        btnSalvar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { salvar(); } });
        LinearLayout botoes = Ui.linha(this);
        botoes.addView(btnDigitalizar, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams ls = new LinearLayout.LayoutParams(0, WC, 1);
        ls.leftMargin = S(8);
        botoes.addView(btnSalvar, ls);
        rodape.addView(botoes, Ui.lp(MP, WC));
        raiz.addView(rodape, Ui.lp(MP, WC));

        setContentView(raiz);
        if (Prefs.logico("painel_recolhido", false)) painel.setVisibility(View.GONE);
        definirOrigem(origem);
        atualizarPrevia();
        Atualizador.limpar(this);
        Atualizador.verificarAuto(this);
    }

    LinearLayout.LayoutParams pesoMargem() {
        LinearLayout.LayoutParams p = new LinearLayout.LayoutParams(0, WC, 1);
        p.leftMargin = S(8);
        return p;
    }

    TextView acao(String ic, String t, final Runnable r) {
        TextView b = Ui.botao(this, ic, t, false);
        b.setTextSize(13);
        b.setPadding(S(6), S(10), S(6), S(10));
        b.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { r.run(); } });
        return b;
    }

    TextView icone(String g) {
        TextView t = Ui.icone(this, g, 19, Tema.TXT);
        t.setBackground(Ui.clicavel(Tema.BG, 6, 0));
        t.setMinWidth(S(46));
        t.setMinHeight(S(46));
        t.setClickable(true);
        return t;
    }

    /** No Android 15 as telas vão até às bordas: afasta o conteúdo das barras do sistema. */
    static void bordas(final View raiz) {
        raiz.setOnApplyWindowInsetsListener(new View.OnApplyWindowInsetsListener() {
            public WindowInsets onApplyWindowInsets(View v, WindowInsets ins) {
                android.graphics.Insets s = ins.getInsets(WindowInsets.Type.systemBars() | WindowInsets.Type.displayCutout() | WindowInsets.Type.ime());
                v.setPadding(s.left, s.top, s.right, s.bottom);
                return WindowInsets.CONSUMED;
            }
        });
    }

    static int indice(String[] a, String v) {
        for (int i = 0; i < a.length; i++) if (a[i].equals(v)) return i;
        return 0;
    }

    void alternarPainel() {
        boolean recolher = painel.getVisibility() == View.VISIBLE;
        painel.setVisibility(recolher ? View.GONE : View.VISIBLE);
        Prefs.definir("painel_recolhido", recolher);
    }

    void definirOrigem(String o) {
        origem = o;
        Prefs.definir("origem", o);
        boolean rede = "rede".equals(o);
        btnRede.setBackground(Ui.clicavel(rede ? Tema.AZUL : Tema.BOTAO, 6, rede ? 0 : Tema.BOTAO_BORDA));
        btnRede.setTextColor(rede ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        btnCamera.setBackground(Ui.clicavel(!rede ? Tema.AZUL : Tema.BOTAO, 6, !rede ? 0 : Tema.BOTAO_BORDA));
        btnCamera.setTextColor(!rede ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        cardScanner.setVisibility(rede ? View.VISIBLE : View.GONE);
        btnDigitalizar.setText(Ui.comIcone(this, rede ? Ui.Ic.DIGITALIZAR : Ui.Ic.CAMERA, L.T("Digitalizar (ação)")));
        if (rede && scanners.isEmpty() && !ocupado && selScanner.itens.length == 0) detectar();
    }

    // ------------------------------------------------------------- estado
    void ocupar(boolean sim, String msg) {
        ocupado = sim;
        progresso.setVisibility(sim ? View.VISIBLE : View.INVISIBLE);
        if (msg != null) status.setText(msg);
        Ui.habilitar(btnDigitalizar, !sim);
        pintarRodape();
        Ui.habilitar(btnDetectar, !sim);
        atualizarAcoes();
    }

    String tipo() { return Config.TIPOS[selTipo.indice]; }
    String modo() { return Config.MODOS[selModo.indice]; }

    void atualizarAcoes() {
        Documento d = Sessao.doc;
        boolean tem = d != null && Sessao.imagem != null && !ocupado;
        if (btnNovaPagina != null) Ui.habilitar(btnNovaPagina, tem);
        if (miniaturas != null) for (int i = 0; i < miniaturas.getChildCount(); i++) miniaturas.getChildAt(i).setEnabled(tem);
        for (int i = 0; i < acoes.getChildCount(); i++) {
            LinearLayout l = (LinearLayout) acoes.getChildAt(i);
            for (int j = 0; j < l.getChildCount(); j++) Ui.habilitar(l.getChildAt(j), tem);
        }
        Ui.habilitar(btnAbrirPasta, tem && d.salvo());
        pintarRodape();
    }

    /** Antes de digitalizar o botão principal é "Digitalizar"; depois, o destaque passa para "Salvar". */
    void pintarRodape() {
        if (btnSalvar == null) return;
        Documento d = Sessao.doc;
        boolean porSalvar = d != null && d.alterado;
        btnSalvar.setVisibility(d == null ? View.GONE : View.VISIBLE);
        Ui.habilitar(btnSalvar, porSalvar && !ocupado);
        btnDigitalizar.setBackground(Ui.clicavel(d == null ? Tema.AZUL : Tema.BOTAO, 6, d == null ? 0 : Tema.BOTAO_BORDA));
        btnDigitalizar.setTextColor(d == null ? Tema.TXT_SOBRE_AZUL : Tema.TXT);
        btnSalvar.setText(Ui.comIcone(this, porSalvar ? Ui.Ic.SALVAR : Ui.Ic.OK, porSalvar ? L.T("Salvar") : L.T("Salvo")));
    }

    void atualizarPrevia() {
        if (Sessao.imagem != null) {
            Bitmap p = Imagens.reduzir(Sessao.imagem, 1600);
            previa.setImageBitmap(p);
            placeholder.setVisibility(View.GONE);
        } else {
            previa.setImageDrawable(null);
            placeholder.setVisibility(View.VISIBLE);
        }
        atualizarPaginas();
        atualizarAcoes();
    }

    // ------------------------------------------------------------- páginas do PDF
    void atualizarPaginas() {
        final Documento d = Sessao.doc;
        boolean pdf = "PDF".equals(tipo());
        // páginas só existem com o tipo PDF escolhido; JPEG e PNG guardam a imagem que está na tela
        if (d == null || Sessao.imagem == null || !pdf) { faixa.setVisibility(View.GONE); return; }
        faixa.setVisibility(View.VISIBLE);
        btnNovaPagina.setVisibility(View.VISIBLE);
        int n = d.paginas.size();
        tituloPaginas.setText(L.T("Páginas") + " · " + L.F("Página {0} de {1}", d.atual + 1, n));
        miniaturas.removeAllViews();
        for (int i = 0; i < n; i++) {
            final int idx = i;
            FrameLayout f = new FrameLayout(this);
            f.setPadding(S(3), S(3), S(3), S(3));
            f.setBackground(Ui.fundo(i == d.atual ? Tema.AZUL : Tema.PREV, 6, i == d.atual ? 0 : Tema.BORDA));
            ImageView iv = new ImageView(this);
            iv.setScaleType(ImageView.ScaleType.CENTER_CROP);
            iv.setImageBitmap(d.paginas.get(i).mini);
            f.addView(iv, new FrameLayout.LayoutParams(S(70), S(96)));
            TextView num = Ui.rotulo(this, String.valueOf(i + 1), 12, Color.WHITE, true);
            num.setGravity(Gravity.CENTER);
            num.setBackground(Ui.fundo(0xaa000000, 9, 0));
            num.setPadding(S(7), S(1), S(7), S(1));
            f.addView(num, new FrameLayout.LayoutParams(WC, WC, Gravity.BOTTOM | Gravity.START));
            if (n > 1) {
                TextView lixo = Ui.icone(this, Ui.Ic.LIXO, 12, Color.WHITE);
                lixo.setBackground(Ui.clicavel(0xccc42b1c, 14, 0));
                lixo.setContentDescription(L.T("Excluir página"));
                lixo.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { excluirPagina(idx); } });
                f.addView(lixo, new FrameLayout.LayoutParams(S(28), S(28), Gravity.TOP | Gravity.END));
            }
            f.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { selecionarPagina(idx); } });
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(WC, WC);
            lp.rightMargin = S(8);
            miniaturas.addView(f, lp);
        }
    }

    void adicionarPagina() {
        if (ocupado || Sessao.doc == null || !"PDF".equals(tipo())) return;
        acrescentar = true;
        digitalizar();
    }

    void selecionarPagina(final int i) {
        final Documento d = Sessao.doc;
        if (ocupado || d == null || i == d.atual) return;
        ocupar(true, null);
        new Thread(new Runnable() {
            public void run() {
                try {
                    final Bitmap b = d.renderizar(d.paginas.get(i));
                    ui.post(new Runnable() { public void run() { d.atual = i; Sessao.imagem = b; atualizarPrevia(); ocupar(false, null); } });
                } catch (final Throwable e) {
                    ui.post(new Runnable() { public void run() { ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
                }
            }
        }).start();
    }

    void excluirPagina(final int i) {
        final Documento d = Sessao.doc;
        if (ocupado || d == null || d.paginas.size() <= 1) return;
        Ui.confirmar(this, L.T("Excluir página"), L.F("Excluir a página {0}?", i + 1), L.T("Excluir"), L.T("Cancelar"), new Runnable() {
            public void run() {
                ocupar(true, null);
                new Thread(new Runnable() {
                    public void run() {
                        try {
                            d.excluir(i);
                            final Bitmap b = d.renderizar(d.paginas.get(d.atual));
                            ui.post(new Runnable() { public void run() { Sessao.imagem = b; atualizarPrevia(); ocupar(false, L.F("Página {0} excluída.", i + 1)); } });
                        } catch (final Throwable e) {
                            ui.post(new Runnable() { public void run() { atualizarPrevia(); ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
                        }
                    }
                }).start();
            }
        });
    }

    /** O modo de cor vale também para o que já foi digitalizado. */
    void mudarModo(String modo) {
        final Documento d = Sessao.doc;
        if (d == null || modo.equals(d.modo)) return;
        d.modo = modo;
        d.alterado = true;
        final String aviso = "Cor".equals(modo) && !d.temCor()
                ? L.T("Esta digitalização foi feita sem cor: para ter cor, digitalize de novo no modo Cor.") : null;
        ocupar(true, L.T("Processando…"));
        new Thread(new Runnable() {
            public void run() {
                try {
                    d.atualizarMiniaturas();
                    final Bitmap b = d.renderizar(d.paginas.get(d.atual));
                    ui.post(new Runnable() { public void run() { Sessao.imagem = b; atualizarPrevia(); ocupar(false, aviso != null ? aviso : L.T("Toque em Salvar para guardar o arquivo.")); } });
                } catch (final Throwable e) {
                    ui.post(new Runnable() { public void run() { ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
                }
            }
        }).start();
    }

    void mudouTipo() {
        Documento d = Sessao.doc;
        if (d == null) return;
        if (d.salvo() && !tipo().equals(d.tipoSalvo)) d.alterado = true;
        atualizarPrevia();
    }

    // ------------------------------------------------------------- scanner da rede    // ------------------------------------------------------------- scanner da rede
    void detectar() {
        ocupar(true, L.T("Procurando scanners…"));
        Escl.detectar(this, 5000, new Escl.Retorno() {
            public void pronto(List<Escl.Scanner> l) {
                if (isFinishing()) return;
                scanners = l;
                String[] nomes = new String[l.size()];
                int sel = l.isEmpty() ? -1 : 0;
                String guardado = Prefs.texto("scanner", "");
                for (int i = 0; i < l.size(); i++) { nomes[i] = l.get(i).nome; if (l.get(i).id().equals(guardado)) sel = i; }
                selScanner.definirItens(nomes, sel);
                ocupar(false, l.isEmpty()
                        ? L.T("Nenhum scanner encontrado. Verifique se o celular e a impressora estão na mesma rede Wi-Fi e toque em 'Detectar'.")
                        : L.F("{0} scanner(s) encontrado(s). Pronto.", l.size()));
            }
        });
    }

    void adicionarIp() {
        Ui.perguntar(this, L.T("Adicionar por IP"), L.T("Endereço IP do scanner") + " (192.168.1.50)", "", false, new Ui.Texto() {
            public void digitado(final String ip) {
                if (ip.trim().isEmpty()) return;
                ocupar(true, L.T("Procurando scanners…"));
                new Thread(new Runnable() {
                    public void run() {
                        final Escl.Scanner s = Escl.provarIp(ip);
                        ui.post(new Runnable() {
                            public void run() {
                                if (s == null) { ocupar(false, L.T("Não foi encontrado nenhum scanner nesse endereço.")); return; }
                                Escl.adicionarManual(s);
                                Prefs.definir("scanner", s.id());
                                ocupar(false, null);
                                detectar();
                            }
                        });
                    }
                }).start();
            }
        });
    }

    void digitalizar() {
        if (ocupado) { acrescentar = false; return; }
        final boolean pag = acrescentar;
        acrescentar = false;
        if (!pag && Sessao.doc != null && Sessao.doc.alterado) {
            Ui.confirmar(this, null, L.T("Descartar a digitalização não salva?"), L.T("Descartar"), L.T("Cancelar"),
                    new Runnable() { public void run() { iniciar(false); } });
            return;
        }
        iniciar(pag);
    }

    void iniciar(final boolean pag) {
        if ("camera".equals(origem)) {
            paginaCamera = pag;
            Intent i = new Intent(this, Camera.class);
            startActivityForResult(i, REQ_CAMERA);
            return;
        }
        if (selScanner.indice < 0 || selScanner.indice >= scanners.size()) {
            status.setText(L.T("Selecione um scanner (clique em 'Detectar')."));
            return;
        }
        final String modo = Config.MODOS[selModo.indice];
        final int dpi = Integer.parseInt(Config.RESOLUCOES[selRes.indice]);
        if (dpi >= 1200 && "Cor".equals(modo)) {
            Ui.confirmar(this, null, L.T("1200 DPI em cor gera um arquivo muito grande e demora vários minutos. Continuar?"),
                    "OK", L.T("Cancelar"), new Runnable() { public void run() { executarScan(modo, dpi, pag); } });
        } else executarScan(modo, dpi, pag);
    }

    void executarScan(final String modo, final int dpi, final boolean pag) {
        final Escl.Scanner s = scanners.get(selScanner.indice);
        final Config.Tamanho t = Config.TAMANHOS[selTamanho.indice];
        ocupar(true, L.F("Digitalizando ({0}, {1}, {2} DPI)…", s.nome, L.T(modo), dpi));
        new Thread(new Runnable() {
            public void run() {
                try {
                    Bitmap b = Escl.digitalizar(s, modo, dpi, t.l, t.a, null);
                    receber(b, "Cor".equals(modo), false, dpi, pag);
                } catch (final Throwable e) {
                    ui.post(new Runnable() { public void run() { ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
                }
            }
        }).start();
    }

    static String msg(Throwable e) {
        if (e instanceof OutOfMemoryError) return "memória insuficiente / out of memory";
        String m = e.getMessage();
        if (e instanceof java.net.SocketTimeoutException || e instanceof java.net.ConnectException) return L.T("O scanner não respondeu. Verifique a rede Wi-Fi.");
        return m == null ? e.getClass().getSimpleName() : m;
    }

    /** Põe a imagem digitalizada no documento em aberto (nada é salvo até tocar em Salvar). */
    void receber(Bitmap b, boolean cor, boolean realce, int dpi, boolean comoPagina) {
        final boolean pagina = comoPagina && Sessao.doc != null;
        try {
            final Documento d;
            if (pagina) d = Sessao.doc;
            else {
                Sessao.imagem = null;
                Documento.limpar(this, null);
                d = new Documento(modo());
            }
            d.adicionar(this, b, cor, realce, dpi);
            final Bitmap r = d.renderizar(d.paginas.get(d.atual));
            b.recycle();
            ui.post(new Runnable() {
                public void run() {
                    Sessao.doc = d;
                    Sessao.imagem = r;
                    atualizarPrevia();
                    ocupar(false, (pagina ? L.F("Página {0} adicionada.", d.paginas.size()) + " " : L.T("Digitalizado.") + " ")
                            + L.T("Toque em Salvar para guardar o arquivo."));
                }
            });
        } catch (final Throwable e) {
            ui.post(new Runnable() { public void run() { atualizarPrevia(); ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
        }
    }

    void salvar() {
        final Documento d = Sessao.doc;
        if (ocupado || d == null) return;
        final String tipo = tipo();
        ocupar(true, L.T("Salvando…"));
        new Thread(new Runnable() {
            public void run() {
                try {
                    d.salvar(Principal.this, tipo);
                    final StringBuilder nomes = new StringBuilder();
                    for (String n : d.nomes) nomes.append(nomes.length() > 0 ? ", " : "").append(n);
                    ui.post(new Runnable() { public void run() { atualizarPrevia(); ocupar(false, L.T("Salvo:") + " " + nomes); } });
                } catch (final Throwable e) {
                    ui.post(new Runnable() { public void run() { ocupar(false, L.T("Não foi possível salvar:") + " " + msg(e)); } });
                }
            }
        }).start();
    }

    @Override public void onBackPressed() {
        if (Sessao.doc != null && Sessao.doc.alterado) {
            Ui.confirmar(this, null, L.T("Há uma digitalização não salva. Sair mesmo assim?"), L.T("Sair"), L.T("Cancelar"),
                    new Runnable() { public void run() { finish(); } });
            return;
        }
        super.onBackPressed();
    }

    // ------------------------------------------------------------- resultados das outras telas
    @Override protected void onActivityResult(int req, int res, Intent data) {
        super.onActivityResult(req, res, data);
        if (req == REQ_OPCOES) { recreate(); return; }
        if (req == REQ_CAMERA && res == RESULT_OK && Sessao.daCamera != null) {
            final Bitmap b = Sessao.daCamera;
            Sessao.daCamera = null;
            ocupar(true, L.T("Processando…"));
            final boolean pag = paginaCamera, realce = Sessao.realceCamera;
            paginaCamera = false;
            new Thread(new Runnable() { public void run() { receber(b, true, realce, Sessao.dpiCamera, pag); } }).start();
            return;
        }
        if (req == REQ_EDITOR && res == RESULT_OK) {
            atualizarPrevia();
            if (data != null && data.getStringExtra("msg") != null) status.setText(data.getStringExtra("msg"));
            return;
        }
        if (req == REQ_PASTA && res == RESULT_OK && data != null && data.getData() != null) {
            Uri u = data.getData();
            try {
                getContentResolver().takePersistableUriPermission(u, Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION);
            } catch (Exception ignored) { }
            Prefs.definir("pasta", u.toString());
            txtPasta.setText(Imagens.nomePasta(this));
        }
    }

    void escolherPasta() {
        Intent i = new Intent(Intent.ACTION_OPEN_DOCUMENT_TREE);
        i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION | Intent.FLAG_GRANT_WRITE_URI_PERMISSION | Intent.FLAG_GRANT_PERSISTABLE_URI_PERMISSION);
        try { startActivityForResult(i, REQ_PASTA); } catch (Exception e) { Ui.aviso(this, L.T("Escolher pasta de destino")); }
    }

    void abrirEditor() {
        if (Sessao.imagem == null) return;
        startActivityForResult(new Intent(this, Editor.class), REQ_EDITOR);
    }

    interface ComArquivo { void pronto(File f); }

    /** Gera o ficheiro temporário (PDF inteiro ou a página atual) em segundo plano. */
    void exportar(final boolean soImagem, final ComArquivo r) {
        final Documento d = Sessao.doc;
        if (d == null || ocupado) return;
        final String tipo = tipo();
        ocupar(true, null);
        new Thread(new Runnable() {
            public void run() {
                try {
                    final File f = d.exportar(Principal.this, tipo, soImagem);
                    ui.post(new Runnable() { public void run() { ocupar(false, null); r.pronto(f); } });
                } catch (final Throwable e) {
                    ui.post(new Runnable() { public void run() { ocupar(false, L.T("Falhou:") + " " + msg(e)); } });
                }
            }
        }).start();
    }

    static String mimeDe(File f) {
        String n = f.getName().toLowerCase();
        return n.endsWith(".pdf") ? "application/pdf" : n.endsWith(".png") ? "image/png" : "image/jpeg";
    }

    void copiar() {
        exportar(true, new ComArquivo() {
            public void pronto(File f) {
                ClipboardManager cm = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
                cm.setPrimaryClip(ClipData.newUri(getContentResolver(), f.getName(), Provedor.uri(f)));
                if (Build.VERSION.SDK_INT < 33) Ui.aviso(Principal.this, L.T("Imagem copiada. Cole onde quiser."));
                status.setText(L.T("Imagem copiada. Cole onde quiser."));
            }
        });
    }

    void compartilhar() {
        exportar(false, new ComArquivo() {
            public void pronto(File f) {
                Uri u = Provedor.uri(f);
                Intent i = new Intent(Intent.ACTION_SEND);
                i.setType(mimeDe(f));
                i.putExtra(Intent.EXTRA_STREAM, u);
                i.setClipData(ClipData.newRawUri(f.getName(), u));
                i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                startActivity(Intent.createChooser(i, L.T("Compartilhar")));
            }
        });
    }

    void abrir() {
        exportar(false, new ComArquivo() {
            public void pronto(File f) {
                Intent i = new Intent(Intent.ACTION_VIEW);
                i.setDataAndType(Provedor.uri(f), mimeDe(f));
                i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
                try { startActivity(i); } catch (Exception e) { Ui.aviso(Principal.this, L.T("Nenhum aplicativo para abrir este arquivo.")); }
            }
        });
    }

    void abrirPasta() {
        Documento d = Sessao.doc;
        Uri u = Imagens.uriPasta(this, d != null && d.salvo() ? d.uris.get(0) : null);
        Intent i = new Intent(Intent.ACTION_VIEW);
        i.setDataAndType(u, DocumentsContract.Document.MIME_TYPE_DIR);
        i.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try { startActivity(i); return; } catch (Exception ignored) { }
        try {
            Intent j = new Intent(Intent.ACTION_VIEW);
            j.setDataAndType(u, "vnd.android.document/root");
            startActivity(j);
            return;
        } catch (Exception ignored) { }
        Ui.aviso(this, L.T("Não foi possível abrir a pasta. Os arquivos estão em:") + " " + Imagens.nomePasta(this));
    }
}
