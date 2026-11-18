# 使い方: python -X utf8 -I chain.py <腕の rawdir> <基準の rawdir> <seeds 1-10>
# (1223) c123iv 記録：鎖の読み（十年ごと・国×年の中央）＝建設の計画÷名目 GDP（月×12）・資本家の稼ぎ÷名目 GDP（月×12）・資本家の貯蓄÷名目 GDP（年）。
import sys, statistics as st
def fl(x):
    try: return float(x)
    except: return float("nan")
def read(raw, sr):
    s0, s1 = int(sr.split('-')[0]), int(sr.split('-')[1]); out = {}
    for s in range(s0, s1 + 1):
        ny = {}
        with open(f"{raw}/nation_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t"); k = (c[ix["nation"]], int(c[ix["year"]]))
                a = ny.setdefault(k, [0, 0.0, 0.0]); a[0] += 1; a[1] += fl(c[ix["constructionPlannedMoneyPerMonth"]]); a[2] += fl(c[ix["nominalGdpPerYear"]])
        with open(f"{raw}/class_s{s}.tsv", encoding="utf-8") as f:
            h = f.readline().rstrip("\n").split("\t"); ix = {k: i for i, k in enumerate(h)}
            for l in f:
                c = l.rstrip("\n").split("\t")
                if c[0] in ("open", "warm") or int(c[3]) != 3: continue
                a = ny.get((c[1], int(c[2])))
                if not a or not a[0]: continue
                g = a[2] / a[0]
                if not (g > 0): continue
                d = out.setdefault(int(c[2]) // 10 * 10, [[], [], []])
                d[0].append(12 * a[1] / a[0] / g); d[1].append(12 * fl(c[ix["earnedPerMonth"]]) / g); d[2].append(fl(c[ix["savingsStock"]]) / g)
    return out
A = read(sys.argv[1], sys.argv[3]); B = read(sys.argv[2], sys.argv[3])
print("十年\t建設÷GDP 腕/基準\t資本家の稼ぎ÷GDP 腕/基準\t資本家の貯蓄÷GDP 腕/基準")
for d in sorted(set(A) & set(B)):
    m = lambda v: st.median([x for x in v if x == x]) if v else float("nan")
    print(f"{d}\t{m(A[d][0]):.4f}/{m(B[d][0]):.4f}\t{m(A[d][1]):.4f}/{m(B[d][1]):.4f}\t{m(A[d][2]):.3f}/{m(B[d][2]):.3f}")
