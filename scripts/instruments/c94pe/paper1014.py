# (1014) c94pe 紙の台本（読むだけ）：今の盤面（keep_1011/base_ins）の merch_s で、門 G1・G1′・G2・G3 の旗 0 の値と、旗 1 の紙の予言を出す。
# 使い方：python paper1014.py <計器つきの生（merch_s1..10）> [種の起点=1] [種の数=10]
# 予言の仮定（名乗る）：錠 A の最初の行の次の四半期から、その州の商人は空でなくなり、以後は空に戻らない（上限側の見込み）。
import os, sys
from collections import defaultdict
d = sys.argv[1]; sb = int(sys.argv[2]) if len(sys.argv) > 2 else 1; ns = int(sys.argv[3]) if len(sys.argv) > 3 else 10
def rows(s):
    with open(os.path.join(d, f'merch_s{s}.tsv'), 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            yield ix, ln.decode('utf-8').rstrip('\r\n').split('\t')
T = defaultdict(int); per = []
for s in range(sb, sb + ns):
    ser = defaultdict(list); P = defaultdict(int)
    for ix, x in rows(s):
        q = int(x[0]); tier = x[ix['tier']]; reg = x[ix['region']]
        me = x[ix['merchantEmpty']] == '1'; la = x[ix['lockABlockedMerchant']] == '1'; laA = x[ix['lockABlockedArtisan']] == '1'
        mk = float(x[ix['merchantK']])
        ser[reg].append((q, tier, me, la, mk))
        if laA: P['lockA_artisan_all'] += 1
        if la and tier != 'Colonial': P['lockA_merchant_nonColonial'] += 1
        if tier == 'Colonial':
            P['colRows'] += 1; P['colEmpty'] += me; P['colLockA'] += la
            if not me:
                P['colNonEmpty'] += 1
                ep = float(x[ix['merchEarnedPerCap']]); wi = float(x[ix['workerIncomePerCap']]); pr = float(x[ix['promotionPremium']])
                if wi == wi and ep < wi * pr: P['g3num'] += 1
                if not (wi == wi): P['g3_wi_nan'] += 1
    beng_empty = 0; beng_pred = 0
    for reg, L in ser.items():
        L.sort()
        first = None
        for i, (q, tier, me, la, mk) in enumerate(L):
            if i + 1 < len(L) and L[i + 1][0] == q + 1 and la:
                P['g1p_den'] += 1; P['g1p_num'] += L[i + 1][4] > 1e-9
            if first is None and la: first = q
            gone = first is not None and q > first
            if tier == 'Colonial' and me and not gone: P['predColEmpty'] += 1
            if reg == '中核ベンガル' and me:
                beng_empty += 1; beng_pred += not gone
    P['bengEmpty'] = beng_empty; P['bengPred'] = beng_pred
    per.append((s, dict(P)))
    for k, v in P.items(): T[k] += v
def show(tag, P):
    g1 = P['colEmpty'] / max(1, P['colRows']); g1n = P['predColEmpty'] / max(1, P['colRows'])
    g3 = P['g3num'] / max(1, P['colNonEmpty']); g1p = P['g1p_num'] / max(1, P['g1p_den'])
    print(f"{tag}: 植民地格 {P['colRows']} 空 {P['colEmpty']} G1 旗0={g1:.4f} 予言={g1n:.4f}（新÷旧 {g1n/max(1e-12,g1):.4f}） 錠A(商人・植民地) {P['colLockA']} 錠A(商人・植民地の外) {P.get('lockA_merchant_nonColonial',0)} 錠A(職人・全格) {P.get('lockA_artisan_all',0)}"
          f" | G1′ 旗0={g1p:.4f}（{P['g1p_num']}/{P['g1p_den']}） | G2 ベンガル空 旗0={P['bengEmpty']} 予言={P['bengPred']} | G3 旗0={g3:.4f}（{P['g3num']}/{P['colNonEmpty']}・wi nan {P.get('g3_wi_nan',0)}）")
for s, P in per: show(f's{s}', defaultdict(int, P))
show('和', T)
b = sorted(P['bengEmpty'] for _, P in per); bp = sorted(P['bengPred'] for _, P in per)
med = lambda a: (a[len(a)//2] + a[(len(a)-1)//2]) / 2
print(f"G2 種の中央 旗0={med(b)} 予言={med(bp)}  旗0 種 {b}  予言 種 {bp}")
g3s = sorted(P['g3num'] / max(1, P['colNonEmpty']) for _, P in per); g1s = sorted(P['colEmpty'] / max(1, P['colRows']) for _, P in per)
print(f"G3 種の幅 {g3s[0]:.4f}〜{g3s[-1]:.4f}  上端×1.5={g3s[-1]*1.5:.4f}   G1 種の幅 {g1s[0]:.4f}〜{g1s[-1]:.4f}")
