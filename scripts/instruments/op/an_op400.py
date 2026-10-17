"""(400) 英・蘭の開幕の財政の整合の読み手（読むだけ・事前登録 measurements/op_20261018_prereg.txt の [5][6] と追記1〜2）。
使い方：python an_op400.py A=<ins> B=<ins> N1=<ins> N2=<ins> [B1=<ins> B2=<ins>]   （各 <ins> は `INS=1 batch.sh run <腕>` の <出力>/ins）
  腕の名前は A・B・N1・N2（必須）と B1・B2（任意＝切り分け）。ヌル腕は N1・N2 の 2 本。
入力：nation_s<seed>.tsv（全ての国・四半期ごと）と fiscal_s<seed>.tsv（POB_INSTRUMENTS_NATIONS に挙げた国だけ・月ごと）。新しい計器の列は読まない（0 本）。
出すもの（事前登録の記号）：
  M1 開幕の段（B の英・蘭の Q0 monopolyTierBeforeDecisionStage が 10/10 種で 3 か）
  M2 国庫（fiscal_s の最初の行〔monthIndex 2〕の treasuryAtClaimStock ÷ 同じ行の revenueBasePerMonth・2.5 の ±10% は追記2 ⑤）
  M3 門（英の Q0 の stateFacilityStartedCount を腕×種で・合格＝B が A の同じ種より小さい種が 5 以上かつ中央値が小さい〔追記2 ③〕）
  P1〜P4・R1〜R7（窓 W1＝最初の 3 行／W2＝Q0〜Q8／W3＝Q5〜Q40／W4＝Q1〜Q40・種の中央値〔最小〜最大〕）
  止める線の判定式：①同じ国が毎回壊れる ②一国の発散 ③片方向の錠（最大の段の張り付き＋追記2 ① の着工 0 の長い連なり L1）／発散（国債÷年GDP>5・金利の天井）
  追記2 ① の記録：L1＝全国の〈着工を続けていた国が 8 四半期以上 1 件も着工しない連なり〉の数／L2＝名簿の国の門の束縛（工事の月の出費÷上限の近似）
⚠指標の限界（自己申告）：pendingWill〔未達の意志の数〕は計器に無い＝L1 は代理（着工 0 の連なり）。L2 は工賃の部分〔claimLastMonthFactoryBuildPerMonth〕だけで上限は
  0.10×FiscalStressBufferMonths×(利払い＋行政＋陸＋海の請求) の近似（FiscalStressBufferMonths を --buf で渡す・既定は Coefficients から 5 を仮定）。名簿の外の国には L2 を出せない。"""
import sys, os, collections, statistics
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, seeds, num

UK, NL = 'イギリス', 'ネーデルラント'
args = sys.argv[1:]
BUF = 5.0
if '--buf' in args:
    BUF = float(args[args.index('--buf') + 1]); i = args.index('--buf'); del args[i:i + 2]
ARMS = {}
for a in args:
    k, v = a.split('=', 1); ARMS[k] = v
ORDER = [k for k in ('A', 'B', 'N1', 'N2', 'B1', 'B2') if k in ARMS]
FINITE = lambda x: x == x and abs(x) != float('inf')
med = lambda xs: statistics.median(xs) if xs else float('nan')
def fmt(xs, p=3):
    xs = [x for x in xs if FINITE(x)]
    if not xs: return '-'
    return f'{med(xs):.{p}f}[{min(xs):.{p}f}~{max(xs):.{p}f}]'

def load_nation(D):
    """seed -> nation -> quarter -> 行"""
    out = {}
    for sd, r in rows(D, 'nation'):
        out.setdefault(sd, {}).setdefault(r['nation'], {})[int(r['quarter'])] = r
    return out
def load_fiscal(D):
    """seed -> nation -> [行を monthIndex 昇順]"""
    out = {}
    for sd, r in rows(D, 'fiscal'):
        out.setdefault(sd, {}).setdefault(r['nation'], []).append(r)
    for sd in out:
        for n in out[sd]: out[sd][n].sort(key=lambda r: int(r['monthIndex']))
    return out

DATA = {}
for k in ORDER:
    DATA[k] = (load_nation(ARMS[k]), load_fiscal(ARMS[k]))
    print(f'腕 {k}: {ARMS[k]}  nation_s の種 {sorted(DATA[k][0])}  fiscal_s の種 {sorted(DATA[k][1])}')
print()

