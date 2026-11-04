"""(822) cearn_s と基線の region_s の突き合わせ（読むだけ）：同じ種・四半期・州で popK（cearn）＝populationK（region_s）・
工場と採取の産出の列が同じ値（どちらも RegionProdRow）・凍結の印が region_s の産出から組んだ印と同じ。行の数も比べる。
使い方: python -I r822_xcheck.py <cearn の生> <基線の region_s の生> <出力.tsv>"""
import sys, os
def fl(x):
    try: return float(x)
    except: return float('nan')
a_dir, b_dir, outp = sys.argv[1], sys.argv[2], sys.argv[3]
o = open(outp, 'w', encoding='utf-8', newline='\n')
o.write('seed\tcearnRows\tregionRows\tmatched\tpopMismatch\tprodMismatch\n')
for sd in range(1, 21):
    pa, pb = os.path.join(a_dir, 'cearn_s%d.tsv' % sd), os.path.join(b_dir, 'region_s%d.tsv' % sd)
    if not (os.path.exists(pa) and os.path.exists(pb)): continue
    reg = {}
    with open(pb, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
        nb = 0
        for line in f:
            x = line.rstrip('\n').split('\t'); nb += 1
            reg[(x[ix['quarter']], x[ix['region']])] = (x[ix['populationK']], x[ix['factoryOutputRefValuePerMonth']], x[ix['extractionOutputRefValuePerMonth']])
    na = m = pm = qm = 0
    with open(pa, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k:i for i,k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t'); na += 1
            k = (x[ix['quarter']], x[ix['region']])
            if k not in reg: continue
            m += 1
            p, fo, eo = reg[k]
            if x[ix['popK']] != p: pm += 1
            if x[ix['factoryOutputRefValuePerMonth']] != fo or x[ix['extractionOutputRefValuePerMonth']] != eo: qm += 1
    o.write('\t'.join(map(str, [sd, na, nb, m, pm, qm])) + '\n')
o.close()
print(open(outp, encoding='utf-8').read())
