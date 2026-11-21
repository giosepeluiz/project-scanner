import numpy as np, random, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter
random.seed(5); rng=np.random.default_rng(5)
TXT=["O processo de digitalização converte o papel em uma imagem que pode ser guardada e enviada",
"Pedro pagou 2450 euros pelo orçamento da obra em Quintão, lote 12, 3º andar direito",
"Quando a gente quer ver uma folha inclinada, basta girar um pouco para os textos ficarem retos",
"Medidas: 120 x 60 cm, espessura 2 cm, tampo com rodatampo de 3 cm e furo para a torneira",
"Gostaria de agradecer a atenção prestada e aguardo o retorno para agendar a medição final",
"Jogador quebrou a janela da garagem porque estava ventando bastante naquele dia de verão"]
def pagina(W=1240,H=1754,fs=30,fonte='/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf',caps=False):
    im=Image.new('L',(W,H),238); d=ImageDraw.Draw(im); f=ImageFont.truetype(fonte,fs)
    y=110; i=0
    while y<H-120:
        t=TXT[i%len(TXT)]; i+=1
        t=t.upper() if caps else t
        d.text((90,y),t[:70],font=f,fill=20); y+=int(fs*1.7)
        if i%5==0: y+=int(fs*1.2)
    return im
def cena(nome,pg,ang,rot,borda=True,margem=60):
    W,H=pg.size
    if borda:
        C=Image.new('L',(W+2*margem+80,H+2*margem+80),18)
        p=pg.rotate(-ang,expand=True,fillcolor=18,resample=Image.BICUBIC) if ang else pg
        # mascara para nao pintar cantos
        m=Image.new('L',pg.size,255).rotate(-ang,expand=True,fillcolor=0,resample=Image.BICUBIC) if ang else None
        C.paste(p,((C.width-p.width)//2+17,(C.height-p.height)//2-9),m)
    else:
        C=pg.rotate(-ang,fillcolor=238,resample=Image.BICUBIC) if ang else pg.copy()
    a=np.array(C).astype(int)+rng.integers(-4,5,(C.height,C.width)); a=np.clip(a,0,255).astype(np.uint8)
    C=Image.fromarray(a)
    k=rot//90
    if k: C=C.rotate(-90*k,expand=True)   # rotacao horaria
    rgb=np.stack([np.array(C)]*3,-1)[:,:,::-1].copy()  # BGR
    h,w=rgb.shape[:2]
    open(nome+'.raw','wb').write(w.to_bytes(4,'little')+h.to_bytes(4,'little')+rgb.tobytes())
    pg.save(nome+'.gt.png'); C.save(nome+'.in.png')
casos=[]
pg=pagina()
n=0
for borda,ang in [(True,0),(True,1.7),(True,-2.4),(False,0),(False,1.3),(False,-0.8)]:
    for rot in (0,90,180,270):
        cena(f'c{n}',pg,ang,rot,borda); casos.append((n,borda,ang,rot,'serif')); n+=1
# outras fontes e maiusculas
pg2=pagina(fonte='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',fs=24)
for rot in (0,180,90,270):
    cena(f'c{n}',pg2,0.9,rot,True); casos.append((n,True,0.9,rot,'sans24')); n+=1
pg3=pagina(fonte='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',fs=34,caps=True)
for rot in (0,180):
    cena(f'c{n}',pg3,0.0,rot,True); casos.append((n,True,0.0,rot,'CAPS')); n+=1
open('casos.txt','w').write('\n'.join(','.join(map(str,c)) for c in casos))
print(len(casos))