def per_seed(arm, fn):
    N, F = DATA[arm]
    return [fn(sd, N[sd], F.get(sd, {})) for sd in sorted(N)]

def win(nrows, q0, q1):
    return [nrows[q] for q in range(q0, q1 + 1) if q in nrows]

# ------------------------------------------------------------------ M1 M2 M3
print('== M1 開幕の段（nation_s・Q0 の monopolyTierBeforeDecisionStage）')
for k in ORDER:
    for nat in (UK, NL):
        v = per_seed(k, lambda sd, N, F, nat=nat: num(N[nat][0]['monopolyTierBeforeDecisionStage']) if nat in N and 0 in N[nat] else float('nan'))
        c = collections.Counter(int(x) for x in v if FINITE(x))
        print(f'  {k}\t{nat}\t{dict(sorted(c.items()))}\t（B/B2 以外で 3 は 10/10 でない・B/B1 は 10/10 が合格）')
print('== M2 国庫（fiscal_s の最初の行の treasuryAtClaimStock ÷ revenueBasePerMonth）')
for k in ORDER:
    for nat in (UK, NL):
        v = per_seed(k, lambda sd, N, F, nat=nat: (num(F[nat][0]['treasuryAtClaimStock']) / num(F[nat][0]['revenueBasePerMonth'])) if nat in F and F[nat] else float('nan'))
        print(f'  {k}\t{nat}\t{fmt(v, 3)}\t2.5±10% の種 {sum(1 for x in v if FINITE(x) and abs(x - 2.5) <= 0.25)}/{len(v)}')
print('== M3 門（英の Q0 の stateFacilityStartedCount・種ごと）')
m3 = {}
for k in ORDER:
    v = per_seed(k, lambda sd, N, F: num(N[UK][0]['stateFacilityStartedCount']) if UK in N and 0 in N[UK] else float('nan'))
    m3[k] = v
    print(f'  {k}\t{[int(x) if FINITE(x) else None for x in v]}\t中央値 {med([x for x in v if FINITE(x)])}')
if 'A' in m3:
    for k in ORDER:
        if k in ('A',) or k.startswith('N'): continue
        lt = sum(1 for a, b in zip(m3['A'], m3[k]) if FINITE(a) and FINITE(b) and b < a)
        ok = lt >= 5 and med([x for x in m3[k] if FINITE(x)]) < med([x for x in m3['A'] if FINITE(x)])
        print(f'  判定 {k}: A より小さい種 {lt}/10・中央値 {"小さい" if med([x for x in m3[k] if FINITE(x)]) < med([x for x in m3["A"] if FINITE(x)]) else "小さくない"} → {"合格" if ok else "⚠不合格＝門が働いていない合図（報告の先頭に書く・調整しない）"}（B2 は門だけ・B1 は段だけ＝B1 は A と同じが正しい）')

# ------------------------------------------------------------------ W1..W4 と R1..R4
def int_rev_w1(arm, nat):
    def f(sd, N, F):
        fr = F.get(nat, [])[:3]
        if len(fr) < 3: return float('nan')
        return sum(num(r['claimInterestPerMonth']) for r in fr) / sum(num(r['revenueBasePerMonth']) for r in fr)
    return per_seed(arm, f)
def int_rev_q(arm, nat, q0, q1):
    def f(sd, N, F):
        rs = win(N.get(nat, {}), q0, q1)
        s = sum(num(r['revenueBaseMeanPerMonth']) for r in rs)
        return sum(num(r['interestMeanPerMonth']) for r in rs) / s if rs and s > 0 else float('nan')
    return per_seed(arm, f)
def rev_gdp_q(arm, nat, q0, q1):
    def f(sd, N, F):
        rs = [r for r in win(N.get(nat, {}), q0, q1) if num(r['nominalGdpPerYear']) > 0]
        return statistics.mean(12 * num(r['revenueBaseMeanPerMonth']) / num(r['nominalGdpPerYear']) for r in rs) if rs else float('nan')
    return per_seed(arm, f)
def pay_below1(arm, nat):
    return per_seed(arm, lambda sd, N, F: sum(num(r['payRateBelowOneMonths']) for r in win(N.get(nat, {}), 0, 8)))
