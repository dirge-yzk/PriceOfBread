# 候補の直し：worldmap.svg の path4682（class="land fi"）の南岸の点（端点と制御点）を緯度で南へ動かす。
# 窓＝経度 [W0,W1] で全量 D 度、外側へ線形に 0 まで（[T0,W0]・[W1,T1]）。緯度 [LA0,LA1] の点だけ（内陸の点・国境は触らない）。
import re,sys
src,dst=sys.argv[1],sys.argv[2]
D=float(sys.argv[3]) if len(sys.argv)>3 else 0.17
mode=sys.argv[4] if len(sys.argv)>4 else 'local'
T0,W0,W1,T1=23.9,24.4,27.0,27.4
LA0,LA1=59.9,60.8
vbW,vbH=2752.766,1537.631
lon=lambda x:-166.4+x/vbW*360
lat=lambda y:98-y/vbH*198
dy_per_deg=vbH/198
s=open(src,encoding='utf-8').read()
i=s.index('id="path4682"')
st=s.rfind('<path',0,i); en=s.index('/>',i)
blk=s[st:en]
m=re.search(r'd="([^"]*)"',blk)
d=m.group(1)
cnt=[0]
def w(lo):
    if lo<=T0 or lo>=T1: return 0.0
    if lo<W0: return (lo-T0)/(W0-T0)
    if lo>W1: return (T1-lo)/(T1-W1)
    return 1.0
def sub(mm):
    x=float(mm.group(1)); y=float(mm.group(2))
    lo,la=lon(x),lat(y)
    if mode=='whole': f=1.0
    elif LA0<=la<=LA1: f=w(lo)
    else: f=0.0
    if f==0: return mm.group(0)
    cnt[0]+=1
    return f"{x:.2f},{y+f*D*dy_per_deg:.2f}"
d2=re.sub(r'(-?\d+\.\d+),(-?\d+\.\d+)',sub,d)
s2=s[:st]+blk.replace(d,d2)+s[en:]
open(dst,'w',encoding='utf-8',newline='').write(s2)
print('moved coords',cnt[0],'D',D,'mode',mode)
