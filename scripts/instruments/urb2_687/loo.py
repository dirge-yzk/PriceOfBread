# (687) 線 urb2：M2（失業）・M3（飢えの門）の門の空振りの率を、ヌル腕の一つ抜きで測る（gaud の型・読むだけ）。
# 使い方：python loo.py <基線> <N1> <N2>
# 規則：3 本（基線・N1・N2）のうち 1 本を〈腕〉と見なし、残る 2 本の幅（最大−最小）の 2 倍だけ最大の上へ出たら〈外〉。M2＝9 国の失業の全四半期の平均（種の中央値）、
#       M3＝9 国×5 窓の飢えの門の割合 famG（種の中央値）。三通りで外の数を出す。空振りの率＝3 通りのうち門（M2＝5／9 国以上・M3＝5／9 国以上の窓を持つ国…は下）が開く通りの数÷3。
import sys, csv, statistics as st, collections
dirs = [x.rstrip("/") for x in sys.argv[1:4]]
NAT9 = ["イギリス", "フランス", "プロイセン", "オーストリア", "ロシア", "オスマン", "日本", "清", "アメリカ"]
EPS = [(1783, 1815), (1815, 1850), (1850, 1880), (1880, 1914), (1914, 1951)]
def ep(y):
    for a, b in EPS:
        if a <= y < b: return f"{a}-{b-1}"
def m2(d):
    per = collections.defaultdict(list)
    for s in range(1, 11):
        with open(f"{d}/labor_s{s}.tsv", encoding="utf-8") as f:
            r = csv.reader(f, delimiter="\t"); h = next(r); ina, ic = h.index("nation"), h.index("plainUnemploymentShare")
            acc = collections.defaultdict(list)
            for row in r:
                if row[ina] not in NAT9: continue
                try: v = float(row[ic])
                except: continue
                if v == v: acc[row[ina]].append(v)
            for n, l in acc.items(): per[n].append(st.mean(l))
    return {n: st.median(l) for n, l in per.items()}
def m3(d):
    per = collections.defaultdict(list)
    for s in range(1, 11):
        with open(f"{d}/urxpd_s{s}.tsv", encoding="utf-8") as f:
            acc = collections.defaultdict(lambda: [0.0, 0.0])
            for row in csv.DictReader(f, delimiter="\t"):
                k = (row["nation"], ep(int(row["year"]))); acc[k][0] += float(row["famineGated"]); acc[k][1] += float(row["regionMonths"])
            for k, (a, b) in acc.items():
                if b > 0: per[k].append(a / b)
    return {k: st.median(l) for k, l in per.items()}
def loo(vals):   # vals: 3 本の {鍵: 値}
    out = []
    for arm in range(3):
        others = [vals[i] for i in range(3) if i != arm]
        n = 0; tot = 0
        for k in vals[arm]:
            if not all(k in o for o in others): continue
            lo, hi = min(o[k] for o in others), max(o[k] for o in others)
            tot += 1
            if vals[arm][k] > hi + 2 * (hi - lo): n += 1
        out.append((n, tot))
    return out
v2 = [m2(d) for d in dirs]; v3 = [m3(d) for d in dirs]
print("M2（9 国の失業・全四半期の平均・種の中央値）：ヌル腕の一つ抜きで〈外〉（残り 2 本の最大＋幅の 2 倍を超える）になった国の数（腕＝基線・N1・N2 の順）:", loo(v2))
print("M3（9 国×5 窓の飢えの門の割合）：同じ規則で外になった窓の数（腕＝基線・N1・N2 の順）:", loo(v3))
