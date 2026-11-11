// Digitalizar com a câmera: pré-visualização ao vivo com deteção do documento (contorno na tela),
// captura automática quando a folha fica parada, lanterna e importação da galeria.
package io.github.giosepeluiz.digitalizar;

import android.Manifest;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Canvas;
import android.graphics.Color;
import android.graphics.ImageFormat;
import android.graphics.Paint;
import android.graphics.Path;
import android.graphics.SurfaceTexture;
import android.hardware.camera2.CameraAccessException;
import android.hardware.camera2.CameraCaptureSession;
import android.hardware.camera2.CameraCharacteristics;
import android.hardware.camera2.CameraDevice;
import android.hardware.camera2.CameraManager;
import android.hardware.camera2.CaptureRequest;
import android.hardware.camera2.params.StreamConfigurationMap;
import android.media.Image;
import android.media.ImageReader;
import android.net.Uri;
import android.os.Bundle;
import android.os.Handler;
import android.os.HandlerThread;
import android.os.Looper;
import android.os.SystemClock;
import android.util.Size;
import android.view.Gravity;
import android.view.Surface;
import android.view.TextureView;
import android.view.View;
import android.view.WindowManager;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.ByteBuffer;
import java.util.Arrays;

import static io.github.giosepeluiz.digitalizar.Tema.S;
import static io.github.giosepeluiz.digitalizar.Ui.MP;
import static io.github.giosepeluiz.digitalizar.Ui.WC;

public class Camera extends Activity {
    static final int REQ_PERM = 10, REQ_GALERIA = 11, REQ_REVISAO = 12;

    final Handler ui = new Handler(Looper.getMainLooper());
    HandlerThread fioCam, fioVisao;
    Handler hCam, hVisao;
    TextureView tela;
    Sobreposicao sobre;
    FrameLayout area;
    TextView dica, btnAuto, btnLuz, btnFoto;
    View clarao;

    CameraDevice cam;
    CameraCaptureSession sessao;
    ImageReader leitorAnalise, leitorFoto;
    Size tamPrevia;
    int orientacaoSensor = 90;
    boolean temFlash, lanterna, auto, capturando, analisando, semAnalise, pediuPermissao;

    // deteção ao vivo (coordenadas normalizadas da pré-visualização em retrato)
    Visao.Quad quadAtual;
    long estavelDesde, ultimoVisto;

