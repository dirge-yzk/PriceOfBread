# (903) 突然変異：使い捨ての木（引数 1）の Simulation.cs の warMult の行を変異ごとに置き換え、機構の試験を回して合否を書く（読むだけの台本・木は使い捨て）
import sys, subprocess, re, os
T = sys.argv[1]; OUT = sys.argv[2]
P = os.path.join(T, "PriceOfBread.SimCore", "Simulation.cs")
orig = open(P, "rb").read()
HEAD = b"double warMult = !(World.Coef.CivilWarArmsDemandUnified > 0) && World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 : (atWar ? World.Coef.MilitaryWarMult * mobilize : 1.0);"
assert orig.count(HEAD) == 1
M = {
 "M1_keep_branch": b"double warMult = World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 : (atWar ? World.Coef.MilitaryWarMult * mobilize : 1.0);",
 "M2_civil_one": b"double warMult = World.InCivilWar ? (World.Coef.CivilWarArmsDemandUnified > 0 ? 1.0 : World.Coef.MilitaryWarMult * 2.0) : (atWar ? World.Coef.MilitaryWarMult * mobilize : 1.0);",
 "M3_no_mobilize": b"double warMult = !(World.Coef.CivilWarArmsDemandUnified > 0) && World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 : (atWar ? World.Coef.MilitaryWarMult * (World.Coef.CivilWarArmsDemandUnified > 0 ? 1.0 : mobilize) : 1.0);",
 "M4_foreign_double": b"double warMult = !(World.Coef.CivilWarArmsDemandUnified > 0) && World.InCivilWar ? World.Coef.MilitaryWarMult * 2.0 : (atWar ? World.Coef.MilitaryWarMult * mobilize * (World.Coef.CivilWarArmsDemandUnified > 0 ? 2.0 : 1.0) : 1.0);",
}
art = os.path.join(os.path.dirname(OUT), "art_mut")
with open(OUT, "w", encoding="utf-8") as o:
    for k, line in M.items():
        open(P, "wb").write(orig.replace(HEAD, line))
        b = subprocess.run(["dotnet", "build", os.path.join(T, "PriceOfBread.Tests", "PriceOfBread.Tests.csproj"), "-c", "Release", "--artifacts-path", art, "--disable-build-servers"], capture_output=True, text=True, encoding="utf-8", errors="replace")
        if b.returncode != 0: o.write(f"== {k}\tBUILDFAIL\n"); continue
        t = subprocess.run(["dotnet", "test", os.path.join(T, "PriceOfBread.Tests", "PriceOfBread.Tests.csproj"), "-c", "Release", "--no-build", "--artifacts-path", art, "--filter", "FullyQualifiedName~CivilWarArmsDemandTests"], capture_output=True, text=True, encoding="utf-8", errors="replace")
        s = t.stdout + t.stderr
        summ = [l.strip() for l in s.splitlines() if "合計" in l and ("成功" in l or "失敗" in l)]
        fails = sorted(set(re.findall(r"失敗 (PriceOfBread\.Tests\.CivilWarArmsDemandTests\.[^\s\[]+(?:\(flag: [0-9.]+\))?)", s)))
        o.write(f"== {k}\n" + "\n".join(summ) + "\n" + "\n".join("  赤 " + f for f in fails) + "\n"); o.flush()
    open(P, "wb").write(orig)
