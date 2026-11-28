"""(1430) 問い 4 の補い：an_h2 -v5 の 1/4 の事象ごとに、開幕・ピーク・落ちた年の 人口÷耕地 と 農村÷耕地、ピークの年の Holding・LandFactor・機会（region_s）を並べる。読むだけ。
python -I -X utf8 q4_land.py <計器の出力ディレクトリ> <an_h2 -v5 の出力>"""
import sys, os, re, collections
d, anf = sys.argv[1], sys.argv[2]
pat = re.compile(r'\[([^\]]+)\] s(\d+) (\S+?)（([^・）]+)・(\w+)(?:・首都)?） ([\d.e+\-]+)\((\d+)\)→([\d.e+\-]+)\((\d+)\)')
ev = [(int(m.group(2)), m.group(3), m.group(1), int(m.group(7)), int(m.group(9))) for m in (pat.search(l) for l in open(anf, encoding='utf-8')) if m and '1/10' not in m.group(1)]
bys = collections.defaultdict(list)
for e in ev: bys[e[0]].append(e)
print('種\t州\t区分\t耕地\t開幕 人口÷耕地\t開幕 農村÷耕地\tピーク(年) 人口÷耕地\tピーク 農村÷耕地\tピーク LandFactor\tピーク 機会\t底(年) 人口÷耕地\t底 農村÷耕地\t底 Holding\t底 雇用')
for s in sorted(bys):
    regs = {e[1] for e in bys[s]}; rows = collections.defaultdict(list)
    with open(os.path.join(d, f'region_s{s}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if v[ix['region']] in regs: rows[v[ix['region']]].append(v)
    for _, reg, kind, py, ly in bys[s]:
        rr = rows[reg]
        def at(y, pick):
            c = [v for v in rr if int(float(v[ix['year']])) == y]
            return pick(c, key=lambda v: float(v[ix['populationK']])) if c else None
        o = rr[0]; p = at(py, max); b = at(ly, min)
        f = lambda v, k: float(v[ix[k]])
        ar = f(o, 'arableLand')
        print(f"{s}\t{reg}\t{kind}\t{ar:.4g}\t{f(o,'populationK')/ar:.3g}\t{f(o,'ruralPopulationK')/ar:.3g}\t{f(p,'populationK')/ar:.3g}({py})\t{f(p,'ruralPopulationK')/ar:.3g}\t{f(p,'landFactor'):.3g}\t{f(p,'opportunityIndex'):.3g}\t{f(b,'populationK')/ar:.3g}({ly})\t{f(b,'ruralPopulationK')/ar:.3g}\t{f(b,'holdingFactor'):.3g}\t{f(b,'employmentShare'):.3g}")