WINS = {'W2': (0, 8), 'W3': (5, 40), 'W4': (1, 40)}
print('\n== R1/P1 利払い÷歳入・歳入÷年名目GDP・払い率 r<1 の月数（種の中央値[最小~最大]）')
for nat in (UK, NL):
    print(f'-- {nat}')
    for k in ORDER:
        s = f'  {k}\tW1 利払い÷歳入 {fmt(int_rev_w1(k, nat))}'
        for w, (a, b) in WINS.items():
            s += f'\t{w} {fmt(int_rev_q(k, nat, a, b))}'
        s += f'\t歳入÷GDP W2 {fmt(rev_gdp_q(k, nat, 0, 8))} W3 {fmt(rev_gdp_q(k, nat, 5, 40))}\tr<1 の月数(Q0-8) {fmt(pay_below1(k, nat), 1)}'
        print(s)
print('  （P1：B の英の W1 は 0.55〜0.75・A は 6/10 種が 1.1 超／蘭は 0.4〜0.8。手元の幅 歳入÷GDP 約 6〜12%・2〜4 倍は記録で NG ではない）')
for nat in (UK, NL):
    for k in ORDER:
        v = int_rev_w1(k, nat)
        print(f'  P1 {nat} {k}: W1 の種ごと {[round(x, 2) if FINITE(x) else None for x in v]}')

print('\n== R2/P2 兌換を離れる最初の月と戻る月・不履行（fiscal_s の onGold・defaultCount）')
def gold_events(F, nat):
    fr = F.get(nat, [])
    leave = back = None; dflt = 0
    prev = None
    for r in fr:
        g = num(r['onGold']); m = int(r['monthIndex'])
        if prev is not None and prev > 0.5 and g < 0.5 and leave is None: leave = m
        if leave is not None and back is None and g > 0.5 and m > leave: back = m
        prev = g; dflt = max(dflt, num(r['defaultCount']))
    return leave, back, dflt
for nat in (UK, NL):
    for k in ORDER:
        ev = per_seed(k, lambda sd, N, F, nat=nat: gold_events(F, nat))
        leaves = [e[0] for e in ev if e[0] is not None]; early = sum(1 for e in ev if e[0] is not None and e[0] <= 3)
        print(f'  {nat}\t{k}\t離れた種 {len(leaves)}/{len(ev)}・月3以前に離れた種 {early}・離れた月 {sorted(leaves)}・戻る月 {[e[1] for e in ev]}・不履行(最大) {[int(e[2]) for e in ev]}')

print('\n== R3 専売の段の軌跡 Q0〜Q8（Before→Target→End の種の中央値）と往復の回数（段が下がってまた上がる回数・Q0〜Q40）')
for nat in (UK, NL):
    for k in ORDER:
        def traj(sd, N, F, nat=nat):
            n = N.get(nat, {})
            return [int(num(n[q]['monopolyTierEndStage'])) for q in range(0, 41) if q in n]
        def swings(tr):
            c = 0
            for i in range(2, len(tr)):
                if tr[i - 1] < tr[i - 2] and tr[i] > tr[i - 1]: c += 1
            return c
        trs = per_seed(k, traj)
        mids = []
        for q in range(0, 9):
            vals = [t[q] for t in trs if len(t) > q]
            mids.append(f'{statistics.median(vals):.0f}' if vals else '-')
        sw = [swings(t) for t in trs]
        print(f'  {nat}\t{k}\tEnd の段 Q0〜Q8 の中央値 {" ".join(mids)}\t往復(Q0-40) 種ごと {sw}（P3：Q1 で 3→2 に狭まる種）')

print('\n== R4 1783〜86 の国営工場の着工数・工事の出費÷歳入（英・蘭）')
for nat in (UK, NL):
    for k in ORDER:
        st = per_seed(k, lambda sd, N, F, nat=nat: sum(num(r['stateFacilityStartedCount']) for r in win(N.get(nat, {}), 0, 15)))
        def ratio(sd, N, F, nat=nat):
            fr = F.get(nat, [])[:48]
            s = sum(num(r['revenueBasePerMonth']) for r in fr)
            return sum(num(r['claimLastMonthFactoryBuildPerMonth']) for r in fr) / s if s > 0 else float('nan')
        rr = per_seed(k, ratio)
        print(f'  {nat}\t{k}\t着工数(Q0-15) {fmt(st, 1)}\t工事の月の出費÷歳入(最初の 48 か月) {fmt(rr)}')

print('\n== R6 開幕の平均クーポン（fiscal_s の最初の行・govAvgCouponPerYear）')
for nat in (UK, NL):
    for k in ORDER:
        v = per_seed(k, lambda sd, N, F, nat=nat: num(F[nat][0]['govAvgCouponPerYear']) if F.get(nat) else float('nan'))
        print(f'  {nat}\t{k}\t{fmt(v, 4)}（額面利率は約 0.035・この線では直さない）')

