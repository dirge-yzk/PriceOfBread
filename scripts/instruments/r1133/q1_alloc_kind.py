# (1133) 問い1の補い：rulepoweralloc_s（四半期 0,40,…,640・10 種）で、エンジンの配り（allocEngIndex）＞0 の行を candKind（計器の影の分け方：1＝その国が既に取り分を持つ・2＝取り分 0 で持ち主と交戦・3＝取り分 0 で陸の隣のみ）と四半期 0 か否かで数える。
# 取り分の大きさ（sShare）が 1e-3 未満の〈尾〉の行も数える。
import sys, os
from collections import defaultdict
ins = sys.argv[1]
cnt = defaultdict(lambda: [0, 0.0, 0.0]); tail = defaultdict(lambda: [0, 0.0])
for s in range(1, 11):
    with open(os.path.join(ins, f'rulepoweralloc_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            try: a = float(c[ix['allocEngIndex']]); F = float(c[ix['fForeignEngIndex']]); sh = float(c[ix['sShare']])
            except ValueError: continue
            if not a > 0: continue
            k = ('q0' if c[0] == '0' else 'q>0', c[ix['candKind']]); x = cnt[k]; x[0] += 1; x[1] += a; x[2] += F
            if c[ix['candKind']] == '1':
                t = tail['尾 s<1e-3' if sh < 1e-3 else '中 1e-3≦s<0.05' if sh < 0.05 else '大 s≧0.05']; t[0] += 1; t[1] += F
print('四半期\tcandKind\t配り＞0 の行\t配りの和\tエンジンの外国の力の和')
for k, x in sorted(cnt.items()): print(f'{k[0]}\t{k[1]}\t{x[0]}\t{x[1]:.4g}\t{x[2]:.4g}')
print('\ncandKind 1 の行を取り分の大きさで分けた：区分\t行\tエンジンの外国の力の和')
for k, x in sorted(tail.items()): print(f'{k}\t{x[0]}\t{x[1]:.4g}')
