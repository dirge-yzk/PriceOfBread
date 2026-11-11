# (998) 問い3：6 大国の貿易÷名目GDP（T1＝国ごとの比の単純平均・T2＝和÷和）と計器の突き合わせ（T3）・fxRate。
# 使い方: python -X utf8 -I q3_trade_gdp.py <生のフォルダ> <ラベル> <出力のファイル> [種の範囲 例 1-10]
import sys, os, statistics as st

RAW, LABEL, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
lo, hi = (sys.argv[4].split('-') if len(sys.argv) > 4 else ('1', '10'))
SEEDS = range(int(lo), int(hi) + 1)
YEARS = [1790, 1830, 1870, 1913, 1948]
MAJ = {0: 'フランス', 2: 'イギリス', 3: 'アメリカ', 4: 'ロシア', 5: 'プロイセン', 11: '清'}

def rows(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t')
        ix = {c: i for i, c in enumerate(h)}
        for line in f:
            yield ix, line.rstrip('\n').split('\t')

def fl(x):
    try: return float(x)
    except: return float('nan')

res = {}   # seed -> year -> dict
names_seen = {k: set() for k in MAJ}
for s in SEEDS:
    p = os.path.join(RAW, f'nation_s{s}.tsv')
    if not os.path.exists(p): continue
    acc = {y: {} for y in YEARS}   # year -> nidx -> [trade, gdp, fx_sum, n]
    allacc = {y: [0.0, 0.0, 0.0, 0.0] for y in YEARS}
    for ix, r in rows(p):
        y = int(r[ix['year']])
        if y not in acc: continue
        ni = int(r[ix['nationIndex']])
        ex, im, cl, g = fl(r[ix['exportsPerClearing']]), fl(r[ix['importsPerClearing']]), fl(r[ix['clearingsPerYear']]), fl(r[ix['nominalGdpPerYear']])
        fx = fl(r[ix['fxRate']])
        tr = (ex + im) * cl
        if g == g and tr == tr and g > 0:
            allacc[y][0] += tr; allacc[y][1] += g; allacc[y][2] += ex * cl; allacc[y][3] += im * cl
        if ni in MAJ:
            names_seen[ni].add(r[ix['nation']])
            a = acc[y].setdefault(ni, [0.0, 0.0, 0.0, 0, 0.0, 0.0])
            a[0] += tr; a[1] += g; a[2] += fx; a[3] += 1; a[4] += ex * cl; a[5] += im * cl
    res[s] = (acc, allacc)

# T3：scalars_s の tradeToGdpRatio と nation_s の (ex+im)*cl/gdp を 四半期×国名で突き合わせ（種 1 つ目だけ・全行）
t3 = None
s0 = next(iter(res)) if res else None
if s0 is not None:
    nmap = {}
    for ix, r in rows(os.path.join(RAW, f'nation_s{s0}.tsv')):
        ex, im, cl, g = fl(r[ix['exportsPerClearing']]), fl(r[ix['importsPerClearing']]), fl(r[ix['clearingsPerYear']]), fl(r[ix['nominalGdpPerYear']])
        nmap[(r[ix['quarter']], r[ix['nation']])] = (ex + im) * cl / max(1.0, g)
    ok = bad = miss = 0; worst = []
    for ix, r in rows(os.path.join(RAW, f'scalars_s{s0}.tsv')):
        k = (r[ix['quarter']], r[ix['nation']])
        v = fl(r[ix['tradeToGdpRatio']])
        if k not in nmap: miss += 1; continue
        m = nmap[k]
        if abs(v - m) <= 1e-6 * max(1e-12, abs(m)) + 1e-12: ok += 1
        else:
            bad += 1
            if len(worst) < 5: worst.append((k, v, m))
    t3 = (s0, ok, bad, miss, worst)

with open(OUT, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# q3 {LABEL} raw={RAW} seeds={list(res)}\n')
    o.write('# T1=6大国の国ごとの比の単純平均（MajorsTradeGdp の形）・T2=和÷和・ALL=全ての国の和÷和・種の中央値（[最小,最大]）\n')
    o.write('year\tT1_med\tT1_min\tT1_max\tT2_med\tT2_min\tT2_max\tALL_med\tWorldImpOverExp_med\n')
    for y in YEARS:
        t1s, t2s, als, wim = [], [], [], []
        for s, (acc, allacc) in res.items():
            a = acc[y]
            if len(a) == 0: continue
            t1s.append(sum(v[0] / v[1] for v in a.values() if v[1] > 0) / len([v for v in a.values() if v[1] > 0]))
            t2s.append(sum(v[0] for v in a.values()) / sum(v[1] for v in a.values()))
            als.append(allacc[y][0] / allacc[y][1]); wim.append(allacc[y][3] / max(1e-300, allacc[y][2]))
        if t1s:
            o.write(f'{y}\t{st.median(t1s):.4f}\t{min(t1s):.4f}\t{max(t1s):.4f}\t{st.median(t2s):.4f}\t{min(t2s):.4f}\t{max(t2s):.4f}\t{st.median(als):.4f}\t{st.median(wim):.4f}\n')
    o.write('\n# 国ごと（比＝和÷和の4四半期・種の中央値）・輸出÷GDP・輸入÷GDP・fxRate の平均\n')
    o.write('year\tnation\ttrade/gdp\texp/gdp\timp/gdp\tfxRate\n')
    for y in YEARS:
        for ni, nm in MAJ.items():
            tg, eg, ig, fxs = [], [], [], []
            for s, (acc, _) in res.items():
                v = acc[y].get(ni)
                if not v or v[1] <= 0: continue
                tg.append(v[0] / v[1]); eg.append(v[4] / v[1]); ig.append(v[5] / v[1]); fxs.append(v[2] / v[3])
            if tg:
                o.write(f'{y}\t{nm}\t{st.median(tg):.4f}\t{st.median(eg):.4f}\t{st.median(ig):.4f}\t{st.median(fxs):.4f}\n')
    o.write('\n# 6大国の index に出た国名（名前の揺れの確かめ）\n')
    for ni, s in names_seen.items(): o.write(f'{ni}\t{"/".join(sorted(s))}\n')
    if t3:
        o.write(f'\n# T3 計器の突き合わせ（種{t3[0]}）：scalars.tradeToGdpRatio vs nation_s 一致 {t3[1]}・不一致 {t3[2]}・nation_s に無い {t3[3]}\n')
        for w in t3[4]: o.write(f'#  不一致の例 {w}\n')
print('ok', OUT)
