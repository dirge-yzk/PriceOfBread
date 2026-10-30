# (682) 門の棚卸し：基線 m675 の生（D:/PoB_work/keep_675 の m675_ins・n1・n2）で、〈壊す〉①②③と navf の G2 をヌル腕どうしに当てる（読むだけ）。
# 使い方：python -X utf8 -I scripts/instruments/gaud_682/null_raw675.py [生の親フォルダ=D:/PoB_work/keep_675]
# 一つ抜き：三つ組の 1 本を〈腕〉に見立て、残りの 2 本を〈ヌル腕〉に見立てる（3 通り）。定義は igi4_667/break.py・lock.py（①②③）と navf606/g_lines.py（G2）の写し。
import csv, sys, collections, statistics as st, os, importlib.util
K = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/keep_675'
ARMS = {'m675': f'{K}/m675_ins', 'N1': f'{K}/n1', 'N2': f'{K}/n2'}
S = range(1, 11)
NATS = ['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
LCOLS = ['educationLaw','suffrage','censorship','bureaucracy','socialSecurityLawCode','antitrustLawCode','womenRightsCode','childLaborLegal','workHoursValue']
def brk(d):
    tops = []; topn = []; halved = collections.Counter(); ukc = 0
    for s in S:
        f = open(f'{d}/nation_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        iy, inn, ig, ip, iq = h.index('year'), h.index('nation'), h.index('realGdpPerYear'), h.index('populationK'), h.index('quarter')
        Y = collections.defaultdict(dict); P0 = {}; Pmin = {}; ukmax = 0.0; ukcol = False
        for x in r:
            if int(x[iq]) % 4: continue
            y = int(x[iy]); n = x[inn]
            try: g = float(x[ig]); p = float(x[ip])
            except ValueError: continue
            Y[y][n] = g
            if n in NATS:
                if y == 1783: P0[n] = p
                if n in P0: Pmin[n] = min(Pmin.get(n, 1e99), p)
            if n == 'イギリス':
                if ukmax > 0 and g < 0.05 * ukmax: ukcol = True
                ukmax = max(ukmax, g)
        f.close()
        best = (0, '')
        for y, dd in Y.items():
            tot = sum(v for v in dd.values() if v > 0)
            if tot <= 0: continue
            n, v = max(dd.items(), key=lambda kv: kv[1])
            if v / tot > best[0]: best = (v / tot, n)
        tops.append(best[0]); topn.append(best[1])
        for n in NATS:
            if n in Pmin and n in P0 and Pmin[n] < 0.5 * P0[n]: halved[n] += 1
        ukc += ukcol
    return tops, topn, halved, ukc
def lock(d):
    cnt = collections.Counter(); tot = 0
    for s in S:
        f = open(f'{d}/scalars_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        idx = {c: h.index(c) for c in LCOLS}; rows = list(r); f.close()
        mx = {c: max(float(x[idx[c]]) for x in rows) for c in LCOLS}
        for x in rows:
            tot += 1
            for c in LCOLS:
                if float(x[idx[c]]) == mx[c] and mx[c] > float(rows[0][idx[c]]): cnt[c] += 1
    return {c: cnt[c] / tot for c in LCOLS}
def g2(d):
    p = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'navf606', 'g_lines.py')
    src = open(p, encoding='utf-8').read().replace('\nmain()', '\n#main()')
    sv = sys.argv; sys.argv = ['g', d, '10', '1']; m = {}
    exec(compile(src, 'gl', 'exec'), m); sys.argv = sv
    ok = tot = 0
    for s in m['SEEDS']:
        fs = m['fleet_series'](s)
        if not fs: continue
        for nat in m['SEVEN']:
            if nat in fs:
                o, t = m['halving_events']({nat: fs[nat]}, 8); ok += o; tot += t
    return ok, tot
B = {a: brk(d) for a, d in ARMS.items()}
L = {a: lock(d) for a, d in ARMS.items()}
G = {a: g2(d) for a, d in ARMS.items()}
print('# 腕ごとの値（種 1〜10）')
for a in ARMS:
    tops, topn, hv, ukc = B[a]
    print(f'{a}\t首位の取り分の最大 種中央値 {st.median(tops):.3f} [{min(tops):.3f},{max(tops):.3f}]\t首位の国 {dict(collections.Counter(topn))}\t人口半減の種数 {dict((k,v) for k,v in hv.items() if v)}\t英の崩れ {ukc}/10\tG2 {G[a][0]}/{G[a][1]}={G[a][0]/max(G[a][1],1):.3f}')
    print('\t段の張り付き ' + ' '.join(f'{c[:10]}={L[a][c]:.4f}' for c in LCOLS))
print('# 一つ抜き（腕に見立てた 1 本 対 残りの 2 本＝ヌル腕に見立てる）')
for a in ARMS:
    refs = [r for r in ARMS if r != a]
    tops, topn, hv, _ = B[a]
    # ① 同じ国が毎回壊れる：腕で 8/10 以上・ヌル腕は 2/10 以下
    one = [n for n in NATS if hv[n] >= 8 and all(B[r][2][n] <= 2 for r in refs)]
    one_soft = [(n, hv[n], [B[r][2][n] for r in refs]) for n in NATS if hv[n] >= 5]
    # ② 一国の発散：首位の取り分がヌル腕の最大を幅の 2 倍以上超え、8/10 以上の種で同じ国
    rv = [v for r in refs for v in B[r][0]]; rmax, rmin = max(rv), min(rv); thr = rmax + 2 * (rmax - rmin)
    c = collections.Counter(n for v, n in zip(tops, topn) if v > thr)
    two = c.most_common(1)[0] if c else ('', 0)
    # ③ 片方向の錠：段の張り付きの割合が、ヌル腕の最大の 2 倍以上の列
    worst = max((L[a][k] / max(L[r][k] for r in refs) if max(L[r][k] for r in refs) > 0 else 0, k) for k in LCOLS)
    # G2（navf2 の線〈0.564 未満は採らない〉の型＝比べる側の下端を割る／navf3 の〈旗 0 と並べて下がらない〉の型）
    ga = G[a][0] / max(G[a][1], 1); gr = [G[r][0] / max(G[r][1], 1) for r in refs]
    print(f'{a} 対 {"・".join(refs)}\t① 該当 {one or "なし"}（5/10 以上の国 {one_soft}）\t② 線 {thr:.3f} を超える種 {two[1]}/10（{two[0]}）\t③ 最悪 {worst[0]:.2f}（{worst[1]}）\tG2 {ga:.3f} 対 {", ".join(f"{x:.3f}" for x in gr)}＝〈下がらない〉{"落ちる" if ga < min(gr) else ("片方で落ちる" if ga < max(gr) else "通る")}')
# ② の〈幅〉の読み方は 2 通りある：上＝種ごとの値の最大−最小（igi4 の〈ヌル腕の最大 0.600〉の型）／下＝腕ごとの種中央値の最大−最小（pcw (615) の〈幅 0.019〉の型）。
print('# ② を種中央値の幅で読む（pcw の型）：m675 の三つ組＋実装ログの値（m655＝igi4 (667)・m609＝pcw (615)）')
MED = {'m675': {a: st.median(B[a][0]) for a in ARMS},
       'm655': {'m655': 0.442, 'N1': 0.420, 'N2': 0.427},
       'm609': {'m609': 0.317, 'N1': 0.306, 'N2': 0.324}}
for b, d in MED.items():
    for a in d:
        rv = [d[r] for r in d if r != a]; thr = max(rv) + 2 * (max(rv) - min(rv))
        print(f'{b}\t{a} {d[a]:.3f} 対 線 {thr:.3f}（残り {", ".join(f"{x:.3f}" for x in rv)}）\t{"落ちる" if d[a] > thr else "通る"}')
