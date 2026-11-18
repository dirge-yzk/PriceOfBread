# (1224) 読むだけ：台帳 S26 の読み直す行を keep_1219 の生で数える（母集合を名乗る）。
#  使い方: python -X utf8 -I s26_reread.py <生のフォルダ> <種 a-b> <プローブの P の tsv（旗 0）> <出力>
#  region＝全ての国の州×四半期（tier 列）。P＝(1224) のプローブ（英仏の植民地格の州の不在地主の大農園×月・種 1〜10・12 か月）。
import sys, collections
raw, rng, ptsv, out = sys.argv[1:5]
s0, s1 = map(int, rng.split('-'))
L = []
def P(*a): L.append(' '.join(str(x) for x in a))
P('== S26-01・02・35：植民地格の州の数（region_s・全ての国の州×四半期・activeFlag 1 の行）')
for s in range(s0, s1 + 1):
    byq = collections.Counter(); ever = set(); exits = 0; last = {}
    with open(f'{raw}/region_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip('\n').split('\t')
            if c[ix['activeFlag']] != '1': continue
            q = int(c[ix['quarter']]); key = c[ix['region']]
            if c[ix['tier']] == 'Colonial': byq[q] += 1; ever.add(key)
            last[key] = (q, c[ix['tier']], c[ix['nation']])
    qs = sorted(byq); mx = max(byq.values())
    first_drop = next((q for q in qs if byq[q] < max(byq[p] for p in qs if p <= q)), None)
    P(f'種 {s}: 最初の四半期 {qs[0]} の植民地格 {byq[qs[0]]}・最大 {mx}・最後の四半期 {qs[-1]} {byq[qs[-1]]}・一度でも植民地格 {len(ever)}・最後に植民地格でない（格が変わった）{sum(1 for k in ever if last[k][1] != "Colonial")}・和が前の最大を下回った最初の四半期 {first_drop}')
P('== S26-07・08・09：不在地主の大農園の地代の割合と清算・造幣の代金（プローブ P・旗 0・PaySales の呼び出しを州×財×種類で積んだ物）')
cnt = collections.Counter(); rs = collections.defaultdict(list); amt = collections.Counter()
with open(ptsv, encoding='utf-8') as f:
    f.readline(); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for l in f:
        c = l.rstrip('\n').split('\t'); g = c[ix['good']]; k = c[ix['kind']]
        try: a = float(c[ix['amount']]) * float(c[ix['prodShare']])
        except ValueError: a = float('nan')
        cnt[(g, k)] += 1; amt[(g, k)] += a if a == a else 0
        rs[g].append(float(c[ix['rentShare']]))
for (g, k), n in sorted(cnt.items(), key=lambda x: -amt[x[0]]):
    v = sorted(rs[g]); P(f'{g} {k}: 行 {n}・大農園の取り分の代金の和 {amt[(g, k)]:.4g}・地代の割合 RentShareOf 最小 {v[0]:.3f} 中央 {v[len(v)//2]:.3f} 最大 {v[-1]:.3f}')
open(out, 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n'); print('\n'.join(L))
