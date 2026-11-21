import numpy as np, random
from PIL import Image, ImageDraw, ImageFont
random.seed(3); rng=np.random.default_rng(3)
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',46)
def croqui(W=1240,H=1754):
    im=Image.new('L',(W,H),250); d=ImageDraw.Draw(im)
    d.rectangle([150,200,1100,1500],outline=10,width=4)
    d.line([150,700,1100,700],fill=10,width=4); d.line([600,700,600,1500],fill=10,width=4)
    d.rectangle([300,300,520,420],outline=10,width=3)
    for (x,y,t) in [(200,120,"285.5"),(700,120,"286.0"),(160,760,"RT 585-0.2"),(640,760,"173.5"),(320,1000,"D=4"),(700,1100,"235.5"),(200,1560,"75 x 14")]:
        d.text((x,y),t,font=F,fill=15)
    return im
def cena(nome,pg,rot,faixa):
    a=np.array(pg).astype(int)
    if faixa=='L': a[:,:5]=15
    if faixa=='LB': a[:,:6]=15; a[-6:,:]=15
    a=np.clip(a+rng.integers(-4,5,a.shape),0,255).astype(np.uint8)
    C=Image.fromarray(a)
    k=rot//90
    if k: C=C.rotate(-90*k,expand=True)
    rgb=np.stack([np.array(C)]*3,-1)[:,:,::-1].copy(); h,w=rgb.shape[:2]
    open(nome+'.raw','wb').write(w.to_bytes(4,'little')+h.to_bytes(4,'little')+rgb.tobytes())
    C.save(nome+'.in.png')
pg=croqui()
for i,(rot,f) in enumerate([(0,'L'),(90,'L'),(270,'L'),(180,'L'),(90,'LB'),(270,''),(90,'')]):
    cena(f's{i}',pg,rot,f)
