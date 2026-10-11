"""(259) 線T2：0g の段A-0（行政の請求の2つの式）と段B-0（戦時段）の読み手（読むだけ）。入力は標準の計器 nation_s<seed>.tsv（国×四半期・全ての国）。
使い方：python -X utf8 an_g0_259.py <計器の出力ディレクトリ> [A|B|all] [国名=イギリス]
 A：行政の請求の2つの式
    ・Fiscal.cs の adminClaim（LastAdminClaimPerMonth＝adminClaimFiscalPerMonth）÷ Simulation の州の予算の和（claimAdmin＝adminClaimSimPerMonth）を国×四半期で比べる（中央・10%点・90%点）。
    ・差の源の分解：Fisc/Sim ＝ (当月の予算基数 ÷ 月初の先月の歳入) × (月初の先月の歳入 ÷ SpendableRevenue) × (Fiscal の距離の割増の平均 ÷ 官僚の居る州の人口の比×距離の割増の和)
      ＝ 月のずれ × 潤沢の上乗せ（≦1）× 官僚の無い州（≧1）。四半期平均の比の積なので、積と直の比の差（残差）も出す。
    ・行政＋上下水道＋電化の 実際（台帳 Admin の月の差分）÷ 請求（Simulation の和）と、Σ請求÷Σ実際。
    ・r＜1 の月を含む四半期と含まない四半期の行政の充足・歳入÷年GDP。
 B：戦時段（AI の判断は四半期の頭〔DecideQuarter〕の値）
    ・交戦中の判断（atWarAtDecisionFlag=1）のうち総動員が立つ割合。立たない判断の戦争の規模（0＝海越え・植民地／0超〜1.2未満）と存亡。
    ・戦時段の解禁（warStageOpenAfterGovFlag=1）の連続（エピソード）：施行が段4へ届いたか・届くまでの四半期・終わり方（講和／交戦中の復員）。
    ・戦時段の解禁の四半期に目標が4／3以下（AI が最上段を置いても法の門で畳まれた）。
    ・国ごとの 交戦中の判断の数 ÷ 戦時段の解禁の数（B-1 で WarStageOpen＝AtWarNow にしたとき他国が何倍になるか）。
窓：1783-1814 1815-1849 1850-1880 1881-1913 1914-1950（暦年）。"""
import os
import sys
import statistics as st

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
from instr import rows, num  # noqa: E402

WINS = [(1783, 1814), (1815, 1849), (1850, 1880), (1881, 1913), (1914, 1950)]


def win_of(y):
    for a, b in WINS:
        if a <= y <= b:
            return f'{a}-{b}'
    return None


