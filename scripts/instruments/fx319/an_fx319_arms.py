"""(319) 流れF 財政の鎖の腕 D の読み手（読むだけ・挙動は変えない）。事前登録 measurements/fx_20261014_prereg.txt の [4][5]。
  python -X utf8 an_fx319_arms.py <出力.md> A=<ins> D=<ins> [SEEDS=<n>＝種を先頭の n 個に絞る（10種の走行から5種の読みを取り出す）] [REF=<ins>＝置き直しだけの腕など・助走の終わりの比較の参照]
入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins
      nation_s（全ての国×四半期）・fiscal_s（英仏蘭葡の月ごと）・class_s（階級）。A＝基線 r314・D＝腕 D（採否の腕）。腕の名前は何でもよい（最初が基線・2番目が判定される腕）。
出す物（英・種の中央〔最小〜最大〕・窓は事前登録 [4][9] の表）：
  線：M1（専売の段が Q2 の末までに上がった種・除外規則つき）・F1 兌換停止のある種（Q40）・F2 不履行のある種（Q40）・F3 払えない月（Q1〜Q8）・F4 歳入÷年GDP（Q5〜Q40）・F6 大衆の Paid÷Earned・F7 往復（Q1〜Q40・種の和）・F8 段5 の四半期数（Q5〜Q40・種の中央）
  記録（線ではない）：兌換停止・不履行の種の数の全ての窓／払えない月と満額借りても足りない月（Q1〜Q40）／F5 Q40 末の債務÷年GDP／利払い÷歳入／段5 の四半期数（Q1〜Q40・全期）・F7 の全期／歳入÷年GDP の最大の種／
  英の財の実質（A の価格で重みづけた売れた量の指数）と価格の指数（Q5〜Q40・種を対にして）／助走の終わり（class_s の warm の行）が A と違う国の数／世界で重さ＞0 かつ r＝1 の国×四半期／世界の専売の段の往復と段5
限界の自己申告（計器が一番バグる）：
  ・兌換停止＝fiscal_s の onGold が 0.5 以上から 0.5 未満へ（l4w.py／an_o1.py と同じ定義）・不履行＝defaultCount＞0。四半期の番号は monthIndex 0,1→Q1・以後 3 か月ずつ。
  ・F3 の月＝payRateShare＜1 の月（monthIndex＜24）。F4／F5／F6 は nation_s・class_s の四半期の行（月平均の列）。種ごとの値を出して中央（窓の中央→種の中央の二重の中央を使わない）。
  ・F7 の往復＝nation_s の monopolyTierEndStage が前の四半期より上がり、次の四半期に下がった件数（英・Q1〜Q40・種の和）。
  ・重さの露出は nation_s の四半期の月平均（利払い／歳入）で数える＝決断の時点の月初の値ではない（見積もり）。
  ・M1 の除外規則（事前登録 [8]①）：種が数に入る＝Q2 の末の段≧3 かつ（Q1 の末の段≧3 または monthIndex 0〜1 に payRateShare＜1 の月が無い）＝先に払えない月があった種で Q2 に上がった分は重さの効きとして数えない（分母は種の全数のまま）。
  ・F8＝nation_s の monopolyTierEndStage が 5（最大）の四半期の数（Q5〜Q40＝q4〜q39 の 36 四半期）。実質と価格の指数は pxgoods_s（英・Q5〜Q40・計器が選んだ財）の lastSoldQty と priceMean を A の同じ種の同じ四半期・同じ財の価格・量で重みづけた指数（A と D の対）＝財の組成の差を A の側へ固定する。
  ・助走の終わり＝class_s の time＝warm の行（階級の人数と貯蓄）が A と D で一致しない国の数（置き直しは warm の後）。"""
import sys, os, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows as irows, seeds as iseeds, num

OUT = sys.argv[1]
ARMS = collections.OrderedDict(); SEEDCAP = 0; REFP = None
for a in sys.argv[2:]:
    k, v = a.split('=', 1)
    if k == 'SEEDS': SEEDCAP = int(v)
    elif k == 'REF': REFP = v
    else: ARMS[k] = v
UK = 'イギリス'
W = [8, 40, 80, 160, 240, 10 ** 6]; WN = ['Q8', 'Q40', 'Q80', 'Q160', 'Q240', '全期']
MASS = ('農民', '工場労働者', '職人')
L = 0.67


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def cell(xs, nd=3):
    xs = [x for x in xs if x == x]
    return '—' if not xs else f'{statistics.median(xs):.{nd}f} ({min(xs):.{nd}f}〜{max(xs):.{nd}f})'


