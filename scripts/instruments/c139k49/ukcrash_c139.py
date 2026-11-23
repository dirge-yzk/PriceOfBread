# 使い方: python -X utf8 -I ukcrash_c139.py <rawdir> <種の起点> <種の数> [国=イギリス]
# (1331) c139k49（c137k49 の写し）記録の読み手（読むだけ）：(1298) 反証役 v・v2 の 2 つの量を全期間の nation_s で読む。
#  ① 価格水準の開幕比の最大（priceLevel÷その国の最初の行の priceLevel）とその年・1 四半期の比の最大（pxmonth_s は開幕の 2 年だけ＝使わない）。
#  国は最初に名前が合った行の nationIndex で追う（政体の名で国の名が変わる＝例〈立憲王政イギリス〉）。
#  ② 実質 GDP（realGdpPerYear）がそれまでの最大の 5% を割った年の数と最初の年（その年の四半期のどれかで割れば数える）。
import sys, os
def fl(x):
    try: return float(x)
    except: return float("nan")
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); NM = sys.argv[4] if len(sys.argv) > 4 else "イギリス"
print(f"# ukcrash_c139 {raw} 種 {s0}..{s0+n-1} 国 {NM}")
print("種\t開幕比の最大\tその年\t1四半期の比の最大\t5%割れの年の数\t最初の年")
for s in range(s0, s0 + n):
    p = f"{raw}/nation_s{s}.tsv"
    if not os.path.exists(p): print(f"{s}\t欠け"); continue
    ni = None; p0 = None; pmax = 0.0; ymax = ""; qmax = 0.0; prev = None; gmax = 0.0; bad = set()
    with open(p, encoding="utf-8") as f:
        h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
        for l in f:
            c = l.rstrip("\n").split("\t")
            if ni is None and c[ix["nation"]] == NM: ni = c[ix["nationIndex"]]
            if ni is None or c[ix["nationIndex"]] != ni: continue
            pl, rg, y = fl(c[ix["priceLevel"]]), fl(c[ix["realGdpPerYear"]]), c[ix["year"]]
            if p0 is None and pl > 0: p0 = pl
            if p0 and pl / p0 > pmax: pmax, ymax = pl / p0, y
            if prev and prev > 0 and pl > 0: qmax = max(qmax, pl / prev, prev / pl)
            prev = pl
            if rg > gmax: gmax = rg
            elif gmax > 0 and rg < 0.05 * gmax: bad.add(y)
    print(f"{s}\t{pmax:.4g}\t{ymax}\t{qmax:.4g}\t{len(bad)}\t{min(bad) if bad else '-'}")
