# (1318) c137ak 計器 sectreas_s の読み手（自分で集計する・エンジンの関数を呼ばない）。
#   門 G1（実際の側・⑮）：全ての行が byMoneyFlag 1 で actualT＝newRuleT（相対 1e-9）・行が 1 以上・表の欠け 0 で緑（旗 0 の生は byMoneyFlag 0＝赤）。byMoneyFlag 0 の行は actualT＝oldRuleT を食い違いに数える。
#   記録：分離の回数・新÷旧の分与の分布（oldRuleT＞0 の行）・比が 1 を超える行（shareOverOneFlag）・史実の桁の量＝生まれの国庫÷移る州の民間の貨幣（actualT÷movedMoney・movedMoney＞0）・
#         生まれの民間÷国庫 < 0.1 の行の数・移る州の負の貯蓄が 0 でない行の数。
#   使い方: python -X utf8 -I st_gate.py <生のフォルダ> <種の最初> <種の数>
import sys, csv, os, statistics
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def near(a, b): return abs(a - b) <= 1e-9 * max(1.0, abs(a))
def q(v, p):
    if not v: return float('nan')
    v = sorted(v); return v[min(len(v) - 1, int(p * (len(v) - 1) + 0.5))]
missing = 0; rows = 0; mism = 0; over = 0; neg = 0; low = 0; ratios = []; mag = []; flags = set()
print('seed\trows\tmismatch\tshareOverOne\tnegSavingsRows\tprivOverT_lt0.1')
for s in range(s0, s0 + n):
    p = os.path.join(raw, f'sectreas_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(sectreas_s が無い)'); missing += 1; continue
    c = [0, 0, 0, 0, 0]
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            g = lambda k: fl(row[ix[k]])
            c[0] += 1; fm = row[ix['byMoneyFlag']]; flags.add(fm)
            want = g('newRuleT') if fm == '1' else g('oldRuleT')
            if not near(want, g('actualT')): c[1] += 1
            if row[ix['shareOverOneFlag']] == '1': c[2] += 1
            if g('movedNegSavings') < 0: c[3] += 1
            o, nw, a, mm = g('oldRuleT'), g('newRuleT'), g('actualT'), g('movedMoney')
            if o > 0: ratios.append(nw / o)
            if mm > 0 and a > 0:
                mag.append(a / mm)
                if mm / a < 0.1: c[4] += 1
    rows += c[0]; mism += c[1]; over += c[2]; neg += c[3]; low += c[4]
    print(f'{s}\t' + '\t'.join(map(str, c)))
print(f'# 合計 rows={rows} mismatch={mism} shareOverOne={over} negSavingsRows={neg} privOverT_lt0.1={low} missing={missing} byMoneyFlag={sorted(flags)}')
print(f'# 新÷旧（oldRuleT>0・{len(ratios)} 行） p10={q(ratios,0.1):.4g} 中央={q(ratios,0.5):.4g} p90={q(ratios,0.9):.4g} 最小={min(ratios) if ratios else float("nan"):.4g} 最大={max(ratios) if ratios else float("nan"):.4g}')
print(f'# 史実の桁の量＝生まれの国庫÷移る州の民間の貨幣（{len(mag)} 行） p10={q(mag,0.1):.4g} 中央={q(mag,0.5):.4g} p90={q(mag,0.9):.4g} 最大={max(mag) if mag else float("nan"):.4g}')
print('GATE ' + ('緑' if mism == 0 and missing == 0 and rows > 0 and flags == {'1'} else '赤') + '（実際＝新の規則の食い違い 0・表の欠け 0・行 1 以上・全ての行が byMoneyFlag 1）')
