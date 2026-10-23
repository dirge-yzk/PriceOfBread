# (511) 線 fs 段0：m508 の計器つき 10 種の既存の生（fd_s・labor_s・house_s）だけを読む。シミュは回さない。
# 使い方: python -I step0.py <計器の出力フォルダ(D:/…)> > measurements/fs511_20261023_step0.txt
# (A) 指示の量＝農業労働の売れ残り(1−売れた量÷産出)が自給率を超える分(失業の式の切った後)がほぼ 0 かを house_s の国×四半期で読む。
# (B) 形の入力の近似 e＝max(0, 1−売れた量÷能力−自給率)（能力＝labor_s の farmCapPerMonth＝生産者の Capacity の和）の大きさを fd_s の州の行へ当てる。
#     国の売れた量÷能力と州の自給率の組み合わせ＝州ごとの値ではない近似。見積り gain＝min(e×h, 1−fillNew)（fillNew は全階級の主食の充足・上限の見積り）。
import csv, sys, os, collections
sys.stdout.reconfigure(encoding='utf-8')
d = sys.argv[1]
def rd(p):
    with open(p, encoding='utf-8', newline='') as f:
        for row in csv.DictReader(f, delimiter='\t'): yield row
def fl(x):
    try: return float(x)
    except: return float('nan')
def wmed(vals):
    vals = sorted((v, w) for v, w in vals if v == v and w > 0)
    if not vals: return float('nan')
    tot = sum(w for _, w in vals); acc = 0
    for v, w in vals:
        acc += w
        if acc >= tot / 2: return v
def q(xs, p):
    xs = sorted(x for x in xs if x == x)
    return xs[min(len(xs) - 1, int(len(xs) * p))] if xs else float('nan')
unsold = []; rows = collections.defaultdict(list); sc = {}
for s in range(1, 11):
    for r in rd(os.path.join(d, f'house_s{s}.tsv')):
        b = fl(r['farmLaborBoughtPerMonth']); p = fl(r['farmLaborProducedPerMonth'])
        if p > 0: unsold.append(1 - b / p)
    for r in rd(os.path.join(d, f'labor_s{s}.tsv')):
        cap = fl(r['farmCapPerMonth']); sold = fl(r['farmSoldPerMonth'])
        sc[(s, r['quarter'], r['nation'])] = (fl(r['farmUnemploymentShare']), sold / cap if cap > 0 else float('nan'))
    for r in rd(os.path.join(d, f'fd_s{s}.tsv')):
        qq = int(r['quarter']); yr = int(r['year'])
        per = 'q0-7' if qq < 8 else ('y1783-1850' if yr <= 1850 else 'y1851-1950')
        fill = fl(r['fillNew']); pop = fl(r['popK']); S = fl(r['subsistenceMean']); h = fl(r['harvestMean']); land = fl(r['landFactor'])
        u0, soldcap = sc.get((s, r['quarter'], r['nation']), (float('nan'), float('nan')))
        e = max(0.0, 1 - soldcap - S) if (soldcap == soldcap and S == S) else float('nan')
        gain = min(e * h, 1 - fill) if (e == e and h == h and fill == fill) else float('nan')
        t = (fill, pop, S, h, land, e, gain, u0)
        rows[(r['tier'], per)].append(t)
        if r['tier'] == 'Colonial' and land >= 0.999: rows[('Colonial-land1', per)].append(t)
print('(511) fs 段0：m508 計器つき 10 種（D:/PoB_work/keep_508/m508_ins）の既存の生だけ。シミュは回していない。')
print('(A) 農業労働の売れ残り 1−売れた量÷産出（house_s・国×四半期 %d 行）：中央値 %.3f・p90 %.3f・p99 %.3f・最大 %.3f' % (len(unsold), q(unsold, .5), q(unsold, .9), q(unsold, .99), max(unsold)))
print('    自給率の中央値は下の表（本国 0.40〜0.56・植民地 0.42〜0.70）＝売れ残りの p90 が自給率に届かない＝指示の量（売れ残り−自給率）は 0 に張り付く（labor_s の farmUnemploymentShare の中央値も全て 0.000）。')
print('(B) 州の行（fd_s）。人口加重の中央値。列＝tier／期／行数／充足fillNew／自給率S／収穫h／S×h／e=max(0,1−売れた÷能力−S)／指示の量=国の農業失業u(切った後)／見積りgain=min(e×h,1−fill)／充足<0.8の人口割合／gain>0.02の人口割合')
for k in sorted(rows):
    v = rows[k]; tot = sum(x[1] for x in v)
    below = sum(x[1] for x in v if x[0] < 0.8) / tot if tot else float('nan')
    gpos = sum(x[1] for x in v if x[6] == x[6] and x[6] > 0.02) / tot if tot else float('nan')
    sh = [(x[2] * x[3], x[1]) for x in v if x[2] == x[2] and x[3] == x[3]]
    print('\t'.join([k[0], k[1], str(len(v)), '%.3f' % wmed([(x[0], x[1]) for x in v]), '%.3f' % wmed([(x[2], x[1]) for x in v]), '%.3f' % wmed([(x[3], x[1]) for x in v]), '%.3f' % wmed(sh),
                     '%.3f' % wmed([(x[5], x[1]) for x in v]), '%.3f' % wmed([(x[7], x[1]) for x in v]), '%.3f' % wmed([(x[6], x[1]) for x in v]), '%.3f' % below, '%.3f' % gpos]))
print('限界：e は国の（売れた量÷能力）と州の自給率の組＝州ごとの値ではない／能力は labor_s の farmCapPerMonth（生産者の Capacity の和・人の頭数ではない＝能力は Size×Productivity〔工場労働と同じ規模感の 7.0〕×boost）／fillNew は全階級の主食の充足（農民だけではない）／gain は上限の見積り／fd_s は挙げた 9 国だけ／売れた量÷産出は labor_s・house_s とも国単位。')
