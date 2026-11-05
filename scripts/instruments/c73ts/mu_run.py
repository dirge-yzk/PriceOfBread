# (877) 使い捨ての木 tree_f0 で単体の試験の突然変異（1 つずつ当てて戻す）
import subprocess, sys, os
T = "D:/PoB_work/c73ts_work/tree_f0"; A = "D:/PoB_work/c73ts_work/art_mu"; OUT = "D:/PoB_work/c73ts_work/mu_unit877.txt"
ES = T + "/PriceOfBread.SimCore/EconomyStep.cs"; CO = T + "/PriceOfBread.SimCore/Coefficients.cs"
S112 = b"            else p.SubsistenceShare = i == lp && p.Size > 0 ? Math.Min(1.0, p.SubsistenceBasis * u * t / (need * p.Size)) : 0.0;\n"
M = {
 "Mu1_sigmaだけTを残す": [(ES, S112, b"            else p.SubsistenceShare = i == lp && p.Size > 0 ? Math.Min(1.0, p.SubsistenceBasis * u * (coef.SubsistenceNoBoughtTech > 0 ? StapleEffTech(region, coef) : t) / (need * p.Size)) : 0.0;\n")],
 "Mu5_lneedにTを残す": [(ES, "        double u = UnsoldFarmCapacity(sold, cap), t = coef.SubsistenceNoBoughtTech > 0 ? 1.0 : StapleEffTech(region, coef);   // ★(877) 同じ旗で".encode(),
                         "        double u = UnsoldFarmCapacity(sold, cap), t = StapleEffTech(region, coef);   // ★(877) 同じ旗で".encode())],
 "Mu2_旗1だけ自給の産出に収穫係数をもう1回": [(ES, b"region.LastSubsistenceOutput += sub;", b"region.LastSubsistenceOutput += coef.SubsistenceNoBoughtTech > 0 ? sub * region.HarvestFactor : sub;")],
 "既定1_旧の規則の番人の予言": [(CO, b"    public double SubsistenceNoBoughtTech = 0;", b"    public double SubsistenceNoBoughtTech = 1;")],
}
FILT = {"既定1_旧の規則の番人の予言": "FullyQualifiedName~SubsistenceStage2Tests|FullyQualifiedName~SubstInstrumentTests|FullyQualifiedName~SubsistenceNoBoughtTechTests"}
log = open(OUT, "w", encoding="utf-8", newline="\n")
env = dict(os.environ); env["PATH"] = "C:/Program Files/dotnet;" + env["PATH"]; env["AVALONIA_TELEMETRY_OPTOUT"] = "1"
for name, edits in M.items():
    orig = {}
    for f, a, b in edits:
        s = open(f, "rb").read(); orig[f] = s
        if s.count(a) != 1: log.write("%s：置き換えの元が %d 個＝飛ばす\n" % (name, s.count(a))); break
    else:
        for f, a, b in edits:
            s = open(f, "rb").read(); open(f, "wb").write(s.replace(a, b))
        r = subprocess.run(["dotnet", "build", T + "/PriceOfBread.Tests/PriceOfBread.Tests.csproj", "-c", "Release", "--artifacts-path", A, "--disable-build-servers"], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        if r.returncode != 0:
            log.write("== %s：ビルド失敗\n%s\n" % (name, r.stdout[-3000:]))
        else:
            r = subprocess.run(["dotnet", "test", T + "/PriceOfBread.Tests/PriceOfBread.Tests.csproj", "-c", "Release", "--no-build", "--artifacts-path", A,
                                "--filter", FILT.get(name, "FullyQualifiedName~SubsistenceNoBoughtTechTests")], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
            lines = [l for l in r.stdout.splitlines() if ("失敗" in l and "[" in l) or "合計" in l or "成功" in l or "Failed " in l]
            log.write("== %s：rc=%d\n%s\n" % (name, r.returncode, "\n".join(lines)))
    for f, s in orig.items(): open(f, "wb").write(s)
    log.flush()
log.close()
r = subprocess.run(["git", "-C", T, "status", "--porcelain"], capture_output=True, text=True); print("tree_f0 の状態：" + (r.stdout.strip() or "きれい"))
