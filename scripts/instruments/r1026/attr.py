# (1026) c96ol: prodcap_s の Σoutput と nationgood_s の producedQty のずれの帰属（読むだけ）
# 使い方: python -X utf8 -I attr.py <生のフォルダ> <種,...> <出力の接頭>
import sys, csv, collections
csv.field_size_limit(1 << 30)
src, seeds, outp = sys.argv[1], [int(s) for s in sys.argv[2].split(',')], sys.argv[3]
REL, ABS = 5e-10, 1e-9

def rows(path):
    with open(path, 'r', encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row

def fl(x):
    try: return float(x)
    except: return float('nan')

summ = open(outp + '_summary.tsv', 'w', encoding='utf-8', newline='\n')
summ.write('seed\tclass\tpairs\tmismatch\tsumA\tsumAbsD\tsumD\tnegD\tmaxAbsResidAfterX\n')
det = open(outp + '_mismatch.tsv', 'w', encoding='utf-8', newline='\n')
det.write('seed\tyear\tq\tnation\tgood\tclass\tA\tB\td\tX\td_minus_X\tmovedRegions\n')
wld = open(outp + '_world.tsv', 'w', encoding='utf-8', newline='\n')
wld.write('seed\tyear\tgood\tsumD\tsumX\tresid\n')
info = open(outp + '_info.txt', 'w', encoding='utf-8', newline='\n')

for s in seeds:
    # nationgood
    B = {}; dupB = 0; ngyears = set()
    for ix, r in rows(f'{src}/nationgood_s{s}.tsv'):
        k = (int(r[ix['year']]), r[ix['nation']], r[ix['good']])
        ngyears.add(k[0])
        if k in B: dupB += 1; B[k] += fl(r[ix['producedQty']])
        else: B[k] = fl(r[ix['producedQty']])
    # 移転の事象（受け手・四半期）
    ev_q = collections.defaultdict(set)   # q -> {(receiver, region)}
    ev_year = collections.defaultdict(set)  # cederule.year -> {(receiver, region)}
    nev = collections.Counter()
    for ix, r in rows(f'{src}/cederule_s{s}.tsv'):
        q = int(r[ix['quarter']]); y = int(r[ix['year']])
        if y != 1783 + q // 4: info.write(f's{s} cederule year/quarter mismatch q={q} y={y}\n')
        ev_q[q].add((r[ix['receiver']], r[ix['region']]))
        ev_year[y].add((r[ix['receiver']], r[ix['region']], q))
        nev[r[ix['how']]] += 1
    # 内戦
    civ = collections.defaultdict(set)  # year -> names
    for ix, r in rows(f'{src}/wars_s{s}.tsv'):
        if r[ix['civilWarFlag']] != '1': continue
        y0 = int(r[ix['startYear']]); m = int(float(r[ix['months']]))
        for y in range(y0, y0 + m // 12 + 2):
            civ[y].add(r[ix['sideA']]); civ[y].add(r[ix['sideB']])
    # prodcap
    A = collections.defaultdict(float)
    X = collections.defaultdict(float)      # (year,nation,good) -> Σoutput of regions received at q
    Xp = collections.defaultdict(float)     # received earlier in year (a')
    PR = collections.defaultdict(float); X0 = collections.defaultdict(float); newNames = collections.defaultdict(set)
    zc = set()                              # groups with output==0 & capacity>0
    qOf = {}; movedNames = collections.defaultdict(set)
    rowsN = 0
    for ix, r in rows(f'{src}/prodcap_s{s}.tsv'):
        y = int(r[ix['year']])
        if y not in ngyears: continue
        rowsN += 1
        q = int(r[ix['q']]); qOf[y] = q
        nat = r[ix['ownerNation']]; g = r[ix['good']]; reg = r[ix['region']]
        o = fl(r[ix['output']]); k = (y, nat, g)
        A[k] += o
        PR[(y, nat, g, reg)] += o
        if (nat, reg) in ev_q.get(q, ()):
            X[k] += o; movedNames[k].add(reg)
        else:
            # 同じ年の別の四半期（暦の年＝cederule.year は q//4 で数える＝読みの q の年）
            qs = [qq for qq in range(q - 3, q) if (nat, reg) in ev_q.get(qq, ())]
            if qs: Xp[k] += o
        if o == 0.0 and fl(r[ix['capacity']]) > 0: zc.add(k)
    # 戦の強さ（region_s・読みの四半期）
    qset = set(qOf.values()); warN = collections.defaultdict(set)
    firstQ = {}; dupRQ = 0; seenRQ = set()
    for ix, r in rows(f'{src}/region_s{s}.tsv'):
        q = int(r[ix['quarter']]); rg = r[ix['region']]
        if (q, rg) in seenRQ: dupRQ += 1
        seenRQ.add((q, rg))
        if rg not in firstQ: firstQ[rg] = q
        if q in qset and fl(r[ix['warIntensity']]) > 0: warN[q].add(r[ix['nation']])
    seenRQ = None
    for (y, nat, g, reg), o in PR.items():
        if firstQ.get(reg, 0) == qOf[y] and qOf[y] > 0:
            k = (y, nat, g); X0[k] += o; newNames[k].add(reg)
    info.write(f's{s} dupRegionQuarter={dupRQ} founded={sum(1 for v in firstQ.values() if v > 0)}\n')
    yOfQ = {v: k for k, v in qOf.items()}
    info.write(f's{s} prodcapRows={rowsN} ngKeys={len(B)} dupB={dupB} events={dict(nev)} years={sorted(ngyears)[:3]}..{len(ngyears)} qOf1790={qOf.get(1790)}\n')
    stats = collections.defaultdict(lambda: [0, 0, 0.0, 0.0, 0.0, 0, 0.0])
    world = collections.defaultdict(lambda: [0.0, 0.0])
    onlyA = onlyB = 0
    keys = set(A) | set(B)
    for k in keys:
        y, nat, g = k
        if k not in A: onlyB += 1; a = 0.0
        else: a = A[k]
        if k not in B: onlyA += 1; b = 0.0
        else: b = B[k]
        d = a - b; x = X.get(k, 0.0) + X0.get(k, 0.0)
        mis = abs(d) > REL * max(abs(a), abs(b)) + ABS
        q = qOf.get(y, -1)
        if k in movedNames: cls = 'a'
        elif k in newNames: cls = 'a0'
        elif k in Xp: cls = 'a2'
        elif nat in civ.get(y - 1, ()) or nat in civ.get(y, ()) or nat in warN.get(q, ()): cls = 'b'
        elif k in zc: cls = 'c'
        else: cls = 'd'
        st = stats[cls]; st[0] += 1; st[2] += a
        resid = d - x
        if mis:
            st[1] += 1; st[3] += abs(d); st[4] += d
            if d < 0: st[5] += 1
            st[6] = max(st[6], abs(resid) if cls in ('a', 'a0') else abs(d))
            det.write(f'{s}\t{y}\t{q}\t{nat}\t{g}\t{cls}\t{a!r}\t{b!r}\t{d!r}\t{x!r}\t{resid!r}\t{"|".join(sorted(set(movedNames.get(k, ())) | set(newNames.get(k, ()))))}\n')
        w = world[(y, g)]; w[0] += d; w[1] += x
    for cls in ['a', 'a0', 'a2', 'b', 'c', 'd']:
        st = stats[cls]
        summ.write(f'{s}\t{cls}\t{st[0]}\t{st[1]}\t{st[2]!r}\t{st[3]!r}\t{st[4]!r}\t{st[5]}\t{st[6]!r}\n')
    info.write(f's{s} onlyA={onlyA} onlyB={onlyB}\n')
    for (y, g), (sd, sx) in sorted(world.items()):
        if abs(sd) > 1e-9 or abs(sx) > 0: wld.write(f'{s}\t{y}\t{g}\t{sd!r}\t{sx!r}\t{sd - sx!r}\n')
    summ.flush(); det.flush(); info.flush(); wld.flush()
print('done')
