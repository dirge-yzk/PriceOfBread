# (575) 線 bd：建屋と住宅の段の耐久の読み手（読むだけ・採否の門を出す）。事前登録 measurements/bd_575_20261026_prereg.txt [4] の G1・G2・G3・G3a・G5 と記録 R1〜R4。
# 使い方：python -X utf8 an_bd.py <新の出力フォルダ> <基線の出力フォルダ> [種=1,2,...]
#   新の出力フォルダ：旗 BuildingLifeEnabled=1 の計器つき実行の POB_INSTRUMENTS（fb_s<種>.tsv・hs_s<種>.tsv がある）。
#   基線の出力フォルダ：旗 0 の同じ種の fb_s<種>.tsv（腕 A の生または m569 の生 D:/PoB_work/keep_560/m560_ins）。
#   種＝読む種（既定＝両方にある全ての種）。門は種 1〜10 の全部（事前登録 [3]）。煙試験（種 21）は種=21 で読む（門に使わない）。
#   ⚠ fb_s の行は挙げた国（POB_INSTRUMENTS_NATIONS）だけ毎四半期・他の国は年末。G5 は毎四半期の行が 200 以上ある国×種だけを系列にする。
#   ⚠ costXxx は首都州のその段の限界費用（自己費用＋投入費）＝投入が k で割られた後の〈役務 1 単位あたり〉。門が閉じた段は nan。
import csv, glob, os, sys, statistics as st, math
NATS = {"英": "イギリス", "仏": "フランス", "普": "プロイセン", "米": "アメリカ", "清": "清"}
STAGES = [("Wood", "outWood", "costWood"), ("Brick", "outBrick", "costBrick"), ("Cast", "outCast", "costCast"), ("Steel", "outSteel", "costSteel"), ("RC", "outRC", "costRC")]
def rows(p):
    if not os.path.exists(p): return []
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r, None)
        if h is None: return []
        return [dict(zip(h, x)) for x in r if len(x) == len(h)]
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def fin(x): return x == x and abs(x) != float('inf')
def med(v):
    v = [x for x in v if fin(x)]
    return st.median(v) if v else float('nan')
def ok(b): return 'OK' if b else 'NG'
def f3(x): return 'nan' if not fin(x) else f'{x:.3f}'
def spearman(xs, ys):
    n = len(xs)
    if n < 3: return float('nan')
    def rank(v):
        o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
        while i < n:
            j = i
            while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
            for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
            i = j + 1
        return r
    rx, ry = rank(xs), rank(ys); mx, my = sum(rx) / n, sum(ry) / n
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sx * sy) if sx > 0 and sy > 0 else float('nan')
def seeds_in(d, pat='fb_s'):
    return sorted(int(os.path.basename(p)[len(pat):-4]) for p in glob.glob(os.path.join(d, pat + '*.tsv')))
def share(x, k):
    tot = fl(x['outTotal'])
    return fl(x[k]) / tot if tot > 1e-9 else float('nan')

def ratio_med(rs, a, b):
    """行の費用 a÷b（両方が正で有限）の中央値。"""
    v = []
    for x in rs:
        ca, cb = fl(x[a]), fl(x[b])
        if fin(ca) and fin(cb) and ca > 0 and cb > 0: v.append(ca / cb)
    return med(v)

def uk_window_share(rs, key, q0=20, q1=120):
    return med([share(x, key) for x in rs if x['nation'] == NATS["英"] and q0 <= int(x['quarter']) <= q1])

def rank_rho(rs):
    """国×四半期の行ごとに、門が開いた段（費用が正で有限）が 3 本以上あれば ρ(安さ, 割合) を出し、中央値を返す。"""
    v = []
    for x in rs:
        cs, ss = [], []
        for _, o, c in STAGES:
            cv = fl(x[c])
            if fin(cv) and cv > 0:
                cs.append(-cv); ss.append(share(x, o))
        if len(cs) >= 3 and all(fin(s) for s in ss):
            v.append(spearman(cs, ss))
    return med(v), len(v)

def gate_violations(rs):
    """段の産出が正なのに、その段の門が開いた本土の州が 0 の行の数（鋳鉄・鋼・鉄筋コンクリート造）。"""
    n = 0
    for x in rs:
        for g, o in (("gateCast", "outCast"), ("gateSteel", "outSteel"), ("gateRC", "outRC")):
            if fl(x[o]) > 1e-9 and int(float(x[g])) == 0: n += 1
    return n