def qof(m):
    return 0 if m < 2 else 1 + (m - 2) // 3


def load(D):
    sds = sorted(set(iseeds(D, 'nation')) & set(iseeds(D, 'fiscal')) & set(iseeds(D, 'class')))
    if SEEDCAP: sds = [s for s in sds if s <= SEEDCAP]
    R = dict(seeds=sds, nat=collections.defaultdict(dict), fis=collections.defaultdict(list), cls=collections.defaultdict(lambda: collections.defaultdict(dict)),
             expo=collections.Counter(), tw=collections.defaultdict(list), warm=collections.defaultdict(lambda: collections.defaultdict(list)), gd=collections.defaultdict(dict))
    for sd, r in irows(D, 'nation'):
        if sd not in sds: continue
        q = int(r['quarter'])
        tv = num(r.get('monopolyTierEndStage', 'nan'))
        if tv == tv: R['tw'][(sd, r.get('nationUid', r['nation']))].append((q, tv))
        if r['nation'] == UK: R['nat'][sd][q] = r
        rev, it = num(r['revenueBaseMeanPerMonth']), num(r['interestMeanPerMonth'])
        if rev == rev and it == it:
            R['expo']['den'] += 1
            if it - L * rev > 0 and not num(r['payRateBelowOneMonths']) > 0: R['expo']['burden_only'] += 1
            if it - L * rev > 0: R['expo']['burden'] += 1
    for sd, r in irows(D, 'fiscal'):
        if sd in sds and r['nation'] == UK: R['fis'][sd].append(r)
    for sd, r in irows(D, 'pxgoods'):
        if sd in sds and r['nation'] == UK and r['time'].startswith('q'):
            q = int(r['time'][1:])
            if 4 <= q < 40: R['gd'][sd][(q, r['goodName'])] = (num(r['lastSoldQty']), num(r['priceMean']))
    for sd, r in irows(D, 'class'):
        if sd in sds and r['time'] == 'warm': R['warm'][sd][r['nation']].append((r['classId'], r['sizeK'], r['savingsStock']))
        if sd in sds and r['nation'] == UK and r['time'].startswith('q') and r['className'] in MASS:
            R['cls'][sd].setdefault(int(r['time'][1:]), []).append((num(r['earnedPerMonth']), num(r['paidPerMonth'])))
    for sd in sds: R['fis'][sd].sort(key=lambda r: int(r['monthIndex']))
    return R


def susp_def(R):
    out = {w: [0, 0, 0] for w in W}   # 不履行のある種, 停止のある種, 停止の総回数
    for sd in R['seeds']:
        prev = None; dq = {w: 0 for w in W}; sq = {w: 0 for w in W}
        for r in R['fis'][sd]:
            q = qof(int(r['monthIndex'])); g = num(r['onGold']); dcv = num(r['defaultCount'])
            for w in W:
                if q < w:
                    if dcv > 0: dq[w] = 1
                    if prev is not None and prev >= .5 and g < .5: sq[w] += 1
            prev = g
        for w in W:
            out[w][0] += dq[w]; out[w][1] += 1 if sq[w] > 0 else 0; out[w][2] += sq[w]
    return out


def short_months(R, mmax):
    """(払えない月＝payRateShare<1, 払えない額のある月で binder=full) の種ごとの値"""
    a, b = [], []
    for sd in R['seeds']:
        x = y = 0
        for r in R['fis'][sd]:
            if int(r['monthIndex']) >= mmax: continue
            if num(r['payRateShare']) < 1.0: x += 1
            if num(r['borrowShortfallPerMonth']) > 0 and r['borrowBinder'] == 'full': y += 1
        a.append(x); b.append(y)
    return a, b


def per_seed(R, fn):
    return [fn(R['nat'][sd]) for sd in R['seeds']]


def rev_gdp(nat):
    xs = [num(nat[q]['revenueBaseMeanPerMonth']) * 12 / num(nat[q]['nominalGdpPerYear']) for q in range(4, 40) if q in nat]
    return statistics.mean(xs) if xs else float('nan')


def int_rev(nat):
    i = sum(num(nat[q]['interestMeanPerMonth']) for q in range(4, 40) if q in nat); v = sum(num(nat[q]['revenueBaseMeanPerMonth']) for q in range(4, 40) if q in nat)
    return i / v if v > 0 else float('nan')


