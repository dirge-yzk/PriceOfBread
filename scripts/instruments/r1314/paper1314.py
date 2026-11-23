# (1314) c136sh 紙：自給の世帯の家計の段 3-0 を盤面 m1308 で（keep_1308/base_ins 種 1〜10・読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I paper1314.py <base_ins> <出力 txt> <出力 tsv>
# 定義（subst_s の s2 の列＝四半期の末の値・s2BasisKind＝2 の行＝焼いた後）：
#   sraw＝U÷Lneed（＝ρ×U×T÷(必要量×頭数)・切る前の食べる割合の影・頭数は s2HeadsEnd）
#   soc＝sold÷cap（市場へ出した労働の割合）・kH＝1−min(U,Lneed)÷cap（売った労働＋自分の畑の余り）
#   sig＝食べる割合（subRateWeighted を subPopK で重み）
# 技術（cons_s の techLevelMean＝国の値・1783〜1823 年だけ）：B(TL)＝1＋min(TL,2)＋0.3×max(0,TL−2)（Improvement.cs:27 の式を写した・呼ばない）
# 案 C の影：sraw_C＝sraw×B(TL_今)÷B(TL_開幕)（ρ は開幕の t を吸う・一次の近似＝労働の帰還を無視）
import sys, os, math, statistics as st
from collections import defaultdict

base, out_txt, out_tsv = sys.argv[1], sys.argv[2], sys.argv[3]
SEEDS = range(1, 11)
MAJ = ['イギリス', 'フランス', 'プロイセン', 'ロシア', '清', '日本', 'アメリカ', 'オーストリア', 'オスマン']

def fnum(x):
    try:
        v = float(x); return v if math.isfinite(v) else None
    except ValueError:
        return None

def B(tl): return 1.0 + min(tl, 2.0) + max(0.0, tl - 2.0) * 0.3

out = []
def P(s): out.append(s)

