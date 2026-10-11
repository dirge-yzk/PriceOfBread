"""(265) 線I：設計の線を出すための計器の列の読み手（読むだけ）。
  python -X utf8 an_i265.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ> [自国=イギリス] [仏=フランス] [蘭=ネーデルラント] [残存国=ポルトガル]
  入力＝標準の計器（`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins）の
        nation_s（全ての国の四半期）・fiscal_s（挙げた国の月ごと）・goods_s（残存国の財ごとの需給）。
  出力＝<表の出力>/ に markdown（a_bond・a_cap・a_policy・b_admin_cf・c_band・d_goods）。
  各表の定義は先に書いた（数字を読む前）：
    a_bond      国債金利の内訳（Fiscal.Step の月の項の四半期平均・兌換割引ずみ）。蘭−英の金利の差のうち債務比の項の寄与＝(債務比の項の差)÷(金利の差)。
                債務比の項が差の8割以上なら〈作者の較正〉（BondRateDebtScale）＝腕は立てず §1-7 の候補（(264) §7-1 段1 の規則）。議会の割引は金利の式で全項に掛かる＝〈割引後〉の列は
                各項×議会の割引の係数（四半期平均の積＝近似）で出し直した物。
    a_cap       段5 の上限 d∈{1/120,1/240,1/480} ごとの拘束（大衆の Paid が min(S,E+S×d) を超える POP の Paid の割合・削られる額の割合・人口の割合）。
    a_policy    専売の段（今の段→AI が置く段・四半期の末の段・上げ下げの往復）・国営工場の売却・逼迫の指標・人頭税と福祉の目標と実際。
    b_admin_cf  A′（行政だけ・必要額で切る）の静的な反事実。余り X＝Σ州 max(0, 請求−必要額)（予算の時点・計器の列）。月ごとに
                  spare′＝spare＋X・払えない月′＝spare′＜0・借入′＝min(借入, max(0,−spare′))・払える額′＝払える額−(借入−借入′)・請求′＝請求−X・率′＝clamp(払える額′÷請求′)。切り下げの額は動かさない（上限の見積り）。
                  P_short＝1−払えない月′÷払えない月・P_borrow＝1−Σ借入′÷Σ借入・P_r＝1−(率′＜1の月)÷(率＜1の月)。fiscal_s（月ごと・英）の再計算が nation_s（計器の列）と合うことも確かめる。
    c_band      総動員の帯（上げる線：規模≧1.2 か 存亡＞0.5・下げる線：規模＜1/1.2 かつ 存亡が0・間は前の選択のまま・NaN は現状維持・交戦中でなければ 0）で数えた〈AI の選択〉N。
                  計器は新しい列を足さず nation_s の warScaleRatioAtDecision・homeLosingScoreAtDecision・atWarAtDecisionFlag から読み手が状態機械を回す（国は nationIndex で引く）。
                  N の上限＝交戦中かつ（規模＞0 または 存亡＞0）。入れ替わり＝交戦中が続く2四半期の間で選択が変わった回数。
    d_goods     残存国（活きた州≦2・人口≧100千人・供給の充足＜0.5 の四半期）の財ごとの需給。
  計器が自分で申告する限界：
    ・必要額・余りは月ごとの請求（r を掛ける前）と必要額（予算の時点の価格）。購買の時点の必要額との一致は番人が見る。反事実は静的（AI の応答・借入の門の再判定・切り下げの再計算は動かさない）。
    ・拘束は Paid＞上限（Paid は月の決算の終わりの値）。上限の予算（RemainingBudget）への当て方そのものではない。挙げた国だけ（NATIONS）。
    ・goods_s は条件に合う四半期だけ（残存国でも補給が足りている四半期は出ない）・市場の値は四半期の末の断面（直近の月）。
    ・窓は四半期の添字（Q1＝q0）。種は 10種の中央〔最小〜最大〕。"""
import sys, os, math, statistics, collections, csv, glob, re

D, OUT = sys.argv[1], sys.argv[2]
UK = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'
FR = sys.argv[4] if len(sys.argv) > 4 else 'フランス'
NL = sys.argv[5] if len(sys.argv) > 5 else 'ネーデルラント'
PT = sys.argv[6] if len(sys.argv) > 6 else 'ポルトガル'
os.makedirs(OUT, exist_ok=True)
NAN = float('nan')
BAND_UP_SCALE, BAND_UP_LOSING = 1.2, 0.5
STR_COLS = {'nation', 'goodName', 'tier', 'borrowBinder'}


def fl(x):
    try:
        return float(x)
    except Exception:
        return NAN


def seeds_of(kind):
    out = []
    for f in glob.glob(os.path.join(D, f'{kind}_s*.tsv')):
        m = re.match(rf'{kind}_s(\d+)\.tsv$', os.path.basename(f))
        if m:
            out.append(int(m.group(1)))
    return sorted(out)


