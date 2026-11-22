# (1292) c133ak 読むだけ：〈0 の近く〉の国×年の頻度と、100 倍の跳びの直前が〈0 の近く〉かの数え
# 使い方: python -X utf8 -I q3_freq.py [--seeds a-b] <名前>=<ins フォルダ> ...
#   定義（T1_M の結果を見た後の定義・1/100 は数を見てから選んだ＝(1294) 合流が反証役 v の低の指摘で注を直した・物差しの候補で門ではない）
#   Z1＝人口 100 千人以上・名目 GDP < 人口K×0.01（指示の定義）
#   Z2＝人口 100 千人以上・名目 GDP の 1 人あたりが、同じ四半期の全ての国（人口 100 千人以上）の 1 人あたりの中央値の 1/100 未満
#   M1＝年の表（stock_s）で 人口 100 千人以上・民間の貨幣 totalMoneyStock < 国庫 treasuryStock の 1/1000（(1285) の型）
#   跳び＝名目 GDP の 1 四半期の比 ≧100（直前の行が正のとき・gdp_jump833 と同じ）。直前の行が Z1／Z2 なら〈0 の近くから〉に数える。
import csv, glob, math, os, sys
from statistics import median
def num(s):
    try: return float(s)
    except ValueError: return float('nan')
argv = sys.argv[1:]
lo, hi = 1, 10**9
DIV = 100.0
if '--div' in argv:
    DIV = float(argv[argv.index('--div') + 1]); del argv[argv.index('--div'):argv.index('--div') + 2]
if '--seeds' in argv:
    a = argv[argv.index('--seeds') + 1]; lo, hi = (int(t) for t in a.split('-')); del argv[argv.index('--seeds'):argv.index('--seeds') + 2]
print(f'# Z2 の分母 DIV={DIV:g}'); print('# 名前\t種\tZ1国×年\tZ1国×四半期\tZ1の国(種:国)\tZ2国×年\tZ2国×四半期\tM1国×年\t跳び100\t跳びのうち直前Z1\t直前Z2\t直前どちらでもない\tZ2の種の数')
for arg in argv:
    name, d = arg.split('=', 1)
    seeds = sorted(s for s in (int(os.path.basename(p)[len('nation_s'):-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv'))) if lo <= s <= hi)
    z1y = set(); z1q = 0; z2y = set(); z2q = 0; m1y = set(); j = jz1 = jz2 = jno = 0; z1n = {}; z2seeds = set(); rows_out = []
    for s in seeds:
        byq = {}
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                byq.setdefault(int(x[ix['quarter']]), []).append((x[ix['nationUid']], x[ix['nation']], int(x[ix['year']]), num(x[ix['populationK']]), num(x[ix['nominalGdpPerYear']])))
        prev = {}
        for q in sorted(byq):
            pcs = [g / p for (_, _, _, p, g) in byq[q] if p >= 100 and math.isfinite(g) and g > 0]
            med = median(pcs) if pcs else float('nan')
            for (u, nm, y, p, g) in byq[q]:
                if not math.isfinite(g): continue
                isz1 = p >= 100 and not (g >= p * 0.01)
                isz2 = p >= 100 and math.isfinite(med) and not (g / p >= med / DIV)
                if isz1: z1y.add((s, u, y)); z1q += 1; z1n[f'{s}:{nm}'] = z1n.get(f'{s}:{nm}', 0) + 1
                if isz2: z2y.add((s, u, y)); z2q += 1; z2seeds.add(s)
                if u in prev and prev[u][0] > 0 and g / prev[u][0] >= 100:
                    j += 1
                    if prev[u][1]: jz1 += 1
                    if prev[u][2]: jz2 += 1
                    if not prev[u][1] and not prev[u][2]: jno += 1
                    rows_out.append((s, nm, y, q, round(g / prev[u][0], 1), prev[u][1], prev[u][2]))
                prev[u] = (g, isz1, isz2)
        sp = os.path.join(d, f'stock_s{s}.tsv')
        if os.path.exists(sp):
            with open(sp, encoding='utf-8', newline='') as f:
                r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
                for x in r:
                    p = num(x[ix['populationK']]); mny = num(x[ix['totalMoneyStock']]); t = num(x[ix['treasuryStock']])
                    if p >= 100 and t > 0 and mny < t / 1000: m1y.add((s, x[ix['nation']], int(x[ix['year']])))
    top = '・'.join(f'{k} {v}' for k, v in sorted(z1n.items(), key=lambda kv: -kv[1])[:6])
    print(f'{name}\t{len(seeds)}\t{len(z1y)}\t{z1q}\t{top}\t{len(z2y)}\t{z2q}\t{len(m1y)}\t{j}\t{jz1}\t{jz2}\t{jno}\t{len(z2seeds)}')
    for row in rows_out: print('   跳び', row)
