# (999) R5 読み手：商人の POP が空の州（読むだけ）
# 使い方: python -X utf8 -I r999_q.py <出力フォルダ> <名前>=<insフォルダ> ...
import csv, glob, os, sys, statistics
from collections import defaultdict

EMPTY = 1e-6
out_dir = sys.argv[1]; os.makedirs(out_dir, exist_ok=True)
boards = [a.split('=', 1) for a in sys.argv[2:]]
BENG = ('中核ベンガル', '内陸ベンガル')

def seeds(d, pfx):
    ps = glob.glob(os.path.join(d, pfx + '_s*.tsv'))
    return sorted(((int(os.path.basename(p)[len(pfx) + 2:-4]), p) for p in ps))

def band(p):
    return '<1000' if p < 1000 else ('1000-5000' if p < 5000 else '>=5000')

fb = open(os.path.join(out_dir, 'q2_bengal.txt'), 'w', encoding='utf-8')
fs = open(os.path.join(out_dir, 'q3_sametype.txt'), 'w', encoding='utf-8')
fo = open(os.path.join(out_dir, 'q4_onset_refill.txt'), 'w', encoding='utf-8')
ft = open(os.path.join(out_dir, 'q3_top_regions.txt'), 'w', encoding='utf-8')

for name, d in boards:
    reg_seeds = dict(seeds(d, 'region'))
    cp_seeds = dict(seeds(d, 'cpop'))
    if not cp_seeds:
        for f in (fb, fs, fo):
            f.write(f'{name}: cpop が無い＝商人の頭数は読めない\n')
        continue
    per_seed_cells = []   # dict (tier,band) -> [empty, total]
    per_seed_onset = []
    top = defaultdict(list)  # region -> list of (seed, emptyQ, meanPop, tierMode)
    for s in sorted(cp_seeds):
        # region_s: (q, region) -> (nation, tier, pop, migIn, xIn, setIn)
        R = {}
        with open(reg_seeds[s], encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                R[(int(x[0]), x[ix['region']])] = (x[ix['nation']], x[ix['tier']], float(x[ix['populationK']]),
                    float(x[ix['migrationInKPerQuarter']]), float(x[ix['crossBorderInKPerQuarter']]), (float(x[ix['settlerInKPerQuarter']]) if 'settlerInKPerQuarter' in ix else 0.0))
        M = defaultdict(dict)  # region -> q -> (popK, savings)
        with open(cp_seeds[s], encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            ci, pk, sv, rg = ix['className'], ix['popK'], ix['savings'], ix['region']
            for x in r:
                if x[ci] != '商人': continue
                M[x[rg]][int(x[0])] = (float(x[pk]), float(x[sv]))
        regions_all = {k[1] for k in R}
        missing = sorted(regions_all - set(M))
        cells = defaultdict(lambda: [0, 0])
        onset = defaultdict(int)
        for reg, ser in M.items():
            qs = sorted(ser)
            emptyQ = 0; popsum = 0; tiers = defaultdict(int)
            prev_nonempty = None
            first_empty_after_nonempty = None
            refill = 0; refill_with_mig = 0
            was_empty = None
            for q in qs:
                rr = R.get((q, reg))
                if rr is None: continue
                nat, tier, pop, mi, xi, si = rr
                if pop <= 0: continue
                e = ser[q][0] <= EMPTY
                c = cells[(tier, band(pop))]; c[1] += 1
                if e: c[0] += 1; emptyQ += 1
                popsum += pop; tiers[tier] += 1
                if was_empty is True and not e:
                    refill += 1
                    pr = R.get((q - 1, reg))
                    if (mi + xi + si) > 0 or (pr and pr[3] + pr[4] + pr[5] > 0): refill_with_mig += 1
                if was_empty is False and e and first_empty_after_nonempty is None:
                    first_empty_after_nonempty = q
                was_empty = e
            q0 = qs[0]
            r0 = R.get((q0, reg))
            e0 = ser[q0][0] <= EMPTY
            if emptyQ == 0: kind = 'never_empty'
            elif e0 and refill == 0: kind = 'empty_from_open_never_filled'
            elif e0: kind = 'empty_from_open_later_filled'
            else:
                # 落ちた：前 4 四半期に持ち主の替わり
                fe = first_empty_after_nonempty
                own = {R[(qq, reg)][0] for qq in range(fe - 4, fe + 1) if (qq, reg) in R}
                kind = 'fell_with_owner_change' if len(own) > 1 else 'fell_no_owner_change'
            onset[kind] += 1
            onset[kind + '_refills'] += refill
            onset[kind + '_refills_mig'] += refill_with_mig
            n = sum(tiers.values())
            if emptyQ > 0 and n > 0:
                tm = max(tiers, key=tiers.get)
                top[reg].append((s, emptyQ, popsum / n, tm, kind))
            if reg in BENG:
                own_changes = []
                lastn = None
                for q in qs:
                    rr = R.get((q, reg))
                    if rr and rr[0] != lastn:
                        own_changes.append(f'{q}:{rr[0]}:{rr[1]}'); lastn = rr[0]
                firstNE = next((q for q in qs if ser[q][0] > EMPTY), None)
                mx = max(ser[q][0] for q in qs)
                pts = ' '.join(f'q{q}:{ser[q][0]:.3g}/{ser[q][1]:.4g}' for q in qs if q % 120 == 0)
                migq = sum(1 for q in qs if (q, reg) in R and R[(q, reg)][3] + R[(q, reg)][4] + R[(q, reg)][5] > 0)
                fb.write(f'{name} s{s} {reg} 空の四半期 {emptyQ}/{n} 最初の空でない q={firstNE} 最大 popK={mx:.4g} 型={kind} 流入のある四半期={migq}\n'
                         f'   持ち主: {" ".join(own_changes)}\n   商人 popK/貯蓄: {pts}\n')
        per_seed_cells.append(cells); per_seed_onset.append(onset)
        fs.write(f'{name} s{s} 商人の行が無い州 {len(missing)}\n')
    keys = sorted({k for c in per_seed_cells for k in c})
    fs.write(f'== {name}: 格×人口の帯 州×四半期（種の中央 空/全・和÷和の割合）\n')
    for k in keys:
        es = [c[k][0] for c in per_seed_cells]; ts = [c[k][1] for c in per_seed_cells]
        fs.write(f'  {k[0]:<11} {k[1]:<10} 空 中央 {statistics.median(es):>9.1f}  全 中央 {statistics.median(ts):>9.1f}  割合 {sum(es)/max(1,sum(ts)):.4f}  (種ごとの空 最小 {min(es)} 最大 {max(es)})\n')
    okeys = sorted({k for o in per_seed_onset for k in o})
    fo.write(f'== {name}: 州の型（州の数・種の中央 / 種の和）\n')
    for k in okeys:
        v = [o.get(k, 0) for o in per_seed_onset]
        fo.write(f'  {k:<45} 中央 {statistics.median(v):>8.1f}  和 {sum(v)}\n')
    ft.write(f'== {name}: 商人が空の四半期のある州（平均人口の降順・上位 40）\n')
    rows = []
    for reg, L in top.items():
        rows.append((statistics.median([x[2] for x in L]), reg, len(L), statistics.median([x[1] for x in L]),
                     ','.join(sorted({x[3] for x in L})), ','.join(sorted({x[4] for x in L}))))
    rows.sort(reverse=True)
    for pm, reg, ns, eq, tm, kd in rows[:40]:
        ft.write(f'  {reg:<14} 平均人口 {pm:>9.0f}  空のあった種 {ns:>2}  空の四半期 中央 {eq:>5.0f}  格 {tm}  型 {kd}\n')
    for f in (fb, fs, fo, ft): f.flush()
for f in (fb, fs, fo, ft): f.close()
print('ok')
