# (1016) 紙 1：国教・官僚制度・秘密投票・検閲・徴兵の法の段の動き（基線とヌル腕・読むだけ）
# 使い方: python -X utf8 -I laws1016.py <出力ファイル> <名>=<生のフォルダ> ...（各フォルダの monlaw_s1..10.tsv を読む）
import sys, os, statistics as st

AXES = ["StateReligion", "Bureaucracy", "SecretBallot", "Censorship", "ConscriptionLaw", "LandReform", "SerfEmancipation"]
MAJ = ["イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "スペイン", "アメリカ", "日本", "清", "オスマン"]
YEARS = [1783, 1830, 1870, 1910, 1950]


def read_tsv(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').split('\n')
    h = lines[0].rstrip('\r').split('\t')
    rows = [l.rstrip('\r').split('\t') for l in lines[1:] if l.strip()]
    return h, rows


def q(xs, p):
    if not xs: return float('nan')
    s = sorted(xs); i = min(len(s) - 1, max(0, int(round(p * (len(s) - 1)))))
    return s[i]


def main():
    out = sys.argv[1]
    arms = [a.split('=', 1) for a in sys.argv[2:]]
    L = []
    for name, d in arms:
        L.append(f"## 腕 {name}  ({d})")
        lvl = {a: {y: {} for y in YEARS} for a in AXES}   # 段→件数
        ups = {a: [] for a in AXES}; downs = {a: [] for a in AXES}
        first = {(a, n, k): [] for a in ("StateReligion", "Bureaucracy") for n in MAJ for k in (1, 2)}
        start0 = {a: [] for a in AXES}
        tgtopen = {a: [0, 0] for a in ("StateReligion", "Bureaucracy", "SecretBallot")}
        seeds = 0
        for s in range(1, 11):
            p = os.path.join(d, f"monlaw_s{s}.tsv")
            if not os.path.exists(p): continue
            seeds += 1
            h, rows = read_tsv(p)
            ix = {c: h.index(c) for c in ["quarter", "year", "nation"] + AXES + ["StateReligionTarget", "BureaucracyTarget", "SecretBallotTarget"]}
            prev = {}
            u = {a: 0 for a in AXES}; dn = {a: 0 for a in AXES}
            reached = {}
            seen_first = set()
            for r in rows:
                n = r[ix["nation"]]; y = int(r[ix["year"]]); qq = int(r[ix["quarter"]])
                for a in AXES:
                    v = int(float(r[ix[a]]))
                    if y in lvl[a]: lvl[a][y][v] = lvl[a][y].get(v, 0) + 1
                    k = (n, a)
                    if k in prev:
                        if v > prev[k]: u[a] += 1
                        elif v < prev[k]: dn[a] += 1
                    else:
                        if qq == 0: start0[a].append(v)
                    prev[k] = v
                    if a in ("StateReligion", "Bureaucracy") and n in MAJ:
                        for kk in (1, 2):
                            if v >= kk and (a, n, kk) not in reached: reached[(a, n, kk)] = y
                for a in tgtopen:
                    cur = int(float(r[ix[a]])); t = int(float(r[ix[a + "Target"]]))
                    tgtopen[a][1] += 1
                    if t > cur: tgtopen[a][0] += 1
            for a in AXES: ups[a].append(u[a]); downs[a].append(dn[a])
            for key in first:
                first[key].append(reached.get(key))
        L.append(f"種の数 {seeds}")
        L.append("### 段の分布（その年の国×四半期の割合・段 0,1,2,3）")
        for a in AXES:
            parts = []
            for y in YEARS:
                c = lvl[a][y]; tot = sum(c.values())
                parts.append(f"{y}:" + "/".join(f"{c.get(k,0)/tot:.3f}" if tot else "nan" for k in range(4)))
            L.append(f"- {a}: " + "  ".join(parts))
        L.append("### 1783 年の開幕の段（国×種）")
        for a in AXES:
            c = {}
            for v in start0[a]: c[v] = c.get(v, 0) + 1
            L.append(f"- {a}: " + " ".join(f"段{k}={c[k]}" for k in sorted(c)))
        L.append("### 段の動き（全ての国の和・種ごと）上げ／下げ：中央 [最小,最大]")
        for a in AXES:
            L.append(f"- {a}: 上げ {st.median(ups[a]):.1f} [{min(ups[a])},{max(ups[a])}]  下げ {st.median(downs[a]):.1f} [{min(downs[a])},{max(downs[a])}]")
        L.append("### 目標＞今（上げの戦線が開いている国×四半期の割合）")
        for a, (o, t) in tgtopen.items():
            L.append(f"- {a}: {o}/{t} = {o/t:.4f}")
        L.append("### 主な国が段 1・段 2 に初めて届いた年（種ごと・届かない種は −）")
        for a in ("StateReligion", "Bureaucracy"):
            for n in MAJ:
                xs1 = first[(a, n, 1)]; xs2 = first[(a, n, 2)]
                if all(x is None for x in xs1) and all(x is None for x in xs2) and not any(xs1): pass
                f1 = [x for x in xs1 if x is not None]; f2 = [x for x in xs2 if x is not None]
                L.append(f"- {a} {n}: 段1 {len(f1)}/{seeds} 中央 {st.median(f1) if f1 else '−'} ／ 段2 {len(f2)}/{seeds} 中央 {st.median(f2) if f2 else '−'}  （{','.join(str(x) if x else '−' for x in xs1)}）")
        L.append("")
    with open(out, 'wb') as f:
        f.write(('\n'.join(L) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