def debt_gdp(nat):
    r = nat.get(39)
    return num(r['govDebtStock']) / num(r['nominalGdpPerYear']) if r else float('nan')


def mass_pe(R, sd):
    xs = []
    for q in range(4, 40):
        c = R['cls'][sd].get(q)
        if c and len(c) == len(MASS):
            e = sum(x[0] for x in c); p = sum(x[1] for x in c)
            if e > 0: xs.append(p / e)
    return statistics.mean(xs) if xs else float('nan')


def tier_stats(R):
    mx, rev, path = [], 0, []
    for sd in R['seeds']:
        t = [num(R['nat'][sd][q]['monopolyTierEndStage']) for q in range(40) if q in R['nat'][sd]]
        mx.append(max(t) if t else float('nan'))
        for i in range(2, len(t)):
            if t[i - 1] > t[i - 2] and t[i] < t[i - 1]: rev += 1
        path.append(' '.join(f'{x:.0f}' for x in t[:6]))
    return mx, rev, path


def tier_series(R, sd, lo=0, hi=40):
    return [num(R['nat'][sd][q]['monopolyTierEndStage']) for q in range(lo, hi) if q in R['nat'][sd]]


def m1_seed(R, sd):
    """(数に入る, 除外された) ＝ Q2 の末の段≧3 のうち、重さだけで上がったと読める種"""
    n = R['nat'][sd]
    if 0 not in n or 1 not in n: return (False, False)
    t1, t2 = num(n[0]['monopolyTierEndStage']), num(n[1]['monopolyTierEndStage'])
    if not t2 >= 3: return (False, False)
    early = any(num(r['payRateShare']) < 1.0 for r in R['fis'][sd] if int(r['monthIndex']) < 2)
    if t1 >= 3 or not early: return (True, False)
    return (False, True)


def tier5_q(R, sd, lo, hi):
    return sum(1 for t in tier_series(R, sd, lo, hi) if t >= 5)


def rev7(R, sd, hi=40):
    t = tier_series(R, sd, 0, hi); k = 0
    for i in range(2, len(t)):
        if t[i - 1] > t[i - 2] and t[i] < t[i - 1]: k += 1
    return k


def real_price_index(RA, RD):
    """英 Q5〜Q40・A の同じ種の同じ四半期の価格・量を重みにした量の指数（Laspeyres）と価格の指数（Laspeyres の価格版）の、種ごとの窓の平均。"""
    qs, ps = [], []
    for sd in RD['seeds']:
        if sd not in RA['gd']: continue
        qq, pp = [], []
        for q in range(4, 40):
            num_q = den_q = num_p = 0.0
            for (q_, g), (soldA, pxA) in RA['gd'][sd].items():
                if q_ != q: continue
                dd = RD['gd'][sd].get((q, g))
                if dd is None or not (soldA == soldA and pxA == pxA and dd[0] == dd[0] and dd[1] == dd[1]): continue
                num_q += dd[0] * pxA; den_q += soldA * pxA; num_p += soldA * dd[1]
            if den_q > 0: qq.append(num_q / den_q); pp.append(num_p / den_q)
        if qq: qs.append(statistics.mean(qq)); ps.append(statistics.mean(pp))
    return qs, ps


def warm_diff(RA, RD):
    ns = set(); tot = set()
    for sd in RD['seeds']:
        for nat, rows_ in RD['warm'][sd].items():
            tot.add((sd, nat))
            if RA['warm'][sd].get(nat) != rows_: ns.add((sd, nat))
    uk = sum(1 for (sd, nat) in ns if nat == UK)
    return len(ns), len(tot), uk