def jump_series(allrows):
    """(種, 国, 段) の系列のうち、毎四半期の行がある物の 4 四半期の割合の動きの最大。"""
    out = {}
    for s, rs in allrows.items():
        by = {}
        for x in rs: by.setdefault(x['nation'], []).append(x)
        for n, xs in by.items():
            if len(xs) < 200: continue
            xs.sort(key=lambda x: int(x['quarter']))
            for key in ("outBrick", "outCast"):
                sh = [share(x, key) for x in xs]
                m = 0.0
                for i in range(4, len(sh)):
                    if fin(sh[i]) and fin(sh[i - 4]): m = max(m, abs(sh[i] - sh[i - 4]))
                out[(s, n, key)] = m
    return out

def main():
    if len(sys.argv) < 3: print(__doc__); sys.exit(1)
    nd, bd = sys.argv[1], sys.argv[2]
    sel = None
    for a in sys.argv[3:]:
        if a.startswith('種='): sel = [int(t) for t in a[2:].split(',')]
    S = sorted(set(seeds_in(nd)) & set(seeds_in(bd)))
    if sel is not None: S = [s for s in S if s in sel]
    print(f'# 読む種 {S}（新 {nd} ・基線 {bd}）')
    new = {s: rows(os.path.join(nd, f'fb_s{s}.tsv')) for s in S}
    base = {s: rows(os.path.join(bd, f'fb_s{s}.tsv')) for s in S}
    n = len(S); need = lambda t: math.ceil(t * n - 1e-9)
    print(f'[門] 種の数 n={n}（門は 10 種全部の想定・煙試験は種 21 の 1 本で門に使わない）')
    # G1
    r_cb = {s: ratio_med(new[s], 'costCast', 'costBrick') / ratio_med(base[s], 'costCast', 'costBrick') for s in S}
    r_cw = {s: ratio_med(new[s], 'costCast', 'costWood') / ratio_med(base[s], 'costCast', 'costWood') for s in S}
    g1a = sum(1 for v in r_cb.values() if fin(v) and 0.35 <= v <= 0.8); g1b = sum(1 for v in r_cw.values() if fin(v) and 0.35 <= v <= 0.8)
    print(f'G1a 鋳鉄÷煉瓦造の費用の〈新÷基線〉が 0.35〜0.8：{g1a}/{n}（要 ≥{need(0.8)}）かつ全種の中央値 <1：{f3(med(list(r_cb.values())))} → {ok(g1a >= need(0.8) and med(list(r_cb.values())) < 1)}  期待 0.5（追記 2：煉瓦造 k＝1）')
    print(f'G1b 鋳鉄÷木造の費用の〈新÷基線〉が 0.35〜0.8：{g1b}/{n}（要 ≥{need(0.8)}）→ {ok(g1b >= need(0.8))}  期待 0.5')
    print('    種ごと 鋳鉄/煉瓦造：' + ' '.join(f'{s}:{f3(r_cb[s])}' for s in S))
    print('    種ごと 鋳鉄/木造　：' + ' '.join(f'{s}:{f3(r_cw[s])}' for s in S))
    # G2
    up = {s: uk_window_share(new[s], 'outCast') - uk_window_share(base[s], 'outCast') for s in S}
    dn = {s: uk_window_share(new[s], 'outWood') - uk_window_share(base[s], 'outWood') for s in S}
    g2a = sum(1 for v in up.values() if fin(v) and v > 0); g2b = sum(1 for v in dn.values() if fin(v) and v < 0)
    print(f'G2a 英 四半期 20〜120 の鋳鉄の割合が基線より高い：{g2a}/{n}（要 ≥{need(0.7)}）→ {ok(g2a >= need(0.7))}')
    print(f'G2b 同じ窓の木造の割合が基線より低い：{g2b}/{n}（要 ≥{need(0.7)}）→ {ok(g2b >= need(0.7))}')
    def sticky(d):
        c = t = 0
        for s in S:
            for x in d[s]:
                if x['nation'] in NATS.values():
                    sh = share(x, 'outCast')
                    if fin(sh):
                        t += 1
                        if sh > 0.95: c += 1
        return c, t
    sn, sb = sticky(new), sticky(base)
    print(f'    [記録] 鋳鉄の割合 >0.95 の国×四半期（主要 5 国・全期間）：新 {sn[0]}/{sn[1]}・基線 {sb[0]}/{sb[1]}（張り付きの件数・門にしない）')
    print('    種ごと 鋳鉄の差：' + ' '.join(f'{s}:{f3(up[s])}' for s in S))
    # G3
    rn = {s: rank_rho(new[s]) for s in S}; rb = {s: rank_rho(base[s]) for s in S}
    g3 = sum(1 for v in rn.values() if fin(v[0]) and v[0] > 0); g3b = sum(1 for v in rb.values() if fin(v[0]) and v[0] > 0)
    print(f'G3 費用の安さと割合の順位相関 ρ>0 の種：{g3}/{n}（要 ≥{need(0.8)}・基線 {g3b}/{n}）→ {ok(g3 >= need(0.8))}')
    print('    種ごと ρ（新/基線・行数）：' + ' '.join(f'{s}:{f3(rn[s][0])}/{f3(rb[s][0])}({rn[s][1]})' for s in S))
    # G3a
    gv = sum(gate_violations(new[s]) for s in S)
    print(f'G3a 門が閉じた段の産出が正の行：{gv} 件（要 0）→ {ok(gv == 0)}')
    # G5
    jn, jb = jump_series(new), jump_series(base)
    cn = sum(1 for v in jn.values() if v > 0.5); cb = sum(1 for v in jb.values() if v > 0.5)
    print(f'G5 4 四半期の割合の動きが 0.5 を超える系列：新 {cn}/{len(jn)}・基線 {cb}/{len(jb)}（要 新 ≤ 基線×1.5＝{cb * 1.5:.1f}）→ {ok(cn <= cb * 1.5)}')
    # 記録
    print('[記録]')
    qs = [0, 20, 40, 80, 120, 160, 240, 320]
    for lab, d in (("新", new), ("基線", base)):
        print(f'R1 英の段の割合の中央値（{lab}）四半期 {qs}')
        for key, name in (("outWood", "木造"), ("outBrick", "煉瓦造"), ("outCast", "鋳鉄"), ("outSteel", "鋼"), ("outRC", "鉄筋")):
            v = []
            for q in qs:
                v.append(med([share(x, key) for s in S for x in d[s] if x['nation'] == NATS["英"] and int(x['quarter']) == q]))
            print(f'    {name:<4}' + ' '.join(f3(t) for t in v))
    for lab, d in (("新", new), ("基線", base)):
        print(f'R2 開幕〜1800 年の建造物の値段÷(参照×物価水準)（{lab}・種の中央値）')
        for k, nat in NATS.items():
            print(f'    {k} ' + f3(med([fl(x['buildPxOverRefMain']) for s in S for x in d[s] if x['nation'] == nat and int(x['year']) <= 1800])))
    for key, name in (("gateSteel", "鋼の骨組みの門が開いた国×種"), ("gateRC", "鉄筋コンクリート造の門が開いた国×種")):
        for lab, d in (("新", new), ("基線", base)):
            opened = sum(1 for s in S if any(int(float(x[key])) > 0 for x in d[s] if x['nation'] in NATS.values()))
            print(f'R4 {name}（{lab}・主要 5 国のどれかで開いた種）：{opened}/{n}')
    # 住居
    hs = {s: rows(os.path.join(nd, f'hs_s{s}.tsv')) for s in S}
    print('R3 住居の段（新の hs_s・種の中央値）')
    for k, nat in NATS.items():
        rs = [x for s in S for x in hs[s] if x['nation'] == nat]
        if not rs: continue
        opened = sum(1 for s in S if any(int(float(x['gateRC'])) > 0 for x in hs[s] if x['nation'] == nat))
        print(f'    {k} 住居の鉄筋コンクリートの門が開いた種 {opened}/{n}・RC の割合の中央値 {f3(med([(fl(x["outRC"]) / fl(x["outTotal"]) if fl(x["outTotal"]) > 1e-9 else float("nan")) for x in rs]))}・住居の値段÷参照 {f3(med([fl(x["housePxOverRefMain"]) for x in rs]))}')
    ho = sum(1 for s in S for nat in set(x['nation'] for x in hs[s]) if any(int(float(x['gateRC'])) > 0 for x in hs[s] if x['nation'] == nat))
    print(f'R3 住居の鉄筋コンクリートの門が開いた国×種：{ho}（0 なら住宅側は観測不能と読む＝追記 2）')
main()
