# (1102) 問い 1 の続き：清の d（L÷人口 の世界比）を〈頭数の密度〉soldierK÷popK と〈1 人あたりの陸軍力〉L÷soldierK に分ける。表を読むだけ。
# 使い方: python -X utf8 -I qing_soldier.py <nat_X.tsv> <X の表のフォルダ> > 出力
# d＝(L_清÷pop_清)÷(ΣL÷Σpop)＝[(兵_清÷pop_清)÷(Σ兵÷Σpop)]×[(L_清÷兵_清)÷(ΣL÷Σ兵)]＝頭数の比×1 人あたりの比（恒等式・兵＝nation_s の soldierK・pop＝nat の popK）。
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')
QING = '清'
natp, folder = sys.argv[1], sys.argv[2]
sol = {}
for s in range(1, 11):
    p = os.path.join(folder, f'nation_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: sol[(s, int(c[ix['quarter']]), c[ix['nation']])] = float(c[ix['soldierK']])
            except ValueError: pass
data = collections.defaultdict(dict)
with open(natp, encoding='utf-8') as f:
    h = f.readline().rstrip('\n').split('\t'); ix = {k: j for j, k in enumerate(h)}
    for line in f:
        c = line.rstrip('\n').split('\t')
        data[(int(c[0]), int(c[1]))][c[ix['nation']]] = (float(c[ix['L']]), float(c[ix['popK']]))
rec = collections.defaultdict(lambda: collections.defaultdict(list)); miss = 0
for (s, q), nat in data.items():
    L = sum(v[0] for v in nat.values()); P = sum(v[1] for v in nat.values())
    S = 0.0
    for nm in nat:
        x = sol.get((s, q, nm))
        if x is None: miss += 1; continue
        S += x
    qg = nat.get(QING); xs = sol.get((s, q, QING))
    if qg is None or xs is None or not (L > 0 and P > 0 and S > 0 and xs > 0 and qg[1] > 0): continue
    head = (xs / qg[1]) / (S / P); per = (qg[0] / xs) / (L / S)
    rec[q]['d'].append(head * per); rec[q]['頭数の比'].append(head); rec[q]['1 人あたりの比'].append(per)
    rec[q]['清の兵÷人口'].append(xs / qg[1]); rec[q]['世界の兵÷人口'].append(S / P)
print(f'soldierK が見つからない国×四半期 {miss}')
keys = ['d', '頭数の比', '1 人あたりの比', '清の兵÷人口', '世界の兵÷人口']
print('四半期\t' + '\t'.join(keys) + '\t（種の中央）')
for q in [0, 20, 40, 100, 200, 300, 400, 500, 600, 660]:
    if q in rec: print(f'{q}\t' + '\t'.join(f'{st.median(rec[q][k]):.4g}' for k in keys))
allq = sorted(rec)
for k in ('頭数の比', '1 人あたりの比'):
    v = [st.median(rec[q][k]) for q in allq]
    print(f'{k}（種の中央・全ての四半期）：最小 {min(v):.4g}・中央 {st.median(v):.4g}・最大 {max(v):.4g}')
