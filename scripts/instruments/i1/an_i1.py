"""(300) 線 i1 段 0：希望を小さくしている項の読み手（読むだけ）。
  python an_i1.py <計器の出力ディレクトリ(ins)> <表の出力ディレクトリ>
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins の cons_s（希望を決める項の列つき）・nation_s（earnedTotalPerMonth）。
  窓（q は 0 始まり・表の Q は q+1）：Q5-40＝q4..39・Q41-160＝q40..159。本土の州＝tier が Colonial でない活きた州（an_d289.py と同じ）。
  出す物（t_stage0.md）：
   (a) 本土の州×四半期の中央値（国×窓）：sat・限界収益（技術の嵩上げ後）・技術の水準・クラウディングの項・金融の過熱の項・hurdle・netReturn・吸収の因子・株式会社の倍率・希望の束・希望の月数の割合。
   (b) 天井との比：netReturn の天井＝IndProductivityGain×(1+IndTechLeverage×techLev)−IndCapitalCost（sat=1・クラウディング0・過熱0）・希望の束の天井＝IndInvestmentSpeed×JointStockInvestBonus×天井の netReturn÷IndPerConstruction（吸収の因子=1）。
       実際の希望÷天井を、因子ごと（sat・hurdle・吸収・株式会社）の寄与の対数（自然対数）に分ける。
   (c) 国の希望（本土の州の希望の金額の和）÷国の所得（nation_s の earnedTotalPerMonth）と、天井のときの同じ量。史実の粗投資÷国民所得 6〜11% との比較。植民地格の州の同じ量も併記。
   (d) 英の本土の州ごとの表（Q41-160 の平均）。
  限界の自己申告：①係数の値は Coefficients.cs の既定（読み手の定数）＝POB_COEF で動かした腕では天井がずれる ②希望の金額は 月あたり ③中央値は (種, 州, 四半期) の行の中央 ④希望の月数の割合は判定に来た月のうち希望が正の月。"""
import sys, os, statistics, collections, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
from instr import num as fl, seeds  # noqa: E402

D, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
SEEDS = seeds(D, 'cons')
NATIONS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
WIN = {'Q5-40': (4, 39), 'Q41-160': (40, 159)}
# Coefficients.cs の既定（2026-10-13 の木）
GAIN0, LEV, HURDLE0, SPEED, JS_MAX, PERCON = 1.2, 2.4, 0.7, 0.012, 1.6, 0.01


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def f3(x, nd=3):
    return '—' if x != x else f'{x:.{nd}f}'


def table(path):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        for ln in fh:
            yield dict(zip(head, ln.rstrip('\n').split('\t')))


COLS = ['gainWithTechMean', 'satMean', 'techLevelMean', 'crowdingPremiumMean', 'financialHeatMean', 'hurdleMean', 'netReturnMean', 'absorbFactorMean', 'jointStockFactorMean', 'desiredUnitsMean', 'unitMoneyMean',
        'desiredMoneyPerMonth', 'plannedMoneyPerMonth', 'budgetMoneyPerMonth', 'paidPerMonth', 'builtUnitsPerMonth', 'investorEarnedPerMonth', 'investorSavingsStock', 'populationK', 'productiveIndustrializationIndex',
        'industrializationIndex', 'termMonthsCount', 'desiringMonthsCount']
CONS = collections.defaultdict(lambda: collections.defaultdict(list))  # (sd) -> nation -> rows
for sd in SEEDS:
    for r in table(os.path.join(D, f'cons_s{sd}.tsv')):
        if r['nation'] not in NATIONS or r['activeFlag'] != '1':
            continue
        row = {'q': int(r['quarter']), 'region': r['region'], 'colonial': r['tier'] == 'Colonial'}
        for c in COLS:
            row[c] = fl(r.get(c, 'nan'))
        CONS[sd][r['nation']].append(row)
EARN = collections.defaultdict(dict)  # (sd, nation) -> q -> earnedTotalPerMonth
for sd in seeds(D, 'nation'):
    for r in table(os.path.join(D, f'nation_s{sd}.tsv')):
        if r['nation'] in NATIONS:
            EARN[(sd, r['nation'])][int(r['quarter'])] = fl(r['earnedTotalPerMonth'])