D = {a: load(p) for a, p in ARMS.items()}
names = list(ARMS)
lines = [f'# (319) 腕 D の読み（英・種 {D[names[0]]["seeds"]}・種の中央〔最小〜最大〕）\n']
for a in names:
    R = D[a]
    sd_ = susp_def(R)
    lines.append(f'\n## {a}\n')
    lines.append('| 窓 | ' + ' | '.join(WN) + ' |\n|---|' + '---|' * len(WN))
    lines.append('| F1 兌換停止のある種 | ' + ' | '.join(f'{sd_[w][1]}/{len(R["seeds"])}' for w in W) + ' |')
    lines.append('| 　総回数 | ' + ' | '.join(str(sd_[w][2]) for w in W) + ' |')
    lines.append('| F2 不履行のある種 | ' + ' | '.join(f'{sd_[w][0]}/{len(R["seeds"])}' for w in W) + ' |')
    sm, fm = short_months(R, 24)
    sm40, fm40 = short_months(R, 120)
    lines.append(f'\n- F3 英の払えない月（monthIndex＜24）：種の中央 {f(med(sm), 1)}・最大 {max(sm)}・月の和 {sum(sm)}（種ごと {sm}）')
    lines.append(f'- 記録：英の払えない月（Q1〜Q40）の和 {sum(sm40)}・払えない額のある月で binder＝full（満額借りても足りない）{sum(fm40)}')
    lines.append(f'- F4 英の歳入÷年GDP（Q5〜Q40・種ごとの窓の平均）：{cell(per_seed(R, rev_gdp), 4)}')
    lines.append(f'- 記録：英の利払い÷歳入（Q5〜Q40・窓の和比）：{cell(per_seed(R, int_rev))}')
    lines.append(f'- F5 英の Q40 末の債務÷年GDP：{cell(per_seed(R, debt_gdp))}')
    lines.append(f'- F6 英の大衆の Paid÷Earned（Q5〜Q40・四半期の平均の窓の平均）：{cell([mass_pe(R, sd) for sd in R["seeds"]])}')
    mx, rv, path = tier_stats(R)
    lines.append(f'- F7 英の専売の段の最大（Q1〜Q40）：{cell(mx, 1)}・往復（上げた次の四半期に下げた件数・種の和）{rv}・Q1〜Q6 の道筋（種ごと）：' + '；'.join(path))
    t5a = [tier5_q(R, sd, 4, 40) for sd in R['seeds']]; t5b = [tier5_q(R, sd, 0, 40) for sd in R['seeds']]; t5c = [tier5_q(R, sd, 0, 10 ** 6) for sd in R['seeds']]
    r7all = [rev7(R, sd, 10 ** 6) for sd in R['seeds']]
    lines.append(f'- F8 英の段5 の四半期数（Q5〜Q40・36 四半期中）：{cell(t5a, 1)}（種ごと {t5a}）／記録：Q1〜Q40 {cell(t5b, 1)}・全期 {cell(t5c, 1)}・往復の全期（種の和）{sum(r7all)}（種ごと {r7all}）')
    m1s = [m1_seed(R, sd) for sd in R['seeds']]
    lines.append(f'- M1 英の専売の段が Q2 の末までに上がった種（除外規則つき）：{sum(1 for a_, b_ in m1s if a_)}/{len(R["seeds"])}（除外 {sum(1 for a_, b_ in m1s if b_)}）・Q2 末の段（種ごと）{[num(R["nat"][sd][1]["monopolyTierEndStage"]) for sd in R["seeds"] if 1 in R["nat"][sd]]}')
    rg = per_seed(R, rev_gdp)
    lines.append(f'- 記録：英の歳入÷年GDP（Q5〜Q40）の最大の種 {f(max(x for x in rg if x == x), 4)}（種ごと {[round(x, 4) for x in rg]}）')
    ex = R['expo']
    lines.append(f'- 記録：世界で重さ＞0 の国×四半期 {ex["burden"]}／{ex["den"]}（うち r＝1 のまま〔AI の新しい逼迫〕{ex["burden_only"]}）')
    wrev = wmax = wn = 0
    for key, ser in R['tw'].items():
        ser.sort()
        for i in range(2, len(ser)):
            if ser[i - 1][1] > ser[i - 2][1] and ser[i][1] < ser[i - 1][1]: wrev += 1
        for _, tv in ser:
            wn += 1
            if tv >= 5: wmax += 1
    lines.append(f'- 記録：世界の専売の段の往復（上げた次の四半期に下げた件数・全国・種の和）{wrev}・段が最大（5）の国×四半期 {wmax}／{wn}')
