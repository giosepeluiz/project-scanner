import random, json, math
from PIL import Image, ImageDraw, ImageFilter, ImageFont
import numpy as np
random.seed(7); np.random.seed(7)
W,H=2000,1500
def fundo(tipo):
    if tipo=='madeira':
        x=np.linspace(0,1,W)[None,:]; y=np.linspace(0,1,H)[:,None]
        v=np.sin((x*40+np.sin(y*6)*2+np.random.rand()*3)*2)*0.5+0.5
        n=np.random.rand(H,W)*0.25
        base=np.array([120,80,45]); a=(v*0.6+n)[...,None]
        img=base*(0.6+a*0.6)
    elif tipo=='escuro':
        img=np.full((H,W,3),45.0)+np.random.randn(H,W,1)*6
    elif tipo=='claro':
        img=np.full((H,W,3),[200,198,192])+np.random.randn(H,W,1)*5
    else: # tecido azul padronizado
        x=np.arange(W)[None,:];y=np.arange(H)[:,None]
        p=((x//25+y//25)%2)*25
        img=np.stack([40+p+0*y,70+p+0*y,130+p+0*y],-1).astype(float)
    return Image.fromarray(np.clip(img,0,255).astype('uint8'))
def papel(pw,ph,amarelo):
    im=Image.new('RGB',(pw,ph),(245,243,235) if amarelo else (250,250,250))
    d=ImageDraw.Draw(im)
    try: f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',int(pw*0.03))
    except: f=None
    d.text((pw*0.08,ph*0.05),'RELATÓRIO DE TESTE',fill=(20,20,20),font=f)
    y=ph*0.12
    while y<ph*0.9:
        x=pw*0.08
        while x<pw*0.9:
            l=random.randint(int(pw*0.03),int(pw*0.12)); d.rectangle([x,y,x+l,y+pw*0.012],fill=(40,40,60)); x+=l+pw*0.02
        y+=ph*0.035
    d.rectangle([pw*0.6,ph*0.6,pw*0.88,ph*0.8],fill=(200,40,40))
    return im
casos=[]
for k,(tipo,fora) in enumerate([('madeira',0),('escuro',0),('claro',0),('tecido',0),('madeira',0),('escuro',1),('claro',0),('madeira',0)]):
    bg=fundo(tipo)
    pw,ph=1000,1414
    pap=papel(pw,ph,k%2==0)
    cx,cy=W/2+random.uniform(-150,150),H/2+random.uniform(-80,80)
    s=random.uniform(0.75,0.95)*H/ph
    hw,hh=pw*s/2,ph*s/2
    rot=math.radians(random.uniform(-25,25)) if k!=7 else math.radians(90)
    pts=[(-hw,-hh),(hw,-hh),(hw,hh),(-hw,hh)]
    persp=random.uniform(0.0,0.18)
    q=[]
    for i,(x,y) in enumerate(pts):
        f=1-persp if y<0 else 1+persp*0.3
        x*=f
        xr=x*math.cos(rot)-y*math.sin(rot); yr=x*math.sin(rot)+y*math.cos(rot)
        q.append((cx+xr,cy+yr))
    if fora: q=[(x+0,y+330) for x,y in q]  # parte da folha sai da foto
    # warp papel para q: coeffs mapeia saida->entrada
    def coeffs(dst,src):
        A=[];B=[]
        for (x,y),(u,v) in zip(dst,src):
            A.append([x,y,1,0,0,0,-u*x,-u*y]);B.append(u)
            A.append([0,0,0,x,y,1,-v*x,-v*y]);B.append(v)
        return np.linalg.solve(np.array(A),np.array(B)).tolist()
    c=coeffs(q,[(0,0),(pw,0),(pw,ph),(0,ph)])
    wp=pap.transform((W,H),Image.PERSPECTIVE,c,Image.BICUBIC)
    mask=Image.new('L',(pw,ph),255).transform((W,H),Image.PERSPECTIVE,c,Image.BICUBIC)
    # sombra
    sh=mask.filter(ImageFilter.GaussianBlur(25)); 
    bgn=np.array(bg).astype(float); shn=np.array(sh)[...,None]/255.0
    bgn*= (1-0.35*np.roll(np.roll(shn,15,0),15,1))
    img=Image.composite(wp,Image.fromarray(bgn.astype('uint8')),mask)
    a=np.array(img).astype(float)
    gx=np.linspace(0.65,1.05,W)[None,:,None]*np.linspace(1.0,0.85,H)[:,None,None]
    a=a*gx*np.array([1.0,0.97,0.88])+np.random.randn(H,W,1)*4
    img=Image.fromarray(np.clip(a,0,255).astype('uint8')).filter(ImageFilter.GaussianBlur(1.2))
    nome=f'caso{k}.jpg'; img.save(nome,quality=88)
    casos.append({'arq':nome,'q':q,'fora':fora})
json.dump(casos,open('casos.json','w'))
print('ok')
