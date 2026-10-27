# (595) 線 aic の読み手（読むだけ）：AI の工場の選び方（根まで作れない財を候補から落とす）の狙いの数字と記録。
# 使い方：PYTHONUTF8=1 python g_lines.py <腕名>=<計器の出力フォルダ（…/ins）> ...（種は存在する分だけ）
# 読む表：aib_s（列は足されている＝旧の腕〔基線 keep_592〕の aib_s には根の列が無い＝その腕では G1 は〈列なし〉と出す）／region_s（本国の州を保つ割合）
# 出す物：
#  G1 州の最良のうち完成しても根が無い個体の割合（bestRootDeadCount÷bestRegionCount・種ごと）／G1b 需要の信号があるのに根が無くて落ちた候補の数
#  G2 着工した財の分布（防水布の割合・種ごと〔基線 m592・N1・N2 の 30 種の最小は 0.175〕）／G2b 落ちた候補の行き止まりの財（blocker）
#  G3 着工の総数（種ごと）
#  記録：英・プロイセンの軍需の着工の数／1880 年に本国の州を保つ割合（種ごと・中央値）／完成した国営の棟のうち実産出が 0 の割合（builtIdleUnits÷builtUnits・四半期の和・種ごと・判定に使わない）
#  判定のブロック（事前登録 §6・追記 1・追記 3）：ラベル A（旗 0）と B（旗 1）を渡すと出す。種 1 は判定から外す（別の行に記録）。閾値は事前登録の値をそのまま定数で書く（結果を見て変えない）。
#   R0＝A の種 2〜5 の G1 の中央値 ≧ 0.20／G1（コードが動いた確認・機構が働いた証拠には数えない）＝B の種 2〜10 の全てで G1 ≦ 0.02 かつ rootDroppedCount ＞ 0／
#   G2＝B の種 2〜10 の 9 種中 8 種以上で防水布の割合 ≦ 0.175 かつ 9 種の中央値 ≦ 0.09／G3＝B の種 2〜10 の着工の総数の中央値 ≧ 1,824。
import csv, glob, os, sys, collections, statistics as st
csv.field_size_limit(10**9)
MIL = {'火砲', '小銃', '弾薬', '帆船', '軍艦', '汽船', '機関車', '機関銃', '戦車', '装甲艦', '弩級戦艦'}
NINE = ['イギリス', 'フランス', 'プロイセン', 'アメリカ', '日本', '清', 'ロシア', 'オスマン', 'オーストリア']
def rd(path):
    fh = open(path, encoding='utf-8-sig', newline=''); r = csv.reader(fh, delimiter='\t'); h = next(r)
    return {n: i for i, n in enumerate(h)}, r
def base(n):
    for m in NINE:
        if m in n: return m
    return None
def med(v):
    v = [x for x in v if x == x]; return st.median(v) if v else float('nan')
def parse(cell, acc):
    for kv in cell.split(';'):
        if ':' in kv:
            g, c = kv.rsplit(':', 1)
            try: acc[g] += int(c)
            except ValueError: pass
def seeds_of(d):
    return sorted(int(os.path.basename(p)[5:-4]) for p in glob.glob(os.path.join(d, 'aib_s*.tsv')))