if len(names) == 2:
    a, d = names
    lines.append(f'\n## 判定（{d} を {a} に対して読む＝事前登録 [4][9]）\n')
    sa, sd2 = susp_def(D[a]), susp_def(D[d])
    n = len(D[d]['seeds'])
    f1 = sd2[40][1]; f2 = sd2[40][0]
    sm_d, _ = short_months(D[d], 24)
    m1 = [m1_seed(D[d], sd) for sd in D[d]['seeds']]; m1n = sum(1 for x, y in m1 if x)
    m1th = 0.8 * n
    lines.append(f'- M1 英の専売の段が Q2 の末までに上がった種（除外規則つき）：{d} {m1n}/{n}（除外 {sum(1 for x, y in m1 if y)}・線 ≧8/10）→ {"合格" if m1n >= m1th else "外れ＝機構が働いていない・他の線を採否に使わない"}')
    lines.append(f'- F1 兌換停止のある種（Q40）：{a} {sa[40][1]}／{d} {f1}（線 ≦3/{n}）→ {"合格" if f1 <= 3 else "外れ"}')
    lines.append(f'- F2 不履行のある種（Q40）：{a} {sa[40][0]}／{d} {f2}（線 0）→ {"合格" if f2 == 0 else "外れ"}')
    lines.append(f'- F3 英の払えない月（Q1〜Q8）：{d} 種の中央 {f(med(sm_d), 1)}・最大 {max(sm_d)}（線 中央 ≦1 かつ最大 ≦6）→ {"合格" if med(sm_d) <= 1 and max(sm_d) <= 6 else "外れ"}')
    ra, rd = med(per_seed(D[a], rev_gdp)), med(per_seed(D[d], rev_gdp))
    lines.append(f'- F4 英の歳入÷年GDP（Q5〜Q40）：{a} {f(ra, 4)}／{d} {f(rd, 4)}（線 ≧0.6×{a}＝{f(0.6 * ra, 4)} かつ ≧0.0851）→ {"合格" if rd >= 0.6 * ra and rd >= 0.0851 else "外れ"}')
    pd = med([mass_pe(D[d], sd) for sd in D[d]['seeds']])
    lines.append(f'- F6 英の大衆の Paid÷Earned（Q5〜Q40）：{d} {f(pd)}（線 ≧0.90）→ {"合格" if pd >= 0.90 else "外れ"}')
    r7 = sum(rev7(D[d], sd) for sd in D[d]['seeds'])
    lines.append(f'- F7 英の専売の段の往復（Q1〜Q40・種の和）：{a} {sum(rev7(D[a], sd) for sd in D[a]["seeds"])}／{d} {r7}（線 ≦10）→ {"合格" if r7 <= 10 else "外れ"}')
    t5 = med([tier5_q(D[d], sd, 4, 40) for sd in D[d]['seeds']])
    lines.append(f'- F8 英の段5 の四半期数（Q5〜Q40・種の中央）：{d} {f(t5, 1)}（線 ≦18＝36 四半期の半分）→ {"合格" if t5 <= 18 else "外れ＝張り付き"}')
    da, dd = med(per_seed(D[a], debt_gdp)), med(per_seed(D[d], debt_gdp))
    lines.append(f'- 記録 F5 英の Q40 末の債務÷年GDP：{a} {f(da)}／{d} {f(dd)}（線ではない）')
    qs, ps = real_price_index(D[a], D[d])
    lines.append(f'- 記録：英の財の実質の指数（{a} の価格で重みづけた売れた量・Q5〜Q40・{d}÷{a}）{cell(qs)}／価格の指数（{a} の量で重みづけた価格・{d}÷{a}）{cell(ps)}')
    wd, wt, wuk = warm_diff(D[a], D[d])
    lines.append(f'- 記録：助走の終わり（class_s の warm の行）が {a} と違う国×種 {wd}/{wt}（うち英 {wuk}）＝置き直しは warm の行の前に入るので {d} は置き直しだけでも違う（{a} との比較は参考）')
    if REFP:
        RR = load(REFP)
        wd2, wt2, wuk2 = warm_diff(RR, D[d])
        lines.append(f'- 記録：助走の終わり（warm の行）が REF（置き直しだけ・{REFP}）と違う国×種 {wd2}/{wt2}（うち英 {wuk2}）＝0 なら助走の中の AI の応答は効いていない（姿勢〔:503〕・選挙〔:748〕・福祉・教育・人頭税の目標の経路を含む）')
    allpass = m1n >= m1th and f1 <= 3 and f2 == 0 and med(sm_d) <= 1 and max(sm_d) <= 6 and rd >= 0.6 * ra and rd >= 0.0851 and pd >= 0.90 and r7 <= 10 and t5 <= 18
    lines.append(f'\n- 線の総括（止める線と計算量・フルスイートは別に読む）：{"M1・F1・F2・F3・F4・F6・F7・F8 が全て合格" if allpass else "外れた線あり"}')
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(lines) + '\n')
print('done', OUT)
