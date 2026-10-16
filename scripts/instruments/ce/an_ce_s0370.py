#!/usr/bin/env python
"""(370) 線 ce5 段0：消費の入口〈使い残しの根〉の読み手（読むだけ・計器の出力 ins を読む）。
  python an_ce_s0370.py <ins のフォルダ> [種の数=10]
  入力：INS=1 で回した旗 1（ConsumptionLayersEnabled=1）の ce_s／flow_s／pxgoods_s／region_s（NATIONS に英・仏・蘭・葡を挙げた走行は ce_s の C 行に全 61 カ国が載る）。
  出す表（国×階級＝英・仏・清の農民と工場労働者・Q5〜Q40・種の和）：
    T1 通常の四半期と塊の四半期（その種・国・階級の E÷人口 の中央値の 1.5 倍で分ける）の収支：cons÷E・tax÷E・food÷E・daily÷E・lux÷E・cap2÷E・dailyPaid÷cap2・dailyWant÷E・dailySup÷E・日用品の充足。
    T2 Y（＝E＋移転−税）で割った収支と、在庫切れの食料の控え（foodUnfilledHeld）・設計 doc の層2 の予算の影（shadowDailyBudget）・上限の実際（layerCap2）。
    T3 籠の費用÷市場所得：食料の供給後の籠÷E・（食料＋日用品）の供給後の籠÷E・貯蓄÷E（月数）。
    T4 フランスの主食の不足額（不足の量×値段）÷主食の販売額（財ごと）・大衆の E との比。
    T5 英の工場労働者の塊の出どころ：ce_s の E（Q0〜Q4）と flow_s の giveIncome の位置別の額（同じ四半期・翌四半期にずれて載る）。
  ⚠限界：ce_s は国×階級（州×階級でない）＝塊が落ちる州の POP だけの様子は見えない。PerMonth は四半期の月平均（塊は 1 か月に落ちて 1/3 に薄まる）。月ごとの行は無い。
  四半期の番号は 0 始まり（年＝1783＋Q÷4 の商）。"""
import sys, os, csv, collections, statistics

sys.stdout.reconfigure(encoding='utf-8')
D = sys.argv[1].rstrip('/\\') + '/'
NS = int(sys.argv[2]) if len(sys.argv) > 2 else 10
SEEDS = range(1, NS + 1)
Q0, Q1 = 5, 40
NATS = ['イギリス', 'フランス', '清']
CLS = ['農民', '工場労働者']


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        yield from csv.DictReader(f, delimiter='\t')


HC = ['popK', 'earnedLastPositivePerMonth', 'transferInPerMonth', 'taxPaidPerMonth', 'foodWantPerMonth', 'foodSuppliedWantPerMonth', 'foodPaidPerMonth',
      'foodUnfilledHeldPerMonth', 'dailyWantPerMonth', 'dailySuppliedWantPerMonth', 'dailyPaidPerMonth', 'layerCap2PerMonth', 'shadowDailyBudgetPerMonth',
      'luxuryPaidPerMonth', 'consumptionPaidPerMonth', 'savingsStartStock', 'savingsDrawPerMonth']
rows = collections.defaultdict(list)   # (nation,class) -> [(seed,q,{h:v})]
for s in SEEDS:
    for r in rd(D + f'ce_s{s}.tsv'):
        if r['scope'] == 'C' and r['nation'] in NATS and Q0 <= int(r['quarter']) <= Q1 and r['className'] in CLS:
            rows[(r['nation'], r['className'])].append((s, int(r['quarter']), {h: fl(r[h]) for h in HC}))


def S(sel, h):
    return sum(x[2][h] for x in sel)


