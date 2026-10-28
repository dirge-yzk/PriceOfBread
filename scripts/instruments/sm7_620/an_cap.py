# (620) 線 sm7：狙いの数字 ①（世界の工場の能力）・②（枠の埋まり）・⑤（遊び＝埋まっていない枠）の読み手（読むだけ）。事前登録 measurements/sm7_620_20261028_prereg.txt [4]。
# 使い方：python -X utf8 an_cap.py <腕 B の計器の出力フォルダ> <基線の計器の出力フォルダ> [<ヌル腕 N1> <ヌル腕 N2>]
#   フォルダ直下に factory_s<種>.tsv（全腕）と slot_s<種>.tsv（腕 B）がある前提。種は B と基線で同じ番号を突き合わせる。
# ①(a)：1790〜1830 年の十年ごと（年末の四半期）に、世界（計器の国の和）の工場の能力 capacityRefValuePerMonth の 種ごとの比 B÷基線 の中央値が [1−w, 1+w] の内。
#       w＝max(0.15, ヌル腕 N1・N2 の同じ比の |比−1| の種×年の最大)。ヌル腕が無ければ 0.15。(b)：1870〜1913 年の十年ごとの比の中央値が 1 を超える年が過半。
# ②：ΣN×(1−idleShare)÷ΣN（国の行を sumN で重み付け）の 1790〜1860 年の年末の系列の最小二乗の傾きが正の種 ≥8/10。
# ⑤：遊び＝idleShare（埋まっていない枠＝N×max(0,1−産出÷能力)÷N・供給曲線×投入充足ではない）。国×種ごとに 1790 年代〜1910 年代の十年の中央値が毎十年上がり続ける国×種 ≤1/4。
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

def seeds(d, pat):
    out = {}
    for p in glob.glob(os.path.join(d, pat)):
        b = os.path.basename(p); s = ''.join(c for c in b.split('_s')[-1] if c.isdigit())
        if s: out[int(s)] = p
    return out

def cap_series(p):
    ser = {}
    for r in rows(p):
        y, q = int(fl(r['year'])), int(fl(r['quarter']))
        if q % 4 != 3: continue
        ser[y] = ser.get(y, 0.0) + fl(r['capacityRefValuePerMonth'])
    return ser

def ratio_table(dB, dA):
    sb, sa = seeds(dB, 'factory_s*.tsv'), seeds(dA, 'factory_s*.tsv'); out = {}
    for s in sorted(set(sb) & set(sa)):
        b, a = cap_series(sb[s]), cap_series(sa[s])
        out[s] = {y: b[y] / a[y] for y in b if y in a and a[y] > 0}
    return out

def decade_med(tab, years):
    res = {}
    for y in years:
        v = [t[y] for t in tab.values() if y in t]
        res[y] = st.median(v) if v else float('nan')
    return res

def fill_slope(p):
    xs, ys = [], []; byy = {}
    for r in rows(p):
        y, q = int(fl(r['year'])), int(fl(r['quarter']))
        if q % 4 != 3 or not (1790 <= y <= 1860): continue
        n = fl(r['sumN']); idle = fl(r['idleShare'])
        if n == n and idle == idle and n > 0:
            a = byy.setdefault(y, [0.0, 0.0]); a[0] += n * (1 - idle); a[1] += n
    for y, (num, den) in sorted(byy.items()):
        if den > 0: xs.append(y); ys.append(num / den)
    if len(xs) < 5: return float('nan')
    mx, my = sum(xs) / len(xs), sum(ys) / len(ys)
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / max(1e-12, sum((x - mx) ** 2 for x in xs))

def idle_monotone(p):
    dec = {}
    for r in rows(p):
        y, q = int(fl(r['year'])), int(fl(r['quarter']))
        nat = r['nation']
        if nat not in LISTED or y < 1790 or y > 1919: continue
        v = fl(r['idleShare'])
        if v == v: dec.setdefault((nat, (y - 1790) // 10), []).append(v)
    res = {}
    for nat in LISTED:
        meds = [st.median(dec[(nat, k)]) for k in range(13) if (nat, k) in dec]
        if len(meds) >= 8: res[nat] = all(b > a for a, b in zip(meds, meds[1:]))
    return res

def mark(b): return 'PASS' if b is True else ('FAIL' if b is False else 'NA')

def main():
    dB, dA = sys.argv[1], sys.argv[2]; nulls = sys.argv[3:5]
    tab = ratio_table(dB, dA)
    w = 0.15
    # ★追記 2：幅 w は〈ヌル腕とヌル腕と同じ木の基線との差〉で出す（第 5 引数 dN0）。基線(dA)が別の木のとき、木の違い（軌道の動き）が w に入って ①(a) が通りやすくなる。dN0 が無ければ床 0.15 のまま（名乗る）。
    dN0 = sys.argv[5] if len(sys.argv) > 5 else None
    if dN0:
        for dn in nulls:
            for s, t in ratio_table(dn, dN0).items():
                for y in range(1790, 1831, 10):
                    if y in t: w = max(w, abs(t[y] - 1))
    early = decade_med(tab, range(1790, 1831, 10)); late = decade_med(tab, range(1870, 1914, 10))
    vals1a = [v for v in early.values() if v == v]
    ok1a = (all(abs(v - 1) <= w for v in vals1a)) if vals1a else None   # ★追記 2：年の値が 1 つも無いときは NA（空の all は真になる）
    n1b = sum(1 for v in late.values() if v == v and v > 1); tot1b = sum(1 for v in late.values() if v == v)
    print(f'[①a] 世界の工場の能力 B÷基線（種の中央値）1790-1830={ {y: round(v, 3) for y, v in early.items()} }・幅 w={w:.3f}  [{mark(ok1a)}]')
    print(f'[①b] 1870-1913 の十年の比が 1 を超える年={n1b}/{tot1b}（過半）値={ {y: round(v, 3) for y, v in late.items()} }  [{mark(n1b * 2 > tot1b if tot1b else None)}]')
    sl = [fill_slope(p) for s, p in sorted(seeds(dB, 'slot_s*.tsv').items())]
    pos = sum(1 for v in sl if v == v and v > 0)
    print(f'[②] 枠の埋まり（N 加重の 1−遊び）の 1790-1860 の傾きが正の種={pos}/{len(sl)}（≥8/10）値={[round(v, 5) for v in sl]}  [{mark(pos >= 8 if sl else None)}]')
    mono = []
    for s, p in sorted(seeds(dB, 'slot_s*.tsv').items()): mono += list(idle_monotone(p).values())
    print(f'[⑤] 遊びが毎十年上がり続ける国×種={sum(1 for v in mono if v)}/{len(mono)}（≤1/4）  [{mark(sum(1 for v in mono if v) * 4 <= len(mono) if mono else None)}]')

if __name__ == '__main__':
    main()
