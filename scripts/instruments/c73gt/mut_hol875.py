# (875) c73gt 振り分けの固定の盤面（合成・紙の台本・事前登録 [3] (d)・[6] M3）：mut_hol867.py の部品を呼び、867 では L2・875 では L3 になる州 c8 を足す。実の生は読まない。
#   c8＝腕は最初 0 からピークで 5 に替わり、ヌル 3 本の持ち主も割れる（0・7・0）が、腕の持ち主 5 はヌルのどれとも違う。
#   実のヌル腕には 1/10 が 0 件なので、ヌルを腕に見立てた試験は空振りしか言えない（867 と同じ名乗り）＝区別力はこの合成の盤面で見る。
#   python -X utf8 -I mut_hol875.py <作業フォルダ> <出力の表>
import sys, os, shutil, importlib.util
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
def load(path, name):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
m7 = load(os.path.join(sd, "..", "c72gt", "mut_hol867.py"), "m7")
h5 = load(os.path.join(sd, "holsplit875.py"), "h5")
h7 = load(os.path.join(sd, "..", "c72gt", "holsplit867.py"), "h7")

def build(root):
    shutil.rmtree(root, ignore_errors=True)
    names = {"c1": "州A", "c2": "州B", "c3": "州C", "c4": "州D", "c5": "州E", "c6": "州F", "c7": "州G", "c8": "州H"}
    z = ("0", "0", "0")
    arm_spec = {names["c1"]: ("0", "5", "5"), names["c2"]: ("0", "5", "5"), names["c3"]: ("0", "0", "0"), names["c4"]: ("0", "5", "5"), names["c5"]: ("0", "5", "5"),
                names["c6"]: ("0", "5", "5"), names["c7"]: ("0", "0", "5"), names["c8"]: ("0", "5", "5")}
    nM = {n: z for n in names.values()}
    nN1 = dict(nM); nN1[names["c1"]] = ("0", "5", "5"); nN1[names["c3"]] = ("0", "5", "5"); nN1[names["c6"]] = ("0", "5", "5"); nN1[names["c7"]] = ("0", "5", "5"); nN1[names["c8"]] = ("0", "7", "7")
    nN2 = dict(nM)
    dirs = {}
    for nm, spec, miss in (("arm", arm_spec, None), ("M", nM, None), ("N1", nN1, None), ("N2", nN2, {names["c6"]})):
        d = os.path.join(root, nm); os.makedirs(d)
        open(os.path.join(d, "region_s1.tsv"), "w", encoding="utf-8", newline="\n").write(m7.owners(spec, miss)); dirs[nm] = d
    arm_an = os.path.join(root, "an_arm.txt")
    open(arm_an, "w", encoding="utf-8", newline="\n").write(m7.an([m7.line("1/10", names[k]) for k in ("c1", "c2", "c3", "c4", "c6", "c7", "c8")] + [m7.line("1/10", names["c5"], True)]))
    open(os.path.join(root, "an_M.txt"), "w", encoding="utf-8", newline="\n").write(m7.an([m7.line("1/4", names["c4"])]))
    for nm in ("N1", "N2"): open(os.path.join(root, "an_%s.txt" % nm), "w", encoding="utf-8", newline="\n").write("## 0 件\n")
    return names, dirs, arm_an

def run(root, mod, **kw):
    names, dirs, arm_an = build(root)
    nulls = [(os.path.join(root, "an_%s.txt" % n), dirs[n]) for n in ("M", "N1", "N2")]
    res, C = mod.classify(arm_an, dirs["arm"], nulls, "peak", **kw)
    lab = {}
    for l, _, _, c in res: lab[c["region"]] = l
    return {k: lab[v] for k, v in names.items()}, C

BASE = {"c1": "L2", "c2": "L3", "c3": "L3", "c4": "L1", "c5": "L0", "c6": "L3", "c7": "L3"}
EXPECT = {"match（875 の既定）": dict(BASE, c8="L3"), "nomatch（--no-match＝条件を外す）": dict(BASE, c8="L2"), "holsplit867 の写し元": dict(BASE, c8="L2")}

def main():
    work, out = sys.argv[1], sys.argv[2]
    L = ["(875) 振り分けの固定の盤面（合成）の表・holsplit875.py（867 では L2・875 では L3 になる州 c8 を足した）", "変異\t予言\t実際\t予言との差"]; allok = True
    got = {}
    got["match（875 の既定）"] = run(os.path.join(work, "m"), h5)[0]
    got["nomatch（--no-match＝条件を外す）"] = run(os.path.join(work, "n"), h5, match=False)[0]
    # 867 は classify に match 引数が無い＝同じ盤面を 867 の関数で分類する
    names, dirs, arm_an = build(os.path.join(work, "o"))
    nulls = [(os.path.join(work, "o", "an_%s.txt" % n), dirs[n]) for n in ("M", "N1", "N2")]
    res, C = h7.classify(arm_an, dirs["arm"], nulls, "peak")
    lab = {c["region"]: l for l, _, _, c in res}
    got["holsplit867 の写し元"] = {k: lab[v] for k, v in names.items()}
    for k, want in EXPECT.items():
        g = got[k]; diff = [x for x in want if g[x] != want[x]]
        L.append("%s\t%s\t%s\t%s" % (k, " ".join("%s=%s" % (x, want[x]) for x in sorted(want)), " ".join("%s=%s" % (x, g[x]) for x in sorted(g)), "予言どおり" if not diff else "予言の外れ：" + ",".join(diff)))
        allok &= not diff
    same = got["nomatch（--no-match＝条件を外す）"] == got["holsplit867 の写し元"]
    L.append("M3 条件を外すと 867 と同じ区分：" + ("確認" if same else "確認できない")); allok &= same
    L.append("新しい条件が働く＝c8 だけが 867 と 875 で違う：" + ("確認" if [k for k in got["match（875 の既定）"] if got["match（875 の既定）"][k] != got["holsplit867 の写し元"][k]] == ["c8"] else "確認できない"))
    allok &= [k for k in got["match（875 の既定）"] if got["match（875 の既定）"][k] != got["holsplit867 の写し元"][k]] == ["c8"]
    L.append("判定：" + ("予言どおり" if allok else "予言と違う所あり"))
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0 if allok else 1

if __name__ == "__main__":
    sys.exit(main())
