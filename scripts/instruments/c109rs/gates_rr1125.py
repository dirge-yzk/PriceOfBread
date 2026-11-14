# (1125) c109rs：G3 に水運の州の線路の分を入れた（gates_rr1103.py から変えたのはこの 3 行だけ）。G4 は移しの読み直しを受け手の次の月の頭へ（計器側）。
# (1103) 追記（S11 の読みの後に直した＝結果の後の読み手の直し・門の規則は同じ）：G1′／G5 の定数の表 trstat を種 1 の名で決め打ちしていた（種 11〜20 で 0 件になる）＝その腕の最初の種の trstat を読む。
# (1103) 合流の写し（gates_rr.py から変えたのは種の範囲を第 5 引数で渡す口〔既定 1-10〕・ヌル腕 N1・N2 の G6 と G5 は種 1〜10 のまま読む・無い表を飛ばす所だけ＝門の規則は同じ）。
# (1097) c105rr 段 R の門の読み手（事前登録 measurements/m1097_20261113_prereg_rr.txt §3）。
# 使い方: python -X utf8 -I gates_rr.py <腕の生 ins のフォルダ> <基準の生 keep_1096/base_ins> <ヌル腕 n1> <ヌル腕 n2> > out
# G0＝最初の国が機関車を見つける年より前の行が基準とバイト一致（宣言した表 trrail と trnation の末尾 3 列を除く・年の列の無い表は記録）。
# G1・G2・G3・G4・G7＝trrail_s の数の和（計器が need を自分で組んだ値）。G6＝世界の貿易÷GDP（scalars_s・人口加重）の 10 種の中央が N1・N2 の中央の最小〜最大の内。G5＝trroute の側の陸の倍率の新÷旧（記録）。
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
arm, base, n1, n2 = sys.argv[1:5]
SEEDS = range(int(sys.argv[5].split('-')[0]), int(sys.argv[5].split('-')[1]) + 1) if len(sys.argv) > 5 else range(1, 11)
NSEEDS = range(1, 11)
out = []
def say(s): print(s, flush=True)

# ---------- 発見の年（種ごと・trnation の railGateFlag が 1 の最初の年） ----------
first = {}
for s in SEEDS:
    p = os.path.join(arm, f'trnation_s{s}.tsv')
    y0 = None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[ix['railGateFlag']] == '1':
                y = int(c[ix['year']]); y0 = y if y0 is None else min(y0, y)
    first[s] = y0
say('発見の年（種ごと・trnation の railGateFlag＝1 の最初の年＝年の終わりの印・記録）：' + ' '.join(f's{s}={first[s]}' for s in SEEDS))
# G0 の切り口＝基準の disc_s の E 行（発見の出来事）で row＝機関車 の最初の四半期の年（trnation の印は年の終わりの書き出しで、発見の年を 1 年遅く読む種がある＝読み手の誤りを直した）
for s in SEEDS:
    p = os.path.join(base, f'disc_s{s}.tsv'); y0 = None
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[ix['kind']] == 'E' and c[ix['row']] == '機関車':
                y = int(c[ix['year']]); y0 = y if y0 is None else min(y0, y)
    first[s] = y0
say('G0 の切り口（基準の disc_s の機関車の最初の発見の年・この年より前の行を比べる）：' + ' '.join(f's{s}={first[s]}' for s in SEEDS))

# ---------- G0 ----------
def year_ix(h):
    for k in ('year', 'Year', 'yr'):
        if k in h: return h.index(k)
    return -1
