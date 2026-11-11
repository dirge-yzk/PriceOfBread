# (816) (ii) 同じ盤面（旗 1）で ln(使った rel) − ln(等重みの rel) を、入った州（推定）とその他に分ける（近似＝続いて居る州の比の変化を 0 と置く）。
import math
rows = []
with open('D:/PoB_work/r816_work/out/a1_entries.tsv', 'rb') as f:
    t = f.read().decode('utf-8').splitlines()
h = t[0].split('\t'); rows = [dict(zip(h, l.split('\t'))) for l in t[1:]]
F1 = 'D:/PoB_work/keep_813/rej/c12svB_10/ins'
def rd(p):
    with open(p, 'rb') as f: t = f.read().decode('utf-8').splitlines()
    h = t[0].split('\t'); return [dict(zip(h, l.split('\t'))) for l in t[1:]]
out = ['seed\tseq\tyear\tN\tdN\tlnUsedMinusLnEqual\tnewShareEst\tlnNewMeanW1Est\tlnNewMeanEqEst\tnewContribEst\tnewContribFraction\tusedMaxShare\tmaxRegionWeight\tnonPicked9Sum\tchinaAuD\tbritainAuD\tmaxRegionExceedsAllNonBritishNationTotals']
for s, seq in ((2, 43), (4, 43)):
    r = [x for x in rows if x['board'] == 'flag1' and int(x['seed']) == s and int(x['seq']) == seq][0]
    sw = [x for x in rd(f'{F1}/silverwt_s{s}.tsv') if int(x['seq']) == seq][0]
    gap = math.log(float(r['usedRel'])) - math.log(float(r['relEq'])) if False else math.log(float(sw['usedRelIndex'])) - math.log(float(sw['relEqualIndex']))
    N = int(r['N']); dN = int(r['dN']); sh = float(r['newWeightShareEst']); x = float(r['lnNewMeanW1Est']); xe = float(r['lnNewMeanEqEst'])
    contrib = sh * x - (dN / N) * xe
    W = float(sw['usedWeightSum']); ms = float(sw['usedMaxShare'])
    q = seq - 21
    mn = [m for m in rd(f'{F1}/metalnat_s{s}.tsv') if int(m['quarter']) == q]
    met = [m for m in rd(f'{F1}/metal_s{s}.tsv') if int(m['quarter']) == q][0]
    picked = sum(float(m['AuDemand']) for m in mn); nonp = float(met['goldDemand']) - picked
    ch = [float(m['AuDemand']) for m in mn if m['nation'] == '清'][0]; br = [float(m['AuDemand']) for m in mn if m['nation'] == 'イギリス'][0]
    oth = max([float(m['AuDemand']) for m in mn if m['nation'] != 'イギリス'] + [nonp])
    out.append('\t'.join(str(v) for v in (s, seq, sw['year'], N, dN, f'{gap:.3f}', f'{sh:.3f}', f'{x:.3f}', f'{xe:.3f}', f'{contrib:.3f}', f'{contrib/gap:.2f}', f'{ms:.3f}', f'{ms*W:.0f}', f'{nonp:.0f}', f'{ch:.0f}', f'{br:.0f}', int(ms * W > oth))))
open('D:/PoB_work/r816_work/out/a5_decomp.tsv', 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('\n'.join(out))