def pct(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    k = (len(xs) - 1) * p
    lo = int(k)
    hi = min(lo + 1, len(xs) - 1)
    return xs[lo] + (xs[hi] - xs[lo]) * (k - lo)


def dist(xs):
    xs = [x for x in xs if x == x and x not in (float('inf'), float('-inf'))]
    if not xs:
        return 'n=0'
    return f'n={len(xs)} 中央{pct(xs, .5):.3f} 10%点{pct(xs, .1):.3f} 90%点{pct(xs, .9):.3f}'


def load(D):
    out = []
    for sd, r in rows(D, 'nation'):
        if 'clearingsPerYear' not in r:   # 書きかけの最後の行は読まない
            continue
        r['_seed'] = sd
        r['_q'] = int(num(r['quarter']))
        r['_y'] = int(num(r['year']))
        out.append(r)
    return out


def part_a(R, nation):
    print('\n######## A：行政の請求の2つの式（Fiscal.cs:274 adminClaim ＝ LastAdminClaimPerMonth ／ Simulation.cs:2103 の州の予算の和）')
    seeds = len({r['_seed'] for r in R})
    print(f'種={seeds}・国×四半期の行={len(R)}')

    def ok(r):
        return num(r['adminMonthsCount']) >= 3 and num(r['adminClaimSimPerMonth']) > 0 and num(r['adminClaimFiscalPerMonth']) > 0

    def block(title, sel):
        print(f'\n== {title}')
        for w in [None] + [f'{a}-{b}' for a, b in WINS]:
            rs = [r for r in sel if ok(r) and (w is None or win_of(r['_y']) == w)]
            if not rs:
                continue
            ratio = [num(r['adminClaimFiscalPerMonth']) / num(r['adminClaimSimPerMonth']) for r in rs]
            # 分解の3因子（四半期平均の比）
            base = [num(r['adminRevenueBasePerMonth']) / num(r['adminLastRevenueAtBudgetPerMonth']) for r in rs if num(r['adminLastRevenueAtBudgetPerMonth']) > 0]
            opul = [num(r['adminLastRevenueAtBudgetPerMonth']) / num(r['adminSpendableAtBudgetPerMonth']) for r in rs if num(r['adminSpendableAtBudgetPerMonth']) > 0]
            cover = [num(r['adminFiscalMeanMul']) / num(r['adminSimMeanMulShare']) for r in rs if num(r['adminSimMeanMulShare']) > 0]
            prod = []
            resid = []
            for r in rs:
                a, b, c = num(r['adminRevenueBasePerMonth']), num(r['adminLastRevenueAtBudgetPerMonth']), num(r['adminSpendableAtBudgetPerMonth'])
                d, e = num(r['adminFiscalMeanMul']), num(r['adminSimMeanMulShare'])
                if b > 0 and c > 0 and e > 0:
                    p = (a / b) * (b / c) * (d / e)
                    prod.append(p)
                    resid.append(p / (num(r['adminClaimFiscalPerMonth']) / num(r['adminClaimSimPerMonth'])) - 1)
            print(f'  {w or "全期間":<10} Fisc/Sim {dist(ratio)}')
            print(f'  {"":<10}   月のずれ(当月基数/先月歳入) {dist(base)}')
            print(f'  {"":<10}   潤沢の上乗せ(先月歳入/Spendable) {dist(opul)}')
            print(f'  {"":<10}   官僚の無い州(距離平均/官僚のある州の和) {dist(cover)}')
            print(f'  {"":<10}   3因子の積÷直の比−1（残差） {dist(resid)}')

    block(f'{nation}', [r for r in R if r['nation'] == nation])
    block('全ての国（国×四半期をプール）', R)
    # 全ての国の中で Fisc/Sim が 1 から離れる国の上位
    by = {}
    for r in R:
        if ok(r):
            by.setdefault(r['nation'], []).append(num(r['adminClaimFiscalPerMonth']) / num(r['adminClaimSimPerMonth']))
    top = sorted(((st.median(v), n, len(v)) for n, v in by.items() if len(v) >= 40), reverse=True)
    print('\n== 国別の Fisc/Sim の中央（四半期40以上の国・上位8と下位5）')
    for m, n, k in top[:8] + top[-5:]:
        print(f'  {n:<14} 中央 {m:.3f}（{k}四半期）')

    print(f'\n== 行政＋上下水道＋電化の 実際／請求（台帳 Admin の月の差分 ÷ Simulation の州の予算の和・r を掛ける前）')

    def pay_ok(r):
        return num(r['adminMonthsCount']) >= 3 and num(r['adminCatClaimSimPerMonth']) > 0 and num(r['adminCatPaidPerMonth']) == num(r['adminCatPaidPerMonth'])

    for title, sel in [(nation, [r for r in R if r['nation'] == nation]), ('全ての国', R)]:
        print(f'  -- {title}')
        for w in [None] + [f'{a}-{b}' for a, b in WINS]:
            rs = [r for r in sel if pay_ok(r) and (w is None or win_of(r['_y']) == w)]
            if not rs:
                continue
            q = [num(r['adminCatPaidPerMonth']) / num(r['adminCatClaimSimPerMonth']) for r in rs]
            sc = sum(num(r['adminCatClaimSimPerMonth']) for r in rs)
            sp = sum(num(r['adminCatPaidPerMonth']) for r in rs)
            print(f'  {w or "全期間":<10} 実際/請求 {dist(q)}・Σ請求÷Σ実際 {sc / sp if sp > 0 else float("nan"):.3f}')

    print(f'\n== r＜1 の月を含む四半期と含まない四半期（{nation}・全ての国）')
    for title, sel in [(nation, [r for r in R if r['nation'] == nation]), ('全ての国', R)]:
        lo = [r for r in sel if num(r['adminRBelow1MonthsCount']) > 0 and num(r['adminMonthsCount']) >= 3]
        hi = [r for r in sel if num(r['adminRBelow1MonthsCount']) == 0 and num(r['adminMonthsCount']) >= 3]
        tot = len(lo) + len(hi)
        print(f'  -- {title}：r＜1 の月を含む四半期 {len(lo)}／{tot}（{len(lo) / tot * 100 if tot else float("nan"):.1f}%）・うち3か月全て r＜1 {sum(1 for r in lo if num(r["adminRBelow1MonthsCount"]) >= 3)}')
        for lab, xs in (('r＜1 を含む', lo), ('r＝1 のみ', hi)):
            fill = [num(r['adminFillMeanShare']) for r in xs]
            rev = [num(r['adminRevenueBasePerMonth']) * 12 / num(r['nominalGdpPerYear']) for r in xs if num(r['nominalGdpPerYear']) > 0]
            pr = [num(r['payRateMeanShare']) for r in xs]
            print(f'     {lab:<10} 行政の充足(四半期平均) {dist(fill)}／支払い率 {dist(pr)}／歳入基数÷年GDP {dist(rev)}')
        fl = [num(r['adminFillMeanInRBelow1Share']) for r in lo]
        print(f'     r＜1 の月だけの行政の充足 {dist(fl)}')
        # 同じ国の中で r＜1 の四半期の前後の差（前の四半期からの変化）
    # r＜1 の四半期の次の四半期の変化（英）
    print('\n== 支払い率 r（四半期平均）の分布と、r が行政の購買に噛む四半期（r を掛けた予算が実際の購買を割る＝r＜Σ実際÷Σ請求）')
    for title, sel in [(nation, [r for r in R if r['nation'] == nation]), ('全ての国', R)]:
        xs = [r for r in sel if num(r['adminMonthsCount']) >= 3 and num(r['payRateMeanShare']) == num(r['payRateMeanShare'])]
        n = len(xs)
        print(f'  -- {title}：四半期 {n}')
        for th in (0.99, 0.9, 0.7, 0.5, 0.2):
            k = sum(1 for r in xs if num(r['payRateMeanShare']) < th)
            print(f'     r＜{th}：{k}（{k / n * 100:.2f}%）')
        bite = [r for r in xs if num(r['adminCatClaimSimPerMonth']) > 0 and num(r['adminCatPaidPerMonth']) == num(r['adminCatPaidPerMonth'])
                and num(r['payRateMeanShare']) < num(r['adminCatPaidPerMonth']) / num(r['adminCatClaimSimPerMonth'])]
        print(f'     r＜(実際/Simulation の請求) の四半期：{len(bite)}（{len(bite) / n * 100:.2f}%）')

    print(f'\n== {nation}：r＜1 を含む四半期の前の四半期→その四半期の 行政の充足と歳入基数÷年GDP の差（同じ種・同じ国）')
    idx = {(r['_seed'], r['nation'], r['_q']): r for r in R}
    dfill, drev = [], []
    for r in R:
        if r['nation'] != nation or not (num(r['adminRBelow1MonthsCount']) > 0 and num(r['adminMonthsCount']) >= 3):
            continue
        p = idx.get((r['_seed'], r['nation'], r['_q'] - 1))
        if p is None or num(p['adminRBelow1MonthsCount']) > 0 or num(p['adminMonthsCount']) < 3:
            continue
        dfill.append(num(r['adminFillMeanShare']) - num(p['adminFillMeanShare']))
        if num(r['nominalGdpPerYear']) > 0 and num(p['nominalGdpPerYear']) > 0:
            drev.append(num(r['adminRevenueBasePerMonth']) * 12 / num(r['nominalGdpPerYear']) - num(p['adminRevenueBasePerMonth']) * 12 / num(p['nominalGdpPerYear']))
    print(f'  行政の充足の差 {dist(dfill)}／歳入基数÷年GDPの差 {dist(drev)}')


def part_b(R, nation):
    print(f'\n######## B：戦時段（AI の判断は四半期の頭）・{nation}と他国')
    seeds = len({r['_seed'] for r in R})
    print(f'種={seeds}・国×四半期の行={len(R)}')
    war = [r for r in R if num(r['atWarAtDecisionFlag']) == 1]
    print(f'交戦中の判断（atWarAtDecisionFlag=1）全ての国 {len(war)}・{nation} {sum(1 for r in war if r["nation"] == nation)}')

    def b_i(title, sel):
        n = len(sel)
        if not n:
            print(f'  -- {title}：交戦中の判断なし')
            return
        nm = [r for r in sel if num(r['mobilizedAtDecisionFlag']) == 0]
        mo = [r for r in sel if num(r['mobilizedAtDecisionFlag']) == 1]
        def cls(r):
            sc, lo = num(r['warScaleRatioAtDecision']), num(r['homeLosingScoreAtDecision'])
            if sc >= 1.2 or lo > 0.5:
                return 'big'
            if sc == 0:
                return 'zero'
            if 0 < sc < 1.2:
                return 'small'
            return 'other'
        kinds = [cls(r) for r in nm]
        z = [r for r, k in zip(nm, kinds) if k == 'zero']
        small = [r for r, k in zip(nm, kinds) if k == 'small']
        mid = [r for r, k in zip(nm, kinds) if k in ('big', 'other')]
        big = []
        dis = [r for r in nm if num(r['warScaleRatioAtDecision']) < 1.2 and num(r['homeLosingScoreAtDecision']) <= 0.5]
        print(f'  -- {title}：交戦中の判断 {n}・総動員あり {len(mo)}（{len(mo) / n * 100:.1f}%）・総動員なし {len(nm)}（{len(nm) / n * 100:.1f}%）')
        if nm:
            print(f'     総動員なしの内訳：規模0（海越え・植民地・抽象標的） {len(z)}（{len(z) / len(nm) * 100:.1f}%）／0超〜1.2未満 {len(small)}（{len(small) / len(nm) * 100:.1f}%）／上げる線以上なのに立たない(NaN等) {len(mid) + len(big)}'
                  f'｜規模<1.2 かつ 存亡≦0.5（上げる線に届かない） {len(dis)}（{len(dis) / len(nm) * 100:.1f}%）')
        # 総動員ありで総動員が立った判断のうち規模が 0（前の四半期から引き継いだ）
        carry = [r for r in mo if num(r['warScaleRatioAtDecision']) < 1.2 and num(r['homeLosingScoreAtDecision']) <= 0.5]
        if mo:
            print(f'     総動員あり {len(mo)} のうち規模<1.2 かつ 存亡≦0.5（現状維持の帯＝線の間）{len(carry)}')

    print('\n== (i) 総動員が立たない')
    b_i(nation, [r for r in war if r['nation'] == nation])
    for w in [f'{a}-{b}' for a, b in WINS]:
        b_i(f'{nation} {w}', [r for r in war if r['nation'] == nation and win_of(r['_y']) == w])
    b_i('他国（イギリス以外・プール）', [r for r in war if r['nation'] != nation])

    print('\n== (ii) 戦時段の解禁（warStageOpenAfterGovFlag=1）の四半期の目標と施行')
    for title, sel in [(nation, [r for r in R if r['nation'] == nation]), ('他国（プール）', [r for r in R if r['nation'] != nation])]:
        op = [r for r in sel if num(r['warStageOpenAfterGovFlag']) == 1]
        if not op:
            print(f'  -- {title}：解禁の四半期 0')
            continue
        t4 = sum(1 for r in op if num(r['militaryStageTargetAfterGovStage']) == 4)
        e4 = sum(1 for r in op if num(r['militaryStageEnforcedAfterGovStage']) == 4)
        e4e = sum(1 for r in op if num(r['militaryStageEnforcedEndStage']) == 4)
        sh = [num(r['militarySpendingAfterGovShare']) for r in op]
        print(f'  -- {title}：解禁の四半期 {len(op)}・目標が段4 {t4}（{t4 / len(op) * 100:.1f}%）・施行が段4（頭）{e4}（{e4 / len(op) * 100:.1f}%）・施行が段4（末）{e4e}（{e4e / len(op) * 100:.1f}%）・施行の水準(頭) {dist(sh)}')

    def ws_share(r, suffix):
        # Military.WarStageShare の写し（読み手の側の計算）：解禁中の min(施行の水準, 目標の段/段数) が段3(0.75)を超えた分×4 を 0〜1 に
        if suffix == 'gov':
            op, sh, tg = num(r['warStageOpenAfterGovFlag']), num(r['militarySpendingAfterGovShare']), num(r['militaryStageTargetAfterGovStage']) / 4
        else:
            op, sh, tg = num(r['warStageOpenEndFlag']), num(r['militarySpendingEndShare']), num(r['militaryStageTargetEndStage']) / 4
        if op != 1:
            return 0.0
        lv = sh if sh <= tg else tg
        v = (lv - 0.75) * 4
        return 0.0 if not v > 0 else (v if v < 1 else 1.0)

    print('\n== 戦時の借入の器（Military.WarStageShare＞0＝施行と目標が段3 を超えた解禁の四半期・四半期の頭と末）')
    for title, sel in [(nation, [r for r in R if r['nation'] == nation]), ('他国（プール）', [r for r in R if r['nation'] != nation])]:
        wd = [r for r in sel if num(r['atWarAtDecisionFlag']) == 1]
        if not wd:
            continue
        g = sum(1 for r in wd if ws_share(r, 'gov') > 0)
        e = sum(1 for r in wd if ws_share(r, 'end') > 0)
        print(f'  -- {title}：交戦中の判断 {len(wd)}・器が立つ四半期（頭）{g}（{g / len(wd) * 100:.2f}%）・（末）{e}（{e / len(wd) * 100:.2f}%）')

    print('\n== 戦時段のエピソード（同じ種・同じ国で warStageOpenAfterGovFlag=1 が続く四半期の塊）')
    ser = {}
    for r in R:
        ser.setdefault((r['_seed'], r['nation']), []).append(r)
    for title, pick in ((nation, lambda n: n == nation), ('他国（プール）', lambda n: n != nation)):
        eps = []
        for (sd, n), xs in ser.items():
            if not pick(n):
                continue
            xs.sort(key=lambda r: r['_q'])
            i = 0
            while i < len(xs):
                if num(xs[i]['warStageOpenAfterGovFlag']) == 1:
                    j = i
                    while j + 1 < len(xs) and num(xs[j + 1]['warStageOpenAfterGovFlag']) == 1 and xs[j + 1]['_q'] == xs[j]['_q'] + 1:
                        j += 1
                    seg = xs[i:j + 1]
                    reach = None
                    for k, r in enumerate(seg):
                        if num(r['militaryStageEnforcedAfterGovStage']) == 4 or num(r['militaryStageEnforcedEndStage']) == 4:
                            reach = k
                            break
                    nxt = xs[j + 1] if j + 1 < len(xs) and xs[j + 1]['_q'] == xs[j]['_q'] + 1 else None
                    if nxt is None:
                        end = '試合の終わり'
                    elif num(nxt['atWarAtDecisionFlag']) == 0:
                        end = '講和'
                    else:
                        end = '交戦中の復員'
                    eps.append((len(seg), reach, end, seg[0]['_y'], max(num(r['militarySpendingEndShare']) for r in seg)))
                    i = j + 1
                else:
                    i += 1
        if not eps:
            print(f'  -- {title}：エピソード 0')
            continue
        reached = [e for e in eps if e[1] is not None]
        print(f'  -- {title}：エピソード {len(eps)}・施行が段4へ届いた {len(reached)}（{len(reached) / len(eps) * 100:.1f}%）・届く前に終わった {len(eps) - len(reached)}')
        print(f'     長さ（四半期） {dist([float(e[0]) for e in eps])}・届いたものの届くまで(0＝解禁の最初の四半期) {dist([float(e[1]) for e in reached])}')
        print(f'     エピソード中の施行の水準の最大(末) {dist([e[4] for e in eps])}（段3＝0.75・段4＝1.0）')
        for lab in ('講和', '交戦中の復員', '試合の終わり'):
            sub = [e for e in eps if e[2] == lab]
            if sub:
                rr = sum(1 for e in sub if e[1] is not None)
                print(f'     終わり方「{lab}」{len(sub)}件（うち段4へ届いた {rr}）・長さ {dist([float(e[0]) for e in sub])}・施行の水準の最大 {dist([e[4] for e in sub])}')

    print('\n== 国ごとの 交戦中の判断の数 ÷ 戦時段の解禁の数（B-1＝解禁を交戦中にしたときの増え方・大国の上位）')
    cnt = {}
    for r in R:
        n = r['nation']
        c = cnt.setdefault(n, [0, 0, 0, 0])
        if num(r['atWarAtDecisionFlag']) == 1:
            c[0] += 1
            if num(r['mobilizedAtDecisionFlag']) == 0:
                c[2] += 1
            if num(r['warScaleRatioAtDecision']) == 0:
                c[3] += 1
        if num(r['warStageOpenAfterGovFlag']) == 1:
            c[1] += 1
    tot_w = sum(c[0] for c in cnt.values())
    tot_o = sum(c[1] for c in cnt.values())
    print(f'  全ての国：交戦中の判断 {tot_w}・解禁 {tot_o}・比 {tot_w / tot_o if tot_o else float("nan"):.2f}')
    others_w = sum(c[0] for n, c in cnt.items() if n != nation)
    others_o = sum(c[1] for n, c in cnt.items() if n != nation)
    print(f'  {nation}以外：交戦中の判断 {others_w}・解禁 {others_o}・比 {others_w / others_o if others_o else float("nan"):.2f}')
    # B-1 の形の反事実（読み手の側の数え）：解禁＝いま（総動員かつ交戦中）／交戦中ならいつでも／交戦中かつ戦争の規模が0でない（相手が本国へ届く）
    print('\n== 戦時段の解禁の形ごとの四半期の数（AI の判断の四半期・この木の観測からの数え＝反事実の効きは測っていない）')
    print(f'  {"国":<14}{"いま":>7}{"交戦中":>8}{"交戦中かつ規模>0":>14}{"倍(交戦中/いま)":>14}{"倍(規模>0/いま)":>14}')
    sums = {}
    for r in R:
        n = r['nation']
        c = sums.setdefault(n, [0, 0, 0])
        if num(r['warStageOpenAfterGovFlag']) == 1:
            c[0] += 1
        if num(r['atWarAtDecisionFlag']) == 1:
            c[1] += 1
            if num(r['warScaleRatioAtDecision']) > 0 or num(r['homeLosingScoreAtDecision']) > 0:
                c[2] += 1
    def row(lab, c):
        print(f'  {lab:<14}{c[0]:>7}{c[1]:>8}{c[2]:>14}{c[1] / c[0] if c[0] else float("nan"):>14.2f}{c[2] / c[0] if c[0] else float("nan"):>14.2f}')
    if nation in sums:
        row(nation, sums[nation])
    oth = [0, 0, 0]
    for n, c in sums.items():
        if n != nation:
            oth = [a + b for a, b in zip(oth, c)]
    row(nation + '以外(プール)', oth)
    big = sorted(((c[1] / c[0] if c[0] else float('inf')), n, c) for n, c in sums.items() if c[1] >= 200 and n != nation)
    over3 = [(m, n, c) for m, n, c in big if m > 3]
    print(f'  交戦中の判断が200以上の{nation}以外の国 {len(big)} のうち 交戦中/いま＞3 の国 {len(over3)}：' + '・'.join(f'{n}({m:.1f})' for m, n, c in over3))
    big2 = [(c[2] / c[0] if c[0] else float('inf'), n) for n, c in sums.items() if c[1] >= 200 and n != nation]
    over3b = [(m, n) for m, n in big2 if m > 3]
    print(f'  同 規模>0/いま＞3 の国 {len(over3b)}：' + '・'.join(f'{n}({m:.1f})' for m, n in sorted(over3b, reverse=True)))
    mult = []
    for n, c in cnt.items():
        if c[0] >= 20:
            mult.append((c[0] / c[1] if c[1] else float('inf'), n, c))
    mult.sort(reverse=True)
    print(f'  {"国":<14}{"交戦中":>7}{"解禁":>6}{"総動員なし":>10}{"規模0":>7}{"比":>8}')
    for m, n, c in mult[:12]:
        print(f'  {n:<14}{c[0]:>7}{c[1]:>6}{c[2]:>10}{c[3]:>7}{m:>8.2f}')
    c = cnt.get(nation)
    if c:
        print(f'  [{nation}] 交戦中 {c[0]}・解禁 {c[1]}・総動員なし {c[2]}・規模0 {c[3]}')


def main():
    D = sys.argv[1]
    which = sys.argv[2] if len(sys.argv) > 2 else 'all'
    nation = sys.argv[3] if len(sys.argv) > 3 else 'イギリス'
    R = load(D)
    if which in ('A', 'all'):
        part_a(R, nation)
    if which in ('B', 'all'):
        part_b(R, nation)


if __name__ == '__main__':
    main()