md = ['# T-S0 段 0：希望を決める項（読むだけ・' + D + '・種 ' + ','.join(map(str, SEEDS)) + '）', '',
      '■ 本土の州＝tier が Colonial でない活きた州。表は (種, 州, 四半期) の行の中央値。希望の束は月あたり。',
      '■ 天井＝sat=1・クラウディング0・過熱0・吸収1・株式会社 1.6 のときの値（技術の水準は行の値）。', '']
for win, (a, b) in WIN.items():
    md += [f'## 窓 {win}', '']
    # (a)(b)
    md += ['### (a) 本土の州×四半期の中央値', '', '| 国 | 行数 | sat | 限界収益(技術後) | 技術の水準 | クラウディング項 | 過熱項 | hurdle | netReturn | 吸収 | 株式会社 | 希望の束/月 | 希望の月の割合 |', '|---|---|---|---|---|---|---|---|---|---|---|---|---|']
    ratios = {}
    for n in NATIONS:
        rows = [r for sd in SEEDS for r in CONS[sd][n] if a <= r['q'] <= b and not r['colonial'] and r['termMonthsCount'] > 0]
        if not rows:
            md.append(f'| {n} | 0 | — |'); continue
        g = lambda k: [r[k] for r in rows]
        share = [r['desiringMonthsCount'] / r['termMonthsCount'] for r in rows]
        md.append(f'| {n} | {len(rows)} | {f3(med(g("satMean")))} | {f3(med(g("gainWithTechMean")))} | {f3(med(g("techLevelMean")))} | {f3(med(g("crowdingPremiumMean")),4)} | {f3(med(g("financialHeatMean")),4)} | {f3(med(g("hurdleMean")))} | {f3(med(g("netReturnMean")))} | {f3(med(g("absorbFactorMean")))} | {f3(med(g("jointStockFactorMean")))} | {f3(med(g("desiredUnitsMean")),2)} | {f3(med(share))} |')
        # (b) 天井との比（行ごとに対数の寄与を出して中央）
        cl = collections.defaultdict(list)
        for r in rows:
            tech = r['techLevelMean']; ceil_gain = GAIN0 * (1 + LEV * tech); ceil_net = ceil_gain - HURDLE0
            if not (ceil_net > 0):
                continue
            ceil_units = SPEED * JS_MAX * ceil_net / PERCON
            u = r['desiredUnitsMean']
            cl['units/ceil'].append(u / ceil_units)
            cl['sat^-2 (限界収益の逓減)'].append(1.0 / (r['satMean'] ** 2))
            cl['(gain-hurdle)/ceilNet'].append(max(0.0, r['netReturnMean']) / ceil_net)
            cl['hurdle/0.7'].append(r['hurdleMean'] / HURDLE0)
            cl['吸収'].append(r['absorbFactorMean'])
            cl['株式会社/1.6'].append(r['jointStockFactorMean'] / JS_MAX)
            cl['希望が正の行の割合'].append(1.0 if u > 0 else 0.0)
            cl['希望(束)'].append(u); cl['天井の希望(束)'].append(ceil_units)
        ratios[n] = {k: med(v) for k, v in cl.items()}
    md += ['', '### (b) 天井との比（本土の州×四半期の行ごとの比の中央値）', '', '| 国 | 実際の希望/天井 | 1/sat² | netReturn/天井のnet | hurdle/0.7 | 吸収 | 株式会社/1.6 | 希望が正の行の割合 | 希望(束) | 天井の希望(束) |', '|---|---|---|---|---|---|---|---|---|---|']
    for n in NATIONS:
        r = ratios.get(n)
        if not r:
            md.append(f'| {n} | — |'); continue
        md.append(f'| {n} | {f3(r["units/ceil"])} | {f3(r["sat^-2 (限界収益の逓減)"])} | {f3(r["(gain-hurdle)/ceilNet"])} | {f3(r["hurdle/0.7"])} | {f3(r["吸収"])} | {f3(r["株式会社/1.6"])} | {f3(r["希望が正の行の割合"])} | {f3(r["希望(束)"],2)} | {f3(r["天井の希望(束)"],2)} |')
    # (c) 国の希望÷所得
    md += ['', '### (c) 国の希望（州の希望の金額の和）÷国の所得（種ごとに窓の月で足してから中央値・括弧は最小〜最大）', '', '| 国 | 本土の希望÷所得 | 本土の天井の希望÷所得 | 本土の計画÷所得 | 本土の払い÷所得 | 植民地格の希望÷所得 | 植民地格の枠÷所得 | 植民地格の払い÷所得 | 本土の州の数 | 植民地格の州の数 |', '|---|---|---|---|---|---|---|---|---|---|']
    for n in NATIONS:
        res = collections.defaultdict(list); nh = set(); nc = set()
        for sd in SEEDS:
            acc = collections.defaultdict(float); inc = 0.0
            qs = {}
            for r in CONS[sd][n]:
                if a <= r['q'] <= b:
                    k = 'c' if r['colonial'] else 'h'
                    (nc if r['colonial'] else nh).add(r['region'])
                    if r['desiredMoneyPerMonth'] == r['desiredMoneyPerMonth']: acc[k + 'des'] += r['desiredMoneyPerMonth']
                    if r['plannedMoneyPerMonth'] == r['plannedMoneyPerMonth']: acc[k + 'plan'] += r['plannedMoneyPerMonth']
                    if r['budgetMoneyPerMonth'] == r['budgetMoneyPerMonth']: acc[k + 'bud'] += r['budgetMoneyPerMonth']
                    if r['paidPerMonth'] == r['paidPerMonth']: acc[k + 'paid'] += r['paidPerMonth']
                    if k == 'h' and r['termMonthsCount'] > 0:
                        tech = r['techLevelMean']; cn = GAIN0 * (1 + LEV * tech) - HURDLE0
                        if cn > 0 and r['unitMoneyMean'] == r['unitMoneyMean']:
                            acc['hceil'] += SPEED * JS_MAX * cn / PERCON * r['unitMoneyMean']
            for q in range(a, b + 1):
                e = EARN[(sd, n)].get(q)
                if e == e and e is not None: inc += e
            if inc > 0:
                for k in ('hdes', 'hceil', 'hplan', 'hpaid', 'cdes', 'cbud', 'cpaid'):
                    res[k].append(acc[k] / inc)

        def c(k):
            xs = res[k]
            return '—' if not xs else f'{100*med(xs):.2f}% ({100*min(xs):.2f}〜{100*max(xs):.2f})'
        md.append(f'| {n} | {c("hdes")} | {c("hceil")} | {c("hplan")} | {c("hpaid")} | {c("cdes")} | {c("cbud")} | {c("cpaid")} | {len(nh)} | {len(nc)} |')
    md.append('')
