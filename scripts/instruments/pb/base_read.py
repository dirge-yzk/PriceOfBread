# -*- coding: utf-8 -*-
"""(384) 政治の束の削除の基準の読み（読むだけ）。使い方：PYTHONUTF8=1 python base_read.py <出力>/ins [<出力>/ins ...]
読む表＝nation_s<種>.tsv（polityCode・civilWarMonths・nationUid・nation：全ての国が毎四半期）／scalars_s<種>.tsv（educationLaw・militancyMean・polityCode：挙げた国が毎四半期・他は年末）。
事前登録 measurements/pb_20261017_prereg.txt の [6] の定義どおり：遷移＝nation_s の polityCode が前の四半期から変わった四半期。内戦の発生＝civilWarMonths が 0 以下から正になった四半期。
腕の読み手（後半の担当）はこの定義を再利用する。腕の結果を見てから式を書かない。"""
import sys, glob, collections, statistics
NAMES = ["Absolutist", "Despotic", "Shogunate", "ConstMon", "Republic", "BD", "PD", "MD", "Anarchy"]
DICT = {5, 6, 7}
DEMO = ["イギリス", "ネーデルラント", "アメリカ", "スイス", "スウェーデン", "デンマーク"]
H2N = ["清", "オスマン", "ロシア", "日本"]
YEAR_END = 1951

def read_nation(d):
    out = {}
    for f in sorted(glob.glob(d + "/nation_s*.tsv")):
        seed = int(f.split("nation_s")[-1].split(".")[0])
        it = open(f, encoding="utf-8"); h = next(it).rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
        rows = []
        for l in it:
            r = l.rstrip("\n").split("\t")
            if len(r) <= ix["polityCode"]: continue
            try: pc = int(float(r[ix["polityCode"]]))
            except ValueError: continue
            cw = r[ix["civilWarMonths"]]; cw = float(cw) if cw not in ("", "nan") else 0.0
            rows.append((int(r[ix["quarter"]]), r[ix["nationUid"]], r[ix["nation"]], pc, cw))
        out[seed] = rows
    return out

def flows(rows):
    last = {}; cwl = {}; start = {}; tr = 0; ent = collections.Counter(); cw = 0; stay = collections.defaultdict(list); demo = collections.Counter()
    for q, u, n, pc, c in rows:
        if u in last and last[u] != pc:
            tr += 1; ent[pc] += 1
            if last[u] in DICT: stay[last[u]].append(q - start[u])
            if pc in DICT and n in DEMO: demo[n] += 1
            start[u] = q
        if u not in last: start[u] = q
        if c > 0 and cwl.get(u, 0) <= 0: cw += 1
        last[u] = pc; cwl[u] = c
    return tr, ent, cw, stay, demo

WINDOWS = {"ロシア": (1917, 1925), "プロイセン": (1930, 1940), "スペイン": (1920, 1945), "日本": (1930, 1945), "ポルトガル": (1926, 1945), "清": (1911, 1925), "オスマン": (1908, 1925)}

def windows(d):
    """史実の独裁の窓：10 種のうち、窓の中で polityCode が独裁（BD・PD・MD）の四半期が 1 回でもある種の数 k（国は四半期 0 の名前の個体に追随）。"""
    k = collections.Counter(); alive = collections.Counter()
    for f in sorted(glob.glob(d + "/nation_s*.tsv")):
        it = open(f, encoding="utf-8"); h = next(it).rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
        uid0 = {}; hit = set(); seen = set()
        for l in it:
            r = l.rstrip("\n").split("\t")
            if len(r) <= ix["polityCode"]: continue
            q = int(r[ix["quarter"]]); u = r[ix["nationUid"]]; n = r[ix["nation"]]
            if q == 0 and n in WINDOWS: uid0[u] = n
            if u in uid0:
                nm = uid0[u]; seen.add(nm); y = int(r[ix["year"]])
                try: pc = int(float(r[ix["polityCode"]]))
                except ValueError: continue
                if WINDOWS[nm][0] <= y <= WINDOWS[nm][1] and pc in DICT: hit.add(nm)
        for nm in hit: k[nm] += 1
        for nm in seen: alive[nm] += 1
    return k, alive

def scalars(d):
    res = collections.defaultdict(list)
    for f in sorted(glob.glob(d + "/scalars_s*.tsv")):
        it = open(f, encoding="utf-8"); h = next(it).rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
        seed = int(f.split("scalars_s")[-1].split(".")[0]); first = {}; mil = []
        for l in it:
            r = l.rstrip("\n").split("\t")
            if len(r) < len(h) - 1: continue
            n = r[ix["nation"]]; y = int(r[ix["year"]])
            if n in H2N and int(r[ix["educationLaw"]]) >= 2 and n not in first: first[n] = y
            try: mil.append(float(r[ix["militancyMean"]]))
            except ValueError: pass
        res["mil"].append(statistics.median(mil) if mil else float("nan"))
        for n in H2N: res["edu_" + n].append(first.get(n, YEAR_END))
    return res

for d in sys.argv[1:]:
    print("== " + d)
    nat = read_nation(d); tot_tr = 0; tot_ent = collections.Counter(); tot_cw = 0; stay = collections.defaultdict(list); demo = collections.Counter(); per = []
    for s, rows in sorted(nat.items()):
        tr, ent, cw, st, dm = flows(rows); tot_tr += tr; tot_cw += cw
        for k, v in ent.items(): tot_ent[k] += v
        for k, v in st.items(): stay[k] += v
        for k, v in dm.items(): demo[k] += v
        per.append((tr, sum(ent[k] for k in DICT), cw))
    print("種の数", len(nat), "遷移の総数", tot_tr, "（種ごと 遷移・独裁への流入・内戦の発生）", per)
    print("流入（政体別）", {NAMES[k]: v for k, v in sorted(tot_ent.items())}, "独裁（BD+PD+MD）への流入", sum(tot_ent[k] for k in DICT), "内戦の発生", tot_cw)
    for k in sorted(stay):
        v = sorted(stay[k]); print("独裁の在任（四半期）", NAMES[k], "n", len(v), "中央値", v[len(v) // 2] if v else None)
    print("民主制の核（" + "・".join(DEMO) + "）への独裁の流入", dict(demo), "計", sum(demo.values()))
    kk, al = windows(d)
    print("史実の独裁の窓の k（10 種のうち窓の中で独裁の政体の四半期がある種の数・生きていた種の数）", {n: (kk[n], al[n]) for n in WINDOWS})
    sc = scalars(d)
    if sc:
        print("militancyMean の種ごとの中央値", [round(x, 4) for x in sc["mil"]], "全体の中央値", round(statistics.median(sc["mil"]), 4))
        for n in H2N: print("教育法 Public 以上の最初の年の中央値", n, statistics.median(sc["edu_" + n]), "届かない種", sum(1 for y in sc["edu_" + n] if y >= YEAR_END), sc["edu_" + n])
