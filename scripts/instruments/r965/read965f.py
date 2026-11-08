# (965) 読むだけ：植民地格の州の外との出入りを〈入ってくる側を引いた純〉で読む（README ★(954)）。
#   純＝inExportPerQuarter − outImportPerQuarter（cearn_s）。人頭税 outTaxPerQuarter は別に並べる。
#   母集合：tier=Colonial・popK ≥ 100 の州×四半期（read965b.py と同じ）。0 の続きのある種×州は <名札>_dead.tsv から。
#   閉じた出来事は <名札>_close.tsv（deadStartLag 9999＝後に 0 の続きが来ない）の前 8・後 8 四半期。
#   使い方: python -X utf8 -I read965f.py <keep の腕のフォルダ> <名札> <出力フォルダ>
import sys, os, csv, statistics as st, collections as C
M, TAG, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
POP = 100.0; W = 8
def fl(x):
    try: return float(x)
    except ValueError: return float('nan')
def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')
dead = set(); close = []
with open(os.path.join(OUT, f'{TAG}_dead.tsv'), encoding='utf-8') as f:
    for r in list(csv.reader(f, delimiter='\t'))[1:]: dead.add((r[0], r[1]))
with open(os.path.join(OUT, f'{TAG}_close.tsv'), encoding='utf-8') as f:
    rr = list(csv.reader(f, delimiter='\t')); h = {n: i for i, n in enumerate(rr[0])}
    for r in rr[1:]: close.append((r[h['seed']], r[h['region']], int(r[h['q']]), r[h['deadStartLag']] != '9999'))
per = C.defaultdict(dict)   # (seed, region) -> q -> (pop, inExp, outImp, outTax)
for s in range(1, 11):
    p = os.path.join(M, f'cearn_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        it = csv.reader(f, delimiter='\t'); h = {n: i for i, n in enumerate(next(it))}
        iq, ir, it_, ip = h['quarter'], h['region'], h['tier'], h['popK']
        ie, io, ix = h['inExportPerQuarter'], h['outImportPerQuarter'], h['outTaxPerQuarter']
        for r in it:
            if r[it_] != 'Colonial': continue
            pop = fl(r[ip])
            if not (pop >= POP): continue
            per[(str(s), r[ir])][int(r[iq])] = (pop, fl(r[ie]), fl(r[io]), fl(r[ix]))
def pc(v): return [(e - o) / p for (p, e, o, t) in v]
rows = {'dead': C.defaultdict(list), 'alive': C.defaultdict(list)}
for key, ser in per.items():
    v = list(ser.values()); k = 'dead' if key in dead else 'alive'
    rows[k]['gross'].append(med([e / p for (p, e, o, t) in v]))
    rows[k]['imp'].append(med([o / p for (p, e, o, t) in v]))
    rows[k]['net'].append(med(pc(v)))
    rows[k]['tax'].append(med([t / p for (p, e, o, t) in v]))
    rows[k]['negShare'].append(sum(1 for x in pc(v) if x < 0) / len(v))
print(f'=== {TAG}  種×州 0 の続きのある {len(rows["dead"]["net"])}  無い {len(rows["alive"]["net"])}  （値は種×州の中央の、さらに中央・1 千人あたり・1 四半期）')
for nm in ['gross', 'imp', 'net', 'tax', 'negShare']:
    print(f'  {nm:9s} 0 の続き {med(rows["dead"][nm]):.4g}\t無い {med(rows["alive"][nm]):.4g}')
ev = {True: C.defaultdict(list), False: C.defaultdict(list)}
for (s, rn, q, later) in close:
    ser = per.get((s, rn))
    if not ser: continue
    b = [ser[x] for x in range(q - W, q) if x in ser]; a = [ser[x] for x in range(q, q + W) if x in ser]
    if not b or not a: continue
    ev[later]['netB'].append(med(pc(b))); ev[later]['netA'].append(med(pc(a)))
    ev[later]['impB'].append(med([o / p for (p, e, o, t) in b])); ev[later]['impA'].append(med([o / p for (p, e, o, t) in a]))
    ev[later]['taxA'].append(med([t / p for (p, e, o, t) in a]))
for later in (True, False):
    d = ev[later]
    print(f'  閉じた出来事・後に 0 の続き={"有" if later else "無"} 件数 {len(d["netB"])}  純 前 {med(d["netB"]):.4g} 後 {med(d["netA"]):.4g}  輸入の払い 前 {med(d["impB"]):.4g} 後 {med(d["impA"]):.4g}  人頭税 後 {med(d["taxA"]):.4g}  純が負の出来事（後） {sum(1 for x in d["netA"] if x < 0)}')
