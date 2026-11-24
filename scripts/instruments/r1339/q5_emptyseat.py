# (1339) q5：〈官僚の POP が大きさ 0 のまま行政の請求が正で払い 0〉の国×行を数える（読むだけ・推奨の形が触る盤面の大きさの見込み）
#   class_s の官僚の sizeK（行の時点）と nation_s の同じ四半期の adminClaimSimPerMonth・adminCatPaidPerMonth・populationK を突き合わせる
import csv, glob, math, os, sys
args = [x for x in sys.argv[1:] if '=' in x]
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
print('# q5_emptyseat 行＝class_s の行の四半期（人口 ≧10 千人の国）')
for arg in args:
    name, d = arg.split('=', 1)
    tot = hit = 0; nat = set(); popk = []
    for p in sorted(glob.glob(os.path.join(d, 'class_s*.tsv'))):
        s = int(os.path.basename(p)[7:-4]); bz = {}
        with open(p, encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                if x[ix['className']] == '官僚' and x[0].startswith('q') and x[0][1:].isdigit(): bz[(int(x[0][1:]), x[ix['nation']])] = num(x[ix['sizeK']])
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                k = (int(x[0]), x[ix['nation']])
                if k not in bz: continue
                pk = num(x[ix['populationK']])
                if not pk >= 10: continue
                tot += 1
                cl = num(x[ix['adminClaimSimPerMonth']]); pd = num(x[ix['adminCatPaidPerMonth']])
                if not (bz[k] > 1e-9) and cl > 0 and not (pd > 1e-9):
                    hit += 1; nat.add((s, k[1])); popk.append(pk)
    popk.sort()
    print(f'{name}\t国×行 {tot}\t官僚 0・請求＞0・払い 0 の国×行 {hit}（{hit/tot if tot else 0:.3%}）\t国×種 {len(nat)}\t人口の中央 {popk[len(popk)//2] if popk else float("nan"):.0f} 千')