def load(kind, cols, want=None):
    """{seed: [行の辞書(cols のみ・数は float・STR_COLS は文字列)]}。want(row)->bool で行を絞る。"""
    res = {}
    for sd in seeds_of(kind):
        rows = []
        with open(os.path.join(D, f'{kind}_s{sd}.tsv'), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t')
            idx = {c: head.index(c) for c in cols if c in head}
            miss = [c for c in cols if c not in head]
            if miss:
                raise SystemExit(f'{kind}_s{sd}: 列が無い {miss}')
            for ln in fh:
                p = ln.rstrip('\n').split('\t')
                if len(p) != len(head):
                    continue   # 書きかけの最後の行
                r = {c: (p[i] if c in STR_COLS else fl(p[i])) for c, i in idx.items()}
                if want is None or want(r):
                    rows.append(r)
        res[sd] = rows
    return res


def med(xs):
    xs = [x for x in xs if x == x and abs(x) != float('inf')]
    return statistics.median(xs) if xs else NAN


def cell(xs, nd=3):
    xs = [x for x in xs if x == x and abs(x) != float('inf')]
    if not xs:
        return '—'
    return f'{statistics.median(xs):.{nd}f} [{min(xs):.{nd}f}〜{max(xs):.{nd}f}]'


def mean(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else NAN


def write(name, lines):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8', newline='\n') as fh:
        fh.write('\n'.join(lines) + '\n')


def hdr(title, note):
    return [f'# {title}', '', note, '']


# ====================================================================== a_bond
def a_bond():
    cols = ['quarter', 'nation', 'bondMonthsCount', 'bondRateMeanPerYear', 'bondBaseTermPerYear', 'bondDebtTermPerYear', 'bondInflationExpectationTermPerYear', 'bondDistrustTermPerYear',
            'bondPolicyRateTermPerYear', 'bondGoldDiscountMemoPerYear', 'bondRepresentationTermPerYear', 'bondClampTermPerYear', 'bondDebtToGdpYearMeanRatio',
            'bondConvertibilityDiscountMeanFactor', 'bondRepresentationDiscountMeanFactor', 'govDebtStock', 'nominalGdpPerYear', 'revenueBaseMeanPerMonth', 'interestMeanPerMonth']
    R = load('nation', cols, lambda r: r['nation'] in (UK, FR, NL) and r['bondMonthsCount'] > 0)
    terms = [('金利（月の平均）', 'bondRateMeanPerYear'), ('基礎', 'bondBaseTermPerYear'), ('債務比', 'bondDebtTermPerYear'), ('期待インフレ', 'bondInflationExpectationTermPerYear'),
             ('信認の喪失', 'bondDistrustTermPerYear'), ('政策金利', 'bondPolicyRateTermPerYear'), ('議会の割引', 'bondRepresentationTermPerYear'), ('上限クランプ', 'bondClampTermPerYear'),
             ('（メモ）金本位の割引', 'bondGoldDiscountMemoPerYear'), ('債務÷年GDP', 'bondDebtToGdpYearMeanRatio'), ('兌換割引の係数', 'bondConvertibilityDiscountMeanFactor'),
             ('議会の割引の係数', 'bondRepresentationDiscountMeanFactor')]
    wins = [('Q1〜Q8', 0, 7), ('Q1〜Q40', 0, 39), ('Q41〜Q80', 40, 79)]
    L = hdr('(265) a. 国債金利の内訳（英・仏・蘭）', '10種の中央〔最小〜最大〕。項は Fiscal.Step の月ごとの項（兌換割引ずみ）の四半期平均。閉じ方＝基礎＋債務比＋期待インフレ＋信認の喪失＋政策金利＋議会の割引＋上限クランプ＝金利。')

    def winmean(sd, nat, col, a, b):
        return mean([r[col] for r in R[sd] if r['nation'] == nat and a <= r['quarter'] <= b])

    sds = sorted(R)
    for wn, a, b in wins:
        L += [f'## {wn}', '', '| 項 | 英 | 仏 | 蘭 | 蘭−英 |', '|---|---|---|---|---|']
        for lab, col in terms:
            v = {n: [winmean(sd, n, col, a, b) for sd in sds] for n in (UK, FR, NL)}
            gap = [x - y for x, y in zip(v[NL], v[UK])]
            nd = 4 if 'Ratio' in col or 'Factor' in col else 4
            L.append(f'| {lab} | {cell(v[UK], nd)} | {cell(v[FR], nd)} | {cell(v[NL], nd)} | {cell(gap, nd)} |')
        # 蘭−英の金利の差のうち債務比の項の寄与（種ごと）
        sh, shd = [], []
        for sd in sds:
            g = winmean(sd, NL, 'bondRateMeanPerYear', a, b) - winmean(sd, UK, 'bondRateMeanPerYear', a, b)
            gd = winmean(sd, NL, 'bondDebtTermPerYear', a, b) - winmean(sd, UK, 'bondDebtTermPerYear', a, b)
            sh.append(gd / g if abs(g) > 1e-9 else NAN)
            # 割引後＝各項×議会の割引の係数（四半期平均の積＝近似）
            def after(nat, col):
                return winmean(sd, nat, col, a, b) * winmean(sd, nat, 'bondRepresentationDiscountMeanFactor', a, b)
            gda = after(NL, 'bondDebtTermPerYear') - after(UK, 'bondDebtTermPerYear')
            shd.append(gda / g if abs(g) > 1e-9 else NAN)
        L += ['', f'蘭−英の金利の差に占める債務比の項の差（議会の割引の前）：{cell(sh)}　／　議会の割引をかけた後：{cell(shd)}（≧0.8 なら〈作者の較正〉＝(264) §7-1 の規則）', '']
        # 債務比の差の中身：債務÷年GDP の差か係数の差か
        gdp = [winmean(sd, NL, 'bondDebtToGdpYearMeanRatio', a, b) - winmean(sd, UK, 'bondDebtToGdpYearMeanRatio', a, b) for sd in sds]
        L += [f'債務÷年GDP の差（蘭−英）：{cell(gdp)}', '']
    L += ['## 種ごとの Q1〜Q8（蘭−英の金利の差と、その債務比の寄与）', '', '| 種 | 蘭の金利 | 英の金利 | 差 | 債務比の項の差 | 寄与 |', '|---|---|---|---|---|---|']
    for sd in sds:
        n_, u_ = winmean(sd, NL, 'bondRateMeanPerYear', 0, 7), winmean(sd, UK, 'bondRateMeanPerYear', 0, 7)
        d_ = winmean(sd, NL, 'bondDebtTermPerYear', 0, 7) - winmean(sd, UK, 'bondDebtTermPerYear', 0, 7)
        L.append(f'| {sd} | {n_:.4f} | {u_:.4f} | {n_ - u_:.4f} | {d_:.4f} | {d_ / (n_ - u_) if abs(n_ - u_) > 1e-9 else NAN:.3f} |')
    # 初期値の側：蘭の国債÷年GDP・歳入÷年GDP・利払い÷歳入
    L += ['', '## 蘭の入力（Q1〜Q8）：国債÷年GDP・歳入÷年GDP・利払い÷歳入', '', '| 国 | 国債÷年GDP | 歳入÷年GDP | 利払い÷歳入 |', '|---|---|---|---|']
    for n in (UK, FR, NL):
        dg = [winmean(sd, n, 'bondDebtToGdpYearMeanRatio', 0, 7) for sd in sds]
        rv = [winmean(sd, n, 'revenueBaseMeanPerMonth', 0, 7) * 12 / winmean(sd, n, 'nominalGdpPerYear', 0, 7) for sd in sds]
        ib = [winmean(sd, n, 'interestMeanPerMonth', 0, 7) / winmean(sd, n, 'revenueBaseMeanPerMonth', 0, 7) for sd in sds]
        L.append(f'| {n} | {cell(dg)} | {cell(rv)} | {cell(ib)} |')
    write('a_bond_i265.md', L)


# ====================================================================== a_cap / a_policy
def a_cap():
    cols = ['quarter', 'nation', 'massPaidPerMonth'] + [f'cap{k}Share{d}' for k in ('BoundPaid', 'Cut', 'BoundPop') for d in ('D120', 'D240', 'D480')] + ['nominalGdpPerYear']
    R = load('nation', cols, lambda r: r['nation'] in (UK, FR, NL) and r['massPaidPerMonth'] == r['massPaidPerMonth'])
    sds = sorted(R)
    wins = [('Q1〜Q8', 0, 7), ('Q9〜Q40', 8, 39), ('Q41〜Q160', 40, 159), ('Q161〜Q320', 160, 319), ('Q321〜', 320, 10 ** 6)]
    L = hdr('(265) a. 段5 の上限の拘束（d ごと・大衆の Paid ベース）', '拘束＝大衆（農民・工場労働者・職人）の POP の Paid が上限 min(S, E+S×d) を超える。3つの割合：拘束された POP の Paid÷大衆の Paid／削られる額÷大衆の Paid／拘束された POP の人口÷大衆の人口。10種の中央〔最小〜最大〕。d が小さいほど上限は低い。')
    for n in (UK, FR, NL):
        L += [f'## {n}', '', '| 窓 | d | 拘束Paid | 削られる額 | 拘束人口 |', '|---|---|---|---|---|']
        for wn, a, b in wins:
            for d, dn in ((1 / 120, 'D120'), (1 / 240, 'D240'), (1 / 480, 'D480')):
                vals = {}
                for k in ('BoundPaid', 'Cut', 'BoundPop'):
                    vals[k] = [mean([r[f'cap{k}Share{dn}'] for r in R[sd] if r['nation'] == n and a <= r['quarter'] <= b]) for sd in sds]
                L.append(f'| {wn} | 1/{round(1 / d)} | {cell(vals["BoundPaid"])} | {cell(vals["Cut"])} | {cell(vals["BoundPop"])} |')
        L.append('')
    write('a_cap_i265.md', L)


def a_policy():
    cols = ['quarter', 'nation', 'nationIndex', 'monopolyTierBeforeDecisionStage', 'monopolyTierTargetDecisionStage', 'monopolyTierEndStage', 'fiscalStressAtDecisionIndex', 'stateFactorySoldCount',
            'pollTaxEndShare', 'pollTaxTargetEndShare', 'socialSecurityEndShare', 'socialSecurityTargetEndShare', 'socialSecurityLawEndStage', 'fireSaleFiredCount', 'populationK']
    R = load('nation', cols)
    sds = sorted(R)
    L = hdr('(265) a. 専売の段・国営工場の売却・逼迫・人頭税・福祉（(264) 段0′(e)）', '10種の中央〔最小〜最大〕。段の上げ下げ＝AI が置く段−今の段（判断の四半期の頭）。往復＝上げた四半期の次が下げ（またはその逆）の回数。')
    wins = [('Q1〜Q4', 0, 3), ('Q1〜Q12', 0, 11), ('Q5〜Q40', 4, 39), ('Q41〜Q160', 40, 159), ('Q161〜', 160, 10 ** 6)]
    for n in (UK, FR, NL):
        L += [f'## {n}', '', '| 窓 | 専売の段（末） | 上げた四半期 | 下げた四半期 | 往復 | 逼迫の指標（判断時） | 人頭税（実際） | 人頭税（目標） | 福祉（実際） | 福祉（目標） | 売却 |', '|---|---|---|---|---|---|---|---|---|---|---|']
        for wn, a, b in wins:
            cc = collections.defaultdict(list)
            for sd in sds:
                rs = sorted([r for r in R[sd] if r['nation'] == n and a <= r['quarter'] <= b], key=lambda r: r['quarter'])
                if not rs:
                    continue
                up = sum(1 for r in rs if r['monopolyTierTargetDecisionStage'] > r['monopolyTierBeforeDecisionStage'] >= 0)
                dn = sum(1 for r in rs if 0 <= r['monopolyTierTargetDecisionStage'] < r['monopolyTierBeforeDecisionStage'])
                sg = [(1 if r['monopolyTierTargetDecisionStage'] > r['monopolyTierBeforeDecisionStage'] >= 0 else -1 if 0 <= r['monopolyTierTargetDecisionStage'] < r['monopolyTierBeforeDecisionStage'] else 0) for r in rs]
                rt = sum(1 for x, y in zip(sg, sg[1:]) if x * y == -1)
                cc['tier'].append(mean([r['monopolyTierEndStage'] for r in rs])); cc['up'].append(up); cc['dn'].append(dn); cc['rt'].append(rt)
                cc['fs'].append(mean([r['fiscalStressAtDecisionIndex'] for r in rs])); cc['pt'].append(mean([r['pollTaxEndShare'] for r in rs])); cc['ptt'].append(mean([r['pollTaxTargetEndShare'] for r in rs]))
                cc['ss'].append(mean([r['socialSecurityEndShare'] for r in rs])); cc['sst'].append(mean([r['socialSecurityTargetEndShare'] for r in rs])); cc['sold'].append(sum(r['stateFactorySoldCount'] for r in rs))
            L.append(f'| {wn} | {cell(cc["tier"], 2)} | {cell(cc["up"], 1)} | {cell(cc["dn"], 1)} | {cell(cc["rt"], 1)} | {cell(cc["fs"])} | {cell(cc["pt"])} | {cell(cc["ptt"])} | {cell(cc["ss"])} | {cell(cc["sst"])} | {cell(cc["sold"], 1)} |')
        L.append('')
    # 全ての国（プール）：専売の段5・往復・売却
    L += ['## 全ての国（プール・10種）', '', '| 量 | 値 |', '|---|---|']
    tot = collections.Counter()
    per_seed = collections.defaultdict(list)
    for sd in sds:
        by = collections.defaultdict(list)
        for r in R[sd]:
            by[r['nationIndex']].append(r)
        for ni, rs in by.items():
            rs.sort(key=lambda r: r['quarter'])
            sg = []
            for r in rs:
                if r['monopolyTierBeforeDecisionStage'] < 0:
                    sg.append(0); continue
                sg.append(1 if r['monopolyTierTargetDecisionStage'] > r['monopolyTierBeforeDecisionStage'] else -1 if r['monopolyTierTargetDecisionStage'] < r['monopolyTierBeforeDecisionStage'] else 0)
                tot['judged'] += 1
            tot['up'] += sum(1 for x in sg if x == 1); tot['dn'] += sum(1 for x in sg if x == -1)
            tot['roundtrip'] += sum(1 for x, y in zip(sg, sg[1:]) if x * y == -1)
            tot['tier5'] += sum(1 for r in rs if r['monopolyTierEndStage'] >= 5)
            tot['sold'] += sum(r['stateFactorySoldCount'] for r in rs)
            tot['fireFired'] += sum(r['fireSaleFiredCount'] for r in rs if r['fireSaleFiredCount'] == r['fireSaleFiredCount'])
            tot['rows'] += len(rs)
            tot['stress>0'] += sum(1 for r in rs if r['fiscalStressAtDecisionIndex'] > 0)
    for k, v in tot.items():
        L.append(f'| {k}（10種の和） | {v} |')
    write('a_policy_i265.md', L)


# ====================================================================== b_admin_cf
def admin_cf(spare0, x, borrowed, claims_total, payable1):
    sp = spare0 + x
    b2 = min(borrowed, max(0.0, -sp))
    rb = False
    if claims_total == claims_total and payable1 == payable1:
        c2 = claims_total - x
        p2 = payable1 - (borrowed - b2)
        r2 = p2 / c2 if c2 > 0 else 1.0
        if not (r2 >= 0):
            r2 = 0.0
        elif r2 > 1:
            r2 = 1.0
        rb = r2 < 1
    return sp < 0, b2, rb


def b_admin_cf():
    fcols = ['monthIndex', 'nation', 'spareBeforeBorrowPerMonth', 'borrowedPerMonth', 'claimAdminPerMonth', 'adminClaimCappedAtBudgetPerMonth', 'adminNeedAtBudgetPerMonth', 'adminNeedAtPurchasePerMonth',
             'claimTotalPerMonth', 'payableAfterBorrowStock', 'payRateShare', 'adminOverNeedRegionsCount']
    F = load('fiscal', fcols, lambda r: r['nation'] == UK)
    sds = sorted(F)
    ncols = ['quarter', 'nation', 'nationIndex', 'year', 'adminMonthsCount', 'adminClaimSimPerMonth', 'adminNeedAtBudgetPerMonth', 'adminClaimCappedAtBudgetPerMonth', 'adminNeedAtPurchasePerMonth',
             'adminOverNeedRegionMonthsCount', 'adminCfObservedMonths', 'adminCfShortMonthsCount', 'adminCfBorrowedMeanPerMonth', 'adminCfRBelow1MonthsCount', 'shortfallMonths', 'borrowedMeanPerMonth',
             'payRateBelowOneMonths', 'populationK']
    N = load('nation', ncols)
    L = hdr('(265) b. 0g 段A-0′：A′（行政だけ・必要額で切る）の静的な反事実',
            '定義は docstring。余り X は計器の列〈請求−min(請求, 必要額)〉の州ごとの和（予算の時点の価格）。基線の英の 払えない月・借入・率＜1 の月 と、A′ で消える割合。')
    # --- 英（fiscal_s の月ごと）
    for wn, a, b in (('全期', 0, 10 ** 9), ('Q1〜Q40', 0, 39), ('Q41〜Q160', 40, 159), ('Q161〜', 160, 10 ** 9)):
        agg = collections.defaultdict(list)
        for sd in sds:
            rs = sorted(F[sd], key=lambda r: r['monthIndex'])
            if not rs:
                continue
            m0 = rs[0]['monthIndex'] - 1
            sel = [r for r in rs if a <= (r['monthIndex'] - m0) // 3 <= b]
            short = short2 = rb0 = rb2 = 0
            bor = bor2 = 0.0
            nx = 0
            xsum = clsum = 0.0
            for r in sel:
                x = r['claimAdminPerMonth'] - r['adminClaimCappedAtBudgetPerMonth']
                if not (x == x):
                    continue
                spare0 = r['spareBeforeBorrowPerMonth']
                if spare0 != spare0:
                    continue
                bo = r['borrowedPerMonth'] if r['borrowedPerMonth'] == r['borrowedPerMonth'] else 0.0
                s2, b2, rb = admin_cf(spare0, x, bo, r['claimTotalPerMonth'], r['payableAfterBorrowStock'])
                short += spare0 < 0; short2 += s2
                bor += bo; bor2 += b2
                rb0 += r['payRateShare'] < 1; rb2 += rb
                nx += x > 1e-9; xsum += x; clsum += r['claimTotalPerMonth']
            agg['months'].append(len(sel)); agg['short'].append(short); agg['short2'].append(short2); agg['bor'].append(bor); agg['bor2'].append(bor2)
            agg['rb'].append(rb0); agg['rb2'].append(rb2); agg['xmonths'].append(nx); agg['xshare'].append(xsum / clsum if clsum > 0 else NAN)
        S = lambda k: sum(agg[k])
        L += [f'## {UK}（fiscal_s の月ごと・{wn}・10種の和）', '', '| 量 | 基線 | A′ | 消える割合 |', '|---|---|---|---|',
              f'| 払えない月（spare＜0） | {S("short")} | {S("short2")} | P_short＝{1 - S("short2") / S("short") if S("short") else NAN:.3f} |',
              f'| 借入の和 | {S("bor"):.0f} | {S("bor2"):.0f} | P_borrow＝{1 - S("bor2") / S("bor") if S("bor") else NAN:.3f} |',
              f'| 率＜1 の月 | {S("rb")} | {S("rb2")} | P_r＝{1 - S("rb2") / S("rb") if S("rb") else NAN:.3f} |',
              f'| 余り X＞0 の月 | {S("xmonths")}／{S("months")} | | X÷請求の合計（種の中央）{cell(agg["xshare"], 4)} |', '']
        if wn == '全期':
            L += ['種ごと（全期）：', '', '| 種 | 月 | 払えない月 | A′ | 借入 | A′ | 率＜1 | A′ | X＞0 の月 |', '|---|---|---|---|---|---|---|---|---|']
            for i, sd in enumerate(sds):
                L.append(f'| {sd} | {agg["months"][i]} | {agg["short"][i]} | {agg["short2"][i]} | {agg["bor"][i]:.0f} | {agg["bor2"][i]:.0f} | {agg["rb"][i]} | {agg["rb2"][i]} | {agg["xmonths"][i]} |')
            L.append('')
    # --- 計器の列(nation_s)との突き合わせ：英
    L += ['## 突き合わせ：計器の列（nation_s・四半期の和）と読み手の再計算（fiscal_s）', '']
    tot = collections.Counter()
    for sd in sds:
        for r in N[sd]:
            if r['nation'] != UK or r['adminCfObservedMonths'] != r['adminCfObservedMonths']:
                continue
            tot['cf_short'] += r['adminCfShortMonthsCount']; tot['base_short'] += r['shortfallMonths']; tot['cf_rb'] += r['adminCfRBelow1MonthsCount']; tot['base_rb'] += r['payRateBelowOneMonths']
            b1_, b0_ = r['adminCfBorrowedMeanPerMonth'] * r['adminMonthsCount'], r['borrowedMeanPerMonth'] * r['adminMonthsCount']
            if b0_ == b0_ and b1_ == b1_:
                tot['cf_bor'] += b1_; tot['base_bor'] += b0_
    L += [f'英・10種・全期（nation_s）：払えない月 {tot["base_short"]:.0f} → A′ {tot["cf_short"]:.0f}（P_short＝{1 - tot["cf_short"] / tot["base_short"] if tot["base_short"] else NAN:.3f}）／'
          f'借入 {tot["base_bor"]:.0f} → {tot["cf_bor"]:.0f}（P_borrow＝{1 - tot["cf_bor"] / tot["base_bor"] if tot["base_bor"] else NAN:.3f}）／率＜1 の月 {tot["base_rb"]:.0f} → {tot["cf_rb"]:.0f}（P_r＝{1 - tot["cf_rb"] / tot["base_rb"] if tot["base_rb"] else NAN:.3f}）', '']
    L += ['', '⚠率＜1 の月は fiscal_s の payRateShare（G10 の文字列）で数えると、率が 0.99999999995 の月が 1 に丸まって落ちる＝fiscal_s の再計算の「率＜1」は下限（基線 556）。nation_s の厳密な列（payRateBelowOneMonths・adminCfRBelow1MonthsCount）が正しい数。払えない月・借入は両方の数が合う。', '']
    # --- 必要額と請求の関係（英・仏・蘭と全ての国）
    L += ['## 請求÷必要額（予算の時点・四半期）と 余りの出る四半期', '', '| 国 | 請求÷必要額（中央〔最小〜最大〕） | 余りのある州の月が1つ以上ある四半期 | 購買の時点の必要額÷予算の時点（最大の乖離） |', '|---|---|---|---|']
    for n in (UK, FR, NL, None):
        ratio, over, tot_q, dev = [], 0, 0, 0.0
        for sd in sds:
            for r in N[sd]:
                if (n is not None and r['nation'] != n) or not (r['adminNeedAtBudgetPerMonth'] > 0):
                    continue
                ratio.append(r['adminClaimSimPerMonth'] / r['adminNeedAtBudgetPerMonth']); tot_q += 1
                if r['adminOverNeedRegionMonthsCount'] > 0:
                    over += 1
                dev = max(dev, abs(r['adminNeedAtPurchasePerMonth'] / r['adminNeedAtBudgetPerMonth'] - 1))
        xs = sorted(ratio)
        if not xs:
            L.append(f'| {n or "全ての国"} | —（行なし） | | |')
            continue
        L.append(f'| {n or "全ての国"} | {statistics.median(xs):.3f} [{xs[0]:.3f}〜{xs[-1]:.3f}]・90%点 {xs[int(len(xs) * .9)]:.3f} | {over}／{tot_q} | {dev:.2e} |')
    # --- 全ての国：反事実のプール
    L += ['', '## 全ての国（nation_s・10種の和）：払えない月・借入・率＜1 の月', '']
    for lab, flt in (('全ての国', lambda r: True), ('人口 1000千人以上', lambda r: r['populationK'] >= 1000)):
        t = collections.Counter()
        for sd in sds:
            for r in N[sd]:
                if r['adminCfObservedMonths'] != r['adminCfObservedMonths'] or not flt(r):
                    continue
                t['obs'] += r['adminCfObservedMonths']; t['s0'] += r['shortfallMonths']; t['s1'] += r['adminCfShortMonthsCount']; t['r0'] += r['payRateBelowOneMonths']; t['r1'] += r['adminCfRBelow1MonthsCount']
                b0 = r['borrowedMeanPerMonth'] * r['adminMonthsCount']; b1 = r['adminCfBorrowedMeanPerMonth'] * r['adminMonthsCount']
                if b0 == b0 and b1 == b1:
                    t['b0'] += b0; t['b1'] += b1
        L.append(f'- {lab}：観測した月 {t["obs"]}・払えない月 {t["s0"]} → {t["s1"]}（{1 - t["s1"] / t["s0"] if t["s0"] else NAN:.3f}）・借入 {t["b0"]:.0f} → {t["b1"]:.0f}（{1 - t["b1"] / t["b0"] if t["b0"] else NAN:.3f}）・率＜1 の月 {t["r0"]} → {t["r1"]}（{1 - t["r1"] / t["r0"] if t["r0"] else NAN:.3f}）')
    write('b_admin_cf_i265.md', L)


# ====================================================================== c_band
def band_step(prev, at_war, scale, losing):
    """総動員の帯（SelfPlay.cs の総動員の分岐と同じ線）。戻り値＝選択 0/1。NaN は比較が偽＝現状維持。"""
    if not at_war:
        return 0
    if scale >= BAND_UP_SCALE or losing > BAND_UP_LOSING:
        return 1
    if scale < 1.0 / BAND_UP_SCALE and not (losing > 0):
        return 0
    return prev


def band_selftest():
    assert band_step(0, 0, 5, 5) == 0                      # 交戦中でない＝0
    assert band_step(0, 1, 1.2, 0) == 1                    # 上げる線（規模ちょうど）
    assert band_step(0, 1, 0.5, 0.51) == 1                 # 存亡
    assert band_step(1, 1, 1.0, 0.0) == 1                  # 線の間＝前のまま
    assert band_step(0, 1, 1.0, 0.0) == 0
    assert band_step(1, 1, 1.0 / 1.2 - 1e-9, 0.0) == 0     # 下げる線
    assert band_step(1, 1, 0.5, 0.01) == 1                 # 存亡が0でない＝下げない
    assert band_step(1, 1, float('nan'), 0.0) == 1         # NaN は現状維持
    assert band_step(0, 1, float('nan'), 0.0) == 0


def c_band():
    band_selftest()
    cols = ['quarter', 'year', 'nationIndex', 'nation', 'atWarAtDecisionFlag', 'warScaleRatioAtDecision', 'homeLosingScoreAtDecision', 'mobilizedAtDecisionFlag', 'aiPickedWarStageFlag',
            'warStageOpenAfterGovFlag', 'conscriptionLaw', 'populationK']
    N = load('nation', cols)
    sds = sorted(N)
    L = hdr('(265) c. 0g 段B-0′：総動員の帯で数えた N（AI の選択）',
            '選択＝帯の状態機械（docstring）を nation_s の判断の列で回した値。N＝選択が1の判断の数（国×四半期）。上限＝交戦中かつ（規模＞0 または存亡＞0）。入れ替わり＝交戦中が続く2四半期の間で選択が変わった回数。B-1（解禁⇔選択）が入れば〈AI が最上段を選ぶ四半期〉は N 以下（器が立つのは総動員の法の門を通る分だけ）。')
    tot = {'UK': collections.Counter(), 'pool': collections.Counter()}
    per_seed_uk = []
    uk_series = {}
    for sd in sds:
        by = collections.defaultdict(list)
        for r in N[sd]:
            by[r['nationIndex']].append(r)
        cu = collections.Counter()
        for ni, rs in by.items():
            rs.sort(key=lambda r: r['quarter'])
            isuk = rs[0]['nation'] == UK or any(r['nation'] == UK for r in rs)
            key = 'UK' if isuk else 'pool'
            prev, prev_war = 0, False
            ser = []
            for r in rs:
                f = r['atWarAtDecisionFlag']
                if not (f >= 0):
                    ser.append(None); continue   # この四半期は判断が来なかった
                at_war = f == 1
                ch = band_step(prev, at_war, r['warScaleRatioAtDecision'], r['homeLosingScoreAtDecision'])
                c = tot[key]
                c['decisions'] += 1
                if at_war:
                    c['atWar'] += 1
                    ub = (r['warScaleRatioAtDecision'] > 0) or (r['homeLosingScoreAtDecision'] > 0)
                    c['upper'] += ub
                    c['N'] += ch
                    if ch != prev and prev_war:
                        c['flips'] += 1
                        c['flipsUp'] += ch == 1
                    if ch == 1 and r['mobilizedAtDecisionFlag'] == 1:
                        c['N&mobilized'] += 1
                    if ch == 0 and r['mobilizedAtDecisionFlag'] == 1:
                        c['notN&mobilized'] += 1
                    c['mobilized'] += r['mobilizedAtDecisionFlag'] == 1
                    c['picked'] += r['aiPickedWarStageFlag'] == 1
                    c['open'] += r['warStageOpenAfterGovFlag'] == 1
                    if ch == 1 and r['conscriptionLaw'] >= 0:
                        c['N@lawLE0'] += r['conscriptionLaw'] <= 0
                if isuk:
                    cu['N'] += ch if at_war else 0; cu['atWar'] += at_war
                    cu['flips'] += (at_war and prev_war and ch != prev)
                    cu['upper'] += at_war and ((r['warScaleRatioAtDecision'] > 0) or (r['homeLosingScoreAtDecision'] > 0))
                prev, prev_war = ch, at_war
                ser.append(ch)
            if isuk:
                uk_series[sd] = [(r['year'], r['quarter'], s) for r, s in zip(rs, ser)]
        per_seed_uk.append((sd, cu))
    for key, lab in (('UK', f'{UK}（10種の和）'), ('pool', '他の国（10種の和・プール）')):
        c = tot[key]
        L += [f'## {lab}', '', '| 量 | 値 |', '|---|---|',
              f'| 判断が来た国×四半期 | {c["decisions"]} |', f'| 交戦中の判断 | {c["atWar"]} |', f'| 上限（交戦中かつ規模＞0 または存亡＞0） | {c["upper"]} |',
              f'| **N（帯の選択が1）** | **{c["N"]}**（交戦中の判断の {c["N"] / c["atWar"] * 100 if c["atWar"] else NAN:.1f}%・上限の {c["N"] / c["upper"] * 100 if c["upper"] else NAN:.1f}%） |',
              f'| 入れ替わり（交戦中が続く2四半期の間） | {c["flips"]}（うち 0→1 {c["flipsUp"]}） |',
              f'| 比べる量：今の総動員（mobilizedAtDecisionFlag＝判断の後の World.Mobilized）が立つ交戦中の判断 | {c["mobilized"]} |',
              f'| 比べる量：AI が最上段を選んだ（aiPickedWarStageFlag） | {c["picked"]} |', f'| 比べる量：戦時段の解禁（warStageOpenAfterGovFlag） | {c["open"]} |',
              f'| 選択1 かつ 今の総動員 | {c["N&mobilized"]}（選択0 かつ 今の総動員 {c["notN&mobilized"]}） |', '']
    L += ['## 英の種ごと', '', '| 種 | 交戦中の判断 | 上限 | N | 入れ替わり |', '|---|---|---|---|---|']
    for sd, cu in per_seed_uk:
        L.append(f'| {sd} | {cu["atWar"]} | {cu["upper"]} | {cu["N"]} | {cu["flips"]} |')
    write('c_band_i265.md', L)


# ====================================================================== d_goods
def d_goods():
    gcols = ['quarter', 'year', 'nation', 'goodName', 'tier', 'activeRegions', 'lastProducedQty', 'lastDemandQty', 'lastSoldQty', 'lastShortageQty', 'lastImportQty', 'lastExportQty', 'stockQty', 'priceMean',
             'importsValuePerClearing', 'exportsValuePerClearing']
    G = load('goods', gcols)
    ncols = ['quarter', 'year', 'nationIndex', 'nation', 'populationK', 'supplyFillShare', 'foodMetShare', 'needMetShare', 'coreRegions', 'integratedRegions', 'colonialRegions']
    N = load('nation', ncols)
    L = hdr('(265) d. 残存国の財ごとの需給（(263) B の切り分け (a)）',
            '条件＝活きた州≦2・人口≧100千人・人口加重の供給の充足＜0.5 の四半期の行だけ（goods_s）。量は州の市場の直近の月の値の国の和（四半期の末の断面）。')
    cnt = collections.Counter()
    for sd, rs in G.items():
        for r in rs:
            cnt[(sd, r['nation'])] += 1
    L += ['## goods_s に現れた国（種×国・行数）', '']
    for (sd, n), c in sorted(cnt.items()):
        L.append(f'- 種{sd} {n}：{c} 行（{len({r["quarter"] for r in G[sd] if r["nation"] == n})} 四半期）')
    # ポルトガル
    L += ['', f'## {PT}', '']
    pt_seeds = sorted({sd for (sd, n) in cnt if n == PT})
    if not pt_seeds:
        L += [f'{PT} の行が無い（条件に合う四半期が無い）。']
    for sd in pt_seeds:
        rs = [r for r in G[sd] if r['nation'] == PT]
        qs = sorted({r['quarter'] for r in rs})
        L += [f'### 種{sd}：{len(qs)} 四半期（Q{int(qs[0]) + 1}〜Q{int(qs[-1]) + 1}）', '']
        nat = [r for r in N[sd] if r['nation'] == PT and r['quarter'] in set(qs)]
        L += [f'- 人口（千人）{cell([r["populationK"] for r in nat], 1)}・供給の充足 {cell([r["supplyFillShare"] for r in nat])}・主食の充足 {cell([r["foodMetShare"] for r in nat])}・満足度の台帳の充足 {cell([r["needMetShare"] for r in nat])}', '']
        goods = collections.defaultdict(list)
        for r in rs:
            goods[(r['tier'], r['goodName'])].append(r)
        L += ['| 階層 | 財 | 産出 | 需要 | 売れた量 | 不足 | 輸入 | 輸出 | 在庫 | 価格 | 需要がある四半期 |', '|---|---|---|---|---|---|---|---|---|---|---|']
        for (tier, g), xs in sorted(goods.items()):
            def m(k):
                return mean([x[k] for x in xs])
            L.append(f'| {tier} | {g} | {m("lastProducedQty"):.1f} | {m("lastDemandQty"):.1f} | {m("lastSoldQty"):.1f} | {m("lastShortageQty"):.1f} | {m("lastImportQty"):.1f} | {m("lastExportQty"):.1f} | {m("stockQty"):.1f} | {m("priceMean"):.2f} | {sum(1 for x in xs if x["lastDemandQty"] > 1e-9)}／{len(xs)} |')
        L.append('')
        # 主食だけ時系列（先頭・中央・末）
        staples = sorted({g for (t, g) in goods if t == 'Staple'})
        L += ['主食の時系列（産出／需要／輸入／不足・先頭・中央・末の四半期）：', '']
        for g in staples:
            xs = sorted(goods[('Staple', g)], key=lambda r: r['quarter'])
            for lab, x in (('先頭', xs[0]), ('中央', xs[len(xs) // 2]), ('末', xs[-1])):
                L.append(f'- {g} {lab}（Q{int(x["quarter"]) + 1}）：産出 {x["lastProducedQty"]:.1f}／需要 {x["lastDemandQty"]:.1f}／輸入 {x["lastImportQty"]:.1f}／不足 {x["lastShortageQty"]:.1f}／在庫 {x["stockQty"]:.1f}')
        L.append('')
    # 他の国：主食の需給の要約（種×国）
    L += ['## 他の残存国（主食の合計の 産出÷需要・輸入÷需要）', '', '| 種 | 国 | 四半期 | 主食の産出÷需要 | 主食の輸入÷需要 | 供給の充足 |', '|---|---|---|---|---|---|']
    for (sd, n), c in sorted(cnt.items()):
        if n == PT:
            continue
        rs = [r for r in G[sd] if r['nation'] == n and r['tier'] == 'Staple']
        byq = collections.defaultdict(lambda: [0.0, 0.0, 0.0])
        for r in rs:
            a = byq[r['quarter']]; a[0] += r['lastProducedQty']; a[1] += r['lastDemandQty']; a[2] += r['lastImportQty']
        pr = [a[0] / a[1] for a in byq.values() if a[1] > 1e-9]
        im = [a[2] / a[1] for a in byq.values() if a[1] > 1e-9]
        nat = [r for r in N[sd] if r['nation'] == n and r['quarter'] in byq]
        L.append(f'| {sd} | {n} | {len(byq)} | {cell(pr)} | {cell(im)} | {cell([r["supplyFillShare"] for r in nat])} |')
    write('d_goods_i265.md', L)


if __name__ == '__main__':
    for fn in (a_bond, a_cap, a_policy, b_admin_cf, c_band, d_goods):
        print('...', fn.__name__, flush=True)
        fn()
    print('done')
