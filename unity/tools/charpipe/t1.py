import time, pickle, numpy as np
from mhcore import *
t=time.time()
V,VT,faces=load_obj(MH+'/3dobjs/base.obj')
print('load',time.time()-t, V.shape, VT.shape, len(faces))
body=[f for f in faces if f[0]=='body']
Fv,Ft=[],[]
for g,vs,ts in body:
    for k in range(1,len(vs)-1):
        Fv.append([vs[0],vs[k],vs[k+1]]); Ft.append([ts[0],ts[k],ts[k+1]])
print('tris',len(Fv))
# locked: uv seams + mesh boundary
vts={}
for f,t_ in zip(Fv,Ft):
    for v,tt in zip(f,t_): vts.setdefault(v,set()).add(tt)
edges={}
for f in Fv:
    for a,b in ((f[0],f[1]),(f[1],f[2]),(f[2],f[0])):
        k=(min(a,b),max(a,b)); edges[k]=edges.get(k,0)+1
locked=np.zeros(len(V),bool)
for v,s in vts.items():
    if len(s)>1: locked[v]=True
for (a,b),c in edges.items():
    if c==1: locked[a]=locked[b]=True
print('locked',locked.sum(),'of',len(vts))
Vavg=morph(V)
t=time.time()
dF,dT=decimate(Vavg,Fv,Ft,7000,locked)
print('decimate',time.time()-t,len(dF))
pickle.dump((dF,dT),open('out/dec7000.pkl','wb'))
