# (638) 事前登録 [4]・[12] の門の読み手（読むだけ）。
# 使い方：python -X utf8 -I gates.py <腕の ins ディレクトリ> <比べる側 K0 の ins ディレクトリ> [K1 の ins ディレクトリ]
#   腕＝B（または B1）。出力＝向き②（一次の疫病の件数・当たった州の集中・極小の州への当たり）・③（核の最小÷ピークの中央値と下端）・⑧（和と 1/4 未満の件数）・
#   ④（本国の都市の割合の中央値と下端・K1−K0 と B−K1 の差とヌル腕の幅）・⑤（価格水準の最大と帰属の規則）。
import csv, sys, collections, statistics as st

CORE = [('ネーデルラント', 'アムステルダム'), ('イギリス', 'ロンドン'), ('プロイセン', 'ベルリン')]
LOWER = {'アムステルダム': 0.323, 'ロンドン': 0.397, 'ベルリン': 0.522}   # [11] ⓐ
BASE = {'m627': 'D:/PoB_work/keep_627/m627_ins', 'n1': 'D:/PoB_work/keep_627/n1', 'n2': 'D:/PoB_work/keep_627/n2'}
NS = 10
UN = ['イギリス', 'フランス', 'プロイセン']
UQ = {1850: 268, 1880: 388, 1913: 520}


def rd(path):
    with open(path, encoding='utf-8') as fh:
        return list(csv.DictReader(fh, delimiter='\t'))


def minpeak(d, s):
    ser = collections.defaultdict(list)
    for r in rd(f'{d}/region_s{s}.tsv'):
        k = (r['nation'], r['region'])
        if k in CORE:
            ser[k].append(float(r['populationK']))
    out = {}
    for k in CORE:
        v = ser[k]
        if not v:
            continue
        pk = v[0]; m = 1e9
        for x in v:
            pk = max(pk, x); m = min(m, x / pk)
        out[k] = m
    return out


def epi(d, s):
    prim = collections.Counter(); byn = collections.defaultdict(collections.Counter); tiny = 0; tot = 0
    popq = collections.defaultdict(float)
    for r in rd(f'{d}/region_s{s}.tsv'):
        popq[(r['nation'], int(r['quarter']))] += float(r['populationK'])
    try:
        rows = rd(f'{d}/epi_s{s}.tsv')
    except FileNotFoundError:
        return prim, byn, tiny, tot
    for r in rows:
        if int(r['origin']) not in (0, 1):
            continue
        prim[(r['nation'], r['region'])] += 1
        byn[r['nation']][r['region']] += 1
        tot += 1
        npop = popq.get((r['nation'], int(r['quarter'])), 0.0)
        if npop > 0 and float(r['regionPopKBefore']) < 0.001 * npop:
            tiny += 1
    return prim, byn, tiny, tot


def urbshare(d, s):
    acc = collections.defaultdict(lambda: [0.0, 0.0])
    for row in rd(f'{d}/region_s{s}.tsv'):
        q = int(row['quarter'])
        if q in UQ.values() and row['nation'] in UN and row['tier'] == 'Core':
            k = (row['nation'], q); acc[k][0] += float(row['populationK']); acc[k][1] += float(row['ruralPopulationK'])
    return {k: 1 - r / p for k, (p, r) in acc.items() if p > 0}


def plmax(d, s):
    best = {}
    for r in rd(f'{d}/nation_s{s}.tsv'):
        try:
            p = float(r['priceLevel'])
        except Exception:
            continue
        if p != p:
            continue
        if float(r['civilWarMonths'] or 0) == 0 and float(r['atWarMonths'] or 0) == 0:
            k = r['nation']
            if p > best.get(k, (0, 0))[0]:
                best[k] = (p, int(r['year']))
    return best


arms = {'B': sys.argv[1], 'K0': sys.argv[2]}
if len(sys.argv) > 3:
    arms['K1'] = sys.argv[3]
med = lambda v: st.median(v) if v else float('nan')

print('## ③⑧ 核の最小÷ピーク（種の中央値）')
mp = {a: {s: minpeak(d, s) for s in range(1, NS + 1)} for a, d in arms.items()}
cnt = {}
for a in arms:
    row = []
    sm = 0.0
    c14 = 0
    for k in CORE:
        v = [mp[a][s][k] for s in range(1, NS + 1) if k in mp[a][s]]
        m = med(v); sm += m
        c14 += sum(1 for x in v if x < 0.25)
        row.append(f'{k[1]} 中央値 {m:.3f}（下端 {LOWER[k[1]]:.3f}：{"通る" if m >= LOWER[k[1]] else "割る"}）種別 ' + ' '.join(f'{x:.2f}' for x in v))
    cnt[a] = (sm, c14)
    print(f'  {a}: ' + ' | '.join(row))
    print(f'  {a}: 3 州の中央値の和 {sm:.3f}・1/4 未満の件数（核の 3 州） {c14}')
sB, cB = cnt['B']; sK, cK = cnt['K0']
print(f'  ⑧ 向き：和 B {sB:.3f} > K0 {sK:.3f} → {"通る" if sB > sK else "破れ"}／1/4 未満 B {cB} ≦ K0 {cK} → {"通る" if cB <= cK else "破れ"}')

