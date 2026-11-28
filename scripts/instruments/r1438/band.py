# (1438) 宗教の項の大きさ（1 点前後〈推〉）で入れ替わり得る帯の行の数：緊張が [40,41.2)・[140,141.2) の四半期の行。使い方: python band.py <生> <種の範囲>
import sys
d=sys.argv[1]; s0,s1=map(int,sys.argv[2].split('-')); a=b=n40=n140=0
for s in range(s0,s1+1):
    L=open(f'{d}/tensionwhy_s{s}.tsv','rb').read().decode().splitlines(); h=L[0].split('\t'); it=h.index('tension')
    for l in L[1:]:
        r=l.split('\t')
        if len(r)!=len(h): continue
        t=float(r[it])
        if t>=40: n40+=1
        if t>=140: n140+=1
        if 40<=t<41.2: a+=1
        if 140<=t<141.2: b+=1
print(f'{d}\tband40={a}\tge40={n40}\tshare40={a/n40:.4f}\tband140={b}\tge140={n140}\tshare140={b/n140:.4f}')
