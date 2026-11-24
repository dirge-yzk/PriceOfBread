# 問い2：基準（a11）と腕（A_wd）の同じ種で、表ごとに最初に違う行の時刻と、その時刻に違う欄を数える（読むだけ）
# 使い方: python q2_first_diff.py <基準のフォルダ> <腕のフォルダ> <種> [国名]
# 時刻の欄＝先頭の欄（quarter・monthIndex・time・year・startYear）。行は同じ並びと見なさず、(時刻, 国, 州…) の鍵で突き合わせる。
import sys, os, csv, collections
A, B, seed = sys.argv[1], sys.argv[2], sys.argv[3]
nat = sys.argv[4] if len(sys.argv) > 4 else None
def tables(d):
    return {f[:-len(f"_s{seed}.tsv")] for f in os.listdir(d) if f.endswith(f"_s{seed}.tsv")}
common = sorted(tables(A) & tables(B))
print(f"# 種 {seed}（basename から）基準={A} 腕={B} 国={nat or '全て'}・共通の表 {len(common)}")
def load(p):
    with open(p, encoding='utf-8') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r)
        rows = list(r)
    return h, rows
KEYCOLS = ['nation', 'nationIndex', 'nationUid', 'region', 'fromRegion', 'toRegion', 'sideA', 'sideB', 'good', 'kind', 'law']
for t in common:
    pa, pb = os.path.join(A, f"{t}_s{seed}.tsv"), os.path.join(B, f"{t}_s{seed}.tsv")
    ha, ra = load(pa); hb, rb = load(pb)
    if ha != hb: print(f"{t}\t見出しが違う"); continue
    tcol = 0
    ix = {k: i for i, k in enumerate(ha)}
    kc = [ix[k] for k in KEYCOLS if k in ix]
    natc = ix.get('nation')
    def keyed(rows):
        d = collections.defaultdict(list)
        for row in rows:
            if nat and natc is not None and row[natc] != nat: continue
            d[(row[tcol],) + tuple(row[i] for i in kc)].append(row)
        return d
    da, db = keyed(ra), keyed(rb)
    def tval(k):
        x = k[0]
        try: return float(x)
        except: pass
        if x == 'open': return -3e6
        if x == 'warm': return -1e6
        if x.startswith('wm'): return -2e6 + float(x[2:])
        if x.startswith('qm'): return float(x[2:]) / 3.0
        if x.startswith('q'): return float(x[1:])
        return 1e18
    keys = sorted(set(da) | set(db), key=tval)
    first = None
    for k in keys:
        if da.get(k) != db.get(k): first = k; break
    if first is None: print(f"{t}\t違い無し（行 {len(ra)}）"); continue
    t0 = first[0]
    colcnt = collections.Counter(); natcnt = collections.Counter(); only = 0
    for k in keys:
        if k[0] != t0: continue
        x, y = da.get(k), db.get(k)
        if x is None or y is None or len(x) != len(y): only += 1; continue
        for rx, ry in zip(x, y):
            if rx != ry:
                if natc is not None: natcnt[rx[natc]] += 1
                for i, (u, v) in enumerate(zip(rx, ry)):
                    if u != v: colcnt[ha[i]] += 1
    print(f"{t}\t最初の違い {ha[tcol]}={t0}\t鍵の片側だけ {only}\t違う欄（行数）: " + ', '.join(f"{c}:{n}" for c, n in colcnt.most_common(25)) + (f"\t国の数 {len(natcnt)}" if natcnt else ''))
