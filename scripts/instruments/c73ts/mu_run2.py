# (877) 使い捨ての木 tree_f0：既定 1＋★(832) ① (a) の直し（初期化子に新しい旗 0）で旧の規則の番人 4 本が緑に戻るかを確かめる（直しは次の回へ送る・この線は点火しない）
import subprocess, os
T = "D:/PoB_work/c73ts_work/tree_f0"; A = "D:/PoB_work/c73ts_work/art_mu"; OUT = "D:/PoB_work/c73ts_work/mu_unit877_afix.txt"
CO = T + "/PriceOfBread.SimCore/Coefficients.cs"; S2 = T + "/PriceOfBread.Tests/SubsistenceStage2Tests.cs"; SI = T + "/PriceOfBread.Tests/SubstInstrumentTests.cs"
L = b"foreach (var n in sim.Nations) n.World.Coef.SubsistenceStage2 = flag;"
N = b"foreach (var n in sim.Nations) { n.World.Coef.SubsistenceStage2 = flag; n.World.Coef.SubsistenceNoBoughtTech = 0; }"
E = [(CO, b"    public double SubsistenceNoBoughtTech = 0;", b"    public double SubsistenceNoBoughtTech = 1;"), (S2, L, N), (SI, L, N)]
orig = {f: open(f, "rb").read() for f, _, _ in E}
log = open(OUT, "w", encoding="utf-8", newline="\n")
ok = all(orig[f].count(a) == 1 for f, a, _ in E)
log.write("置き換えの元が全て 1 個：%s\n" % ok)
env = dict(os.environ); env["PATH"] = "C:/Program Files/dotnet;" + env["PATH"]; env["AVALONIA_TELEMETRY_OPTOUT"] = "1"
if ok:
    for f, a, b in E:
        s0 = open(f, "rb").read()
        open(f, "wb").write(s0.replace(a, b))
    r = subprocess.run(["dotnet", "build", T + "/PriceOfBread.Tests/PriceOfBread.Tests.csproj", "-c", "Release", "--artifacts-path", A, "--disable-build-servers"], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
    if r.returncode != 0: log.write("ビルド失敗\n" + r.stdout[-3000:])
    else:
        r = subprocess.run(["dotnet", "test", T + "/PriceOfBread.Tests/PriceOfBread.Tests.csproj", "-c", "Release", "--no-build", "--artifacts-path", A, "--filter",
                            "FullyQualifiedName~SubsistenceStage2Tests|FullyQualifiedName~SubstInstrumentTests|FullyQualifiedName~SubsistenceNoBoughtTechTests"], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        log.write("rc=%d\n%s\n" % (r.returncode, "\n".join(l for l in r.stdout.splitlines() if ("失敗" in l and "[" in l) or "合計" in l or "成功" in l)))
for f, s in orig.items(): open(f, "wb").write(s)
log.close()
r = subprocess.run(["git", "-C", T, "status", "--porcelain"], capture_output=True, text=True); print("tree_f0 の状態：" + (r.stdout.strip() or "きれい"))
