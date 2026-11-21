import numpy as np, os
from PIL import Image
bad=0
for l in open('casos.txt'):
    n,borda,ang,rot,tipo=l.strip().split(',')
    gt=Image.open(f'c{n}.gt.png').convert('L'); 
    if os.path.exists(f'c{n}.out'):
        r=open(f'c{n}.out','rb').read(); w=int.from_bytes(r[:4],'little'); h=int.from_bytes(r[4:8],'little')
        a=np.frombuffer(r[8:],np.uint8).reshape(h,w,3)[:,:,0]; im=Image.fromarray(a)
    else:
        r=open(f'c{n}.raw','rb').read(); w=int.from_bytes(r[:4],'little'); h=int.from_bytes(r[4:8],'little')
        im=Image.fromarray(np.frombuffer(r[8:],np.uint8).reshape(h,w,3)[:,:,0])
    G=np.array(gt.resize((300,420),Image.BOX)).astype(float); best=None
    for k in range(4):
        t=im.rotate(-90*k,expand=True).resize((300,420),Image.BOX); d=np.abs(np.array(t).astype(float)-G).mean()
        if best is None or d<best[1]: best=(k,d)
    ok = best[0]==0 and best[1]<12
    if not ok: bad+=1
    print(n,tipo,'rot',rot,'-> melhor giro extra',best[0]*90,'dif %.1f'%best[1],'OK' if ok else 'ERRO')
print('erros',bad)
