"""(749) 合流：c3h1 の反証役 中 1 の記録＝近い流れ（3,000 km 以下）の計画の新÷旧＝(liveAll−liveFar)/(oldAll−oldFar)を国ごとに（migform_s・読むだけ・全四半期と種の和）。
python -X utf8 nearflow.py <計器の出力> [種...]  （計画は受け入れの上限で縮める前の量＝実現した流れではない）"""
import sys, os, collections
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('migform_s'):-4]) for f in os.listdir(d) if f.startswith('migform_s'))
acc = collections.defaultdict(lambda: [0.0] * 4)
for sd in seeds:
    with open(os.path.join(d, f'migform_s{sd}.tsv'), encoding='utf-8') as fh:
        h = next(fh).rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) != len(h): continue
            a = acc[c[ix['nation']]]
            for j, k in enumerate(['liveFarPlanK', 'liveAllPlanK', 'oldFarPlanK', 'oldAllPlanK']): a[j] += float(c[ix[k]])
tot = [0.0] * 4
rows = []
for n, (lf, la, of, oa) in acc.items():
    for j, v in enumerate((lf, la, of, oa)): tot[j] += v
    rows.append((oa, n, (la - lf) / (oa - of) if oa - of else float('nan'), lf / of if of else float('nan'), la / oa if oa else float('nan')))
print('nation\tnear(新÷旧)\tfar(新÷旧)\tall(新÷旧)\toldAllPlanK')
for oa, n, nr, fr, ar in sorted(rows, reverse=True)[:25]: print(f'{n}\t{nr:.3f}\t{fr:.3f}\t{ar:.3f}\t{oa:.0f}')
lf, la, of, oa = tot
print(f'WORLD\t{(la-lf)/(oa-of):.3f}\t{lf/of:.3f}\t{la/oa:.3f}\t{oa:.0f}')
