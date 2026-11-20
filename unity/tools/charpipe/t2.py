import pickle, numpy as np, matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from mhcore import *; from render import draw
V,VT,faces=load_obj(MH+'/3dobjs/base.obj')
dF,dT=pickle.load(open('out/dec7000.pkl','rb'))
body=[f for f in faces if f[0]=='body']
Ff=[]
for g,vs,ts in body:
    for k in range(1,len(vs)-1): Ff.append([vs[0],vs[k],vs[k+1]])
fig,axs=plt.subplots(1,5,figsize=(20,8))
m=morph(V,gender=1.0); f=morph(V,gender=0.0,weight=0.6); o=morph(V,gender=1.0,age=0.85,weight=0.8)
draw(axs[0],m,Ff); draw(axs[1],m,dF); draw(axs[2],f,dF); draw(axs[3],o,dF); draw(axs[4],m,dF,yaw=1.2)
for a,t in zip(axs,['male full 26.7k','male 7k','female 7k','old heavy male 7k','male side']): a.set_title(t)
plt.tight_layout(); plt.savefig('out/prev1.png',dpi=70)
