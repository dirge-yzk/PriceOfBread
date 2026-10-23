#!/usr/bin/env python
# (494) 開幕の製法の行の読み手。事前登録 measurements/op_20261023_prereg.txt の追記 3 ③〜⑤ の門を計器の生から出す。
# 使い方：python judge.py <腕 B の ins フォルダ> <腕 A（旗 0）の ins フォルダ> [種の数=すべて]
# 入力：disc_s<種>.tsv（E 行）・procrow_s<種>.tsv。出力は標準出力（日本語）。閾値は追記 3 のとおり（M2a<=25% M2b<=15% M2c 機械紡績<=25%・コークス製鉄<=50% M1<=1% M3 10×）。
import sys, os, csv, statistics, collections
sys.stdout.reconfigure(encoding='utf-8')
B, A = sys.argv[1], sys.argv[2]
NS = int(sys.argv[3]) if len(sys.argv) > 3 else 99
ROWS = ['コークス製鉄', '機械紡績']
ALL5 = ROWS + ['熱風炉', '三段膨張', '自動ミュール']   # (503) 追記 3：M1（門が働いた）と M3（発散）は工程の 3 行も読む。M2（追う国の入口）と M4 は op の 2 行だけ（工程の 3 行に追う国の概念は無い）
TH = {'M2a': 0.25, 'M2b': 0.15, 'M2c': {'コークス製鉄': 0.50, '機械紡績': 0.25}, 'M1': 0.01, 'M3': 10.0}

