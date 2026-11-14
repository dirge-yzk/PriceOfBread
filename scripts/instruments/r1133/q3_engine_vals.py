# (1133) 問い3：rule_s（エンジンの関数の値・計器の名簿 9 国は毎四半期・他の国は年の末）と nation_s から、
# 不平等条約の旗・港の外国の取り分（関税の倍率＝1−これ）・StateReach・本国の StateReach・歳入÷GDP・兵÷人口 を国ごとの平均で並べる（腕どうし＝別の盤面）。
import sys, os
from collections import defaultdict
FOCUS = ['イギリス', 'ロシア', 'フランス', 'プロイセン', 'オーストリア', '清', '日本', 'オスマン', 'アメリカ']
def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')
print('腕\t国\t国×四半期\t不平等条約の旗\t港の外国の取り分\tStateReach\t本国のStateReach\t歳入÷GDP\t兵÷人口')
for a in sys.argv[1:]:
    lb, ins = a.split('=', 1)
    acc = defaultdict(lambda: defaultdict(list))
    for s in range(1, 11):
        with open(os.path.join(ins, f'rule_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}; seen = set()
            for ln in f:
                c = ln.rstrip('\n').split('\t'); n = c[ix['nation']]; k = (c[0], n)
                if k in seen or n not in FOCUS: continue
                seen.add(k); d = acc[n]
                d['t'].append(c[ix['treatyPortInForceFlag']] == '1'); d['p'].append(fnum(c[ix['portForeignShare']]))
                d['sr'].append(fnum(c[ix['stateReachIndex']])); d['srh'].append(fnum(c[ix['stateReachHomeIndex']]))
                g = fnum(c[ix['gdpPerMonth']]); d['rv'].append(fnum(c[ix['revenuePerMonth']]) / g if g > 0 else float('nan'))
        with open(os.path.join(ins, f'nation_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t'); n = c[ix['nation']]
                if n not in FOCUS: continue
                p = fnum(c[ix['totalPopulationK']]); acc[n]['so'].append(fnum(c[ix['soldierK']]) / p if p > 0 else float('nan'))
    for n in FOCUS:
        d = acc[n]
        def m(k):
            xs = [x for x in d[k] if x == x]; return sum(xs) / len(xs) if xs else float('nan')
        print(f'{lb}\t{n}\t{len(d["t"])}\t{m("t"):.4f}\t{m("p"):.4f}\t{m("sr"):.4f}\t{m("srh"):.4f}\t{m("rv"):.4f}\t{m("so"):.5f}')
