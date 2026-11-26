# (1396) c148dm 問い 1・2：同盟の参戦の国難の項を生で数える（読むだけ）。
#   (1) 支払い率の項（Diplomacy.cs:1618 `!(FiscalFundingRatio >= AllyJoinFundingMin 0.6)`）が立つ数
#       ・nation の payRateMeanShare（全ての国・四半期の平均）と、fiscal の payRateShare（9 国・月の値＝Simulation.cs の FiscalFundingRatio に書く payRate と同じ値）
#   (2) 盾：wars の entryCode=main（TriggerWars の開戦・q＝始まりの四半期）のうち、どちらかが国難の物を 3 つの述語で
#       P4＝今の 4 項から厭戦を除いた物（内戦・食 < 0.7・支払い率 < 0.6・正統性 < 0.4）／Ta＝内戦 ∨ 支払い率／Tc＝内戦だけ
#   (3) 代わりの物差しの雑音：entryCode=ally の参戦国（sideA）は、今の規則では参戦の月に国難でない。四半期の値で国難に見える数＝物差しのずれ
#   (4) entryCode=suzerain の宗主（sideA）が、その四半期に内戦だった数（旗 1 で :1551 が新しく塞ぐ数の目安〈推〉）
#   使い方: python crisis.py [生のフォルダ]
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from load import rows, fl, SEEDS, BASE

base = sys.argv[1] if len(sys.argv) > 1 else BASE
FOOD_MIN, PAY_MIN, LEG_MIN = 0.7, 0.6, 0.4   # Coefficients.p06_rank_discovery.cs:41〜:43 の既定値（読むだけ）

T = collections.Counter()
for s in SEEDS:
    leg = {(int(v['quarter']), v['nation']): fl(v['legitimacyIndex']) for v in rows('crownpc', s, base)}
    st = {}
    for v in rows('nation', s, base):
        q = int(v['quarter']); nm = v['nation']
        civ = fl(v['civilWarMonths']) > 0
        food = not (fl(v['foodMetShare']) >= FOOD_MIN)
        payv = fl(v['payRateMeanShare'])
        pay = not (payv >= PAY_MIN)
        L = leg.get((q, nm), float('nan'))
        lg = not (L >= LEG_MIN)
        war = fl(v['atWarMonths']) > 0
        st[(q, nm)] = (civ, food, pay, lg)
        T['n_rows'] += 1; T['n_war'] += war
        T['pay_q'] += pay; T['pay_q_nan'] += payv != payv; T['pay_q_war'] += pay and war
        T['pay_q_and_civ'] += pay and civ; T['civ_q'] += civ; T['civ_q_war'] += civ and war
        T['leg_nan'] += L != L
    # 月の支払い率（9 国）
    qmon = collections.defaultdict(list)
    for v in rows('fiscal', s, base):
        m = int(v['monthIndex']); r = fl(v['payRateShare'])
        qmon[(m // 3, v['nation'])].append(r)
        T['m_rows'] += 1; T['m_pay'] += not (r >= PAY_MIN); T['m_nan'] += r != r
    for (q, nm), a in qmon.items():
        T['m9_q'] += 1
        T['m9_q_anymonth'] += any(not (r >= PAY_MIN) for r in a)
        if (q, nm) in st:
            T['m9_q_meanrow'] += st[(q, nm)][2]
    for v in rows('wars', s, base):
        e = v['entryCode']; q = int(v['q']); A, B = v['sideA'], v['sideB']
        if e == 'main':
            T['main'] += 1
            for lag in (0, 1):
                a, b = st.get((q - lag, A)), st.get((q - lag, B))
                if a is None or b is None:
                    T[f'main_unknown_lag{lag}'] += 1; continue
                for nm_, f in (('P4', lambda x: any(x)), ('Ta', lambda x: x[0] or x[2]), ('Tc', lambda x: x[0])):
                    T[f'main_{nm_}_any_lag{lag}'] += f(a) or f(b); T[f'main_{nm_}_both_lag{lag}'] += f(a) and f(b)
                if lag == 0:
                    for i, k in enumerate(('civ', 'food', 'pay', 'leg')):
                        T[f'main_term_{k}'] += a[i] or b[i]
        elif e in ('ally', 'suzerain'):
            a = st.get((q, A))
            T[e] += 1
            if a is None:
                T[f'{e}_unknown'] += 1; continue
            for i, k in enumerate(('civ', 'food', 'pay', 'leg')):
                T[f'{e}_{k}'] += a[i]
            T[f'{e}_P4'] += any(a)

print(f'== {base}')
for k in sorted(T):
    print(f'{k}\t{T[k]}')
