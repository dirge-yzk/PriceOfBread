# (1133) 問い3の確かめ：rule_s（計器の名簿 9 国）の treatyPortInForceFlag・portForeignShare（エンジンの関数の値）と、q3_readers.py の組み直しを並べる。
# 併せて rulepoweralloc_s（40 四半期ごと）で、指定の州へエンジンが配った外国の国（キー）と candKind を数える。
import sys, os
from collections import defaultdict
ins = sys.argv[1]; regs = set(sys.argv[2].split(','))
fl = defaultdict(lambda: [0, 0, 0.0])
for s in range(1, 11):
    with open(os.path.join(ins, f'rule_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        seen = set()
        for ln in f:
            c = ln.rstrip('\n').split('\t'); k = (c[0], c[ix['nation']])
            if k in seen: continue
            seen.add(k); a = fl[c[ix['nation']]]; a[0] += 1; a[1] += c[ix['treatyPortInForceFlag']] == '1'; a[2] += float(c[ix['portForeignShare']])
print('rule_s の値：国\t国×四半期\t不平等条約の旗の割合\t港の外国の取り分の平均（関税の倍率＝1−これ）')
for n, a in sorted(fl.items()): print(f'{n}\t{a[0]}\t{a[1] / a[0]:.4f}\t{a[2] / a[0]:.4f}')
al = defaultdict(lambda: defaultdict(lambda: [0, 0.0]))
for s in range(1, 11):
    p = os.path.join(ins, f'rulepoweralloc_s{s}.tsv')
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[ix['region']] not in regs: continue
            try: a = float(c[ix['allocEngIndex']])
            except ValueError: continue
            if a > 0:
                x = al[c[ix['region']]][(c[ix['nation']], c[ix['candKind']])]; x[0] += 1; x[1] += float(c[ix['fForeignEngIndex']])
print('\n配りの表（四半期 0,40,…・10 種）：州\t外国の国\tcandKind（1＝取り分あり 2＝交戦 3＝陸の隣のみ）\t配り＞0 の行\tエンジンの力の和')
for r in sorted(al):
    for (n, k), x in sorted(al[r].items(), key=lambda z: -z[1][0])[:4]: print(f'{r}\t{n}\t{k}\t{x[0]}\t{x[1]:.4g}')