    @Override protected void onCreate(Bundle b) {
        super.onCreate(b);
        Tema.aplicar(this);
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        getWindow().setStatusBarColor(Color.BLACK);
        getWindow().setNavigationBarColor(Color.BLACK);
        auto = Prefs.logico("captura_auto", true);

        LinearLayout raiz = Ui.coluna(this);
        raiz.setBackgroundColor(Color.BLACK);
        Principal.bordas(raiz);

        LinearLayout topo = Ui.linha(this);
        topo.setPadding(S(8), S(6), S(8), S(6));
        TextView fechar = Ui.icone(this, Ui.Ic.FECHAR, 20, Color.WHITE);
        fechar.setPadding(S(12), S(6), S(12), S(6));
        fechar.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { finish(); } });
        topo.addView(fechar);
        topo.addView(new View(this), new LinearLayout.LayoutParams(0, 1, 1));
        btnAuto = pilula("");
        btnAuto.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { auto = !auto; Prefs.definir("captura_auto", auto); pintarBotoes(); } });
        topo.addView(btnAuto);
        raiz.addView(topo, Ui.lp(MP, WC));

        area = new FrameLayout(this);
        tela = new TextureView(this);
        sobre = new Sobreposicao(this);
        clarao = new View(this);
        clarao.setBackgroundColor(Color.WHITE);
        clarao.setAlpha(0f);
        area.addView(tela, new FrameLayout.LayoutParams(MP, MP, Gravity.CENTER));
        area.addView(sobre, new FrameLayout.LayoutParams(MP, MP, Gravity.CENTER));
        area.addView(clarao, new FrameLayout.LayoutParams(MP, MP));
        raiz.addView(area, new LinearLayout.LayoutParams(MP, 0, 1));

        dica = branco(L.T("Aponte a câmera para o documento."), 14);
        dica.setGravity(Gravity.CENTER);
        dica.setPadding(S(16), S(10), S(16), S(4));
        raiz.addView(dica, Ui.lp(MP, WC));

        LinearLayout base = Ui.linha(this);
        base.setPadding(S(20), S(10), S(20), S(16));
        TextView galeria = pilula(Ui.comIcone(this, Ui.Ic.IMAGEM, L.T("Galeria")));
        galeria.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { abrirGaleria(); } });
        btnFoto = new TextView(this);
        btnFoto.setBackground(Ui.clicavel(Color.WHITE, 40, 0x88ffffff));
        btnFoto.setContentDescription(L.T("Digitalizar (ação)"));
        btnFoto.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { capturar(); } });
        btnLuz = pilula("");
        btnLuz.setOnClickListener(new View.OnClickListener() { public void onClick(View v) { lanterna = !lanterna; pintarBotoes(); repetir(); } });
        base.addView(galeria, new LinearLayout.LayoutParams(0, WC, 1));
        LinearLayout.LayoutParams lf = new LinearLayout.LayoutParams(S(74), S(74));
        lf.leftMargin = S(16); lf.rightMargin = S(16);
        base.addView(btnFoto, lf);
        base.addView(btnLuz, new LinearLayout.LayoutParams(0, WC, 1));
        raiz.addView(base, Ui.lp(MP, WC));
        setContentView(raiz);
        pintarBotoes();
    }

    TextView branco(String t, float sp) { return Ui.rotulo(this, t, sp, Color.WHITE, false); }

    TextView pilula(CharSequence t) {
        TextView v = branco("", 13.5f);
        v.setText(t);
        v.setGravity(Gravity.CENTER);
        v.setSingleLine(true);
        v.setPadding(S(12), S(10), S(12), S(10));
        v.setBackground(Ui.clicavel(0x33ffffff, 20, 0));
        return v;
    }

    void pintarBotoes() {
        btnAuto.setText(Ui.comIcone(this, auto ? Ui.Ic.OK : Ui.Ic.MAGIA, L.T("Captura automática")));
        btnAuto.setBackground(Ui.clicavel(auto ? Tema.AZUL : 0x33ffffff, 20, 0));
        btnAuto.setTextColor(auto ? Tema.TXT_SOBRE_AZUL : Color.WHITE);
        btnLuz.setText(Ui.comIcone(this, Ui.Ic.RAIO, L.T("Lanterna")));
        btnLuz.setBackground(Ui.clicavel(lanterna ? Tema.AZUL : 0x33ffffff, 20, 0));
        btnLuz.setTextColor(lanterna ? Tema.TXT_SOBRE_AZUL : Color.WHITE);
        btnLuz.setVisibility(temFlash || cam == null ? View.VISIBLE : View.INVISIBLE);
    }

    // ------------------------------------------------------------- ciclo de vida
    @Override protected void onResume() {
        super.onResume();
        capturando = false;
        fioCam = new HandlerThread("camera"); fioCam.start(); hCam = new Handler(fioCam.getLooper());
        fioVisao = new HandlerThread("visao"); fioVisao.start(); hVisao = new Handler(fioVisao.getLooper());
        if (checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
            if (!pediuPermissao) { pediuPermissao = true; requestPermissions(new String[] { Manifest.permission.CAMERA }, REQ_PERM); }
            else dica.setText(L.T("É preciso permitir o acesso à câmera."));
            return;
        }
        iniciarQuandoPronto();
    }

    void iniciarQuandoPronto() {
        if (tela.isAvailable()) abrirCamera();
        else tela.setSurfaceTextureListener(new TextureView.SurfaceTextureListener() {
            public void onSurfaceTextureAvailable(SurfaceTexture s, int w, int h) { abrirCamera(); }
            public void onSurfaceTextureSizeChanged(SurfaceTexture s, int w, int h) { }
            public boolean onSurfaceTextureDestroyed(SurfaceTexture s) { return true; }
            public void onSurfaceTextureUpdated(SurfaceTexture s) { }
        });
    }

    @Override protected void onPause() {
        fecharCamera();
        if (fioCam != null) fioCam.quitSafely();
        if (fioVisao != null) fioVisao.quitSafely();
        super.onPause();
    }

    @Override public void onRequestPermissionsResult(int req, String[] p, int[] r) {
        if (req != REQ_PERM) return;
        // se foi concedida, o onResume que vem a seguir abre a câmera
        if (r.length == 0 || r[0] != PackageManager.PERMISSION_GRANTED) dica.setText(L.T("É preciso permitir o acesso à câmera."));
    }

    // ------------------------------------------------------------- câmera
    static Size maior43(Size[] tamanhos, long maxArea, int maxLargura) {
        Size melhor = null;
        for (Size s : tamanhos) {
            if ((long) s.getWidth() * s.getHeight() > maxArea || s.getWidth() > maxLargura) continue;
            boolean q = Math.abs(s.getWidth() * 3 - s.getHeight() * 4) <= s.getWidth() / 50;
            if (!q) continue;
            if (melhor == null || (long) s.getWidth() * s.getHeight() > (long) melhor.getWidth() * melhor.getHeight()) melhor = s;
        }
        if (melhor == null) {
            for (Size s : tamanhos)
                if ((long) s.getWidth() * s.getHeight() <= maxArea && s.getWidth() <= maxLargura
                        && (melhor == null || (long) s.getWidth() * s.getHeight() > (long) melhor.getWidth() * melhor.getHeight())) melhor = s;
        }
        return melhor != null ? melhor : tamanhos[0];
    }

    void abrirCamera() {
        CameraManager cm = (CameraManager) getSystemService(Context.CAMERA_SERVICE);
        try {
            String id = null;
            for (String c : cm.getCameraIdList()) {
                Integer f = cm.getCameraCharacteristics(c).get(CameraCharacteristics.LENS_FACING);
                if (f != null && f == CameraCharacteristics.LENS_FACING_BACK) { id = c; break; }
            }
            if (id == null && cm.getCameraIdList().length > 0) id = cm.getCameraIdList()[0];
            if (id == null) { falhou(); return; }
            CameraCharacteristics ch = cm.getCameraCharacteristics(id);
            Integer o = ch.get(CameraCharacteristics.SENSOR_ORIENTATION);
            orientacaoSensor = o == null ? 90 : o;
            Boolean fl = ch.get(CameraCharacteristics.FLASH_INFO_AVAILABLE);
            temFlash = fl != null && fl;
            StreamConfigurationMap mapa = ch.get(CameraCharacteristics.SCALER_STREAM_CONFIGURATION_MAP);
            if (mapa == null) { falhou(); return; }
            Size foto = maior43(mapa.getOutputSizes(ImageFormat.JPEG), 13_000_000L, 5000);
            tamPrevia = maior43(mapa.getOutputSizes(SurfaceTexture.class), 1920L * 1080, 1920);
            Size analise = maior43(mapa.getOutputSizes(ImageFormat.YUV_420_888), 800L * 600, 800);

            leitorFoto = ImageReader.newInstance(foto.getWidth(), foto.getHeight(), ImageFormat.JPEG, 2);
            leitorFoto.setOnImageAvailableListener(new ImageReader.OnImageAvailableListener() {
                public void onImageAvailable(ImageReader r) { fotoPronta(r); }
            }, hCam);
            leitorAnalise = ImageReader.newInstance(analise.getWidth(), analise.getHeight(), ImageFormat.YUV_420_888, 2);
            leitorAnalise.setOnImageAvailableListener(new ImageReader.OnImageAvailableListener() {
                public void onImageAvailable(ImageReader r) { quadroAnalise(r); }
            }, hCam);

            ui.post(new Runnable() { public void run() { ajustarTela(); pintarBotoes(); } });
            cm.openCamera(id, new CameraDevice.StateCallback() {
                public void onOpened(CameraDevice c) { cam = c; criarSessao(); }
                public void onDisconnected(CameraDevice c) { c.close(); cam = null; }
                public void onError(CameraDevice c, int e) { c.close(); cam = null; falhou(); }
            }, hCam);
        } catch (CameraAccessException | RuntimeException e) {
            falhou();
        }
    }

    void falhou() {
        ui.post(new Runnable() { public void run() { dica.setText(L.T("Não foi possível abrir a câmera.")); } });
    }

    /** A pré-visualização ocupa a largura toda, na proporção da câmera (em retrato). */
    void ajustarTela() {
        if (tamPrevia == null) return;
        int aw = area.getWidth(), ah = area.getHeight();
        if (aw == 0 || ah == 0) { area.post(new Runnable() { public void run() { ajustarTela(); } }); return; }
        boolean deitado = orientacaoSensor % 180 != 0;
        float prop = deitado ? tamPrevia.getWidth() / (float) tamPrevia.getHeight() : tamPrevia.getHeight() / (float) tamPrevia.getWidth();
        int w = aw, h = Math.round(aw * prop);
        if (h > ah) { h = ah; w = Math.round(ah / prop); }
        FrameLayout.LayoutParams p = new FrameLayout.LayoutParams(w, h, Gravity.CENTER);
        tela.setLayoutParams(p);
        sobre.setLayoutParams(new FrameLayout.LayoutParams(w, h, Gravity.CENTER));
    }

    Surface superficie;

    void criarSessao() {
        try {
            SurfaceTexture st = tela.getSurfaceTexture();
            if (st == null || cam == null) return;
            st.setDefaultBufferSize(tamPrevia.getWidth(), tamPrevia.getHeight());
            superficie = new Surface(st);
            java.util.List<Surface> sup = semAnalise ? Arrays.asList(superficie, leitorFoto.getSurface())
                    : Arrays.asList(superficie, leitorAnalise.getSurface(), leitorFoto.getSurface());
            cam.createCaptureSession(sup,
                    new CameraCaptureSession.StateCallback() {
                        public void onConfigured(CameraCaptureSession s) { sessao = s; repetir(); }
                        public void onConfigureFailed(CameraCaptureSession s) {
                            // alguns aparelhos não aceitam três saídas: continua sem a deteção ao vivo
                            if (!semAnalise) { semAnalise = true; criarSessao(); } else falhou();
                        }
                    }, hCam);
        } catch (Exception e) { falhou(); }
    }

    void repetir() {
        if (sessao == null || cam == null) return;
        try {
            CaptureRequest.Builder r = cam.createCaptureRequest(CameraDevice.TEMPLATE_PREVIEW);
            r.addTarget(superficie);
            if (!semAnalise) r.addTarget(leitorAnalise.getSurface());
            r.set(CaptureRequest.CONTROL_MODE, CaptureRequest.CONTROL_MODE_AUTO);
            r.set(CaptureRequest.CONTROL_AF_MODE, CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_PICTURE);
            r.set(CaptureRequest.CONTROL_AE_MODE, CaptureRequest.CONTROL_AE_MODE_ON);
            r.set(CaptureRequest.FLASH_MODE, lanterna && temFlash ? CaptureRequest.FLASH_MODE_TORCH : CaptureRequest.FLASH_MODE_OFF);
            sessao.setRepeatingRequest(r.build(), null, hCam);
        } catch (Exception ignored) { }
    }

    void fecharCamera() {
        try { if (sessao != null) sessao.close(); } catch (Exception ignored) { }
        sessao = null;
        try { if (cam != null) cam.close(); } catch (Exception ignored) { }
        cam = null;
        if (leitorAnalise != null) leitorAnalise.close();
        if (leitorFoto != null) leitorFoto.close();
        leitorAnalise = null; leitorFoto = null;
    }

    // ------------------------------------------------------------- análise ao vivo
    void quadroAnalise(ImageReader r) {
        Image img;
        try { img = r.acquireLatestImage(); } catch (Exception e) { return; }
        if (img == null) return;
        if (analisando || capturando) { img.close(); return; }
        final int w = img.getWidth(), h = img.getHeight();
        Image.Plane y = img.getPlanes()[0];
        final int passo = y.getRowStride();
        if (y.getPixelStride() != 1) { img.close(); return; }
        ByteBuffer bb = y.getBuffer();
        final byte[] dados = new byte[bb.remaining()];
        bb.get(dados);
        img.close();
        analisando = true;
        hVisao.post(new Runnable() {
            public void run() {
                Visao.Quad q = null;
                try {
                    int[] t = new int[3];
                    int[] g = Visao.reduzirPlano(dados, w, h, passo, 280, t);
                    Visao.Quad d = Visao.detectar(g, t[0], t[1]);
                    if (d != null) q = paraRetrato(d, t[0], t[1]);
                } catch (Throwable ignored) { }
                final Visao.Quad res = q;
                ui.post(new Runnable() { public void run() { analisando = false; novaDetecao(res); } });
            }
        });
    }

    /** Converte do referencial do sensor para a tela em retrato (0..1). */
    Visao.Quad paraRetrato(Visao.Quad d, int sw, int sh) {
        Visao.Quad q = new Visao.Quad();
        for (int i = 0; i < 4; i++) {
            float x = d.x[i], y = d.y[i];
            switch (orientacaoSensor) {
                case 90: q.x[i] = (sh - y) / sh; q.y[i] = x / sw; break;
                case 270: q.x[i] = y / sh; q.y[i] = (sw - x) / sw; break;
                case 180: q.x[i] = (sw - x) / sw; q.y[i] = (sh - y) / sh; break;
                default: q.x[i] = x / sw; q.y[i] = y / sh;
            }
        }
        q.ordenar();
        q.confianca = d.confianca;
        return q;
    }

    void novaDetecao(Visao.Quad q) {
        long agora = SystemClock.uptimeMillis();
        if (q == null) {
            if (agora - ultimoVisto > 600) { quadAtual = null; estavelDesde = 0; sobre.invalidate(); dica.setText(L.T("Aponte a câmera para o documento.")); }
            return;
        }
        ultimoVisto = agora;
        float mov = 1f;
        if (quadAtual != null) {
            mov = 0;
            for (int i = 0; i < 4; i++) mov = Math.max(mov, (float) Math.hypot(q.x[i] - quadAtual.x[i], q.y[i] - quadAtual.y[i]));
        }
        if (quadAtual != null && mov < 0.08f) {
            for (int i = 0; i < 4; i++) { q.x[i] = quadAtual.x[i] * 0.55f + q.x[i] * 0.45f; q.y[i] = quadAtual.y[i] * 0.55f + q.y[i] * 0.45f; }
        }
        if (mov > 0.02f || estavelDesde == 0) estavelDesde = agora;
        quadAtual = q;
        long parado = agora - estavelDesde;
        sobre.progresso = auto ? Math.min(1f, parado / 1500f) : 0f;
        sobre.invalidate();
        dica.setText(L.T("Documento encontrado. Segure firme…"));
        if (auto && parado >= 1500 && q.confianca >= 0.45f && !capturando) capturar();
    }

    final class Sobreposicao extends View {
        final Paint p = new Paint(Paint.ANTI_ALIAS_FLAG);
        float progresso;
        Sobreposicao(Context c) { super(c); }
        @Override protected void onDraw(Canvas c) {
            Visao.Quad q = quadAtual;
            if (q == null) return;
            float w = getWidth(), h = getHeight();
            Path path = new Path();
            path.moveTo(q.x[0] * w, q.y[0] * h);
            for (int i = 1; i < 4; i++) path.lineTo(q.x[i] * w, q.y[i] * h);
            path.close();
            p.setStyle(Paint.Style.FILL);
            p.setColor((Tema.AZUL & 0x00ffffff) | 0x40000000);
            c.drawPath(path, p);
            p.setStyle(Paint.Style.STROKE);
            p.setStrokeWidth(S(3));
            p.setColor(Tema.AZUL);
            c.drawPath(path, p);
            p.setStyle(Paint.Style.FILL);
            for (int i = 0; i < 4; i++) c.drawCircle(q.x[i] * w, q.y[i] * h, S(6), p);
            if (progresso > 0) {
                float cx = 0, cy = 0;
                for (int i = 0; i < 4; i++) { cx += q.x[i] * w / 4; cy += q.y[i] * h / 4; }
                p.setStyle(Paint.Style.STROKE);
                p.setStrokeWidth(S(5));
                p.setColor(0x55ffffff);
                c.drawCircle(cx, cy, S(26), p);
                p.setColor(Color.WHITE);
                c.drawArc(cx - S(26), cy - S(26), cx + S(26), cy + S(26), -90, 360 * progresso, false, p);
            }
        }
    }

    // ------------------------------------------------------------- captura
    void capturar() {
        if (capturando || sessao == null || cam == null) return;
        capturando = true;
        clarao.setAlpha(0.8f);
        clarao.animate().alpha(0f).setDuration(350).start();
        dica.setText(L.T("Processando…"));
        try {
            CaptureRequest.Builder r = cam.createCaptureRequest(CameraDevice.TEMPLATE_STILL_CAPTURE);
            r.addTarget(leitorFoto.getSurface());
            r.addTarget(superficie);
            r.set(CaptureRequest.CONTROL_AF_MODE, CaptureRequest.CONTROL_AF_MODE_CONTINUOUS_PICTURE);
            r.set(CaptureRequest.CONTROL_AE_MODE, CaptureRequest.CONTROL_AE_MODE_ON);
            r.set(CaptureRequest.FLASH_MODE, lanterna && temFlash ? CaptureRequest.FLASH_MODE_TORCH : CaptureRequest.FLASH_MODE_OFF);
            r.set(CaptureRequest.JPEG_ORIENTATION, orientacaoSensor);
            r.set(CaptureRequest.JPEG_QUALITY, (byte) 95);
            sessao.capture(r.build(), null, hCam);
        } catch (Exception e) {
            capturando = false;
            falhou();
        }
    }

    void fotoPronta(ImageReader r) {
        Image img;
        try { img = r.acquireNextImage(); } catch (Exception e) { return; }
        if (img == null) return;
        try {
            ByteBuffer bb = img.getPlanes()[0].getBuffer();
            byte[] d = new byte[bb.remaining()];
            bb.get(d);
            File f = new File(getCacheDir(), "camera.jpg");
            try (OutputStream o = new FileOutputStream(f)) { o.write(d); }
            final float[] dicaQuad = quadAtual == null ? null : new float[] {
                    quadAtual.x[0], quadAtual.y[0], quadAtual.x[1], quadAtual.y[1], quadAtual.x[2], quadAtual.y[2], quadAtual.x[3], quadAtual.y[3] };
            ui.post(new Runnable() { public void run() { revisar(dicaQuad); } });
        } catch (Exception e) {
            capturando = false;
            falhou();
        } finally { img.close(); }
    }

    void revisar(float[] dicaQuad) {
        Intent i = new Intent(this, Revisao.class);
        if (dicaQuad != null) i.putExtra("quad", dicaQuad);
        startActivityForResult(i, REQ_REVISAO);
    }

    void abrirGaleria() {
        Intent i = new Intent(Intent.ACTION_GET_CONTENT);
        i.setType("image/*");
        i.addCategory(Intent.CATEGORY_OPENABLE);
        try { startActivityForResult(Intent.createChooser(i, L.T("Galeria")), REQ_GALERIA); } catch (Exception ignored) { }
    }

    @Override protected void onActivityResult(int req, int res, Intent data) {
        super.onActivityResult(req, res, data);
        if (req == REQ_REVISAO) {
            if (res == RESULT_OK) { setResult(RESULT_OK); finish(); }
            quadAtual = null;
            return;
        }
        if (req == REQ_GALERIA && res == RESULT_OK && data != null && data.getData() != null) {
            Uri u = data.getData();
            try (InputStream in = getContentResolver().openInputStream(u);
                 OutputStream o = new FileOutputStream(new File(getCacheDir(), "camera.jpg"))) {
                if (in == null) return;
                byte[] b = new byte[65536];
                int n;
                while ((n = in.read(b)) > 0) o.write(b, 0, n);
            } catch (Exception e) { Ui.aviso(this, e.getMessage()); return; }
            revisar(null);
        }
    }
}
