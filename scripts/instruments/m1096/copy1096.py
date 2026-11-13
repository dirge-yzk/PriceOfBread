# (1096) 合流：読む線 ar (1092)・al (1093)・s14 (1094) と設計の材料 rc (1095) の台本と表を写す（中身は同じ・CRLF だけ LF へ）。
# 台本（.py・.sh）→ scripts/instruments/r<番号>/・それ以外 → measurements/m<番号>_20261113/。印（.done・.mark）と r1093 の *_rows.tsv（1.68GB・q1scan.py で作り直せる）は写さない。
import os,shutil
R='D:/PoB_work/wt_m1096'
def put(src,dst):
    os.makedirs(os.path.dirname(dst),exist_ok=True)
    b=open(src,'rb').read()
    if b'\0' not in b[:4096]: b=b.replace(b'\r\n',b'\n')
    open(dst,'wb').write(b)
n=0
for num,src in (('1092','D:/PoB_work/r1092_work'),('1093','D:/PoB_work/r1093_work'),('1094','D:/PoB_work/r1094_work'),('1095','D:/PoB_work/r1095_work'),('1095','D:/PoB_work/d1095_work')):
    for root,_,fs in os.walk(src):
        for f in sorted(fs):
            if f.endswith(('.done','.mark')) or f.endswith('_rows.tsv'): continue
            p=os.path.join(root,f); rel=os.path.relpath(p,src).replace(os.sep,'/')
            if src.endswith('d1095_work'): rel='d1095/'+rel
            if num=='1095' and rel in ('material.md','draft.md'): continue
            if f.endswith(('.py','.sh')) and '/' not in rel: d=f'{R}/scripts/instruments/r{num}/{rel}'
            else: d=f'{R}/measurements/m{num}_20261113/{rel}'
            put(p,d); n+=1
print('copied',n)
