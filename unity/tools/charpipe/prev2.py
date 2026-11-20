import pickle, sys, numpy as np, matplotlib; matplotlib.use('Agg'); import matplotlib.pyplot as plt
from render import draw
C={'skin':(0.86,0.66,0.55),'hair':(0.35,0.22,0.12),'top':(0.25,0.4,0.7),'bottom':(0.25,0.25,0.3),'shoes':(0.1,0.08,0.06),'eyes':(0.95,0.95,0.95),'lashes':(0.05,0.05,0.05)}
names=sys.argv[1:]
fig,axs=plt.subplots(2,len(names),figsize=(4*len(names),12))
for i,n in enumerate(names):
    M=pickle.load(open(f'out/chars/{n}.pkl','rb'))
    F=[];cols=[]
    for m,t in M['tris'].items():
        F+=t; cols+=[C[m]]*len(t)
    P=M['pos'].astype(float).copy(); P[:,0]*=-1  # back to RH for the renderer
    draw(axs[0][i],P,F,cols); axs[0][i].set_title(n)
    draw(axs[1][i],P,F,cols,yaw=2.6)
plt.tight_layout(); plt.savefig('out/prev2.png',dpi=60)
