# -*- coding: utf-8 -*-
"""(384) 政治の束の発散の読み手（読むだけ）。使い方：PYTHONUTF8=1 python an_pb_div.py <出力>/ins [<比べる基準の出力>/ins ...]
事前登録 measurements/pb_20261017_prereg.txt の追記3 の発散 (i')(ii')(iii) の定義どおり。腕の結果を見てから式を書かない（基準 m377_ins で先に動かした）。
(i')  国債の利回り bondRateMeanPerYear が上限 0.30 に張り付いた（≧0.295）国×四半期の数（人口 100 以上＝populationK≧100）。上限は Fiscal.cs の BondRateMaxPerYear＝年率 1.0 は作りの上で出ない。
(ii') 人口 100 以上の国の 1 四半期に populationK か totalPopulationK が 3 倍以上／1/3 未満へ跳んだ数のうち、国の州の集合が前の四半期と同じもの（併合・分離・州の移りでない跳び）。
(iii) priceLevel の最終の四半期の値が、その種を除く基準の種の同じ国（名前）の中央値の 10 倍以上／1/10 未満の国の数（種ごと）。"""
import sys, glob, collections, statistics, math

def cols(it, names):
    h = next(it).rstrip("\n").split("\t"); ix = {n: i for i, n in enumerate(h)}
    return [ix[n] for n in names], len(h)

def fnum(s):
    try: return float(s)
    except ValueError: return float("nan")

def read_seed(d, seed):
    """nation_s と region_s を読む。戻り値：(行 {(q,uid): dict}, 州の集合 {(q,nationIndex): frozenset}, 最終の四半期)。"""
    rows = {}; ni_uid = {}
    with open("%s/nation_s%d.tsv" % (d, seed), encoding="utf-8") as f:
        ix, n = cols(f, ["quarter", "nationUid", "nationIndex", "nation", "populationK", "totalPopulationK", "bondRateMeanPerYear", "bondClampTermPerYear", "priceLevel"])
        for l in f:
            r = l.rstrip("\n").split("\t")
            if len(r) < n - 1: continue
            q = int(r[ix[0]]); u = r[ix[1]]
            rows[(q, u)] = dict(ni=r[ix[2]], name=r[ix[3]], pop=fnum(r[ix[4]]), tpop=fnum(r[ix[5]]), bond=fnum(r[ix[6]]), clamp=fnum(r[ix[7]]), price=fnum(r[ix[8]]))
    regs = collections.defaultdict(set)
    with open("%s/region_s%d.tsv" % (d, seed), encoding="utf-8") as f:
        ix, n = cols(f, ["quarter", "nationIndex", "region"])
        for l in f:
            r = l.rstrip("\n").split("\t")
            if len(r) < n - 1: continue
            regs[(int(r[ix[0]]), r[ix[1]])].add(r[ix[2]])
    return rows, regs

def seeds(d):
    return sorted(int(f.split("nation_s")[-1].split(".")[0]) for f in glob.glob(d + "/nation_s*.tsv"))

def analyse(d, base_final=None):
    res = {"clamped": 0, "popjump": 0, "popjump_all": 0, "bond_max": 0.0, "final": {}, "per_seed": {}}
    for s in seeds(d):
        rows, regs = read_seed(d, s)
        by_uid = collections.defaultdict(list)
        for (q, u), r in rows.items(): by_uid[u].append((q, r))
        cl = jm = jm_all = 0; last_q = max(q for q, _ in rows)
        for u, lst in by_uid.items():
            lst.sort(key=lambda x: x[0])
            for (q0, a), (q1, b) in zip(lst, lst[1:]):
                if q1 != q0 + 1: continue
                if a["pop"] >= 100 or a["tpop"] >= 100:
                    jumped = False
                    for k in ("pop", "tpop"):
                        x, y = a[k], b[k]
                        if x >= 100 and y == y and (y >= 3 * x or y < x / 3): jumped = True
                    if jumped:
                        jm_all += 1
                        if regs.get((q0, a["ni"])) == regs.get((q1, b["ni"])): jm += 1
            for q, r in lst:
                if r["pop"] >= 100:
                    if r["bond"] == r["bond"]:
                        res["bond_max"] = max(res["bond_max"], r["bond"])
                        if r["bond"] >= 0.295: cl += 1
        final = {}
        for u, lst in by_uid.items():
            q, r = max(lst, key=lambda x: x[0])
            if q == last_q and r["pop"] >= 100 and r["price"] == r["price"] and r["price"] > 0: final[r["name"]] = r["price"]
        res["final"][s] = final; res["clamped"] += cl; res["popjump"] += jm; res["popjump_all"] += jm_all
        res["per_seed"][s] = (cl, jm, jm_all)
    return res

def price_div(final, ref_final_by_seed):
    """種ごとの (iii)：その種を除く基準の種の同じ国の中央値の 10 倍以上／1/10 未満の国の数。"""
    out = {}
    for s, fin in final.items():
        n = 0
        for nm, p in fin.items():
            ref = [f[nm] for t, f in ref_final_by_seed.items() if t != s and nm in f]
            if len(ref) >= 3:
                m = statistics.median(ref)
                if p >= 10 * m or p < m / 10: n += 1
        out[s] = n
    return out

if __name__ == "__main__":
    dirs = sys.argv[1:]
    ref = analyse(dirs[0])
    print("== " + dirs[0])
    print("(i') 利回り≧0.295 の国×四半期", ref["clamped"], "（利回りの最大", round(ref["bond_max"], 4), "）")
    print("(ii') 州の集合が同じ 3 倍／1/3 の跳び", ref["popjump"], "（州の移りを除かない総数", ref["popjump_all"], "）種ごと(張り付き,跳び,総数)", ref["per_seed"])
    print("(iii) 自己比較（その種を除く自腕の中央値）", price_div(ref["final"], ref["final"]))
    for d in dirs[1:]:
        r = analyse(d)
        print("== " + d)
        print("(i') ", r["clamped"], "(ii') ", r["popjump"], "総数", r["popjump_all"], "種ごと", r["per_seed"])
        print("(iii) 基準 " + dirs[0] + " に対して", price_div(r["final"], ref["final"]))