def rd(path):
    with open(path, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t')
        h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        for row in r:
            yield ix, row

def fl(x):
    try: return float(x)
    except: return float('nan')

def seeds_in(d):
    s = []
    for k in range(1, 100):
        if os.path.exists(os.path.join(d, f'procrow_s{k}.tsv')) and os.path.exists(os.path.join(d, f'disc_s{k}.tsv')): s.append(k)
    return s[:NS]

def load_proc(d, s):
    """(row, uid) -> {q: (known, defOut, goodOut, altShare, real)} と名前"""
    ser = collections.defaultdict(dict); nm = {}
    for ix, r in rd(os.path.join(d, f'procrow_s{s}.tsv')):
        q = int(r[ix['quarter']]); uid = int(r[ix['nationUid']]); row = r[ix['row']]
        ser[(row, uid)][q] = (int(r[ix['known']]), fl(r[ix['defaultOutputPerMonth']]), fl(r[ix['goodOutputPerMonth']]), fl(r[ix['altOutputShare']]), fl(r[ix['goodPriceReal']]))
        nm[uid] = r[ix['nation']]
    return ser, nm

def load_open(d, s):
    """(row, uid) -> (q, tech, need, trans)"""
    o = {}
    for ix, r in rd(os.path.join(d, f'disc_s{s}.tsv')):
        if r[ix['kind']] != 'E' or r[ix['row']] not in ROWS: continue
        o[(r[ix['row']], int(r[ix['nationUid']]))] = (int(r[ix['quarter']]), fl(r[ix['techTerm']]), fl(r[ix['needTerm']]), fl(r[ix['transmitTerm']]), r[ix['nation']])
    return o

def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')

def roll_med_max(vals, w=8):
    v = [x for x in vals]
    best = float('nan')
    for i in range(0, max(1, len(v) - w + 1)):
        m = med(v[i:i + w])
        if m == m and (best != best or m > best): best = m
    return best

seedsB = seeds_in(B); seedsA = [s for s in seeds_in(A) if s in seedsB]
print(f'# 腕 B={B}（種 {seedsB}）／腕 A={A}（種 {seedsA}）')
verdict = {}
per = {r: {'M2a': [], 'M2b': [], 'M2c': [], 'M2c_nm': [], 'm90': [], 'n': [], 'open': []} for r in ROWS}
m1num = m1den = 0; alt_pos = alt_den = 0
first = {r: collections.defaultdict(list) for r in ROWS}
for s in seedsB:
    ser, nm = load_proc(B, s); op = load_open(B, s)
    for row in ROWS:
        q0 = {uid: v[0] for (rr, uid), v in ser.items() if rr == row and 0 in v}
        init = {uid for uid, v in ((u, ser[(row, u)][0]) for u in q0) if v[0] == 1 and (row, uid) not in op}
        ch = [u for u in q0 if u not in init]
        ev = [op[(row, u)] for u in ch if (row, u) in op]
        n = len(ch)
        per[row]['n'].append(n); per[row]['open'].append(len(ev))
        if ev:
            per[row]['M2a'].append(sum(1 for e in ev if e[1] + e[2] >= 100.0) / len(ev))
            per[row]['m90'].append(sum(1 for e in ev if e[1] + e[2] >= 90.0) / len(ev))
        if n:
            per[row]['M2b'].append(sum(1 for e in ev if e[0] <= 1) / n)
            per[row]['M2c'].append(sum(1 for e in ev if e[0] <= 7) / n)
        for u in ch:
            if (row, u) in op: first[row][nm[u]].append(op[(row, u)][0])
        # M1
        for (rr, uid), qs in ser.items():
            if rr != row: continue
            for q, (kn, d, g, ash, px) in qs.items():
                if kn == 0:
                    m1den += 1
                    if d > 1e-9: m1num += 1
                    if g > 0:
                        alt_den += 1
                        if ash > 0: alt_pos += 1
for s in seedsB:
    ser, nm = load_proc(B, s)
    for (rr, uid), qs in ser.items():
        if rr in ROWS or rr not in ALL5: continue
        for q, (kn, d, g, ash, px) in qs.items():
            if kn == 0:
                m1den += 1
                if d > 1e-9: m1num += 1
                if g > 0:
                    alt_den += 1
                    if ash > 0: alt_pos += 1
print('\n[M2 入口の門]（行ごと・種の中央値）')
ok = True
for row in ROWS:
    p = per[row]
    a, b, c = med(p['M2a']), med(p['M2b']), med(p['M2c'])
    ta, tb, tc = TH['M2a'], TH['M2b'], TH['M2c'][row]
    oka, okb, okc = a <= ta, b <= tb, c <= tc
    ok &= oka and okb and okc
    print(f'  {row}: 追う国の数（種ごと）{p["n"]}・開いた国 {p["open"]}')
    print(f'    M2a 接触なしで開く割合（tech+need>=100）{a:.3f} <= {ta}  {"OK" if oka else "NG"}   （記録：tech+need>=90 の割合 {med(p["m90"]):.3f}）')
    print(f'    M2b Q0〜Q1 に開く割合 {b:.3f} <= {tb}  {"OK" if okb else "NG"}')
    print(f'    M2c Q0〜Q7 に開く割合 {c:.3f} <= {tc}  {"OK" if okc else "NG"}')
verdict['M2'] = ok
r1 = m1num / m1den if m1den else float('nan')
r2 = alt_pos / alt_den if alt_den else float('nan')
print(f'\n[M1 門が働いた] known=0 の行で既定のレシピの産出が正の割合 {r1:.5f} ({m1num}/{m1den}) <= {TH["M1"]}  {"OK" if r1 <= TH["M1"] else "NG"}')
print(f'    known=0 かつその財を作る行のうち古いやり方の産出が正の割合 {r2:.3f} ({alt_pos}/{alt_den}) >= 0.5  {"OK" if r2 >= 0.5 else "NG"}')
verdict['M1'] = (r1 <= TH['M1']) and (r2 >= 0.5)

print('\n[M4 記録：追いつき] 追う国のうち 668 四半期の最後まで開かなかった国の割合（種ごと）')
for row in ROWS:
    print(f'  {row}: ' + ' '.join(f'{(p_n - p_o) / p_n:.2f}' for p_n, p_o in zip(per[row]['n'], per[row]['open']) if p_n))

print('\n[M3 発散の線] 8 四半期の移動中央値の最大 B_max ÷ 腕 A の期間の中央値 A_med > 10×max(1, A_max÷A_med) を発散と数える')
bad = []; ratios = {r: [] for r in ALL5}
for s in seedsA:
    sB, nmB = load_proc(B, s); sA, nmA = load_proc(A, s)
    for key in sB:
        if key not in sA: continue
        bq = [sB[key][q][4] for q in sorted(sB[key]) if q >= 0]; aq = [sA[key][q][4] for q in sorted(sA[key]) if q >= 0]
        if len(aq) < 40 or len(bq) < 40: continue   # 追記 4：短命の国（建国・消滅で系列が 40 四半期未満）は比べない（canary で両シチリア〔13／18 四半期〕が見かけの発散を出した）
        amed = med(aq); amax = roll_med_max(aq); bmax = roll_med_max(bq)
        if not (amed > 0) or bmax != bmax: continue
        lim = TH['M3'] * max(1.0, amax / amed)
        ratios[key[0]].append(bmax / amed)
        if bmax / amed > lim: bad.append((s, key[0], nmB.get(key[1], '?'), bmax / amed, lim))
for row in ALL5:
    r = sorted(ratios[row])
    if r: print(f'  {row}: 件数 {len(r)}・B_max÷A_med の中央値 {med(r):.2f}・90 分位 {r[int(0.9*(len(r)-1))]:.2f}・最大 {r[-1]:.2f}')
print(f'  発散の件数 {len(bad)}  {"OK" if not bad else "NG"}')
for x in sorted(bad, key=lambda t: -t[3])[:15]: print(f'    種 {x[0]} {x[1]} {x[2]} 比 {x[3]:.1f}（線 {x[4]:.1f}）')
verdict['M3'] = not bad

print('\n[記録] 行を得た四半期（追う国・種をまたいだ中央値）')
for row in ROWS:
    pairs = sorted(((med(v), k, len(v)) for k, v in first[row].items()))
    print(f'  {row}: 最初の 10 国 {[(k, round(m)) for m, k, n in pairs[:10]]}')
    for nmk in ['清', '日本', 'フランス', 'アメリカ', 'ロシア', 'プロイセン', 'オスマン', 'オーストリア']:
        if nmk in first[row]: print(f'    {nmk}: 中央値 Q{med(first[row][nmk]):.0f}（{len(first[row][nmk])} 種）')
print('\n[総合] ' + ' '.join(f'{k}={"OK" if v else "NG"}' for k, v in verdict.items()))