# 既知の揺れの列（ident.cfg の KNOWN と ident_check.py の経路の列＝ソースの行番号・計時）＝比べる前に外す（ident の判定と同じ扱い）
KNOWN = {'flow': ('sourceFile', 'sourceLine'), 'burscan': ('seconds',), 'clock': ('worldTotalPerMonth',), 'dep': ('callerLine',), 'cearn': ('outTop1Route', 'outTop2Route', 'inTop1Route'), 'cpop': ('outOtherTop1Route',)}
say('G0 の比べる前に外す列（既知の揺れ）：' + '・'.join(f'{k}:{",".join(v)}' for k, v in KNOWN.items()))
names = sorted(set(fn for fn in os.listdir(base) if fn.endswith('.tsv')))
g0_rows = collections.Counter(); g0_bad = collections.Counter(); g0_noyear = set(); g0_missing = set()
for fn in names:
    stem = fn.rsplit('_s', 1)[0]
    try: s = int(fn.rsplit('_s', 1)[1].split('.')[0])
    except ValueError: continue
    if s not in SEEDS or stem == 'trrail': continue
    pa, pb = os.path.join(arm, fn), os.path.join(base, fn)
    if not os.path.exists(pa): g0_missing.add(stem); continue
    Y = first[s]
    if Y is None: continue
    def rows(p, ncut):
        with open(p, encoding='utf-8', errors='replace') as f:
            h = f.readline().rstrip('\n').split('\t'); yi = year_ix(h)
            if yi < 0: return None
            drop = set(h.index(x) for x in KNOWN.get(stem, ()) if x in h)
            r = []
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                try: y = int(float(c[yi]))
                except (ValueError, IndexError): continue
                if y > Y + 2: break
                if y < Y:
                    if drop: c = [v for i2, v in enumerate(c) if i2 not in drop]; ln = '\t'.join(c)
                    r.append('\t'.join(c[:ncut]) if ncut else ln)
            return r
    ncut = 12 if stem == 'trnation' else 0   # trnation：末尾 3 列（宣言）を除く元の 12 列
    ra, rb = rows(pa, ncut), rows(pb, ncut)
    if ra is None or rb is None: g0_noyear.add(stem); continue
    if stem == 'flow': ra, rb = sorted(ra), sorted(rb)   # flow_s は呼び出し位置を除いた行の多重集合で比べる（ident_check.py と同じ）
    g0_rows[s] += len(rb)
    if ra != rb:
        nb = sum(1 for x, y in zip(ra, rb) if x != y) + abs(len(ra) - len(rb))
        g0_bad[(s, stem)] = nb
say(f'G0：比べた行（種ごと）' + ' '.join(f's{s}={g0_rows[s]}' for s in SEEDS) + f'・違う行のある（種,表）{len(g0_bad)}・年の列の無い表 {len(g0_noyear)}・腕に無い表 {len(g0_missing)}')
for k, v in list(g0_bad.items())[:40]: say(f'  G0 違う：種 {k[0]} {k[1]} {v} 行')
G0 = 'pass' if not g0_bad and all(g0_rows[s] > 0 for s in SEEDS if first[s]) else 'FAIL'