# (d) 英の本土の州ごとの表
md += ['## (d) 英の本土の州ごとの表（Q41-160・活きた四半期の平均・種の合算）', '', '| 州 | 人口(千) | 生産的工業化度 | sat | 限界収益(技術後) | hurdle | netReturn | 希望の束/月 | 希望の金額/月 | 投資家の所得/月 | 希望÷投資家の所得 | 払い/月 |', '|---|---|---|---|---|---|---|---|---|---|---|---|']
byreg = collections.defaultdict(lambda: collections.defaultdict(list))
for sd in SEEDS:
    for r in CONS[sd]['イギリス']:
        if 40 <= r['q'] <= 159 and not r['colonial']:
            for k in COLS:
                if r[k] == r[k]:
                    byreg[r['region']][k].append(r[k])
for reg, d in sorted(byreg.items(), key=lambda kv: -statistics.mean(kv[1]['populationK']) if kv[1]['populationK'] else 0):
    m = lambda k: statistics.mean(d[k]) if d[k] else float('nan')
    ie = m('investorEarnedPerMonth')
    md.append(f'| {reg} | {f3(m("populationK"),0)} | {f3(m("productiveIndustrializationIndex"),2)} | {f3(m("satMean"))} | {f3(m("gainWithTechMean"))} | {f3(m("hurdleMean"))} | {f3(m("netReturnMean"))} | {f3(m("desiredUnitsMean"),2)} | {f3(m("desiredMoneyPerMonth"),0)} | {f3(ie,0)} | {f3(m("desiredMoneyPerMonth")/ie if ie==ie and ie>0 else float("nan"))} | {f3(m("paidPerMonth"),0)} |')
open(os.path.join(OUT, 't_stage0.md'), 'w', encoding='utf-8').write('\n'.join(md) + '\n')
print('\n'.join(md))