# 1) 技術の道：国×種×年（四半期の平均）
tl = defaultdict(list)          # (seed, nation, year) -> [TL]
tl0 = {}                        # (seed, nation) -> TL at quarter 0
for sd in SEEDS:
    with open(os.path.join(base, f'cons_s{sd}.tsv'), 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        seen = set()
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            q, y, n = int(c[ix['quarter']]), int(c[ix['year']]), c[ix['nation']]
            if (q, n) in seen: continue
            seen.add((q, n))
            v = fnum(c[ix['techLevelMean']])
            if v is None: continue
            tl[(sd, n, y)].append(v)
            if q == 0: tl0[(sd, n)] = v

def tlmean(sd, n, y):
    a = tl.get((sd, n, y)); return sum(a) / len(a) if a else None

nations = sorted({k[1] for k in tl0})
P('# (1314) 紙（m1308・keep_1308/base_ins 種 1〜10・国ごとに種の中央）')
P('## 1. 技術の道（cons_s の techLevelMean・1783〜1823）')
t0all = [tl0[k] for k in tl0]
P(f'開幕 q0 の TL：国×種 {len(t0all)}・最大 {max(t0all):.4f}・中央 {st.median(t0all):.4f}')
P('国\tTL 開幕\tTL 1803\tTL 1823\tB(1823)÷B(開幕)')
ratios = {}
for n in nations:
    r0, r1, r2, rr = [], [], [], []
    for sd in SEEDS:
        a, b, c = tl0.get((sd, n)), tlmean(sd, n, 1803), tlmean(sd, n, 1823)
        if a is None or c is None: continue
        r0.append(a); r2.append(c); rr.append(B(c) / B(a))
        if b is not None: r1.append(b)
    if not rr: continue
    ratios[n] = st.median(rr)
    if n in MAJ: P(f'{n}\t{st.median(r0):.3f}\t{st.median(r1) if r1 else float("nan"):.3f}\t{st.median(r2):.3f}\t{st.median(rr):.3f}')
rv = sorted(ratios.values())
P(f'B の比の国の分布（{len(rv)} 国）：最小 {rv[0]:.3f}・中央 {st.median(rv):.3f}・最大 {rv[-1]:.3f}・1.5 以上 {sum(1 for x in rv if x >= 1.5)} 国')

# 2) 州の行：開幕・1823・1850・1913（四半期の最初の行＝その年の第 1 四半期）
WIN = {'open': None, '1823': 1823, '1850': 1850, '1913': 1913}
rowsum = defaultdict(lambda: [0, 0, 0.0, 0.0, 0, 0.0, 0, 0.0])   # (win, tier) -> [rows, rows sraw>1, pop, pop sraw>1, rowsC>1, popC>1]
eshare = defaultdict(list)   # (win) -> 案 C の余りの割合 1−1/sraw_C（sraw_C>1 の行）
sig = defaultdict(lambda: [0.0, 0.0])   # (seed, nation, win) -> [pop, pop*sig]
kap = defaultdict(lambda: [0.0, 0.0, 0.0])   # (seed, nation, win) -> [cap, sold, minUL]
coreSig = defaultdict(lambda: [0.0, 0.0]); coreKap = defaultdict(lambda: [0.0, 0.0])   # (tier, year) -> sums
for sd in SEEDS:
    with open(os.path.join(base, f'subst_s{sd}.tsv'), 'r', encoding='utf-8', newline='') as f:
        h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            c = line.rstrip('\r\n').split('\t')
            q, y, n, tier = int(c[ix['quarter']]), int(c[ix['year']]), c[ix['nation']], c[ix['tier']]
            sp, sr = fnum(c[ix['subPopK']]), fnum(c[ix['subRateWeighted']])
            kind = c[ix['s2BasisKind']]
            cap, sold, U, Ln = (fnum(c[ix[k]]) for k in ('s2CapEnd', 's2SoldEnd', 's2UEnd', 's2LneedEnd'))
            if sp is not None and sr is not None and sp > 0 and tier in ('Core', 'Integrated', 'Colonial'):
                tk = 'Home' if tier in ('Core', 'Integrated') else 'Colonial'
                coreSig[(tk, y)][0] += sp; coreSig[(tk, y)][1] += sp * sr
                if kind == '2' and cap and cap > 0 and sold is not None and U is not None and Ln is not None:
                    coreKap[(tk, y)][0] += cap; coreKap[(tk, y)][1] += cap - min(U, Ln)
            win = 'open' if q == 0 else None
            if win is None:
                for w, yy in WIN.items():
                    if yy == y and q % 4 == 0: win = w
            if win is None: continue
            if sp is not None and sr is not None and sp > 0:
                a = sig[(sd, n, win)]; a[0] += sp; a[1] += sp * sr
            if kind != '2' or not cap or cap <= 0 or U is None or Ln is None or not Ln or Ln <= 0 or sold is None: continue
            k = kap[(sd, n, win)]; k[0] += cap; k[1] += sold; k[2] += min(U, Ln)
            sraw = U / Ln
            tl_now = tlmean(sd, n, y) if y <= 1823 else None
            t_0 = tl0.get((sd, n))
            srC = sraw * B(tl_now) / B(t_0) if (tl_now is not None and t_0 is not None) else None
            for tk in (tier, 'all'):
                r = rowsum[(win, tk)]; r[0] += 1; r[2] += sp or 0
                if sraw > 1: r[1] += 1; r[3] += sp or 0
                if srC is not None: r[6] += 1; r[7] += sp or 0
                if srC is not None and srC > 1: r[4] += 1; r[5] += sp or 0
            if srC is not None and srC > 1: eshare[win].append(1 - 1 / srC)

P('## 2. 州の行（s2BasisKind＝2）：sraw＞1 の行（今）と案 C の影 sraw_C＞1 の行（1823 まで）')
P('窓\ttier\t行\tsraw>1 行\tsraw>1 人口割合\tsraw_C>1 行\tsraw_C>1 人口割合（分母＝TL のある 9 国の行）\t分母の行')
for (w, tk), r in sorted(rowsum.items()):
    if tk not in ('all', 'Core', 'Colonial'): continue
    P(f'{w}\t{tk}\t{r[0]}\t{r[1]}\t{(r[3] / r[2] if r[2] else 0):.4f}\t{r[4]}\t{(r[5] / r[7] if r[7] else 0):.4f}\t{r[6]}')
for w, a in eshare.items():
    P(f'案 C の余りの割合 1−1/sraw_C（{w}・sraw_C>1 の行 {len(a)}）：中央 {st.median(a):.3f}・最大 {max(a):.3f}')

P('## 3. 国ごと（種の中央）：開幕の食べる割合・κ の向き')
def nmed(d, n, w, fn):
    v = [fn(d[(sd, n, w)]) for sd in SEEDS if (sd, n, w) in d and d[(sd, n, w)][0] > 0]
    return st.median(v) if v else None
sgf = lambda a: a[1] / a[0]
kf = lambda a: 1 - a[2] / a[0]
ns = sorted({k[1] for k in sig})
openS = {n: nmed(sig, n, 'open', sgf) for n in ns}
openS = {n: v for n, v in openS.items() if v is not None}
inb = [n for n, v in openS.items() if 0.68 <= v <= 0.72]
P(f'開幕の sig が [0.68,0.72] の国 {len(inb)} / {len(openS)}・外：' + '・'.join(f'{n} {v:.3f}' for n, v in sorted(openS.items(), key=lambda x: x[1]) if not (0.68 <= v <= 0.72)))
K = {w: {n: nmed(kap, n, w, kf) for n in ns} for w in WIN}
up1 = sum(1 for n in ns if K['open'].get(n) is not None and K['1850'].get(n) is not None and K['1850'][n] > K['open'][n])
dn2 = sum(1 for n in ns if K['1850'].get(n) is not None and K['1913'].get(n) is not None and K['1913'][n] < K['1850'][n])
mono = sum(1 for n in ns if all(K[w].get(n) is not None for w in ('open', '1850', '1913')) and K['open'][n] < K['1850'][n] < K['1913'][n])
both = sum(1 for n in ns if K['open'].get(n) is not None and K['1850'].get(n) is not None)
both2 = sum(1 for n in ns if K['1850'].get(n) is not None and K['1913'].get(n) is not None)
P(f'κ(kH)：開幕→1850 で増えた国 {up1}/{both}・1850→1913 で下がった国 {dn2}/{both2}・開幕＜1850＜1913 の国 {mono}')

P('## 4. 本国格（Core＋Integrated）と植民地格の年ごと：食べる割合 sig と κ（kH）')
ys = sorted({y for (_, y) in coreSig})
hs = {y: coreSig[('Home', y)][1] / coreSig[('Home', y)][0] for y in ys if coreSig[('Home', y)][0] > 0}
hk = {y: coreKap[('Home', y)][1] / coreKap[('Home', y)][0] for y in ys if coreKap[('Home', y)][0] > 0}
ck = {y: coreKap[('Colonial', y)][1] / coreKap[('Colonial', y)][0] for y in ys if coreKap[('Colonial', y)][0] > 0}
y0 = ys[0]
trough = min((y for y in hs if y <= 1900), key=lambda y: hs[y])
ret = next((y for y in sorted(hs) if y > trough and hs[y] >= hs[y0]), None)
kpk = max(hk, key=lambda y: hk[y])
P(f'本国格の sig：{y0} {hs[y0]:.4f}・谷 {trough} {hs[trough]:.4f}・開幕を超えた最初の年 {ret}（{hs[ret]:.4f}）' if ret else f'本国格の sig：谷 {trough}・開幕を超える年なし')
P(f'本国格の κ の頂：{kpk} {hk[kpk]:.4f}・1950 {hk.get(max(hk)):.4f}・植民地格の κ {ys[0]} {ck.get(ys[0], float("nan")):.4f} → {max(ck)} {ck[max(ck)]:.4f}')
with open(out_tsv, 'wb') as g:
    s = 'year\thomeSig\thomeKappa\tcolKappa\n' + ''.join(f'{y}\t{hs.get(y, float("nan")):.5f}\t{hk.get(y, float("nan")):.5f}\t{ck.get(y, float("nan")):.5f}\n' for y in ys)
    g.write(s.encode('utf-8'))
with open(out_txt, 'wb') as g:
    g.write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
