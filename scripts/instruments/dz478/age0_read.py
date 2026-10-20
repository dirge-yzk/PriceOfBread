# (478) 段0の読み：計器つき 10 種の pop_s（N 行）から、国×期間の年齢の集計（0〜14・15〜64 の働き手・60 以上・出産の女の割合）と
# 粗出生・粗死亡を、期間の人口加重の平均→種どうしの中央値で出す。層の数に依らない列だけを読む＝8 層（A）と 11 層（B）の両方に使える。
# 使い方: PYTHONUTF8=1 python -I age0_read.py <計器の生のフォルダ> [出力のファイル] [比べる側のフォルダ]
#   比べる側を渡すと、国×期間×列の B÷A（同じ種どうしの比の中央値）も出す。
import csv, sys, statistics as st, collections
NATIONS = ["イギリス", "フランス", "プロイセン", "ロシア", "日本", "清", "アメリカ", "オーストリア"]
ERAS = [(1783, 1799), (1800, 1849), (1850, 1899), (1900, 1949)]
COLS = ["share0to14", "adultShare", "share60plus", "fertileWomenShare", "cbrPerMille", "cdrPerMille"]
def read(D):
    res = {}   # seed -> {(nation, era) -> {col: weighted mean}}
    for s in range(1, 11):
        acc = collections.defaultdict(lambda: collections.defaultdict(float)); wt = collections.defaultdict(float)
        try: f = open(f"{D}/pop_s{s}.tsv", encoding="utf-8")
        except FileNotFoundError: continue
        with f:
            for row in csv.DictReader(f, delimiter="\t"):
                if row["scope"] != "N": continue
                n = row["nation"]
                if n not in NATIONS: continue
                y = int(row["year"])
                try: w = float(row["popK"]) * float(row["monthsCount"])
                except ValueError: continue
                for i, (a, b) in enumerate(ERAS):
                    if a <= y <= b:
                        for c in COLS:
                            try: v = float(row[c])
                            except ValueError: continue
                            if v != v: continue
                            acc[(n, i)][c] += v * w
                        wt[(n, i)] += w
        res[s] = {k: {c: v / wt[k] for c, v in d.items()} for k, d in acc.items() if wt[k] > 0}
    return res
def med(xs): return st.median(xs) if xs else float("nan")
if __name__ == "__main__":
    A = read(sys.argv[1]); out = open(sys.argv[2], "w", encoding="utf-8", newline="\n") if len(sys.argv) > 2 and sys.argv[2] != "-" else sys.stdout
    B = read(sys.argv[3]) if len(sys.argv) > 3 else None
    out.write("国\t期間\t" + "\t".join(COLS) + ("\t" + "\t".join(c + "_B/A" for c in COLS) if B else "") + "\n")
    for n in NATIONS:
        for i, (a, b) in enumerate(ERAS):
            vals = [med([A[s][(n, i)][c] for s in A if (n, i) in A[s] and c in A[s][(n, i)]]) for c in COLS]
            line = f"{n}\t{a}-{b}\t" + "\t".join(f"{v:.4f}" for v in vals)
            if B:
                rs = []
                for c in COLS:
                    r = [B[s][(n, i)][c] / A[s][(n, i)][c] for s in A if s in B and (n, i) in A[s] and (n, i) in B[s] and c in A[s][(n, i)] and c in B[s][(n, i)] and A[s][(n, i)][c] != 0]
                    rs.append(med(r))
                line += "\t" + "\t".join(f"{v:.4f}" for v in rs)
            out.write(line + "\n")