# ------------------------------------------------------------------ 止める線
print('\n== 止める線（事前登録 [6]・ヌル腕 N1・N2 の幅で見る）')
NULLS = [k for k in ('N1', 'N2') if k in ORDER]
def nullmax(fn):
    vs = [fn(k) for k in NULLS]
    return max(vs) if vs else float('nan')
# 発散①：英・蘭の国債÷年GDP の最大
def debt_gdp_max(arm, nat):
    def f(sd, N, F):
        v = [num(r['govDebtStock']) / num(r['nominalGdpPerYear']) for r in N.get(nat, {}).values() if num(r['nominalGdpPerYear']) > 0 and FINITE(num(r['govDebtStock']))]
        return max(v) if v else float('nan')
    return per_seed(arm, f)
for nat in (UK, NL):
    for k in ORDER:
        v = debt_gdp_max(k, nat)
        print(f'  発散 国債÷年GDP の最大 {nat}\t{k}\t{fmt(v, 2)}\t5 を超えた種 {sum(1 for x in v if FINITE(x) and x > 5)}/{len(v)}')
# 発散②：金利の天井（interestAtLimitMonthsCount の全国×四半期の和）
def at_limit(arm):
    return sum(sum(num(r['interestAtLimitMonthsCount']) for n in N.values() for r in n.values() if FINITE(num(r['interestAtLimitMonthsCount']))) for N in DATA[arm][0].values())
al = {k: at_limit(k) for k in ORDER}
nm = nullmax(lambda k: al[k]) if NULLS else float('nan')
for k in ORDER:
    print(f'  発散 金利の天井の張り付き月（全国×全期の和）\t{k}\t{al[k]:.0f}\tヌル腕の最大 {nm:.0f}\t{"⚠止める線（2 倍以上）" if FINITE(nm) and nm > 0 and al[k] >= 2 * nm and k not in NULLS and k != "A" else ""}')
# ①同じ国が毎回壊れる
def broken(arm):
    c = collections.Counter()
    for sd, N in DATA[arm][0].items():
        for nat, qs in N.items():
            pop = [num(r['totalPopulationK']) for _, r in sorted(qs.items())]; core = [num(r['coreRegions']) for _, r in sorted(qs.items())]
            pop = [x for x in pop if FINITE(x)]
            if pop and max(pop) > 0 and pop[-1] < 0.5 * max(pop): c[nat] += 1
            elif core and max(core) > 0 and core[-1] < 0.5 * max(core): c[nat] += 1
    return c
BR = {k: broken(k) for k in ORDER}
print('  ① 人口か領土の半分以上を失う（最後の行がピークの 1/2 未満）：種の数（腕ごとに 5 種以上の国だけ）')
for k in ORDER:
    top = [(n, c) for n, c in BR[k].most_common() if c >= 5]
    print(f'    {k}\t{top}')
for k in ORDER:
    if k in NULLS or k == 'A': continue
    flagged = [n for n, c in BR[k].items() if c >= 8 and all(BR[nk].get(n, 0) <= 2 for nk in NULLS)]
    print(f'  ① 判定 {k}: 8/10 以上で壊れ、ヌル腕は 2/10 以下の国 {flagged or "なし"}')
# ②一国の発散：首位の国の世界の実質GDP の取り分（Q524＝1914 と 最後）
def top_share(arm, q):
    out = []
    for sd, N in sorted(DATA[arm][0].items()):
        vals = {nat: num(qs[q]['realGdpPerYear']) for nat, qs in N.items() if q in qs and FINITE(num(qs[q]['realGdpPerYear']))}
        tot = sum(vals.values())
        if tot > 0:
            n, v = max(vals.items(), key=lambda kv: kv[1]); out.append((n, v / tot))
    return out
for q in (524, 667):
    ts = {k: top_share(k, q) for k in ORDER}
    nvals = [s for k in NULLS for _, s in ts[k]]
    nmax = max(nvals) if nvals else float('nan'); nrng = (max(nvals) - min(nvals)) if nvals else float('nan')
    for k in ORDER:
        names = collections.Counter(n for n, _ in ts[k]); shares = [s for _, s in ts[k]]
        tag = ''
        if k not in NULLS and k != 'A' and nvals:
            same = names.most_common(1)[0][1] if names else 0
            if med(shares) > nmax + 2 * nrng and same >= 8: tag = '⚠止める線②（首位の取り分がヌルの最大を幅の 2 倍超え・8/10 以上の種で同じ国）'
        print(f'  ② Q{q} 首位の国の世界の実質GDP の取り分\t{k}\t{fmt(shares, 3)}\t首位の国 {dict(names.most_common(3))}\t{tag}')
    if nvals: print(f'     ヌル腕の最大 {nmax:.3f}・幅 {nrng:.3f}')
