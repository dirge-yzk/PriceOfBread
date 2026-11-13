# (1109) c106rn 設計の材料：段 R の φ＜0.99 の州×年を 3 つに分ける（事前登録 D:/PoB_work/d1109_work/prereg.txt §0〜§2・§3(ii)(iii)）。
# 読むだけ：<腕の ins>/trrail_s*.tsv と trnation_s*.tsv。呼び方：python -X utf8 -I split_rn.py <ins のフォルダ> <出力の txt>
import sys, os, glob, math, statistics as st

ins, outp = sys.argv[1], sys.argv[2]
LOW = 0.99
BUILD, DECAY = 0.15, 0.02   # Coefficients.TransportBuildRate・TransportDecay（m1103 の既定・Coefficients.cs:1221・:1215）

def quant(v, ps):
    if not v: return ['nan'] * len(ps)
    v = sorted(v); n = len(v)
    return [f'{v[min(n - 1, int(p * (n - 1) + 0.5))]:.3f}' for p in ps]

def ranks(v):
    o = sorted(range(len(v)), key=lambda i: v[i]); r = [0.0] * len(v)
    i = 0
    while i < len(o):
        j = i
        while j + 1 < len(o) and v[o[j + 1]] == v[o[i]]: j += 1
        for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
        i = j + 1
    return r

def spearman(a, b):
    if len(a) < 3: return float('nan')
    ra, rb = ranks(a), ranks(b); ma, mb = st.fmean(ra), st.fmean(rb)
    num = sum((x - ma) * (y - mb) for x, y in zip(ra, rb))
    den = math.sqrt(sum((x - ma) ** 2 for x in ra) * sum((y - mb) ** 2 for y in rb))
    return num / den if den > 0 else float('nan')

def full_speed_want(t_prev, need):
    # 前年末の T から need を 12 か月追う全速の欲しい容量の和（Transport.cs:227 の式・買えた分だけ積む＝全部買えたとする）
    t, w = t_prev, 0.0
    for _ in range(12):
        td = t * (1 - DECAY)
        want = BUILD * max(0.0, need - td) + DECAY * min(td, need)
        w += want; t = td + want
    return w

FACT = ['S', 'C', 'B']
SUBSETS = [s for s in (('S',), ('C',), ('B',), ('S', 'C'), ('S', 'B'), ('C', 'B'), ('S', 'C', 'B'))]