R0_MIN, G1_MAX, G2_MAX, G2_MED, G2_NEED, G3_MIN = 0.20, 0.02, 0.175, 0.09, 8, 1824   # 事前登録の定数（結果を見て変えない）
JUDGE_ALL = list(range(2, 11)); JUDGE_R0 = [2, 3, 4, 5]
RES = {}   # ラベル → 種ごとの値の辞書（判定のブロックが読む）
for lab, d in [a.split('=', 1) for a in sys.argv[1:]]:
    print(f'######## {lab}  {d}')
    SS = seeds_of(d)
    ws_share = []; tot_started = []; dead_share = []; mil_uk = []; mil_pr = []
    blockers = collections.Counter(); dropped = collections.Counter(); bestdead = collections.Counter(); drop_by_seed = []; idle_share = []
    for s in SS:
        ix, r = rd(os.path.join(d, f'aib_s{s}.tsv'))
        has = 'bestRegionCount' in ix
        started = 0; ws = 0; best = 0; dead = 0; drop = 0; mu = 0; mp = 0; bu = 0; bi = 0
        for x in r:
            started += int(x[ix['startedCount']])
            sg = collections.Counter(); parse(x[ix['startedGoods']], sg); ws += sg['防水布']
            nat = base(x[ix['nation']])
            if nat in ('イギリス', 'プロイセン'):
                m = sum(v for g, v in sg.items() if g in MIL)
                if nat == 'イギリス': mu += m
                else: mp += m
            if has:
                best += int(x[ix['bestRegionCount']]); dead += int(x[ix['bestRootDeadCount']]); drop += int(x[ix['rootDroppedCount']])
                parse(x[ix['rootDroppedGoods']], dropped); parse(x[ix['bestRootDeadGoods']], bestdead)
                if 'rootDroppedBlockers' in ix: parse(x[ix['rootDroppedBlockers']], blockers)
            if 'builtUnits' in ix:
                bu += int(x[ix['builtUnits']]); bi += int(x[ix['builtIdleUnits']])
        tot_started.append(started); ws_share.append(ws / max(1, started)); mil_uk.append(mu); mil_pr.append(mp)
        dead_share.append(dead / best if has and best > 0 else float('nan')); drop_by_seed.append(drop if has else None); idle_share.append(bi / bu if bu > 0 else float('nan'))
    f = lambda v: [round(x, 3) for x in v]
    print(f'# 種 {SS}')
    print('G1  州の最良のうち根が無い個体の割合（種ごと）:', f(dead_share), ' 中央値 %.3f' % med(dead_share))
    print('G2  着工した財に占める防水布の割合（種ごと）:', f(ws_share), ' 中央値 %.3f 最小 %.3f 最大 %.3f' % (med(ws_share), min(ws_share) if ws_share else float('nan'), max(ws_share) if ws_share else float('nan')))
    print('G1  rootDroppedCount（種ごと）:', drop_by_seed)
    print('記録 完成した国営の棟のうち実産出 0 の割合（種ごと・四半期の和）:', f(idle_share))
    print('G3  着工の総数（種ごと）:', tot_started, ' 中央値 %s 最小 %s' % (med(tot_started), min(tot_started) if tot_started else None))
    if dropped:
        print('G1b 落ちた候補の財（上位）:', dropped.most_common(12))
        print('G2b 行き止まりの財 blocker（上位）:', blockers.most_common(12))
        print('    旧の選び方の最良が根無しだった財（上位）:', bestdead.most_common(8))
    else:
        print('G1/G1b/G2b: 根の列が無いか、落ちた候補が 0（旗 0 の腕または基線）')
    print('記録 英の軍需の着工（種ごと）:', mil_uk, ' プロイセン:', mil_pr)
    RES[lab] = dict(seeds=SS, dead=dict(zip(SS, dead_share)), ws=dict(zip(SS, ws_share)), tot=dict(zip(SS, tot_started)), drop=dict(zip(SS, drop_by_seed)))
    # 記録 本国の州を保つ割合（1880 年・種ごと）：1783 年の四半期 0 で Core の州のうち、1880 年の最後の四半期に同じ国の名で active の割合
    keep = {'イギリス': [], 'プロイセン': []}
    for s in SS:
        p = os.path.join(d, f'region_s{s}.tsv')
        if not os.path.exists(p): continue
        ix, r = rd(p)
        home = collections.defaultdict(set); last = {}; lastq = -1
        rows = []
        for x in r:
            q = int(x[ix['quarter']]); y = int(x[ix['year']])
            rows.append((q, y, x[ix['nation']], x[ix['region']], x[ix['tier']], x[ix['activeFlag']]))
        for q, y, n, rg, t, a in rows:
            b = base(n)
            if q == 0 and t == 'Core' and b in keep: home[b].add(rg)
        qy = max((q for q, y, n, rg, t, a in rows if y == 1880), default=None)
        if qy is None: continue
        now = collections.defaultdict(set)
        for q, y, n, rg, t, a in rows:
            if q == qy and a == '1':
                b = base(n)
                if b in keep: now[b].add(rg)
        for b in keep:
            if home[b]: keep[b].append(len(home[b] & now[b]) / len(home[b]))
    for b, v in keep.items():
        if v: print(f'記録 {b} の本国の州を保つ割合（1880 年・種ごと）:', f(v), ' 中央値 %.2f' % med(v))

# ---- 判定のブロック（ラベル A と B が揃ったときだけ） ----
if 'A' in RES and 'B' in RES:
    A, B = RES['A'], RES['B']
    print('######## 判定（種 1 は判定から外す＝記録のみ）')
    print('種 1 の記録 A 根無し割合 %s ・ B 根無し割合 %s ・ B 防水布 %s ・ B 着工 %s' % (A['dead'].get(1), B['dead'].get(1), B['ws'].get(1), B['tot'].get(1)))
    r0v = [A['dead'][s] for s in JUDGE_R0 if s in A['dead']]
    r0 = len(r0v) == len(JUDGE_R0) and med(r0v) >= R0_MIN
    print('R0  A 種 2〜5 の根無し割合 %s 中央値 %.3f（≧ %.2f）→ %s' % ([round(x, 3) for x in r0v], med(r0v), R0_MIN, 'OK' if r0 else 'NG'))
    miss = [s for s in JUDGE_ALL if s not in B['dead']]
    if miss: print('B に種が足りない:', miss)
    g1v = [(s, B['dead'].get(s), B['drop'].get(s)) for s in JUDGE_ALL]
    g1 = not miss and all(d is not None and d <= G1_MAX and dr is not None and dr > 0 for s, d, dr in g1v)
    print('G1  （コードが動いた確認）種 2〜10 の (種, 根無し割合, rootDropped):', [(s, None if d is None else round(d, 4), dr) for s, d, dr in g1v], '→', 'OK' if g1 else 'NG')
    wsv = [B['ws'][s] for s in JUDGE_ALL if s in B['ws']]
    n_ok = sum(1 for x in wsv if x <= G2_MAX)
    g2 = not miss and n_ok >= G2_NEED and med(wsv) <= G2_MED
    print('G2  B 種 2〜10 の防水布の割合 %s ・ 閾値以下 %d/9（≧ %d）・中央値 %.3f（≦ %.2f）→ %s' % ([round(x, 3) for x in wsv], n_ok, G2_NEED, med(wsv), G2_MED, 'OK' if g2 else 'NG'))
    totv = [B['tot'][s] for s in JUDGE_ALL if s in B['tot']]
    g3 = not miss and med(totv) >= G3_MIN
    print('G3  B 種 2〜10 の着工の総数 %s 中央値 %s（≧ %d）→ %s' % (totv, med(totv), G3_MIN, 'OK' if g3 else 'NG'))
    print('総合（R0・G1・G2・G3 が全て OK）:', 'OK＝機構が働いた' if (r0 and g1 and g2 and g3) else 'NG＝採らない（G4 の番人は別・外れの扱いは事前登録 §6 のとおり）')
