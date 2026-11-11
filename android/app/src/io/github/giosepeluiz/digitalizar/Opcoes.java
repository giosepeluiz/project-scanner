// Opções: idioma, tema e cor de destaque (tudo fica guardado).
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.Intent;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.Paint;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.GridLayout;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Opcoes extends Activity {
    TextView reiniciar;

    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Tema.BG);
        Principal.bordas(raiz);

        LinearLayout barra = Ui.linha(this);
        barra.setPadding(S(6), S(6), S(6), S(6));
        TextView voltar = Ui.icone(this, Ui.Ic.VOLTAR, 19, Tema.TXT);
        voltar.setMinWidth(S(46)); voltar.setMinHeight(S(46));
        voltar.setBackground(Ui.clicavel(Tema.BG, 6, 0));
        voltar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { finish(); } });
        barra.addView(voltar);
        barra.addView(Ui.rotulo(this, L.T("Opções"), 19, Tema.TXT, true));
        raiz.addView(barra);

        ScrollView sv = new ScrollView(this);
        LinearLayout c = Ui.coluna(this);
        c.setPadding(S(14), S(4), S(14), S(20));
        sv.addView(c);
        raiz.addView(sv, new LinearLayout.LayoutParams(MP, 0, 1));

        // idioma
        LinearLayout ci = Ui.card(this);
        ci.addView(Ui.rotulo(this, L.T("Idioma"), 16, Tema.TXT, true), Ui.lpM(MP, WC, 0, 0, 0, 8));
        final Ui.Seletor sel = new Ui.Seletor(this, L.T("Idioma da interface"));
        String[] nomes = new String[L.CODIGOS.length];
        int atual = 0;
        for (int i = 0; i < nomes.length; i++) { nomes[i] = L.CODIGOS[i].equals(L.SEPARADOR) ? L.SEPARADOR : L.nome(L.CODIGOS[i]); if (L.CODIGOS[i].equals(L.escolhido)) atual = i; }
        sel.definirItens(nomes, atual);
        sel.aoEscolher = new Ui.Escolha() {
            public void escolhido(int i) {
                Prefs.definir("idioma", L.CODIGOS[i]);
                reiniciar.setVisibility(View.VISIBLE);
            }
        };
        ci.addView(sel, Ui.lp(MP, WC));
        ci.addView(Ui.rotulo(this, L.T("O novo idioma é aplicado ao reiniciar o programa."), 12.5f, Tema.TXT2, false), Ui.lpM(MP, WC, 0, 8, 0, 4));
        reiniciar = Ui.botao(this, Ui.Ic.ATUALIZAR, L.T("Reiniciar agora"), true);
        reiniciar.setVisibility(View.GONE);
        reiniciar.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                L.reiniciar(Opcoes.this);
                Intent i = new Intent(Opcoes.this, Principal.class);
                i.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
                startActivity(i);
                finish();
            }
        });
        ci.addView(reiniciar, Ui.lpM(WC, WC, 0, 6, 0, 0));
        c.addView(ci, Ui.lpM(MP, WC, 0, 0, 0, 12));

        // tema
        LinearLayout ct = Ui.card(this);
        ct.addView(Ui.rotulo(this, L.T("Tema"), 16, Tema.TXT, true), Ui.lpM(MP, WC, 0, 0, 0, 6));
        String[][] temas = { { "claro", Ui.Ic.SOL, L.T("Claro") }, { "escuro", Ui.Ic.LUA, L.T("Escuro") }, { "sistema", Ui.Ic.METADE, L.T("Usar o tema do sistema") } };
        for (final String[] t : temas) {
            boolean ativo = t[0].equals(Tema.modo);
            LinearLayout l = Ui.linha(this);
            l.setPadding(S(10), S(12), S(10), S(12));
            l.setBackground(Ui.clicavel(ativo ? Tema.MENU_SEL : Tema.CARD, 6, 0));
            View marca = new View(this);
            marca.setBackground(Ui.fundo(ativo ? Tema.AZUL : Color.TRANSPARENT, 2, 0));
            l.addView(marca, Ui.lpM(S(3), S(18), 0, 0, 10, 0));
            TextView tx = Ui.rotulo(this, "", 15, Tema.TXT, false);
            tx.setText(Ui.comIcone(this, t[1], t[2]));
            l.addView(tx);
            l.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { Prefs.definir("tema", t[0]); recreate(); } });
            ct.addView(l, Ui.lp(MP, WC));
        }
        c.addView(ct, Ui.lpM(MP, WC, 0, 0, 0, 12));

        // cor de destaque
        LinearLayout cd = Ui.card(this);
        cd.addView(Ui.rotulo(this, L.T("Cor de destaque"), 16, Tema.TXT, true), Ui.lpM(MP, WC, 0, 0, 0, 8));
        GridLayout g = new GridLayout(this);
        g.setColumnCount(6);
        g.addView(bolinha("sistema", Tema.escuro ? Tema.misturar(Tema.corSistema(this), Color.WHITE, 0.45) : Tema.misturar(Tema.corSistema(this), Color.BLACK, 0.12), L.T("Cor de destaque do sistema")));
        for (Tema.Destaque d : Tema.PALETA) g.addView(bolinha(d.id, Tema.escuro ? d.escuro : d.claro, L.T(d.nome)));
        cd.addView(g, Ui.lp(WC, WC));
        TextView nomeCor = Ui.rotulo(this, "", 13, Tema.TXT2, false);
        String n = L.T("Cor de destaque do sistema");
        for (Tema.Destaque d : Tema.PALETA) if (d.id.equals(Tema.destaque)) n = L.T(d.nome);
        nomeCor.setText(n + " · " + L.T("O tom é ajustado automaticamente para o modo claro ou escuro."));
        cd.addView(nomeCor, Ui.lpM(MP, WC, 0, 8, 0, 0));
        c.addView(cd, Ui.lpM(MP, WC, 0, 0, 0, 12));

        TextView fechar = Ui.botao(this, "", L.T("Fechar"), false);
        fechar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { finish(); } });
        c.addView(fechar, Ui.lp(MP, WC));
        setContentView(raiz);
    }

    View bolinha(final String id, final int cor, String nome) {
        final boolean ativo = id.equals(Tema.destaque);
        View v = new View(this) {
            final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
            @Override protected void onDraw(Canvas c) {
                float cx = getWidth() / 2f, cy = getHeight() / 2f, r = Math.min(cx, cy) - S(6);
                p.setStyle(Paint.Style.FILL);
                p.setColor(cor);
                c.drawCircle(cx, cy, r, p);
                if ("sistema".equals(id)) {
                    p.setColor(Tema.luminancia(cor) > 0.5 ? Color.BLACK : Color.WHITE);
                    p.setTextSize(S(14)); p.setTextAlign(Paint.Align.CENTER);
                    c.drawText("A", cx, cy + S(5), p);
                }
                if (ativo) {
                    p.setStyle(Paint.Style.STROKE);
                    p.setStrokeWidth(S(2.5f));
                    p.setColor(Tema.TXT);
                    c.drawCircle(cx, cy, r + S(4), p);
                }
            }
        };
        v.setContentDescription(nome);
        v.setOnClickListener(new View.OnClickListener() { public void onClick(View x) { Prefs.definir("destaque", id); recreate(); } });
        GridLayout.LayoutParams lp = new GridLayout.LayoutParams();
        lp.width = S(48); lp.height = S(48);
        lp.setMargins(S(2), S(2), S(2), S(2));
        v.setLayoutParams(lp);
        return v;
    }
}
