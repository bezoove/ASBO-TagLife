import bpy, sys, json, math
import numpy as np
res={}
for name in sys.argv[sys.argv.index('--')+1:]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'fbx/{name}.fbx')
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
    act=arm.animation_data.action; f0,f1=int(act.frame_range[0])+1,int(act.frame_range[1])
    sc=bpy.context.scene
    L=[];R=[];H=[];HL=[];HR=[]
    for f in range(f0,f1+1):
        sc.frame_set(f)
        w=arm.matrix_world
        L.append(tuple(w@arm.pose.bones['LeftFoot'].head)); R.append(tuple(w@arm.pose.bones['RightFoot'].head)); H.append(tuple(w@arm.pose.bones['Hips'].head))
        HL.append(tuple(w@arm.pose.bones['LeftHand'].head)); HR.append(tuple(w@arm.pose.bones['RightHand'].head))
    L=np.array(L);R=np.array(R);H=np.array(H);HL=np.array(HL);HR=np.array(HR)
    n=len(H); fps=120.0
    hv=np.linalg.norm(np.diff(H[:,:2],axis=0),axis=1)*fps
    info={'f0':f0,'f1':f1,'n':n,'mean_speed':float(hv.mean())}
    # left heel strikes: local minima of left foot height where foot moving forward stops
    lz=L[:,2]; lsp=np.r_[0,np.linalg.norm(np.diff(L[:,:2],axis=0),axis=1)*fps]
    strikes=[i for i in range(5,n-5) if lz[i]==lz[i-5:i+6].min() and lsp[i]<0.6]
    # dedupe
    st=[]
    for s in strikes:
        if not st or s-st[-1]>30: st.append(s)
    info['strikes']=st
    if len(st)>=3:
        # choose the cycle closest to the middle
        mid=n/2; best=min(range(len(st)-1), key=lambda k: abs((st[k]+st[k+1])/2-mid))
        a,b=st[best],st[best+1]
        d=np.linalg.norm(H[b,:2]-H[a,:2]); t=(b-a)/fps
        info['cycle']=[a+f0,b+f0]; info['cycle_speed']=float(d/t); info['cycle_secs']=t
    # quiet window (idle/talk): min hip speed over 6 s
    win=int(6*fps)
    if n>win*1.2:
        cs=np.r_[0,np.cumsum(hv)]
        sums=[cs[i+win]-cs[i] for i in range(0,n-win-1,30)]
        i=int(np.argmin(sums))*30
        info['quiet']=[i+f0,i+win+f0]
        # wave: window where hands highest
        hh=np.maximum(HL[:,2],HR[:,2])
        j=int(np.argmax([hh[k:k+int(3*fps)].mean() for k in range(0,n-int(3*fps),30)]))*30
        info['handsup']=[j+f0,j+int(3*fps)+f0]
    # jump: hip height peak
    k=int(np.argmax(H[:,2])); info['hip_peak']=k+f0; info['hips_rest_z']=float(np.median(H[:,2]))
    res[name]=info
    print(name, json.dumps(info))
json.dump(res,open('clips.json','w'),indent=1)