tot = dict(rows=0, low=0, Z=0, Z1=0, Z2=0, a=0, a2=0, c=0, b0=0, b1=0, b2=0, b3=0, b3n=0, excess_rows=0, highphi=0)
att = dict(short=0, nothreat_ok=0, ok=0)
suff = {s: 0 for s in SUBSETS}; single_any = 0; joint_only = 0; none_ok = 0
rs_all, rs_low, rth_low, rb_low = [], [], [], []
fb_a, fb_b = [], []
per_seed = []
seeds = sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv')), key=lambda p: int(p.rsplit('_s', 1)[1].split('.')[0]))
for path in seeds:
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    nat = {}
    with open(os.path.join(ins, f'trnation_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: nat[(int(c[ix['year']]), c[ix['nation']])] = (float(c[ix['railTargetIndex']]), float(c[ix['railTargetNoThreatIndex']]))
            except (ValueError, IndexError): pass
    rows = {}; disc = {}
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            need = float(c[ix['needEnd']])
            if not (need > 0): continue
            y = int(c[ix['year']]); nn = c[ix['nation']]
            if nn not in disc or y < disc[nn]: disc[nn] = y
            if c[ix['waterFlag']] == '1': continue
            rows[(c[ix['region']], y)] = (nn, float(c[ix['railTrackEnd']]), need, float(c[ix['phiEnd']]), int(c[ix['monthsCount']]),
                                         float(c[ix['boughtCapPerYear']]), float(c[ix['warLossPerYear']]), int(c[ix['g7ExcessRows']]))
    s_low = 0
    for (rg, y), (nn, t, need, phi, months, bought, war, excess_rows) in rows.items():
        tot['rows'] += 1
        tg, tn = nat.get((y, nn), (float('nan'), float('nan')))
        prev = rows.get((rg, y - 1))
        valid_prev = prev is not None and prev[0] == nn and (y - 1, nn) in nat
        if valid_prev:
            tg0, tn0 = nat[(y - 1, nn)]
            s1, s0 = need / tg, prev[2] / tg0
            th1, th0 = tg / tn, tg0 / tn0
            rs, rth, rb = s1 / s0, th1 / th0, tn / tn0
            if all(math.isfinite(x) and x > 0 for x in (rs, rth, rb)):
                rs_all.append(rs)
            else: valid_prev = False
        # (iii) 帰還の向き（全 φ の行）：φ の前年の変化と当年の ln rs
        if valid_prev:
            p2 = rows.get((rg, y - 2))
            if p2 is not None and p2[0] == nn:
                fb_a.append(prev[3] - p2[3]); fb_b.append(math.log(rs))
        # attrib ② の再現（脅威を除いた分母なら足りるか）
        if phi < LOW:
            if math.isfinite(tg) and tg > 0 and t >= need * tn / tg: att['nothreat_ok'] += 1
            else: att['short'] += 1
        else: att['ok'] += 1
        if not (phi < LOW):
            tot['highphi'] += 1; continue
        tot['low'] += 1; s_low += 1
        if excess_rows > 0: tot['excess_rows'] += 1
        if not valid_prev:
            tot['Z'] += 1
            if prev is not None and prev[0] != nn: tot['Z2'] += 1
            else: tot['Z1'] += 1
            continue
        rs_low.append(rs); rth_low.append(rth); rb_low.append(rb)
        r = {'S': rs, 'C': rth, 'B': rb}
        ok = {}
        for sub in SUBSETS:
            nh = need
            for k in sub: nh /= max(1.0, r[k])
            ok[sub] = t >= LOW * nh
            if ok[sub]: suff[sub] += 1
        singles = [k for k in FACT if ok[(k,)]]
        if singles: single_any += 1
        elif any(ok[s] for s in SUBSETS): joint_only += 1
        else: none_ok += 1
        best = None
        for size in (1, 2, 3):
            cand = [s for s in SUBSETS if len(s) == size and ok[s]]
            if cand:
                # 最小の組の中で ln r が最大の要因（組が複数なら全ての組の要素から）
                elems = sorted({k for s in cand for k in s}, key=lambda k: -math.log(max(1.0, r[k])))
                best = elems[0]; break
        if best == 'S': tot['a'] += 1
        elif best == 'B': tot['a2'] += 1
        elif best == 'C': tot['c'] += 1
        else:
            nh_all = need / (max(1.0, rs) * max(1.0, rth) * max(1.0, rb))
            if months < 12 and not (bought > 0): tot['b0'] += 1
            elif war > LOW * nh_all - t: tot['b1'] += 1
            elif bought < 0.5 * full_speed_want(prev[1], nh_all): tot['b2'] += 1
            else:
                tot['b3'] += 1
                if y - disc.get(nn, y) <= 3: tot['b3n'] += 1
    per_seed.append((sd, s_low))

lines = []
P = lines.append
P(f'== {ins}')
P(f'母集合（水運でない・needEnd＞0）州×年 {tot["rows"]}・φ＜0.99 {tot["low"]}（{tot["low"] / max(1, tot["rows"]):.3f}）・φ≧0.99 {tot["highphi"]}')
P(f'自己検査①：取り分の前年比（前年の行のある州×年・全 φ）の 10／50／90 点 {"／".join(quant(rs_all, (0.1, 0.5, 0.9)))}（n={len(rs_all)}・v2 は 0.37／1.00／2.72）')
tt = att['short'] + att['nothreat_ok'] + att['ok']
P(f'自己検査②：attrib ② の再現 足りない {att["short"] / tt:.3f}・脅威を除けば足りる {att["nothreat_ok"] / tt:.3f}・φ≧0.99 {att["ok"] / tt:.3f}（attrib は 0.072／0.409／0.519）')
P(f'φ＜0.99 で同じ年に need を超えた月がある（g7ExcessRows＞0）{tot["excess_rows"]}（{tot["excess_rows"] / max(1, tot["low"]):.3f}）')
nv = tot['low'] - tot['Z']
P(f'前年なし Z {tot["Z"]}（Z1 前年の行が無い {tot["Z1"]}・Z2 国が変わった {tot["Z2"]}）・前年あり {nv}')
P('主の表（前年ありの φ＜0.99・要因を前年に止めれば足りる行・重なりあり）：' + '・'.join(f'{"+".join(s)} {suff[s]}（{suff[s] / max(1, nv):.3f}）' for s in SUBSETS))
P(f'  単独のどれかで足りる {single_any}（{single_any / max(1, nv):.3f}）・組でだけ足りる {joint_only}（{joint_only / max(1, nv):.3f}）・全てを止めても足りない {none_ok}（{none_ok / max(1, nv):.3f}）')
P(f'3 分け（射影・同着は最小の組の中で ln r 最大）：(a) 取り分 {tot["a"]}・(a2) 基底の伸び {tot["a2"]}・(c) 脅威 {tot["c"]}・(b) {tot["b0"] + tot["b1"] + tot["b2"] + tot["b3"]}〔b0 買い手の居ない月 {tot["b0"]}・b1 戦災 {tot["b1"]}・b2 買えない {tot["b2"]}・b3 追いつき中 {tot["b3"]}（うち発見から 3 年以内 {tot["b3n"]}）〕・Z {tot["Z"]}')
tot_sum = tot['a'] + tot['a2'] + tot['c'] + tot['b0'] + tot['b1'] + tot['b2'] + tot['b3'] + tot['Z']
P(f'  検算：和 {tot_sum} ＝ φ＜0.99 {tot["low"]}：{"一致" if tot_sum == tot["low"] else "不一致"}')
den = max(1, tot['low'])
P(f'  割合（φ＜0.99 の全体に対し）：(a)+(a2) {(tot["a"] + tot["a2"]) / den:.3f}〔取り分 {tot["a"] / den:.3f}・基底 {tot["a2"] / den:.3f}〕・(c) {tot["c"] / den:.3f}・(b) {(tot["b0"] + tot["b1"] + tot["b2"] + tot["b3"]) / den:.3f}〔b2 買えない {tot["b2"] / den:.3f}〕・Z {tot["Z"] / den:.3f}')
P(f'φ＜0.99（前年あり）の前年比の 10／50／90 点：取り分 {"／".join(quant(rs_low, (0.1, 0.5, 0.9)))}・脅威の倍率 {"／".join(quant(rth_low, (0.1, 0.5, 0.9)))}・基底 {"／".join(quant(rb_low, (0.1, 0.5, 0.9)))}')
P(f'(iii) 帰還の向き：全 φ の行で (φ_y−1 − φ_y−2) と ln(取り分の前年比) の順位相関 {spearman(fb_a, fb_b):.3f}（n={len(fb_a)}）')
P('種ごとの φ＜0.99 の行：' + ' '.join(f's{a}={b}' for a, b in per_seed))
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(lines) + '\n')
print('\n'.join(lines))
