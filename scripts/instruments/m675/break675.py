# (675) 合流：igi4_667/break.py の写し（腕を引数 <名前>=<計器の出力フォルダ> で渡す・種 1〜10）。
import csv, sys, collections, statistics as st
NATS = ['清','ロシア','オスマン','フランス','イギリス','プロイセン','オーストリア','日本','アメリカ']
def run(d, S):
    top_share_max = []; topn = collections.Counter(); halved = collections.Counter()
    for s in S:
        f = open(f'{d}/nation_s{s}.tsv', encoding='utf-8'); r = csv.reader(f, delimiter='\t'); h = next(r)
        iy, inn, ig, ip, iq = h.index('year'), h.index('nation'), h.index('realGdpPerYear'), h.index('populationK'), h.index('quarter')
        Y = collections.defaultdict(dict); P0 = {}; Pmin = {}
        for x in r:
            if int(x[iq]) % 4: continue
            y = int(x[iy]); n = x[inn]
            try: g = float(x[ig]); p = float(x[ip])
            except ValueError: continue
            Y[y][n] = g
            if n in NATS:
                if y == 1783: P0[n] = p
                elif n in P0 and p < P0[n] * 0.5 and y <= 1913: halved[n] += 0  # placeholder
                if n in P0: Pmin[n] = min(Pmin.get(n, 1e99), p)
        f.close()
        best = (0, '')
        for y, dd in Y.items():
            tot = sum(v for v in dd.values() if v > 0)
            if tot <= 0: continue
            n, v = max(dd.items(), key=lambda kv: kv[1])
            if v / tot > best[0]: best = (v / tot, n)
        top_share_max.append(best[0]); topn[best[1]] += 1
        for n in NATS:
            if n in Pmin and n in P0 and Pmin[n] < 0.5 * P0[n]: halved[n] += 1
    return top_share_max, topn, halved
import sys as _s
_args=[x.split('=',1) for x in _s.argv[1:]]
for lab, d in _args:
    a = 1
    m, tn, hv = run(d, range(a, a + 10))
    print(f'{lab}: 首位の世界GDP取り分の最大 種中央値={st.median(m):.3f} 範囲=[{min(m):.3f},{max(m):.3f}] 首位の国={dict(tn)}  人口が半分未満になった種数(9国)={ {k:v for k,v in hv.items() if v} }')
