import numpy as np, matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
def draw(ax, V, F, cols=None, yaw=0.0, light=(0.4,0.6,0.7)):
    c,s=np.cos(yaw),np.sin(yaw)
    R=np.array([[c,0,s],[0,1,0],[-s,0,c]])
    P=V@R.T
    F=np.asarray(F)
    tri=P[F]
    n=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]); n/=np.linalg.norm(n,axis=1,keepdims=True)+1e-12
    vis=n[:,2]>0
    L=np.array(light);L/=np.linalg.norm(L)
    sh=np.clip(n@L,0,1)*0.75+0.25
    order=np.argsort(tri[:,:,2].mean(1))
    base=np.ones((len(F),3))*0.8 if cols is None else np.asarray(cols)
    fc=base*sh[:,None]
    idx=[i for i in order if vis[i]]
    pc=PolyCollection(tri[idx][:,:,:2],facecolors=np.clip(fc[idx],0,1),edgecolors='none')
    ax.add_collection(pc); ax.set_aspect('equal'); ax.autoscale(); ax.axis('off')
