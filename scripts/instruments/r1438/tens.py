# (1438) tensionwhy_s の行数・緊張の和・閾値以上の行（m1428 paper_rg と同じ定義：四半期の行の tension 列）。使い方: python tens.py <生> <種の範囲>
import sys
d=sys.argv[1]; s0,s1=map(int,sys.argv[2].split('-')); rows=0; S=0.0; th={40:0,110:0,140:0}
for s in range(s0,s1+1):
    L=open(f'{d}/tensionwhy_s{s}.tsv','rb').read().decode().splitlines(); h=L[0].split('\t'); it=h.index('tension')
    for l in L[1:]:
        r=l.split('\t')
        if len(r)!=len(h): continue
        t=float(r[it]); rows+=1; S+=t
        for k in th:
            if t>=k: th[k]+=1
print(f'{d}\trows={rows}\tsumTension={S:.6e}\tge40={th[40]}\tge110={th[110]}\tge140={th[140]}')