# ③片方向の錠：最大の段 5 に張り付く国×四半期（monopolyTierEndStage==5）
def at_max_tier(arm):
    return sum(1 for N in DATA[arm][0].values() for n in N.values() for r in n.values() if num(r['monopolyTierEndStage']) >= 5)
mt = {k: at_max_tier(k) for k in ORDER}
nm3 = nullmax(lambda k: mt[k]) if NULLS else float('nan')
for k in ORDER:
    print(f'  ③ 最大の段（5）に張り付く国×四半期\t{k}\t{mt[k]}\tヌル腕の最大 {nm3}\t{"⚠止める線③（2 倍以上）" if k not in NULLS and k != "A" and FINITE(nm3) and mt[k] >= 2 * max(nm3, 1) else ""}')

# ------------------------------------------------------------------ 追記2 ① の記録 L1 L2
print('\n== 追記2 ① 記録 L1（全国・着工を続けていた国の 8 四半期以上の着工 0 の連なり）')
def l1(arm):
    runs = 0; nations_seeds = 0; qsum = 0
    for sd, N in DATA[arm][0].items():
        for nat, qs in N.items():
            st = [num(qs[q]['stateFacilityStartedCount']) for q in sorted(qs)]
            if sum(st) <= 0: continue
            nations_seeds += 1
            run = 0; started_before = False
            for x in st:
                if x > 0: started_before = True; run = 0
                elif started_before:
                    run += 1
                    if run == 8: runs += 1
                    if run >= 8: qsum += 1
    return runs, qsum, nations_seeds
L = {k: l1(k) for k in ORDER}
nmx = nullmax(lambda k: L[k][0]) if NULLS else float('nan')
for k in ORDER:
    print(f'  L1\t{k}\t連なり {L[k][0]}・その四半期の和 {L[k][1]}・着工のあった国×種 {L[k][2]}\tヌル腕の最大の連なり {nmx}\t{"⚠止める線③として扱う（2 倍以上）" if k not in NULLS and k != "A" and FINITE(nmx) and L[k][0] >= 2 * max(nmx, 1) else ""}')
def total_starts(arm, q0, q1):
    return per_seed(arm, lambda sd, N, F: sum(num(r['stateFacilityStartedCount']) for n in N.values() for r in win(n, q0, q1)))
for k in ORDER:
    print(f'  全国の着工数 Q0-8 {fmt(total_starts(k, 0, 8), 0)}  Q5-40 {fmt(total_starts(k, 5, 40), 0)}  全期 {fmt(total_starts(k, 0, 667), 0)}')
print('\n== 追記2 ③ 記録 L2（名簿の国の工事の月の出費÷上限の近似・W2＝最初の 27 か月/W3＝その後 Q5〜Q40 相当 = 月 15〜120）')
def gate_bind(F, nat, m0, m1):
    fr = [r for r in F.get(nat, []) if m0 <= int(r['monthIndex']) <= m1]
    if not fr: return None
    over = 0
    for r in fr:
        buf = max(1.0, BUF * (num(r['claimInterestPerMonth']) + num(r['claimAdminPerMonth']) + num(r['claimArmyPerMonth']) + num(r['claimNavyPerMonth'])))
        if num(r['claimLastMonthFactoryBuildPerMonth']) > 0.10 * buf: over += 1
    return over
for k in ORDER:
    N, F = DATA[k]
    names = sorted({nat for sd in F for nat in F[sd]})
    for w, (m0, m1) in (('W2', (2, 27)), ('W3', (15, 120))):
        tot = {}
        for nat in names:
            vs = [gate_bind(F[sd], nat, m0, m1) for sd in F if nat in F[sd]]
            vs = [x for x in vs if x is not None]
            tot[nat] = sum(vs)
        print(f'  L2 {k} {w}\t工賃の月の出費が上限（近似）をこえた国×月の数：' + '・'.join(f'{n} {c}' for n, c in tot.items()))
print('  （名簿の外の国には門の効果を測れない＝nation_s の着工数と L1 だけ。pendingWill は計器に無い）')
