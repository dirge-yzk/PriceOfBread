# (588) 線 sm6：狙いの数字 ④（採算の高い足りない部門が伸びる）と ⑤（遊び）の読み手（読むだけ）。事前登録 measurements/sm6_588_20261027_prereg.txt [4]。
# 使い方：python -X utf8 an_good.py <腕 B の計器の出力フォルダ> [<基線の計器の出力フォルダ>]
# ④：国×種ごとに、財の ΣN の 開幕→1850 年の伸び と、その財の N 加重の rate（1790〜1850 年の年末の平均）の Spearman（slotgood_s・開幕で ΣN が正の財・財が 8 つ以上の国×種だけ）。
#     正の国×種が ≥8/10 の種で、主要 9 国の過半＝PASS（NA＝slotgood_s が無い）。
# ⑤：遊び＝1−産出÷能力（factory_s の参照価値・基線と同じ定義）の十年の中央値の、同じ国×種の基線との差。1800〜1860 年代の 10 年ごと。差が +0.10 を超える国×種の割合と最大（基線の定義と同じ物差し）。
import csv, glob, os, sys, math, statistics as st
LISTED = ["清", "ロシア", "オスマン", "フランス", "イギリス", "プロイセン", "オーストリア", "日本", "アメリカ"]

def rows(p):
    if not os.path.exists(p): return
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r, None)
        if h is None: return
        for x in r:
            if len(x) == len(h): yield dict(zip(h, x))

def fl(x):
    try: return float(x)
    except Exception: return float('nan')

def rank(v):
    n = len(v); o = sorted(range(n), key=lambda i: v[i]); r = [0.0] * n; i = 0
    while i < n:
        j = i
        while j + 1 < n and v[o[j + 1]] == v[o[i]]: j += 1
        for k in range(i, j + 1): r[o[k]] = (i + j) / 2.0
        i = j + 1
    return r

def spearman(xs, ys):
    if len(xs) < 3: return float('nan')
    rx, ry = rank(xs), rank(ys); mx, my = sum(rx) / len(rx), sum(ry) / len(ry)
    sxy = sum((a - mx) * (b - my) for a, b in zip(rx, ry)); sx = sum((a - mx) ** 2 for a in rx); sy = sum((b - my) ** 2 for b in ry)
    return sxy / math.sqrt(sx * sy) if sx > 0 and sy > 0 else float('nan')

B = sys.argv[1]; A = sys.argv[2] if len(sys.argv) > 2 else None
seeds = sorted(int(os.path.basename(p)[len('slot_s'):-4]) for p in glob.glob(os.path.join(B, 'slot_s*.tsv')))
print('seeds', seeds)

# ---- ④a（配線の確認＝記録）と ④′（門）：国×種ごとに 財の ΣN の 開幕→1850 年の伸び と、その財の 1790〜1850 年の年末の平均 との Spearman。
#   ④a＝x が N 加重の rate（配りの重み＝rate に比例する規則の言い直しに近い＝配線の確認・記録）
#   ④′＝x が 値段÷（参照価格×物価水準）の N 加重（pxN÷sumN）＝rate と独立な〈足りなさ〉。高い（足りない）財ほど ΣN が伸びる＝正
def read_good(B, seeds):
    out = {}
    for s in seeds:
        g = {}
        for r in rows(os.path.join(B, f'slotgood_s{s}.tsv')):
            key = (r['nation'], int(r['good'])); y = int(r['year'])
            g.setdefault(key, []).append((y, fl(r['sumN']), fl(r['rateN']), fl(r.get('pxN', 'nan'))))
        out[s] = g
    return out
def spear_by(gd, col):
    """gd＝{(nation, good): [(y, sumN, rateN, pxN)]}。col＝2（rate）か 3（px）。戻り＝{nation: rho}"""
    res = {}
    for nat in LISTED:
        xs, ys = [], []
        for (n, gd_), v in gd.items():
            if n != nat: continue
            v = sorted(v); n0 = v[0][1]
            if not (n0 > 0): continue
            n1850 = [x[1] for x in v if x[0] == 1850]
            if not n1850: continue
            vals = [x[col] / x[1] for x in v if 1790 <= x[0] <= 1850 and x[1] > 0 and x[col] == x[col]]
            if not vals: continue
            xs.append(sum(vals) / len(vals)); ys.append(n1850[0] / n0)
        if len(xs) >= 8: res[nat] = spearman(xs, ys)
    return res
gd_all = read_good(B, seeds)
def report4(name, col, gate):
    res = {}
    for s in seeds:
        for nat, rho in spear_by(gd_all[s], col).items(): res.setdefault(nat, []).append(rho)
    if not res:
        print(f'[{name}] NA（slotgood_s が無い・または 1850 年に届いていない）'); return
    npos = 0; nn = 0
    for nat in LISTED:
        v = [x for x in res.get(nat, []) if x == x]
        pos = sum(1 for x in v if x > 0)
        print(f'[{name}] {nat}: 正の種 {pos}/{len(v)}  値={["%.2f" % x for x in v]}')
        if v:
            nn += 1
            if pos >= 0.8 * len(v): npos += 1
    ok = npos > nn / 2
    print(f'[{name}] 8/10 の線を越えた国 {npos}/{nn}（過半で PASS）  [{"PASS" if ok else "FAIL"}]（{"門" if gate else "記録＝配線の確認"}）')
report4('④a rate 版', 2, False)
report4('④′ 足りなさ（値段÷参照）版', 3, True)

# ---- ⑤
def idle(path):
    d = {}
    for r in rows(path):
        n = r['nation']
        if n not in LISTED: continue
        dec = (int(r['year']) // 10) * 10
        c = fl(r['capacityRefValuePerMonth']); o = fl(r['outputRefValuePerMonth'])
        if c > 0 and o == o: d.setdefault((n, dec), []).append(1 - o / c)
    return {k: st.median(v) for k, v in d.items()}
if A:
    worst = []; over = tot = 0
    for s in seeds:
        fb = idle(os.path.join(B, f'factory_s{s}.tsv')); fa = idle(os.path.join(A, f'factory_s{s}.tsv'))
        for (n, dec), v in fb.items():
            if dec < 1800 or dec > 1860 or (n, dec) not in fa: continue
            tot += 1; diff = v - fa[(n, dec)]
            if diff > 0.10: over += 1
            worst.append((diff, s, n, dec))
    worst.sort(reverse=True)
    gate5 = all(s >= 11 for s in seeds)
    print(f'[⑤] 遊び（B−基線・同じ定義）が +0.10 を超えた 国×十年×種 {over}/{tot}・最大 {worst[0][0]:.2f}（{worst[0][1:]})  [{"PASS" if tot > 0 and over <= tot / 4 else "FAIL"}]（{"門＝種 11〜20" if gate5 else "記録＝種 1〜10 は canary を見てから置いた線"}）' if worst else '[⑤] NA')
