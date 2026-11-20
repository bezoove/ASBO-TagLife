import bpy, sys, json
import numpy as np
clips=json.load(open('clips.json'))
spec={'jog':(0.5,1.0),'run':(0.45,0.9),'run2':(0.45,0.9)}
for name,(lo,hi) in spec.items():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'fbx/{name}.fbx')
    arm=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'][0]
    act=arm.animation_data.action; f0,f1=int(act.frame_range[0])+1,int(act.frame_range[1])
    sc=bpy.context.scene; P=[];H=[]
    names=['LeftFoot','RightFoot','LeftHand','RightHand','LeftLeg','RightLeg','Head']
    for f in range(f0,f1+1):
        sc.frame_set(f); w=arm.matrix_world
        h=np.array(w@arm.pose.bones['Hips'].head); H.append(h)
        # hips-relative positions in the hips heading frame (yaw removed)
        m=(w@arm.pose.bones['Hips'].matrix).to_3x3()
        fwd=np.array(m.col[1]); fwd[2]=0; fwd/=np.linalg.norm(fwd)+1e-9
        side=np.array([fwd[1],-fwd[0],0])
        v=[]
        for n in names:
            p=np.array(w@arm.pose.bones[n].head)-h
            v+= [p.dot(fwd), p.dot(side), p[2]]
        P.append(v)
    P=np.array(P);H=np.array(H); n=len(P); fps=120
    hv=np.r_[0,np.linalg.norm(np.diff(H[:,:2],axis=0),axis=1)*fps]
    moving=hv>0.4
    best=None
    for lag in range(int(lo*fps), int(hi*fps)):
        for s in range(int(n*0.1), n-lag-int(n*0.05), 2):
            if not (moving[s:s+lag].mean()>0.97): continue
            d=np.linalg.norm(P[s]-P[s+lag]) + 0.5*np.linalg.norm(P[s+1]-P[s]-(P[s+lag+1]-P[s+lag]))*10
            if best is None or d<best[0]: best=(d,s,lag)
    d,s,lag=best
    dist=np.linalg.norm(H[s+lag,:2]-H[s,:2]); spd=dist/(lag/fps)
    clips[name]['loop']=[s+f0,s+lag+f0]; clips[name]['loop_speed']=float(spd); clips[name]['loop_err']=float(d)
    print(name,'loop',s+f0,s+lag+f0,'secs',round(lag/fps,3),'speed',round(spd,2),'err',round(d,3))
json.dump(clips,open('clips.json','w'),indent=1)
