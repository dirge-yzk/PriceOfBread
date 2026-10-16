# (372) 合流：主要国の開幕と 1914 年（四半期 524）の popK・旧の錨・州の和・使った錨・扶養力・Σ耕地・活きた州・本国起源の州（10 種の中央・腕 A と B）。読むだけ。
import csv, statistics as st, sys
W = sys.argv[1]
MAJ = ['イギリス','フランス','プロイセン','オーストリア','スペイン','ポルトガル','サルデーニャ＝ピエモンテ','スウェーデン']
def rows(arm, s, qs):
    d = {}
    with open(f"{W}/out_{arm}_10/ins/anchor_s{s}.tsv", encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        for ln in r:
            q = int(ln[ix['quarter']]); n = ln[ix['nation']]
            if q in qs and n in MAJ:
                d[(n, q)] = tuple(float(ln[ix[c]]) for c in ('popK','anchorOld','anchorRegions','anchorUsed','capacity','arableSum','activeRegionsN','anchorRegionsN'))
    return d
res = {(a, s): rows(a, s, {0, 524}) for a in ('A', 'B') for s in range(1, 11)}
print('国 腕 四半期: popK・anchorOld・anchorRegions・anchorUsed・capacity・arableSum・活きた州・本国起源の州（10 種の中央）')
for n in MAJ:
    for q in (0, 524):
        for a in ('A', 'B'):
            vals = [v for v in (res[(a, s)].get((n, q)) for s in range(1, 11)) if v]
            if vals:
                print(n, a, q, ' '.join(f"{st.median(v[i] for v in vals):.0f}" for i in range(8)))
