#!/usr/bin/env python
# (497) 線 mw 後半：段 0 の続き＝規則が現行法へ書き戻した件（an_mw497_s0.py の erased_net）を、行 a の失業の帯（<0.25 / 0.25〜0.5）と bindNext（>1/3 / <=1/3 / nan）で分ける（読むだけ）。
# 使い方：python an_mw497_s0b.py   （入力は D:/PoB_work/keep_491/m491_ins の ma_s1〜10 を固定で読む）　出力＝measurements/mw497_20261023_s0b.txt
# 限界：行 a の失業・bindNext は四半期の末の断面で、規則の判断は次の四半期の初め。wantNew==lawCode(b) の分けも行 b の末の断面。
import csv,collections,math
D='D:/PoB_work/keep_491/m491_ins/'
def f(x):
    try: return float(x)
    except: return float('nan')
c=collections.Counter(); wn=collections.Counter()
for s in range(1,11):
    rows=collections.defaultdict(list)
    for r in csv.DictReader(open(D+f'ma_s{s}.tsv',encoding='utf-8'),delimiter='\t'):
        rows[r['nation']].append((int(r['quarter']),int(r['lawCode']),int(r['ruleTargetOrd']),int(r['policyTarget']),f(r['unemployment']),f(r['bindNext']),int(r['wantNew'])))
    for n,rs in rows.items():
        rs.sort()
        for a,b in zip(rs,rs[1:]):
            if b[0]!=a[0]+1: continue
            cur0,prior=a[1],a[3]
            if prior==cur0: continue
            if b[2]==cur0 and b[3]==b[1] and b[1]==cur0:
                u=a[4]; bn=a[5]
                band='gap' if 0.25<=u<0.5 else ('lo' if u<0.25 else 'hi')
                bk='nan' if math.isnan(bn) else ('>1/3' if bn>1/3 else '<=1/3')
                c[(band,bk)]+=1
                wn['wantNew==lawCode(b)' if b[6]==b[1] else 'wantNew!=lawCode(b)']+=1
print(sorted(c.items())); print(dict(wn))
