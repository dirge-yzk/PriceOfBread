# (1000) c91ld：r932/rentshare932.py の写し（import の名と表示名だけ替えた・式は同じ・盤面 m988 の生へ当てる）
# (932) c81ld：r898/rentshare898.py の写し（盤面の名 m873→m919 と import の名だけ替えた・式は同じ）
# (898) c76ds 読むだけ：ブロックごとの地代の割合 engineRentShare0..2（cornint_s）＝閉鎖の層の外が持つ地代の大きさ（InterestGroups.cs:62 の資本化の非対称の材料）
import sys, os, math
SEEDS = range(1, 11)
PER = [(1783, 1846), (1847, 1913), (1914, 1950)]
FOCUS = ["イギリス", "プロイセン", "ロシア", "アメリカ", "フランス", "清", "日本"]
def q(v, p):
    v = sorted(x for x in v if not math.isnan(x))
    return v[int((len(v) - 1) * p)] if v else float("nan")
d = sys.argv[1]
acc = {}
allr = {p: [[], [], []] for p in PER}
for s in SEEDS:
    with open(os.path.join(d, f"cornint_s{s}.tsv"), "rb") as fh:
        L = fh.read().decode("utf-8").splitlines()
    h = L[0].split("\t"); ix = {n: i for i, n in enumerate(h)}
    for ln in L[1:]:
        c = ln.split("\t"); y = int(c[ix["year"]]); n = c[ix["nation"]]
        per = next((p for p in PER if p[0] <= y <= p[1]), None)
        if per is None: continue
        sh = [float(c[ix[f"engineRentShare{b}"]]) for b in range(3)]
        if float(c[ix["rentDomTotalPerMonth"]]) <= 1e-9: continue
        for b in range(3): allr[per][b].append(sh[b])
        if n in FOCUS: acc.setdefault((n, per), [[], [], []])
        if n in FOCUS:
            for b in range(3): acc[(n, per)][b].append(sh[b])
print(f"## {d}")
print("| 範囲 | 時期 | 旧支配層 0 中央 | 近代 1 中央（p90） | 大衆 2 中央（p90） |")
print("|---|---|---|---|---|")
for p in PER:
    a = allr[p]; print(f"| 全ての国 | {p[0]}〜{p[1]} | {q(a[0],.5):.3f} | {q(a[1],.5):.3f}（{q(a[1],.9):.3f}） | {q(a[2],.5):.3f}（{q(a[2],.9):.3f}） |")
for n in FOCUS:
    for p in PER:
        a = acc.get((n, p))
        if a: print(f"| {n} | {p[0]}〜{p[1]} | {q(a[0],.5):.3f} | {q(a[1],.5):.3f}（{q(a[1],.9):.3f}） | {q(a[2],.5):.3f}（{q(a[2],.9):.3f}） |")
