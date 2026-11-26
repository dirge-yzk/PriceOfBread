# (1402) 読む線：戦時の代わりの道の残り 2 本（死傷・徴兵）を生で読む。人口 5,000 千人以上の国×四半期を、交戦中（atWarMonths>civilWarMonths）と平時（どちらも 0）に分け、
#   兵の割合（soldierK/populationK）・その四半期の徴兵の割合（soldiervac の drawWar*K÷populationK）・徴兵で足された闘争度×人口（drawWarMilPop*÷populationK＝Military.cs:247〜:250 の直の足し）・
#   闘争度の平均・厭戦・正統性、dcont があれば闘争度の目標の項（声・戦・民族・飢え）の平均を並べる。死傷は casualty_s の年の deadK÷人口（交戦の月がある国×年）。
#   python -X utf8 -I war_paths2.py <タグ> <生> <種の起点> [...]  → 標準出力
import sys, os, csv
a = sys.argv[1:]
def f(x):
    try: return float(x)
    except ValueError: return float('nan')
def rd(p):
    with open(p, encoding='utf-8') as fh:
        r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {k: j for j, k in enumerate(h)}
        for x in r: yield ix, x
print('arm\tside\tnq\tsoldierSh\tdraftShQ\tconscrMilPushQ\tpush/0.30\tmilitancy\twarExh\tlegit\tmilVoice\tmilWar\tmilNation\tmilFamine\tresidual\tnyWar\tdeadShY\tdeadSh/soldierMonths')
for i in range(0, len(a), 3):
    tag, d, sb = a[i], a[i + 1], int(a[i + 2])
    acc = {k: {} for k in ('war', 'peace')}; dead = [0, 0.0, 0.0, 0.0]
    for s in range(sb, sb + 10):
        N = {}
        for ix, x in rd(os.path.join(d, f'nation_s{s}.tsv')):
            pk = f(x[ix['populationK']])
            if not pk >= 5000: continue
            aw, cw = f(x[ix['atWarMonths']]), f(x[ix['civilWarMonths']])
            side = 'war' if aw > cw else ('peace' if aw == 0 and cw == 0 else None)
            if side: N[(x[ix['quarter']], x[ix['nation']])] = [side, pk, f(x[ix['soldierK']]) / pk, 0.0, 0.0, int(x[ix['year']])]
        for ix, x in rd(os.path.join(d, f'soldiervac_s{s}.tsv')):
            k = (x[ix['q']], x[ix['nation']])
            if k in N:
                N[k][3] += (f(x[ix['drawWarHomeK']]) + f(x[ix['drawWarColonialK']])) / N[k][1]
                N[k][4] += (f(x[ix['drawWarMilPopHome']]) + f(x[ix['drawWarMilPopColonial']])) / N[k][1]
        S = {}
        for ix, x in rd(os.path.join(d, f'scalars_s{s}.tsv')):
            S[(x[ix['quarter']], x[ix['nation']])] = (f(x[ix['militancyMean']]), f(x[ix['warExhaustion']]), f(x[ix['legitimacy']]))
        DC = {}
        p = os.path.join(d, f'dcont_s{s}.tsv')
        if os.path.exists(p):
            for ix, x in rd(p):
                DC[(x[ix['quarter']], x[ix['nation']])] = tuple(f(x[ix[c]]) for c in ('milVoiceMeanShare', 'milWarMeanShare', 'milNationMeanShare', 'milFamineMeanShare', 'militancyMeanShare'))
        for k, v in N.items():
            side = v[0]; sc = S.get(k, (float('nan'),) * 3); dc = DC.get(k)
            row = [v[2], v[3], v[4], sc[0], sc[1], sc[2]] + (list(dc[:4]) + [dc[4] - sum(dc[:4])] if dc else [float('nan')] * 5)
            A = acc[side]
            for j, val in enumerate(row):
                if val == val: A.setdefault(j, [0.0, 0]); A[j][0] += val; A[j][1] += 1
            A.setdefault('n', [0, 0]); A['n'][1] += 1
        W = {}
        for (q, nat), v in N.items():
            if v[0] == 'war': W[(v[5], nat)] = v[1]
        for ix, x in rd(os.path.join(d, f'casualty_s{s}.tsv')):
            k = (int(x[ix['year']]), x[ix['nation']])
            if k in W and f(x[ix['months']]) > 0:
                dead[0] += 1; dead[1] += f(x[ix['deadK']]) / W[k]; dead[2] += f(x[ix['deadK']]); dead[3] += f(x[ix['soldierMonthsK']])
    for side in ('war', 'peace'):
        A = acc[side]; m = lambda j: A[j][0] / A[j][1] if j in A and A[j][1] else float('nan')
        dd = (f'{dead[0]}\t{dead[1] / dead[0]:.5f}\t{dead[2] / dead[3]:.5f}' if side == 'war' and dead[0] else '\t\t')
        print('\t'.join([tag, side, str(A.get('n', [0, 0])[1])] + [f'{m(j):.5f}' for j in range(3)] + [f'{m(2) / 0.30:.5f}'] + [f'{m(j):.4f}' for j in range(3, 11)]) + '\t' + dd)
