#!/usr/bin/env python3
# (792) c10tg：基準の計器の生（keep_790/base_ins）の portch_s の portNew と portlm_s の portA を同じ（種・年・国・州）で突き合わせる。
# 使い方：PYTHONUTF8=1 python -I cmp_portnew.py  → rows（突き合わせた行数）・mismatch（不一致）・keymiss（キーの欠け）
import sys,io
base='D:/PoB_work/keep_790/base_ins/'
tot=0;mis=0;miss=0;ex=[]
for s in range(1,11):
    def load(f,cols):
        d={}
        with open(base+f'{f}_s{s}.tsv','rb') as fh:
            h=fh.readline().decode('utf-8').rstrip('\r\n').split('\t')
            idx=[h.index(c) for c in cols]
            n=0
            for line in fh:
                p=line.decode('utf-8').rstrip('\r\n').split('\t')
                key=(p[h.index('year')],p[h.index('nation')],p[h.index('region')])
                d.setdefault(key,[]).append(tuple(p[i] for i in idx))
        return d
    a=load('portch',['portNew']); b=load('portlm',['portA'])
    for k,v in a.items():
        w=b.get(k)
        if w is None or len(w)!=len(v): miss+=1; continue
        for x,y in zip(v,w):
            tot+=1
            if x[0]!=y[0]:
                mis+=1
                if len(ex)<5: ex.append((s,k,x,y))
    for k in b:
        if k not in a: miss+=1
print('rows',tot,'mismatch',mis,'keymiss',miss,ex)
