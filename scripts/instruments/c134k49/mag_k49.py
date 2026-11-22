# 使い方: python -X utf8 -I mag_k49.py <腕の rawdir> <基準の rawdir> <種の起点> <種の数>
# (1298) c134k49 史実の桁の線（この線が一番動かす量＝工場の産出）の読み手（読むだけ・c123iv/magn_iv.py の H1 と同じ物差しを 1870 年へ広げた）。
#  量＝factory_s の outputRefValuePerMonth の年の四半期の平均（6 大国：イギリス・フランス・プロイセン〔ドイツ帝国があればその行〕・アメリカ・ロシア・オーストリア）。
#  O1870・O1913・伸び G＝O1913÷O1870 の種の中央を国ごとに出す。
#  門 H-b（同じ盤面の旧の規則を物差しに）：腕÷基準（種の中央どうし）が 10 以上か 0.1 以下なら赤（基準の値が 0 か欠けなら判定不能）。
#  門 H-a（史実）：腕の G÷史実が 10 以上か 0.1 以下で、基準の同じ比が (0.1,10) の内なら赤（基準がすでに外なら〈元から〉＝記録）。
#  史実の G（1870→1913 の工業の産出の伸び・全て〈推〉＝AI の記憶の値・Bairoch 1982 の工業化の水準の系列ほか）：
#   英 2.2・独 4.5・米 6.6・仏 2.6・露 5.6・墺 3.3。
import sys, os, statistics as st
SIX = ["イギリス", "フランス", "プロイセン", "アメリカ", "ロシア", "オーストリア"]
HIST = {"イギリス": 2.2, "フランス": 2.6, "プロイセン": 4.5, "アメリカ": 6.6, "ロシア": 5.6, "オーストリア": 3.3}
NAN = float("nan")
def fl(x):
    try: return float(x)
    except: return NAN
def read(raw, s0, n):
    out = {k: {"O1870": [], "O1913": [], "G": []} for k in SIX}; ns = 0
    for s in range(s0, s0 + n):
        p = f"{raw}/factory_s{s}.tsv"
        if not os.path.exists(p): continue
        ns += 1
        acc = {}
        with open(p, encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                y = c[ix["year"]]
                if y not in ("1870", "1913"): continue
                nm = c[ix["nation"]]
                if nm not in SIX and nm != "ドイツ帝国": continue
                a = acc.setdefault((y, nm), [0, 0.0]); a[0] += 1; a[1] += fl(c[ix["outputRefValuePerMonth"]])
        for y in ("1870", "1913"):
            if (y, "ドイツ帝国") in acc: acc[(y, "プロイセン")] = acc.pop((y, "ドイツ帝国"))
        for k in SIX:
            o70 = acc[("1870", k)][1] / acc[("1870", k)][0] if ("1870", k) in acc else NAN
            o13 = acc[("1913", k)][1] / acc[("1913", k)][0] if ("1913", k) in acc else NAN
            out[k]["O1870"].append(o70); out[k]["O1913"].append(o13)
            out[k]["G"].append(o13 / o70 if o70 > 0 and o13 == o13 else NAN)
    med = {k: {q: (st.median([x for x in v if x == x]) if any(x == x for x in v) else NAN) for q, v in d.items()} for k, d in out.items()}
    return med, ns
def main():
    arm, base, s0, n = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    A, na = read(arm, s0, n); B, nb = read(base, s0, n)
    print(f"# mag_k49 腕={arm} 基準={base} 種 {s0}..{s0+n-1} 読めた種 腕 {na} 基準 {nb}")
    if na == 0 or na != nb: print("判定不能（種の数）"); return
    red = 0
    print("国\t量\t腕\t基準\t腕÷基準\tH-b")
    for k in SIX:
        for q in ("O1870", "O1913", "G"):
            a, b = A[k][q], B[k][q]
            r = a / b if b > 0 and a == a else NAN
            v = "判定不能" if r != r else ("赤" if r >= 10 or r <= 0.1 else "緑")
            if v == "赤": red += 1
            print(f"{k}\t{q}\t{a:.6g}\t{b:.6g}\t{r:.4f}\t{v}")
    print("国\tG 腕÷史実\tG 基準÷史実\tH-a")
    for k in SIX:
        ra = A[k]["G"] / HIST[k]; rb = B[k]["G"] / HIST[k]
        inb = rb == rb and 0.1 < rb < 10
        v = "判定不能" if ra != ra else ("赤" if (ra >= 10 or ra <= 0.1) and inb else ("元から（記録）" if not inb else "緑"))
        if v == "赤": red += 1
        print(f"{k}\t{ra:.4f}\t{rb:.4f}\t{v}")
    print(f"門 H 赤 {red}")
main()
