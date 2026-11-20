import pickle, sys, numpy as np, matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from render import draw
C={'skin':(0.86,0.66,0.55),'hair':(0.35,0.22,0.12),'top':(0.25,0.4,0.7),'bottom':(0.25,0.25,0.3),'shoes':(0.1,0.08,0.06),'eyes':(0.95,0.95,0.95),'lashes':(0.05,0.05,0.05)}
names=sys.argv[1:]
fig,axs=plt.subplots(len(names),3,figsize=(12,4.2*len(names)))
for i,n in enumerate(names):
    M=pickle.load(open(f'out/chars/{n}.pkl','rb'))
    F=[];cols=[]
    for m,t in M['tris'].items(): F+=t; cols+=[C[m]]*len(t)
    P=M['pos'].astype(float).copy(); P[:,0]*=-1
    F=np.array(F); cols=np.array(cols)
    keep=P[F][:,:,1].mean(1)>M['eyeY']-0.2
    for j,yaw in enumerate([0,1.57,3.14]):
        draw(axs[i][j],P,F[keep],cols[keep],yaw=yaw)
    axs[i][0].set_title(n)
plt.tight_layout(); plt.savefig('out/prev3.png',dpi=55)
