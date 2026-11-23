# (1323) c138lp 史実の桁の線と記録の読み手（monlaw_s を自分で読む・エンジンの関数を呼ばない）。
#   L1 英（イギリス）の参政権が男子普通（Suffrage 段 2＝AdultMale）以上に初めて届く年（種ごと・届かなければ none）。錨＝史実 1918〈推〉。
#   L2 参政権の段が前の行より下がった国×年の数（種ごと・全ての国）。錨＝史実の 1783〜1950 の後退の国×年〈推〉は下の注。
#   R1 軸ごとの段の上げ・下げの回数（全ての国・全ての種の和）＝記録。
#   使い方: python lp_hist.py <ins の dir> <種の起点> <種の数> [ラベル]
import sys, os, csv, collections
d, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); lab = sys.argv[4] if len(sys.argv) > 4 else d
up = collections.Counter(); down = collections.Counter(); l1 = []; l2 = []; miss = 0
for s in range(s0, s0 + n):
    p = os.path.join(d, f"monlaw_s{s}.tsv")
    if not os.path.exists(p): miss += 1; l1.append("欠け"); l2.append(-1); continue
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r)
        ix = {k: i for i, k in enumerate(h)}
        axes = [k for k in h[ix["EducationLaw"]:ix["EducationLawTarget"]]]
        ai = [ix["EducationLaw"] + j for j in range(len(axes))]
        prev = {}; first = None; regy = set()
        for row in r:
            if len(row) < len(h): continue
            uid = row[ix["nationUid"]]; yr = int(row[ix["year"]]); nat = row[ix["nation"]]
            cur = [int(float(row[i])) for i in ai]
            if uid in prev:
                pv = prev[uid]
                for j, a in enumerate(axes):
                    if cur[j] > pv[j]: up[a] += 1
                    elif cur[j] < pv[j]: down[a] += 1
                js = axes.index("Suffrage")
                if cur[js] < pv[js]: regy.add((uid, yr))
            prev[uid] = cur
            if nat == "イギリス" and first is None and cur[axes.index("Suffrage")] >= 2: first = yr
        l1.append(str(first) if first is not None else "none"); l2.append(len(regy))
print(f"# lp_hist {lab} 種 {s0}..{s0+n-1} 欠け {miss}")
print("L1 英の男子普通以上の最初の年（種ごと）: " + " ".join(l1))
print("L2 参政権が下がった国×年（種ごと）: " + " ".join(map(str, l2)) + f"  和 {sum(x for x in l2 if x >= 0)}")
print("R1 軸\t上げ\t下げ")
for a in sorted(set(up) | set(down), key=lambda k: -(up[k] + down[k])): print(f"{a}\t{up[a]}\t{down[a]}")