print('## T1 通常の四半期と塊の四半期（E÷人口 が その種・国・階級の中央値の 1.5 倍以下を〈通常〉）  Q%d〜Q%d・種 1〜%d の和' % (Q0, Q1, NS))
for k in sorted(rows):
    by = collections.defaultdict(list)
    for s, q, h in rows[k]:
        by[s].append(h['earnedLastPositivePerMonth'] / h['popK'] if h['popK'] > 0 else 0)
    med = {s: statistics.median(v) for s, v in by.items()}
    for nm, f in (('通常', lambda s, h: h['earnedLastPositivePerMonth'] / max(h['popK'], 1e-9) <= 1.5 * med[s]),
                  ('塊', lambda s, h: h['earnedLastPositivePerMonth'] / max(h['popK'], 1e-9) > 1.5 * med[s])):
        sel = [x for x in rows[k] if f(x[0], x[2])]
        if not sel:
            continue
        E = S(sel, 'earnedLastPositivePerMonth')
        allE = S(rows[k], 'earnedLastPositivePerMonth')
        print(f"{k[0]} {k[1]} {nm} n={len(sel):3d} Eの割合={E/allE:4.2f} | cons/E={S(sel,'consumptionPaidPerMonth')/E:5.3f} tax/E={S(sel,'taxPaidPerMonth')/E:5.3f} "
              f"food/E={S(sel,'foodPaidPerMonth')/E:5.3f} daily/E={S(sel,'dailyPaidPerMonth')/E:5.3f} lux/E={S(sel,'luxuryPaidPerMonth')/E:5.3f} | "
              f"cap2/E={S(sel,'layerCap2PerMonth')/E:5.3f} dailyPaid/cap2={S(sel,'dailyPaidPerMonth')/max(S(sel,'layerCap2PerMonth'),1e-9):5.3f} | "
              f"dailyWant/E={S(sel,'dailyWantPerMonth')/E:6.2f} dailySup/E={S(sel,'dailySuppliedWantPerMonth')/E:5.2f} 日用品の充足={S(sel,'dailyPaidPerMonth')/S(sel,'dailyWantPerMonth'):5.3f}")

print('\n## T2 Y＝E＋移転−税 で割った収支・在庫切れの食料の控え・層2 の上限の実際と設計 doc の式の影（全ての四半期の和）')
for k in sorted(rows):
    sel = rows[k]
    E = S(sel, 'earnedLastPositivePerMonth')
    Y = E + S(sel, 'transferInPerMonth') - S(sel, 'taxPaidPerMonth')
    print(f"{k[0]} {k[1]} Y/E={Y/E:5.3f} | foodPaid/Y={S(sel,'foodPaidPerMonth')/Y:5.3f} foodUnfilledHeld/Y={S(sel,'foodUnfilledHeldPerMonth')/Y:6.3f} | "
          f"layerCap2/Y={S(sel,'layerCap2PerMonth')/Y:5.3f} shadowDailyBudget/Y={S(sel,'shadowDailyBudgetPerMonth')/Y:5.3f} dailyPaid/Y={S(sel,'dailyPaidPerMonth')/Y:5.3f} cons/Y={S(sel,'consumptionPaidPerMonth')/Y:5.3f}")

print('\n## T3 籠の費用（供給後・買い手の値段）÷市場所得 E と貯蓄の月数')
for k in sorted(rows):
    sel = rows[k]
    E = S(sel, 'earnedLastPositivePerMonth')
    print(f"{k[0]} {k[1]} foodSup/E={S(sel,'foodSuppliedWantPerMonth')/E:5.2f} (food+daily)Sup/E={(S(sel,'foodSuppliedWantPerMonth')+S(sel,'dailySuppliedWantPerMonth'))/E:5.2f} "
          f"貯蓄/E(月数)={S(sel,'savingsStartStock')/E:6.1f} savingsDraw/E={S(sel,'savingsDrawPerMonth')/E:5.2f}")

