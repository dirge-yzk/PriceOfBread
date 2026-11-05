# (867) c72gt 振り分けの固定の盤面（紙の台本・事前登録 [4][6]）：合成した region_s と an_h2 の行で holsplit867.py の区分を突然変異ごとに確かめる。実の生は読まない。
#   実のヌル腕には 1/10 が 0 件なので、ヌルを腕に見立てた試験は区別力の証拠にならない（空振りしか言えない）＝区別力はこの合成の盤面で見る。
#   python -X utf8 -I mut_hol867.py <作業フォルダ> <出力の表>
import sys, os, shutil, importlib.util
sys.dont_write_bytecode = True
sd = os.path.dirname(os.path.abspath(__file__))
def load(path, name):
    sp = importlib.util.spec_from_file_location(name, path); m = importlib.util.module_from_spec(sp); sp.loader.exec_module(m); return m
hs = load(os.path.join(sd, "holsplit867.py"), "hs")

NQ = 41; PEAKY = 1788      # 年＝1783＋q÷4 → 1788 の最初の四半期は 20
HDR = "quarter\tyear\tnationIndex\tnation\tregion\n"

def owners(spec, qmiss=None):
    """spec＝{州: (最初, ピークの時点, 最後)} の持ち主を、q<20 は最初・20≦q<24 はピークの時点・q≧24 は最後で返す。"""
    rows = []
    for q in range(NQ):
        for reg, (a, b, c) in spec.items():
            if qmiss and reg in qmiss and q == 20: continue
            o = a if q < 20 else b if q < 24 else c
            rows.append("%d\t%d\t%s\tX\t%s" % (q, 1783 + q // 4, o, reg))
    return HDR + "\n".join(rows) + "\n"

def an(lines):
    return "\n".join(lines) + "\n"

def line(level, reg, ex=False):
    return "  [移民・%s%s] s1 %s（日本・col） 112.9(%d)→11.2(1798)  州間 83.2 入植 0 越境 4.09 他 14.5" % (level, "・例外の名簿" if ex else "", reg, PEAKY)

def build(root):
    shutil.rmtree(root, ignore_errors=True)
    R = {"c1": "州A", "c2": "州B", "c3": "州C", "c4": "州D", "c5": "州E", "c6": "州F", "c7": "州G"}
    arm_spec = {R["c1"]: ("0", "5", "5"), R["c2"]: ("0", "5", "5"), R["c3"]: ("0", "0", "0"), R["c4"]: ("0", "5", "5"), R["c5"]: ("0", "5", "5"), R["c6"]: ("0", "5", "5"), R["c7"]: ("0", "0", "5")}
    nM = {R["c1"]: ("0", "0", "0"), R["c2"]: ("0", "0", "0"), R["c3"]: ("0", "0", "0"), R["c4"]: ("0", "0", "0"), R["c5"]: ("0", "0", "0"), R["c6"]: ("0", "0", "0"), R["c7"]: ("0", "0", "0")}
    nN1 = dict(nM); nN1[R["c1"]] = ("0", "5", "5"); nN1[R["c3"]] = ("0", "5", "5"); nN1[R["c6"]] = ("0", "5", "5"); nN1[R["c7"]] = ("0", "5", "5")
    nN2 = dict(nM)
    dirs = {}
    for nm, spec, miss in (("arm", arm_spec, None), ("M", nM, None), ("N1", nN1, None), ("N2", nN2, {R["c6"]})):
        d = os.path.join(root, nm); os.makedirs(d)
        open(os.path.join(d, "region_s1.tsv"), "w", encoding="utf-8", newline="\n").write(owners(spec, miss)); dirs[nm] = d
    arm_an = os.path.join(root, "an_arm.txt")
    open(arm_an, "w", encoding="utf-8", newline="\n").write(an([line("1/10", R[k]) for k in ("c1", "c2", "c3", "c4", "c6", "c7")] + [line("1/10", R["c5"], True)]))
    open(os.path.join(root, "an_M.txt"), "w", encoding="utf-8", newline="\n").write(an([line("1/4", R["c4"])]))
    for nm in ("N1", "N2"): open(os.path.join(root, "an_%s.txt" % nm), "w", encoding="utf-8", newline="\n").write("## 0 件\n")
    return R, dirs, arm_an

def run(root, mode):
    R, dirs, arm_an = build(root)
    nulls = [(os.path.join(root, "an_%s.txt" % n), dirs[n]) for n in ("M", "N1", "N2")]
    res, C = hs.classify(arm_an, dirs["arm"], nulls, mode)
    lab = {}
    for l, _, _, c in res: lab[c["region"]] = l
    return {k: lab[v] for k, v in R.items()}, C

EXPECT = {"peak": {"c1": "L2", "c2": "L3", "c3": "L3", "c4": "L1", "c5": "L0", "c6": "L3", "c7": "L3"}}
EXPECT["late"] = dict(EXPECT["peak"], c7="L2", c6="L2")      # ピークでなく最後で読むと、ピークの後に替わった州が L2 になる（要を外した写し）
EXPECT["nocmp"] = dict(EXPECT["peak"], c2="L2", c6="L2")     # ヌルの割れの条件を外すと、ヌルが一致した州と行が無い州が L2 になる

def main():
    work, out = sys.argv[1], sys.argv[2]
    L = ["(867) 振り分けの固定の盤面（合成）の表・holsplit867.py", "変異\t予言\t実際\t予言との差"]
    allok = True
    for mode in ("peak", "late", "nocmp"):
        got, C = run(os.path.join(work, mode), mode)
        want = EXPECT[mode]; diff = [k for k in want if got[k] != want[k]]
        L.append("%s\t%s\t%s\t%s" % (mode, " ".join("%s=%s" % (k, want[k]) for k in sorted(want)), " ".join("%s=%s" % (k, got[k]) for k in sorted(got)), "予言どおり" if not diff else "予言の外れ：" + ",".join(diff)))
        allok &= not diff
        if mode == "peak": L.append("  ヌルに行が無い（割れと数えない）件数 %d・読めない %d" % (C["ヌルに行が無い（割れと数えない）"], C["読めない"]))
    L.append("要を外した写し（late・nocmp）が peak と違う区分を出す＝規則の要を外すと表が崩れる：" + ("確認" if allok else "確認できない"))
    L.append("判定：" + ("予言どおり" if allok else "予言と違う所あり"))
    open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n"); print("\n".join(L))
    return 0 if allok else 1

if __name__ == "__main__":
    sys.exit(main())
