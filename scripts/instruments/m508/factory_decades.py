# (508) 工場の産出の年代表：6 国（英・仏・普・米・日・清）の factory_s の outputRefValuePerMonth（年の行＝その年の最後の四半期）の 10 種の中央値。
# 使い方: PYTHONUTF8=1 python factory_decades.py <名前>=<計器の ins フォルダ> [<名前>=<フォルダ> ...]
#   1 番目の腕を分母に比を出す（m500 を先に渡す）。m491 の生は消えている＝m491 比は線 eq の報告の比（eq の腕 B ÷ m491）を掛けた近似で別に書く。
import os, sys, csv, statistics as st

NATIONS = ["イギリス", "フランス", "プロイセン", "アメリカ", "日本", "清"]
YEARS = [1800, 1820, 1850, 1880, 1900, 1913, 1930, 1950]


def read(d):
    out = {}
    for sd in range(1, 21):
        p = os.path.join(d, f"factory_s{sd}.tsv")
        if not os.path.exists(p):
            continue
        with open(p, encoding="utf-8", newline="") as f:
            r = csv.reader(f, delimiter="\t")
            h = next(r)
            iy, inat, iv = h.index("year"), h.index("nation"), h.index("outputRefValuePerMonth")
            for row in r:
                try:
                    out[(sd, row[inat], int(row[iy]))] = float(row[iv])   # 同じ年の行は後の四半期が上書き＝年の最後の四半期
                except (ValueError, IndexError):
                    pass
    return out


def med(tab, n, y):
    vs = [v for (sd, nn, yy), v in tab.items() if nn == n and yy == y]
    return (st.median(vs), len(vs)) if vs else (float("nan"), 0)


arms = []
for a in sys.argv[1:]:
    k, d = a.split("=", 1)
    arms.append((k, read(d)))
base_k, base = arms[0]
print("# 工場の産出 outputRefValuePerMonth（10 種の中央値・年の最後の四半期）。比は " + base_k + " を分母")
for n in NATIONS:
    print(f"## {n}")
    print("年\t" + "\t".join(f"{k}" for k, _ in arms) + "\t" + "\t".join(f"{k}/{base_k}" for k, _ in arms[1:]))
    for y in YEARS:
        vals = [med(t, n, y) for _, t in arms]
        b = vals[0][0]
        ratios = [(v / b if b and b == b and b > 0 else float("nan")) for v, _ in vals[1:]]
        print(f"{y}\t" + "\t".join(f"{v:.4g}(n{c})" for v, c in vals) + "\t" + "\t".join(f"{r:.3f}" for r in ratios))
