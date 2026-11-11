// Janela "Sobre": autoria, contato e informações técnicas.
package io.github.giosepeluiz.digitalizar;

import android.app.Activity;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.view.Gravity;
import android.view.View;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.TextView;

import java.util.Calendar;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Sobre extends Activity {
    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Tema.BG);
        Principal.bordas(raiz);
        ScrollView sv = new ScrollView(this);
        final LinearLayout c = Ui.coluna(this);
        c.setPadding(S(20), S(20), S(20), S(20));
        sv.addView(c);
        raiz.addView(sv, new LinearLayout.LayoutParams(MP, 0, 1));

        LinearLayout cab = Ui.linha(this);
        ImageView ic = new ImageView(this);
        ic.setImageResource(R.mipmap.ic_launcher);
        cab.addView(ic, Ui.lpM(S(64), S(64), 0, 0, 16, 0));
        LinearLayout tt = Ui.coluna(this);
        tt.addView(Ui.rotulo(this, "Digitalizar", 26, Tema.TXT, false));
        tt.addView(Ui.rotulo(this, L.T("Versão") + " " + Config.VERSAO + " · Android", 14, Tema.TXT2, false));
        cab.addView(tt);
        c.addView(cab, Ui.lpM(MP, WC, 0, 0, 0, 14));
        c.addView(Ui.rotulo(this, L.T("Programa de digitalização para Android: usa o scanner da rede (Wi-Fi) ou a câmera, com detecção automática do documento e edição rápida da imagem (recorte, rotação, brilho, contraste, níveis, curvas e limiar)."), 14, Tema.TXT2, false), Ui.lpM(MP, WC, 0, 0, 0, 16));

        LinearLayout cartao = Ui.card(this);
        cartao.addView(Ui.rotulo(this, L.T("Criado por"), 12.5f, Tema.TXT2, false));
        cartao.addView(Ui.rotulo(this, Config.AUTOR, 18, Tema.TXT, true));
        cartao.addView(Ui.rotulo(this, L.T("Desenvolvido com a ajuda do Anthropic Claude Opus 5.5"), 13, Tema.TXT2, false), Ui.lpM(MP, WC, 0, 4, 0, 4));
        int ano = Math.max(2026, Calendar.getInstance().get(Calendar.YEAR));
        cartao.addView(Ui.rotulo(this, "© " + ano + " " + Config.AUTOR + ". " + L.T("Todos os direitos reservados."), 12, Tema.TXT2, false));
        c.addView(cartao, Ui.lpM(MP, WC, 0, 0, 0, 18));

        c.addView(Ui.rotulo(this, L.T("Redes e contato"), 16, Tema.TXT, true), Ui.lpM(MP, WC, 0, 0, 0, 6));
        String[][] links = { { "Instagram", "instagram.com/giosepeluiz", "https://instagram.com/giosepeluiz" },
                { "GitHub", "github.com/giosepeluiz", "https://github.com/giosepeluiz" },
                { "Facebook", "facebook.com/giosepeluiz", "https://facebook.com/giosepeluiz" },
                { L.T("E-mail"), "contato@arqueirover.de", "mailto:contato@arqueirover.de" } };
        for (final String[] l : links) {
            LinearLayout li = Ui.linha(this);
            li.setPadding(S(4), S(8), S(4), S(8));
            li.setBackground(Ui.clicavel(Tema.BG, 6, 0));
            li.addView(Ui.rotulo(this, l[0], 14, Tema.TXT2, false), Ui.lp(S(100), WC));
            li.addView(Ui.rotulo(this, l[1], 14, Tema.AZUL, false));
            li.setOnClickListener(new View.OnClickListener() {
                public void onClick(View v) { try { startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse(l[2]))); } catch (Exception ignored) { } }
            });
            c.addView(li, Ui.lp(MP, WC));
        }

        c.addView(Ui.rotulo(this, L.T("Informações do software"), 16, Tema.TXT, true), Ui.lpM(MP, WC, 0, 18, 0, 6));
        final String[][] info = { { L.T("Versão"), Config.VERSAO },
                { L.T("Plataforma"), "Android " + Build.VERSION.RELEASE + " (API " + Build.VERSION.SDK_INT + ")" },
                { L.T("Sistema"), Build.MANUFACTURER + " " + Build.MODEL },
                { L.T("Digitalização"), L.T("Rede Wi-Fi (eSCL / AirScan) e câmera com detecção automática do documento") },
                { L.T("Formatos"), L.T("JPEG, PNG e PDF") },
                { L.T("Ícones"), "Font Awesome Free 6.7.2 (CC BY 4.0 · SIL OFL 1.1)" } };
        for (String[] i : info) {
            LinearLayout li = Ui.linha(this);
            li.setPadding(S(4), S(5), S(4), S(5));
            li.setGravity(Gravity.TOP);
            li.addView(Ui.rotulo(this, i[0], 13.5f, Tema.TXT2, false), Ui.lp(S(110), WC));
            li.addView(Ui.rotulo(this, i[1], 13.5f, Tema.TXT, false), new LinearLayout.LayoutParams(0, WC, 1));
            c.addView(li, Ui.lp(MP, WC));
        }

        LinearLayout bts = Ui.linha(this);
        final TextView copiar = Ui.botao(this, Ui.Ic.COPIAR, L.T("Copiar informações"), false);
        copiar.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                StringBuilder sb = new StringBuilder("Digitalizar " + Config.VERSAO + " (Android)\n" + L.T("Criado por") + " " + Config.AUTOR
                        + " · " + L.T("Desenvolvido com a ajuda do Anthropic Claude Opus 5.5") + "\n");
                for (String[] i : info) sb.append(i[0]).append(": ").append(i[1]).append('\n');
                ((ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE)).setPrimaryClip(ClipData.newPlainText("Digitalizar", sb.toString()));
                copiar.setText(Ui.comIcone(Sobre.this, Ui.Ic.OK, L.T("Copiado")));
            }
        });
        TextView fechar = Ui.botao(this, "", L.T("Fechar"), true);
        fechar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { finish(); } });
        bts.addView(copiar, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lf = new LinearLayout.LayoutParams(0, WC, 1); lf.leftMargin = S(8);
        bts.addView(fechar, lf);
        c.addView(bts, Ui.lpM(MP, WC, 0, 20, 0, 0));
        setContentView(raiz);
    }
}