# ---------- trrail の和 ----------
tot = collections.Counter(); g3_max = 0.0; g3_rows = 0; g3_bad = 0; paid_w = 0.0; g3_water = 0
for s in SEEDS:
    if not os.path.exists(os.path.join(arm, f'trrail_s{s}.tsv')): say(f'trrail_s{s} が無い（旗 0 の前の木の生）'); continue
    with open(os.path.join(arm, f'trrail_s{s}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            for k in ('g2Rows', 'g2Viol', 'g7ExcessRows', 'g7FellRows', 'g7LockRows', 'g7Hold12Rows', 'g1ZeroSides', 'g1ZeroViol', 'g1JumpRows', 'g1PosSides', 'g1FormulaViol', 'g3NoShareRows', 'g4Moves', 'g4Viol'):
                tot[k] += int(c[ix[k]])
            p = float(c[ix['railFreightPaidGoldPerYear']]); q = float(c[ix['railFreightInsGoldPerYear']])
            tot['paid'] += p; tot['ins'] += q
            if c[ix['waterFlag']] == '1': paid_w += p; g3_water += 1   # (1125) 水運の州も G3 の比べに入れる（旧は continue で外していた）
            if p > 0 or q > 0:
                g3_rows += 1; rel = abs(p - q) / max(1e-9, p, q); g3_max = max(g3_max, rel)
                if rel > 1e-6: g3_bad += 1
say('trrail の和：' + '・'.join(f'{k} {v}' for k, v in tot.items()))
G1 = 'pass' if tot['g1ZeroViol'] == 0 and tot['g1JumpRows'] == 0 and tot['g1FormulaViol'] == 0 and tot['g1PosSides'] > 0 else 'FAIL'
G2 = 'pass' if tot['g2Viol'] == 0 and tot['g2Rows'] > 0 else 'FAIL'
G3 = 'pass' if tot['g3NoShareRows'] == 0 and g3_bad == 0 and tot['paid'] > 0 else 'FAIL'
G4 = ('pass' if tot['g4Viol'] == 0 else 'FAIL') + ('' if tot['g4Moves'] > 0 else '（移しが 0 件＝機構の試験だけで読む）')
G7 = 'pass' if tot['g7LockRows'] == 0 and tot['g7FellRows'] == tot['g7ExcessRows'] and tot['g7ExcessRows'] > 0 else 'FAIL'   # 追記 5：余りの行＞0（空の母集合で緑にしない）
say(f'G3：州×年（水運の州を含む・うち水運 {g3_water} 行）{g3_rows} 行・払いと計器の相対の差の最大 {g3_max:.3g}・1e-6 を超えた行 {g3_bad}・水運の州へ払った線路の分 {paid_w:.6g}（記録）')

# ---------- G6 ----------
YS = [1790, 1820, 1840, 1850, 1860, 1870, 1880, 1900, 1913, 1930, 1949]
def tg_med(d, seeds=SEEDS):
    byy = collections.defaultdict(list)
    for s in seeds:
        p = os.path.join(d, f'scalars_s{s}.tsv')
        if not os.path.exists(p): continue
        acc = collections.defaultdict(lambda: [0.0, 0.0])
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                try: y = int(float(c[ix['year']])); q = int(c[ix['quarter']]); pk = float(c[ix['popK']]); tg = float(c[ix['tradeToGdpRatio']])
                except (ValueError, KeyError, IndexError): continue
                if y not in YS or not (pk > 0) or tg != tg: continue
                acc[(y, q)][0] += pk * tg; acc[(y, q)][1] += pk
        per = collections.defaultdict(list)
        for (y, q), (a, b) in acc.items():
            if b > 0: per[y].append(a / b)
        for y, v in per.items(): byy[y].append(st.mean(v))
    return {y: st.median(v) for y, v in byy.items() if v}
ma, m1, m2 = tg_med(arm), tg_med(n1, NSEEDS), tg_med(n2, NSEEDS)
g6bad = []
for y in YS:
    if y in ma and y in m1 and y in m2:
        lo, hi = min(m1[y], m2[y]), max(m1[y], m2[y]); ok = lo <= ma[y] <= hi
        say(f'G6 {y}：腕 {ma[y]:.4f}・N1 {m1[y]:.4f}・N2 {m2[y]:.4f}・{"内" if ok else "外"}')
        if not ok: g6bad.append(y)
G6 = 'pass' if not g6bad else f'FAIL（外の年 {g6bad}）'

# ---------- G5 と働いた大きさ（G1′）：trroute の側（水運の州を除く）で、その年にその国が機関車を見つけている側のうち、陸の倍率が旧の式（3×鉄道の地形）と違う側の割合 ----------
def g5(d, seeds=SEEDS):
    Z = {}
    if not os.path.exists(os.path.join(d, f'trstat_s{seeds[0]}.tsv')): return 0, 0, (float('nan'),) * 3
    with open(os.path.join(d, f'trstat_s{seeds[0]}.tsv'), encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f:
            c = ln.rstrip('\n').split('\t')
            if c[0] == 'Z': Z[c[ix['name']]] = float(c[ix['value']])
    TN = ['Plain', 'WaterMajor', 'WaterMinor', 'Hill', 'Forest', 'Mountain', 'Barren']
    rail = [Z['TerrainRailCost[%s]' % t] for t in TN]; ratio = Z['LandSeaCostRatio'] * Z['RailCostFraction']
    post = 0; diff = 0; ratios = []
    for s in seeds:
        if not (os.path.exists(os.path.join(d, f'trnation_s{s}.tsv')) and os.path.exists(os.path.join(d, f'trroute_s{s}.tsv'))): continue
        disc = set()
        with open(os.path.join(d, f'trnation_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                if c[ix['railGateFlag']] == '1': disc.add((int(c[ix['year']]), c[ix['nation']]))
        with open(os.path.join(d, f'trroute_s{s}.tsv'), encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                if not c[ix['sellerTerrainCode']]: continue
                y = int(c[ix['year']])
                for side, nk in (('Seller', 'sellerNation'), ('Buyer', 'buyerNation')):
                    t = int(c[ix[side.lower() + 'TerrainCode']])
                    if t in (1, 2) or (y, c[ix[nk]]) not in disc: continue
                    m = float(c[ix['terrainCost' + side]]) * float(c[ix['landSeaRatio' + side]]); old = ratio * rail[t]
                    post += 1
                    if abs(m - old) > 1e-9 * old: diff += 1
                    ratios.append(m / old)
    ratios.sort()
    q = lambda p: ratios[min(len(ratios) - 1, int(p * len(ratios)))] if ratios else float('nan')
    return post, diff, (q(0.1), q(0.5), q(0.9))
for nm, d in (('腕', arm), ('M', base), ('N1', n1), ('N2', n2)):
    post, diff, qs = g5(d, NSEEDS if nm in ('N1', 'N2') else SEEDS)
    say(f'G1′（働いた大きさ）／G5：{nm} 発見後の側 {post}・旧の式と違う側 {diff}（割合 {diff / max(1, post):.4f}）・新÷旧の 10／50／90 点 {qs[0]:.3f}／{qs[1]:.3f}／{qs[2]:.3f}')
    if nm == '腕': G1b = 'pass' if post > 0 and diff / post >= 0.01 else 'FAIL'
say(f'判定：G0 {G0}・G1 {G1}・G1′ {G1b}・G2 {G2}・G3 {G3}・G4 {G4}・G7 {G7}・G6 {G6}（弱い門）')