print('\n## T4 フランスの主食：不足の量×値段 ÷ 主食の販売額（財ごと・Q%d〜Q%d・全種）' % (Q0, Q1))
res = collections.defaultdict(lambda: collections.defaultdict(float))
for s in SEEDS:
    for r in rd(D + f'pxgoods_s{s}.tsv'):
        t = r['time']
        if r['nation'] == 'フランス' and t.startswith('q') and Q0 <= int(t[1:]) <= Q1 and r['tier'] == 'Staple':
            g = r['goodName']; p = fl(r['priceMean'])
            res[g]['short'] += fl(r['lastShortageQty']) * p; res[g]['sold'] += fl(r['lastSoldQty']) * p; res[g]['dem'] += fl(r['lastDemandQty']) * p
tot = sum(v['sold'] for v in res.values())
for g, v in sorted(res.items(), key=lambda x: -x[1]['sold']):
    print(f"  {g}: 販売額の割合 {v['sold']/tot:5.3f}  不足額÷主食の販売額の総和 {v['short']/tot:5.3f}  需要額の割合 {v['dem']/max(sum(x['dem'] for x in res.values()),1e-9):5.3f}")
print(f"  不足額の総和÷主食の販売額の総和 = {sum(v['short'] for v in res.values())/tot:5.3f}   （⚠分母は全階級・全州の販売額〔値段は州の平均〕＝大衆の E との比は取らない）")

print('\n## T5 英の工場労働者の塊の出どころ（種 1・Q0〜Q4）：ce_s の E と flow_s の giveIncome の位置別の額（月平均）')
for r in rd(D + 'ce_s1.tsv'):
    if r['scope'] == 'C' and r['nation'] == 'イギリス' and r['className'] == '工場労働者' and int(r['quarter']) <= 4:
        print(f"  ce_s Q{r['quarter']}: popK={fl(r['popK']):.0f} E={fl(r['earnedLastPositivePerMonth']):.0f} (E÷人口 {fl(r['earnedLastPositivePerMonth'])/fl(r['popK']):.2f})")
for r in rd(D + 'flow_s1.tsv'):
    if r['nation'] == 'イギリス' and r['classId'] == '2' and r['kind'] == 'giveIncome' and int(r['quarter']) <= 4 and abs(fl(r['amountPerMonth'])) > 100:
        print(f"  flow_s Q{r['quarter']}: giveIncome {r['sourceFile']}:{r['sourceLine']} 相手={r['counterpartySide']} {fl(r['amountPerMonth']):.0f}/月 件数={r['eventCount']}")

print('\n## T6 四半期ごとの並び（種 1・Q0〜Q12・月平均）：Y・食料の支払い・層2 の上限・日用品の支払い・奢侈の支払い・予算3・消費の支払い・Earned の塊（月ごとの行は計器に無い＝四半期の月平均）')
for nat in NATS:
    for cls in CLS:
        print(f'  [{nat} {cls}]')
        for r in rd(D + 'ce_s1.tsv'):
            if r['scope'] == 'C' and r['nation'] == nat and r['className'] == cls and int(r['quarter']) <= 12:
                E, T, X, P = fl(r['earnedLastPositivePerMonth']), fl(r['transferInPerMonth']), fl(r['taxPaidPerMonth']), fl(r['popK'])
                print(f"    Q{r['quarter']:>2}: popK={P:6.0f} E={E:8.0f}(E÷人口 {E/P if P else 0:6.2f}) Y≈{E+T-X:8.0f} 食料{fl(r['foodPaidPerMonth']):7.0f} 控え{fl(r['foodUnfilledHeldPerMonth']):7.0f} 上限{fl(r['layerCap2PerMonth']):8.0f} "
                      f"日用品{fl(r['dailyPaidPerMonth']):6.0f} 奢侈{fl(r['luxuryPaidPerMonth']):6.0f} 予算3 {fl(r['layerBud3PerMonth']):7.0f} 消費{fl(r['consumptionPaidPerMonth']):7.0f} 日用品の欲しい量{fl(r['dailyWantPerMonth']):7.0f}")