print('## ② 一次の疫病の件数（chance の後・origin 0/1）')
ep = {a: [epi(d, s) for s in range(1, NS + 1)] for a, d in arms.items()}
tot_med = {}
for a in arms:
    row = []
    s_med = 0.0
    for k in CORE:
        v = [e[0].get(k, 0) for e in ep[a]]
        row.append(f'{k[1]} 中央値 {med(v):.1f}（{" ".join(map(str, v))}）')
        s_med += med(v)
    tot_med[a] = s_med
    print(f'  {a}: ' + ' | '.join(row))
for k in CORE:
    b = med([e[0].get(k, 0) for e in ep['B']]); k0 = med([e[0].get(k, 0) for e in ep['K0']])
    print(f'  ② {k[1]}：B {b:.1f} ≦ K0 {k0:.1f} → {"通る" if b <= k0 else "破れ"}')
print(f'  ② 和：B {tot_med["B"]:.1f} < K0 {tot_med["K0"]:.1f} → {"通る" if tot_med["B"] < tot_med["K0"] else "破れ"}')
for a in arms:
    shares = []
    for e in ep[a]:
        for n, c in e[1].items():
            if len(c) >= 3 and sum(c.values()) >= 5:
                shares.append(max(c.values()) / sum(c.values()))
    tinies = [e[2] for e in ep[a]]
    tots = [e[3] for e in ep[a]]
    print(f'  {a}: 3 州以上の国×種の最大の州の占める割合の中央値 {med(shares):.3f}（{len(shares)} 組）・極小の州（国の人口の 0.1% 未満）への当たり 平均 {sum(tinies)/NS:.1f}/種・一次の総数 平均 {sum(tots)/NS:.1f}/種')
nat_tot = {a: collections.Counter() for a in arms}
for a in arms:
    for e in ep[a]:
        for n, c in e[1].items():
            nat_tot[a][n] += sum(c.values())
print('  国ごとの一次の疫病の件数（10 種の合計）B/K0：' + '、'.join(f'{n} {nat_tot["B"][n]}/{nat_tot["K0"][n]}' for n in ['イギリス', 'フランス', 'プロイセン', 'ネーデルラント', '日本', '清', 'アメリカ', 'ロシア', 'オーストリア']))

print('## ④ 本国の都市の割合（種の中央値）と下端・差')
b_us = {a: {s: urbshare(d, s) for s in range(1, NS + 1)} for a, d in arms.items()}
bs = {a: {s: urbshare(d, s) for s in range(1, NS + 1)} for a, d in BASE.items()}
for n in UN:
    for y, q in UQ.items():
        meds = [med([bs[a][s][(n, q)] for s in range(1, NS + 1) if (n, q) in bs[a][s]]) for a in BASE]
        sd = st.pstdev([bs['m627'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in bs['m627'][s]])
        lo = min(meds) - 2 * sd / NS ** 0.5
        vb = [b_us['B'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in b_us['B'][s]]
        line = f'  {n} {y} 下端 {lo:.3f}：B 中央値 {med(vb):.3f} {"通る" if med(vb) >= lo else "割る"}'
        # ヌル腕の幅（同じ種の対の差の中央値の大きい方）
        nw = max(abs(med([bs[a][s][(n, q)] - bs['m627'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in bs[a][s] and (n, q) in bs['m627'][s]])) for a in ('n1', 'n2'))
        d_bk0 = med([b_us['B'][s][(n, q)] - b_us['K0'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in b_us['B'][s] and (n, q) in b_us['K0'][s]])
        line += f'｜幅 {nw:.3f}｜B−K0 {d_bk0:+.3f}'
        if 'K1' in arms:
            d_k1k0 = med([b_us['K1'][s][(n, q)] - b_us['K0'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in b_us['K1'][s] and (n, q) in b_us['K0'][s]])
            d_bk1 = med([b_us['B'][s][(n, q)] - b_us['K1'][s][(n, q)] for s in range(1, NS + 1) if (n, q) in b_us['B'][s] and (n, q) in b_us['K1'][s]])
            line += f'｜K1−K0 {d_k1k0:+.3f}・B−K1 {d_bk1:+.3f}（負で幅超え：{"K1−K0 " if d_k1k0 < -nw else ""}{"B−K1" if d_bk1 < -nw else ""}）'
        print(line)

print('## ⑤ 価格水準の最大（内戦なし・戦争なし）')
pb = {s: plmax(arms['B'], s) for s in range(1, NS + 1)}
pk = {s: plmax(arms['K0'], s) for s in range(1, NS + 1)}
mx = max((v[0], n, s, v[1]) for s in pb for n, v in pb[s].items())
mk = max((v[0], n, s, v[1]) for s in pk for n, v in pk[s].items())
print(f'  B 最大 {mx}・K0 最大 {mk}（ヌル腕の上端 55.71）')
hit = []
for s in pb:
    for n, (p, y) in pb[s].items():
        if p > 55.71:
            k0 = pk[s].get(n, (0, 0))[0]
            hit.append((n, s, p, k0, p / k0 if k0 > 0 else float('inf'), k0 <= 55.71 and (p / k0 > 3 if k0 > 0 else True)))
print('  55.71 超の行（国・種・B・K0・比・門に当たる条件）：' + (str(hit) if hit else 'なし'))
