# (1173) c116k1 試験の突然変異：使い捨ての木の Politics.cs に 1 つずつ入れ、SecessionInclusionOnceTests だけを回して戻す。
# 引数：使い捨ての木 出力のフォルダ 突然変異の名（M1〜M5 か all）
import sys, subprocess, os
TREE, OUT, WHICH = sys.argv[1], sys.argv[2], sys.argv[3]
P = os.path.join(TREE, "PriceOfBread.SimCore", "Politics.cs")
A893 = "            if (!(c.SecessionInclusionOnce > 0)) pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));"
FN = "        if (!(c.SecessionInclusionOnce > 0))\n            return w.MigrantNativeSuffrage"
L165 = "            if (w.EthnicEnabled)\n                minorityMil *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, milReg));"
MUT = {
    "M1": [(A893, "            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));"),
           (FN, "        if (true)\n            return w.MigrantNativeSuffrage")],
    "M2": [(A893, "            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));")],
    "M3": [(L165, "            if (w.EthnicEnabled && !(c.SecessionInclusionOnce > 0))\n                minorityMil *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, milReg));")],
    "M4": [(A893, "            pressure *= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));"),
           (FN, "        if (c.SecessionInclusionOnce > 0 && w.Suffrage >= Suffrage.AdultMale) return 1.0;\n        if (!(c.SecessionInclusionOnce > 0))\n            return w.MigrantNativeSuffrage")],
    "M5": [(A893, A893 + "\n            else pressure /= 1.0 - PoliticalQuantities.Clamp01(RegionAvgSuffrageInclusion(w, c, r));")],
}
orig = open(P, "rb").read()
names = list(MUT) if WHICH == "all" else [WHICH]
env = dict(os.environ, TEMP="D:/PoB_work/c116k1_work/tmp", TMP="D:/PoB_work/c116k1_work/tmp")
art = "D:/PoB_work/c116k1_mutart"
os.makedirs(OUT, exist_ok=True)
try:
    for m in names:
        b = orig
        for old, new in MUT[m]:
            o = old.replace("\n", "\r\n").encode("utf-8"); n = new.replace("\n", "\r\n").encode("utf-8")
            assert b.count(o) == 1, (m, old[:50], b.count(o))
            b = b.replace(o, n)
        open(P, "wb").write(b)
        r1 = subprocess.run(["dotnet", "build", os.path.join(TREE, "PriceOfBread.Tests", "PriceOfBread.Tests.csproj"), "-c", "Release", "--artifacts-path", art, "-nologo", "-v", "q"],
                            capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        r2 = subprocess.run(["dotnet", "test", os.path.join(TREE, "PriceOfBread.Tests", "PriceOfBread.Tests.csproj"), "-c", "Release", "--artifacts-path", art, "--no-build",
                             "--filter", "FullyQualifiedName~SecessionInclusionOnceTests"], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        lines = [l for l in (r2.stdout + r2.stderr).splitlines() if ("失敗" in l or "合格" in l or "[FAIL]" in l or "Failed " in l)]
        with open(os.path.join(OUT, f"tmut_{m}.txt"), "w", encoding="utf-8", newline="\n") as o:
            o.write(f"# (1173) 試験の突然変異 {m}・ビルド rc={r1.returncode}・試験 rc={r2.returncode}\n")
            for l in lines: o.write(l.strip() + "\n")
finally:
    open(P, "wb").write(orig)
