# (836) 追加の読み（事前登録の外＝結果を見た後に足した内訳・採否や結論の規則には使わない・〈追加〉と名乗る）
# ① R1 のほぼ 0 の POP の内訳（階級・人数が厳密に 0 か・窓の流れ）② R2 を入りの後 8 四半期（onsetQ〜onsetQ+7）でも同じ述語で数える ③ 売上ありで所得 0 の行の売上と費用
import csv, sys, os, math
from collections import defaultdict
RAW, EV, CT, OUT = sys.argv[1:5]
csv.field_size_limit(10**9)
def fl(x):
    try: return float(x)
    except: return float('nan')
events = list(csv.DictReader(open(EV, encoding='utf-8'), delimiter='\t'))
controls = list(csv.DictReader(open(CT, encoding='utf-8'), delimiter='\t'))
ctl = defaultdict(list)
for c in controls: ctl[(c['seed'], c['event'])].append(c['control'])
need = defaultdict(set)
for e in events:
    oq = int(e['onsetQ'])
    for r in [e['region']] + ctl[(e['seed'], e['region'])]:
        for q in range(oq - 8, oq + 8): need[e['seed']].add((r, q))
rows = {}; cearn = {}
for s in sorted(need, key=int):
    with open(os.path.join(RAW, f'cpop_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['region'], int(r['quarter']))
            if k in need[s]: rows.setdefault((s,) + k, []).append(r)
    with open(os.path.join(RAW, f'cearn_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['region'], int(r['quarter']))
            if k in need[s]: cearn[(s,) + k] = r
INC = ['inMarketPerQuarter', 'inExportPerQuarter', 'inStatePerQuarter', 'inCreditPerQuarter', 'inFromPopsPerQuarter', 'inMigrationPerQuarter', 'inOtherPerQuarter']
OUTC = ['outMarketPerQuarter', 'outTaxPerQuarter', 'outBondPerQuarter', 'outDuesPerQuarter', 'outColonialRentPerQuarter', 'outCapitalPerQuarter', 'outImportPerQuarter', 'outMigrationPerQuarter', 'outOtherPerQuarter']
L = ['(836) 追加の読み（事前登録の外・結果を見た後に足した内訳＝記録・結論の規則には使わない）', '']
# ① ほぼ 0 の POP の内訳（出来事の窓＝入りの前 8 四半期）
cls = defaultdict(lambda: defaultdict(float))
for side in ('event', 'control'):
    for e in events:
        oq = int(e['onsetQ'])
        regs = [e['region']] if side == 'event' else ctl[(e['seed'], e['region'])]
        for reg in regs:
            for q in range(oq - 8, oq):
                rs = [r for r in rows.get((e['seed'], reg, q), []) if r['nation'] == e['nation']]
                tot = sum(fl(r['popK']) for r in rs)
                if not (tot > 0): continue
                for r in rs:
                    if fl(r['popK']) / tot >= 0.01: continue
                    d = cls[(side, r['className'])]
                    d['n'] += 1; d['sav'] += fl(r['savings']); d['zero'] += fl(r['popK']) == 0
                    d['popK'] += fl(r['popK'])
                    for c in INC: d['in'] += fl(r[c])
                    for c in OUTC: d['out'] += fl(r[c])
                    d['inMarket'] += fl(r['inMarketPerQuarter']); d['outMarket'] += fl(r['outMarketPerQuarter'])
                    d['claimM'] += int(r['claimedMonths']); d['months'] += int(r['monthsClosed'])
for side in ('event', 'control'):
    tot = sum(v['sav'] for (s, c), v in cls.items() if s == side)
    L.append(f'① ほぼ 0 の POP の階級（{side}・入りの前 8 四半期・州の人数の 1% 未満）：在庫の割合（ほぼ 0 の POP の在庫の和に対して）・POP×四半期・人数が厳密に 0 の割合・人数の平均・四半期の流入と流出の平均・精算の所得の平均・請求の立つ月の割合')
    for (s, c), v in sorted(cls.items(), key=lambda x: -x[1]['sav']):
        if s != side: continue
        L.append(f"   {c}：{v['sav'] / tot if tot > 0 else float('nan'):.3f}・{v['n']:.0f}・{v['zero'] / v['n']:.2f}・{v['popK'] / v['n']:.3g}・流入 {v['in'] / v['n']:.4g}・流出 {v['out'] / v['n']:.4g}・精算の所得 {v['inMarket'] / v['n']:.4g}・請求 {v['claimM'] / max(1, v['months']):.2f}")
L.append('')
# ② R2 を入りの後 8 四半期でも（同じ述語）
R2K = ['行なし', '能力0', '採算0', '望むが産出0', '産出が正で売上0', '売上あり']
for win, lo, hi in (('入りの前 8 四半期', -8, 0), ('入りの後 8 四半期', 0, 8)):
    for side in ('event', 'control'):
        t = defaultdict(float); fl0 = defaultdict(float); cl = defaultdict(float); gd = defaultdict(float); lossy = defaultdict(float)
        for e in events:
            oq = int(e['onsetQ'])
            regs = [e['region']] if side == 'event' else ctl[(e['seed'], e['region'])]
            for reg in regs:
                for q in range(oq + lo, oq + hi):
                    rs = [r for r in rows.get((e['seed'], reg, q), []) if r['nation'] == e['nation']]
                    tot = sum(fl(r['popK']) for r in rs)
                    if not (tot > 0): continue
                    ce = cearn.get((e['seed'], reg, q))
                    for r in rs:
                        if fl(r['popK']) / tot < 0.01: continue
                        st = fl(r['savingsStart'])
                        if fl(r['earnedSettlePerQuarter']) > 1e-9 * max(1.0, abs(st) if not math.isnan(st) else 1.0): t['所得が正'] += 1; continue
                        if int(r['laborLines']) == 0: k = 0
                        elif not (fl(r['laborCapacity']) > 0): k = 1
                        elif not (fl(r['laborDesiredOutput']) > 0): k = 2
                        elif not (fl(r['laborOutput']) > 0): k = 3
                        elif not (fl(r['laborRevenuePerMonth']) > 0): k = 4
                        else: k = 5
                        t[R2K[k]] += 1; cl[r['className']] += 1; gd[r['topLaborGood']] += 1
                        if k == 2: fl0['床' if ce is not None and ce['localPriceAtFloorFlag'] == '1' else '床でない'] += 1
                        if k == 5: lossy['最大の行の売上0' if not (fl(r['topLaborRevenuePerMonth']) > 0) else ('費用≧売上＞0' if fl(r['topLaborCostPerMonth']) >= fl(r['topLaborRevenuePerMonth']) else '費用＜売上')] += 1
        n0 = sum(t[k] for k in R2K)
        L.append(f"② R2（{win}・{side}）：所得が正 {t['所得が正']:.0f}・所得 0 {n0:.0f}＝" + '・'.join(f'{k} {t[k]:.0f}' for k in R2K) + f"・採算0 のうち床 {fl0['床']:.0f}／床でない {fl0['床でない']:.0f}・売上ありの最大の行 " + '・'.join(f'{k} {v:.0f}' for k, v in lossy.items()))
        L.append('    階級：' + '・'.join(f'{k} {v:.0f}' for k, v in sorted(cl.items(), key=lambda x: -x[1])[:8]) + '／最大の賃金の行の財：' + '・'.join(f'{k} {v:.0f}' for k, v in sorted(gd.items(), key=lambda x: -x[1])[:8]))
open(os.path.join(OUT, 'extra.txt'), 'w', encoding='utf-8', newline='\n').write('\n'.join(L) + '\n')
