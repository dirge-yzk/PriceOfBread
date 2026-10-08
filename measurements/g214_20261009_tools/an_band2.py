import sys
sys.path.insert(0,'.')
from an_g214 import *
d=sys.argv[1]; R=load(d)
for ln in open(d+'/s1.txt',encoding='utf-8'):
    if ln.startswith('#N'): hdr=ln[1:].rstrip('\n').split('|'); break
ix={k:i for i,k in enumerate(hdr)}
for band in (0.675,1.0,2.0):
  for lo,hi in ((1783,1842),(1843,1872),(1873,1913),(1914,1950)):
    tot=0;dn=0;nat=set();ukd=0;ukt=0
    for seed,(W,O,N) in R.items():
        for c in N:
            y=int(c[2])
            if lo<=y<=hi and int(c[ix['law(0不換1銀2金)']])==2 and float(c[ix['pop']])>0 and c[ix['wishUp']]!='nan':
                x=float(c[ix['wishUp']]); tot+=1
                if -x>band: dn+=1; nat.add((seed,c[4]))
                if c[4]=='イギリス':
                    ukt+=1
                    if -x>band: ukd+=1
    if tot: print(f"金の国 帯{band} {lo}-{hi}: 下げ {dn/tot:.3f} (国×四半期{tot})  下げが一度でも立った国×種 {len(nat)}  英 {ukd}/{ukt}")
