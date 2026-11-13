# (1109) c106rn：問い 2 の揺れの出どころ（事前登録 §3 の (i) と州の型）＋集計の後の追加の読み（〈追加〉と名乗る行）。
# 読むだけ：<ins>/trrail_s・trnation_s・port_s・region_s。呼び方：python -X utf8 -I types_rn.py <ins> <出力の txt>
import sys, os, glob, math, statistics as st
from collections import defaultdict

ins, outp = sys.argv[1], sys.argv[2]
LOW = 0.99
BUILD, DECAY = 0.15, 0.02

def quant(v, ps=(0.1, 0.5, 0.9)):
    if not v: return 'nan'
    v = sorted(v); n = len(v)
    return '／'.join(f'{v[min(n - 1, int(p * (n - 1) + 0.5))]:.3f}' for p in ps)

def full_speed_want(t_prev, need):
    t, w = t_prev, 0.0
    for _ in range(12):
        td = t * (1 - DECAY); want = BUILD * max(0.0, need - td) + DECAY * min(td, need); w += want; t = td + want
    return w

out = []; P = out.append
P(f'== {ins}')
seeds = sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv')), key=lambda p: int(p.rsplit('_s', 1)[1].split('.')[0]))
# (i) 港の州の潜在量の揺れ（四半期）
adj = defaultdict(list); yoy = defaultdict(list); adj_sh = []; yoy_sh = []
phase_cnt = defaultdict(list)
type_abs = defaultdict(list)
b2_g7 = [0, 0]; auto_a, auto_b = [], []
lag_hit = [0, 0]
for path in seeds:
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    pot = {}; thr = {}; port_names = set(); natsum = defaultdict(float)
    pp = os.path.join(ins, f'port_s{sd}.tsv')
    if os.path.exists(pp):
        with open(pp, encoding='utf-8') as f:
            hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
            for line in f:
                c = line.rstrip('\n').split('\t')
                try: q = int(c[ix['quarter']]); p = float(c[ix['tradePotentialWeight']]); th = float(c[ix['tradeThrottleIndex']])
                except ValueError: continue
                key = (c[ix['nation']], c[ix['region']])
                pot[(key, q)] = p; thr[(key, q)] = th; port_names.add(c[ix['region']]); natsum[(c[ix['nation']], q)] += max(0.0, p)
        for (key, q), p in pot.items():
            if not (p > 0): continue
            p1 = pot.get((key, q - 1)); p4 = pot.get((key, q - 4))
            th1 = thr.get((key, q - 1), 1.0)
            cls = '律速' if th1 < 1.0 else '律速でない'
            if p1 is not None and p1 > 0: adj[cls].append(p / p1)
            if p4 is not None and p4 > 0: yoy[cls].append(p / p4)
            s = p / natsum[(key[0], q)] if natsum[(key[0], q)] > 0 else None
            s1 = pot.get((key, q - 1)); n1 = natsum.get((key[0], q - 1), 0)
            s4 = pot.get((key, q - 4)); n4 = natsum.get((key[0], q - 4), 0)
            if s and s1 and s1 > 0 and n1 > 0: adj_sh.append(s / (s1 / n1))
            if s and s4 and s4 > 0 and n4 > 0: yoy_sh.append(s / (s4 / n4))
    # 州の型（region_s の年の最後の行）
    tier = {}; popk = {}
    rp = os.path.join(ins, f'region_s{sd}.tsv')
    with open(rp, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        iy, irg, iti, ipo, iq = ix['year'], ix['region'], ix['tier'], ix['populationK'], ix['quarter']
        for line in f:
            c = line.split('\t', ipo + 1)
            try: k = (c[irg], int(c[iy]))
            except ValueError: continue
            tier[k] = c[iti]
            try: popk[k] = float(c[ipo])
            except ValueError: pass
    nat = {}
    with open(os.path.join(ins, f'trnation_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: nat[(int(c[ix['year']]), c[ix['nation']])] = (float(c[ix['railTargetIndex']]), float(c[ix['railTargetNoThreatIndex']]))
            except (ValueError, IndexError): pass
    rows = {}; lastnat = {}; moved = {}
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        allrows = []
        for line in f:
            c = line.rstrip('\n').split('\t')
            allrows.append((int(c[ix['year']]), c[ix['region']], c[ix['nation']], c))
        allrows.sort(key=lambda x: x[0])
        for y, rg, nn, c in allrows:
            if rg in lastnat and lastnat[rg] != nn: moved[rg] = y
            lastnat[rg] = nn
            need = float(c[ix['needEnd']])
            if not (need > 0) or c[ix['waterFlag']] == '1': continue
            rows[(rg, y)] = (nn, float(c[ix['railTrackEnd']]), need, float(c[ix['phiEnd']]), int(c[ix['monthsCount']]), float(c[ix['boughtCapPerYear']]),
                             float(c[ix['warLossPerYear']]), int(c[ix['g7ExcessRows']]), moved.get(rg, -999))
    shares = []
    for (rg, y), v in rows.items():
        tg = nat.get((y, v[0]), (float('nan'),))[0]
        if tg > 0: shares.append(v[2] / tg)
    shares.sort(); qs = [shares[int(len(shares) * p)] for p in (0.25, 0.5, 0.75)] if shares else [0, 0, 0]
    for (rg, y), (nn, t, need, phi, months, bought, war, excess_rows, mv) in rows.items():
        prev = rows.get((rg, y - 1))
        if prev is None or prev[0] != nn or (y - 1, nn) not in nat or (y, nn) not in nat: continue
        tg, tn = nat[(y, nn)]; tg0, tn0 = nat[(y - 1, nn)]
        s1, s0 = need / tg, prev[2] / tg0
        if not (s1 > 0 and s0 > 0): continue
        lr = abs(math.log(s1 / s0))
        tr = tier.get((rg, y), '?'); type_abs['格 ' + tr].append(lr)
        pk = popk.get((rg, y))
        if pk is not None: type_abs['人口 ' + ('＜100千' if pk < 100 else '＜500千' if pk < 500 else '＜2000千' if pk < 2000 else '≧2000千')].append(lr)
        type_abs['取り分の 4 分位 ' + str(sum(1 for qv in qs if s0 >= qv) + 1)].append(lr)
        if rg in port_names: type_abs['港の州（9 国の港の表に出る州）'].append(lr)
        type_abs['割譲の後 3 年以内' if 0 <= y - mv <= 3 else '割譲の後 3 年以内でない'].append(lr)
        # 〈追加〉自己相関：前年の ln rs と当年の ln rs（揺れが戻るか）
        p2 = rows.get((rg, y - 2))
        if p2 is not None and p2[0] == nn and (y - 2, nn) in nat:
            sm = p2[2] / nat[(y - 2, nn)][0]
            if sm > 0: auto_a.append(math.log(s0 / sm)); auto_b.append(math.log(s1 / s0))
        # 〈追加〉b2（買えない）の行のうち、同じ年に need を下回った月がある（g7ExcessRows＞0）＝年の中の need の道で全速の欲しい量を過大に見た恐れ
        if phi < LOW:
            rs = s1 / s0; rth = (tg / tn) / (tg0 / tn0); rb = tn / tn0
            nh = need / (max(1.0, rs) * max(1.0, rth) * max(1.0, rb))
            if t < LOW * nh and not (months < 12 and not (bought > 0)) and not (war > LOW * nh - t) and bought < 0.5 * full_speed_want(prev[1], nh):
                b2_g7[1 if excess_rows > 0 else 0] += 1

P(f'(i) 港の州の潜在量（tradePotentialWeight）の比の 10／50／90 点：隣の四半期 律速 {quant(adj["律速"])}（n={len(adj["律速"])}）・律速でない {quant(adj["律速でない"])}（n={len(adj["律速でない"])}）')
P(f'    同じ四半期の前年比 律速 {quant(yoy["律速"])}（n={len(yoy["律速"])}）・律速でない {quant(yoy["律速でない"])}（n={len(yoy["律速でない"])}）')
P(f'    港の中の取り分（その国の港の和に対する）の比：隣の四半期 {quant(adj_sh)}（n={len(adj_sh)}）・同じ四半期の前年比 {quant(yoy_sh)}（n={len(yoy_sh)}）')
P('州の型ごとの |ln 取り分の前年比| の中央／90 点（前年あり・全 φ）：')
for k in sorted(type_abs):
    v = type_abs[k]
    if len(v) >= 50: P(f'  {k}\tn={len(v)}\t中央 {st.median(v):.3f}\t90 点 {sorted(v)[int(0.9 * (len(v) - 1))]:.3f}')
if auto_a:
    ma, mb = st.fmean(auto_a), st.fmean(auto_b)
    cov = sum((x - ma) * (y - mb) for x, y in zip(auto_a, auto_b)); va = sum((x - ma) ** 2 for x in auto_a); vb = sum((y - mb) ** 2 for y in auto_b)
    P(f'〈追加〉ln 取り分の前年比の 1 年の自己相関 {cov / math.sqrt(va * vb):.3f}（n={len(auto_a)}・負＝上がった翌年に戻る揺れ）')
P(f'〈追加〉b2（買えない）の行のうち同じ年に need を下回った月がある {b2_g7[1]}・無い {b2_g7[0]}（無い方が〈年中ずっと need の下で買いが半分未満〉＝買えないの確かな側）')
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
