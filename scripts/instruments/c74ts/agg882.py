# (882) c74ts 集計（subst_s の 1 本→窓ごとの和）：門 GW〈働いた・世界の κ〉と G5h〈σ の外の倍率・収穫係数の散らばり〉の材料。
#   エンジンの関数を呼ばない。計器の列から Σ2′ の σ′ を台本が数える（gates_ts877.py の sigma2 と同じ式を写した＝c74ts/gates_ts877.py から import）。
#   呼び方：python -X utf8 agg882.py <出力の json> <盤面の名=ディレクトリ> ... [--seeds 1-10] [--jobs 8]
#   出力：{盤面: {種: {窓: {...和...}}}}。κ の和は RhoSource＝1・BasisKind＝2・subPopK＞0・d0＞0 の行（GC と同じ母集合）。
#     k_sp＝Σsp・k_s0＝Σsp×σ_e（エンジンの σ＝s2SigmaEngine）・k_sI＝Σsp×s2SigmaInst（S1-a の計器の σ）・k_s2＝Σsp×σ′（Σ2′ の紙・決まらない行は下限）・
#     k_m1／k_m3／k_m6＝紙の突然変異 Mu1・Mu3・Mu6 の σ（gates_ts877.apply_mut と同じ式）。
#     T の升（T÷T0）：t0＝≦1+1e-9・t1＝≦1.02・t2＝≦1.1・t3＝＞1.1 の 4 つに同じ和を分ける（キー k_<升>_<和>）。
#     収穫係数 H＝engineSubOutputPerMonth÷nominalSubPerMonth（nominal＞0・H＞0 の同じ母集合の行）：h_sp＝Σsp・h_abs＝Σsp×|ln H|・h_ln＝Σsp×ln H・h_n＝行の数。
import sys, os, json, math, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gates_ts877 as G

def tbin(r):
    if not (r == r): return None
    return "t0" if r <= 1 + 1e-9 else "t1" if r <= 1.02 else "t2" if r <= 1.1 else "t3"

def one(args):
    name, d, seed = args
    p = os.path.join(d, "subst_s%d.tsv" % seed)
    if not os.path.exists(p): return name, seed, None
    W = collections.defaultdict(lambda: collections.defaultdict(float))
    u0 = {}
    with open(p, encoding="utf-8") as f:
        ix = G.read_header(f)
        need_cols = ("quarter", "year", "nation", "region", "subPopK", "engineSubOutputPerMonth", "nominalSubPerMonth", "s2BasisKind", "s2RhoSource",
                     "s2D0Inst", "s2SigmaEngine", "s2SigmaInst", "s2TechHead", "s2Tech0Inst", "s2UHead")
        g = {k: ix[k] for k in need_cols}
        for line in f:
            c = line.rstrip("\r\n").split("\t")
            if c[g["s2RhoSource"]] != "1" or c[g["s2BasisKind"]] != "2": continue
            sp = G.fnum(c[g["subPopK"]]); d0 = G.fnum(c[g["s2D0Inst"]])
            if not (sp > 0 and d0 > 0): continue
            reg = c[g["nation"]] + "/" + c[g["region"]]
            se = G.fnum(c[g["s2SigmaEngine"]]); si = G.fnum(c[g["s2SigmaInst"]]); T = G.fnum(c[g["s2TechHead"]]); T0 = G.fnum(c[g["s2Tech0Inst"]])
            uh = G.fnum(c[g["s2UHead"]])
            if reg not in u0: u0[reg] = uh
            wn = G.win(int(c[g["year"]]))
            sg, det, lo = G.sigma2(si, T0, T, None); s2 = sg if det else lo
            m1, d1_, l1 = G.sigma2(si, T0, T, "Mu1"); m1 = m1 if m1 == m1 else l1
            m6, d6_, l6 = G.sigma2(si, T0, T, "Mu6"); m6 = m6 if m6 == m6 else l6
            uu = u0[reg]
            m3 = min(1.0, s2 * uu / uh) if (uh > 0 and uu == uu and s2 < 1.0) else s2
            if not (se == se and si == si and s2 == s2): continue
            tb = tbin(T / T0 if T0 > 0 else float("nan"))
            for key in ("all", tb):
                if key is None: continue
                a = W[wn]; pre = "k_" if key == "all" else "k_%s_" % key
                a[pre + "sp"] += sp; a[pre + "s0"] += sp * se; a[pre + "sI"] += sp * si; a[pre + "s2"] += sp * s2
                a[pre + "m1"] += sp * m1; a[pre + "m3"] += sp * m3; a[pre + "m6"] += sp * m6; a[pre + "n"] += 1
            eng = G.fnum(c[g["engineSubOutputPerMonth"]]); nom = G.fnum(c[g["nominalSubPerMonth"]])
            if nom > 0 and eng > 0:
                lh = math.log(eng / nom); a = W[wn]
                a["h_sp"] += sp; a["h_abs"] += sp * abs(lh); a["h_ln"] += sp * lh; a["h_n"] += 1
    return name, seed, {wn: dict(v) for wn, v in W.items()}

def main():
    a = sys.argv[1:]; out = a[0]; boards = []; seeds = (1, 10); jobs = 8; i = 1
    while i < len(a):
        if a[i] == "--seeds": s0, s1 = a[i + 1].split("-"); seeds = (int(s0), int(s1)); i += 2
        elif a[i] == "--jobs": jobs = int(a[i + 1]); i += 2
        else: n, d = a[i].split("=", 1); boards.append((n, d)); i += 1
    tasks = [(n, d, s) for n, d in boards for s in range(seeds[0], seeds[1] + 1)]
    import multiprocessing as mp
    R = collections.defaultdict(dict)
    with mp.Pool(jobs) as pool:
        for name, seed, res in pool.imap_unordered(one, tasks):
            R[name][str(seed)] = res
            print("集計 %s 種 %d：%s" % (name, seed, "無い" if res is None else "%d 窓" % len(res)), flush=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f: json.dump(R, f, ensure_ascii=False, indent=0, sort_keys=True)
    return 0

if __name__ == "__main__":
    sys.exit(main())
