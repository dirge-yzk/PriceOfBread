# (870) c72ss 種 11〜20 の門（記録だけ・〈近似〉）＝c72gt の gates_ss867.py（sha256 a0ea2796…）を 1 字も変えずに import して回す包み。
#   種 11〜20 にはヌル腕が無い＝腕と基線（keep_852/a11）の種 11〜20 を種 1〜10 の名へハードリンクした見かけのフォルダを作り、N1＝N2＝基線で run_gates を回す。
#   ＝ヌル腕に依らない門（G1b・G2・G3a・G4・G4m）はそのまま読める。w に依る門は w＝0 になる＝G1a は〈d＜0 の割合〉（w＝0 の近似）として記録し、
#     G5b と G5p は印字された |dB の中央|・窓ごとの |e の中央| を、種 1〜10 の読み直し (a)（c72gt の g5_ready.mark.log・c70ssB_10）の w の中央・wp の中央へ当てる（〈近似〉）。
#   呼び方：python -X utf8 gates_s11_870.py <腕の ins（種 11〜20）> <基線の ins（種 11〜20）> <見かけのフォルダ（作る・D:/ の形）> <出力>
import sys, os, re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gates_ss867 as G

# 種 1〜10 の読み直し (a) の閾値（c72gt の g5_ready.mark.log の〈読み直し (a) c70ssB_10〉の段の印字を写した＝旗 0 とヌル腕の生だけから出る量）
W5_MED = 0.0103
WP_MED = {"1783-85": 0.00357, "1786-99": 0.00989, "1800-49": 0.02907, "1850-99": 0.06666, "1900-50": 0.09481}
TABLES = ("subst_s", "region_s")

def view(src, dst):
    os.makedirs(dst, exist_ok=True)
    for t in TABLES:
        for s in range(11, 21):
            a = os.path.join(src, "%s%d.tsv" % (t, s)); b = os.path.join(dst, "%s%d.tsv" % (t, s - 10))
            if os.path.exists(b): os.remove(b)
            os.link(a, b)

def main():
    arm, base, vroot, out = sys.argv[1:5]
    va, vb = os.path.join(vroot, "arm"), os.path.join(vroot, "base")
    view(arm, va); view(base, vb)
    thr = G.read_thr(os.path.join(os.path.dirname(os.path.abspath(__file__)), "gates_ss867_thresholds.tsv"))
    ok, C, OUT = G.run_gates(va, vb, vb, vb, 10, 1.0, thr)
    L = ["(870) 種 11〜20 の門（記録・〈近似〉）：腕 %s・基線 %s・N1＝N2＝基線（ヌル腕が無い）・種の名は 11〜20→1〜10 の見かけ" % (arm, base)]
    L += ["  " + x for x in OUT]
    L.append("-- 種 1〜10 の閾値を当てた読み（〈近似〉）")
    g1 = C["G1 P1 行"]; L.append("G1a（w＝0 の近似）：P1 %d・d＜0 の割合 %.4f（種 1〜10 の門は w の外の割合≧0.5）" % (g1, C["G1a 外"] / g1 if g1 else float("nan")))
    rec = {}
    for x in OUT:
        m = re.search(r"G5b .*\|dB の中央\| ([0-9.naninf]+)", x)
        if m: rec["G5b"] = float(m.group(1))
        m = re.search(r"G5p 窓 (\S+)：行 (\d+)・\|e の中央\| ([0-9.naninf]+)", x)
        if m: rec[m.group(1)] = (int(m.group(2)), float(m.group(3)))
    g5b = rec.get("G5b", float("nan")); okb = g5b <= W5_MED
    L.append("G5b（近似）：|dB の中央| %.5f ≦ 種 1〜10 の w の中央 %.4f → %s" % (g5b, W5_MED, okb))
    nev = 0; allp = True
    for wn, wp in WP_MED.items():
        n, e = rec.get(wn, (0, float("nan"))); ev = n >= 500; p = ev and e <= wp
        L.append("  G5p 窓 %s（近似）：行 %d・|e の中央| %.5f ≦ 種 1〜10 の wp の中央 %.5f → %s" % (wn, n, e, wp, ("通る" if p else "落ちる") if ev else "評価しない（行が 500 未満）"))
        if ev: nev += 1; allp = allp and p
    L.append("G5p（近似）：評価した窓 %d・全て通る %s → %s" % (nev, allp, nev >= 2 and allp))
    keep = {k: v for k, v in ok.items() if k in ("G1b", "G2", "G3a", "G4", "G4m")}
    L.append("門の読み（記録・〈近似〉）：%s・G5b=%s・G5p=%s" % ("・".join("%s=%s" % (k, "通" if v else "落") for k, v in keep.items()), "通" if okb else "落", "通" if (nev >= 2 and allp) else "落"))
    with open(out, "w", encoding="utf-8", newline="\n") as f: f.write("\n".join(L) + "\n")
    print("\n".join(L)); return 0

if __name__ == "__main__":
    sys.exit(main())
