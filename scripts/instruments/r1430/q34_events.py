"""(1430) 問い 3・4：an_h2.py -v5 の事象の行ごとに、popclose_s の口（国内の移民・越境・技師・守備兵・植民地の合わせ・出生・平常の死・飢えの死・暴力の死）を
膨らむ窓（開幕〜ピークの年）と減る窓（ピークの年〜落ちた年）で足し、持ち主の移りの四半期を並べる。読むだけ。
python -I -X utf8 q34_events.py <計器の出力ディレクトリ> <an_h2 -v5 の出力> [-all 州の名]（-all＝その州を全ての種で）
出力の口の単位は千人（窓の中の四半期の和）。守備兵＝garrisonIn−garrisonOut。"""
import sys, os, re, collections
d, anf = sys.argv[1], sys.argv[2]
ALL = sys.argv[sys.argv.index('-all') + 1] if '-all' in sys.argv else None
pat = re.compile(r'\[([^\]]+)\] s(\d+) (\S+?)（([^・）]+)・(\w+)(?:・首都)?） ([\d.e+\-]+)\((\d+)\)→([\d.e+\-]+)\((\d+)\)')
ev = []
for line in open(anf, encoding='utf-8'):
    m = pat.search(line)
    if m and '1/10' not in m.group(1): ev.append((int(m.group(2)), m.group(3), m.group(1), m.group(4), float(m.group(6)), int(m.group(7)), float(m.group(8)), int(m.group(9))))
if ALL:
    seeds = sorted(int(re.match(r'popclose_s(\d+)\.tsv', f).group(1)) for f in os.listdir(d) if re.match(r'popclose_s\d+\.tsv$', f))
    ev = [(s, ALL, '全ての種', '', 0, 0, 0, 0) for s in seeds]
K = ['migrationInK', 'migrationOutK', 'crossBorderInK', 'crossBorderOutK', 'specialistInK', 'specialistOutK', 'garrisonInK', 'garrisonOutK', 'colonyMergeInK', 'birthsK', 'deathsNormalK', 'deathsFamineK', 'killedViolentK', 'killedOtherK']
bys = collections.defaultdict(list)
for e in ev: bys[e[0]].append(e)
print('種\t州\t区分\t持ち主の移り（年:国）\t開幕\tピーク(年)\t落ちた(年)\t窓\t国内の入−出\t越境の入−出\t技師の入−出\t守備兵の入−出\t植民地の合わせ\t出生\t平常の死\t飢えの死\t暴力の死\t他の死\t守備兵の最大の1四半期(年)')
for s in sorted(bys):
    regs = {e[1] for e in bys[s]}
    rows = collections.defaultdict(list)
    with open(os.path.join(d, f'popclose_s{s}.tsv'), encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            v = line.rstrip('\n').split('\t')
            if v[ix['region']] in regs: rows[v[ix['region']]].append(v)
    for e in bys[s]:
        _, reg, kind, nat, pk, pky, lo, loy = e
        rr = rows[reg]
        if not rr: print(f'{s}\t{reg}\t{kind}\t行なし'); continue
        own = []; prev = None
        for v in rr:
            n = v[ix['nation']]
            if n != prev: own.append(f"{v[ix['year']]}:{n}"); prev = n
        pop = [(int(float(v[ix['year']])), float(v[ix['populationK']])) for v in rr]
        if ALL:
            pky, pk = max(pop, key=lambda t: t[1]); loy = 1950
            after = [t for t in pop if t[0] >= pky]; lo = min(t[1] for t in after)
        open_pop = pop[0][1]
        gmax = max(rr, key=lambda v: float(v[ix['garrisonInK']]) - float(v[ix['garrisonOutK']]))
        gtxt = f"{float(gmax[ix['garrisonInK']]) - float(gmax[ix['garrisonOutK']]):.3g}({gmax[ix['year']]})"
        for wn, a, b in (('膨らむ', 0, pky), ('減る', pky, loy)):
            t = collections.Counter()
            for v in rr:
                y = int(float(v[ix['year']]))
                if a <= y <= b if wn == '膨らむ' else a < y <= b:
                    for k in K: t[k] += float(v[ix[k]])
            print(f"{s}\t{reg}\t{kind}\t{' '.join(own)}\t{open_pop:.4g}\t{pk:.4g}({pky})\t{lo:.4g}({loy})\t{wn}\t{t['migrationInK']-t['migrationOutK']:.3g}\t{t['crossBorderInK']-t['crossBorderOutK']:.3g}\t{t['specialistInK']-t['specialistOutK']:.3g}\t{t['garrisonInK']-t['garrisonOutK']:.3g}\t{t['colonyMergeInK']:.3g}\t{t['birthsK']:.3g}\t{t['deathsNormalK']:.3g}\t{t['deathsFamineK']:.3g}\t{t['killedViolentK']:.3g}\t{t['killedOtherK']:.3g}\t{gtxt}")
