"""(403) 事前登録 [4]② 発散の 1 本＝国債÷年GDP（nation_s の govDebtStock ÷ nominalGdpPerYear）の国×全期の最大を種ごとに出し、5 を超える種の数を腕ごとに数える。読むだけ。使い方：python debtgdp.py A=<ins> B=<ins> N1=<ins> N2=<ins>"""
import csv, sys, glob, re, os
csv.field_size_limit(1<<30)
arms={}
for a in sys.argv[1:]:
    k,v=a.split('=',1); arms[k]=v
for k,d in arms.items():
    per=[]
    for f in sorted(glob.glob(os.path.join(d,'nation_s*.tsv')), key=lambda s:int(re.search(r'_s(\d+)',s).group(1))):
        mx=0.0; who=''
        with open(f,'rb') as fh:
            rd=csv.reader(fh.read().decode('utf-8').splitlines(),delimiter='\t'); h=next(rd)
            ix={n:i for i,n in enumerate(h)}; iN=ix['nation']; iD=ix['govDebtStock']; iG=ix['nominalGdpPerYear']
            for r in rd:
                if len(r)!=len(h): continue
                try: d_=float(r[iD]); g=float(r[iG])
                except: continue
                if g>0 and d_==d_ and g==g:
                    x=d_/g
                    if x>mx: mx=x; who=r[iN]
        per.append((mx,who))
    over=sum(1 for m,_ in per if m>5)
    print(f"{k}\t5超の種 {over}/{len(per)}\t種ごとの最大 "+' '.join(f"{m:.2f}({w})" for m,w in per))
