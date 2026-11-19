# (1237) 突然変異：使い捨ての木（git archive）に 1 つずつ入れ、試験 FoodProcessingNoEngineerTests を回して赤か緑かを書く。
import subprocess, sys, os, shutil
W = "D:/PoB_work/c125al_work/mut"; T = W + "/tree"; ART = W + "/art"
M = {
 "mu1": ("PriceOfBread.SimCore/Simulation.cs", "coef.FoodProcessingNoEngineer > 0 && Catalog.IsFoodProcessingGood(d.Id)", "Catalog.IsFoodProcessingGood(d.Id)"),
 "mu2": ("PriceOfBread.SimCore/Catalog.cs", "public static bool IsFoodProcessingGood(int id) => id is FlourId", "public static bool IsFoodProcessingGood(int id) => id >= 0 || id is FlourId"),
 "mu3": ("PriceOfBread.SimCore/Catalog.cs", "=> id is FlourId or BreadId", "=> id is BreadId"),
 "mu4": ("PriceOfBread.SimCore/Simulation.cs", "? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput)]", "? [.. d.Inputs, new Input(Catalog.LaborId, LaborPerOutput), new Input(Catalog.AdvLaborId, 0.0)]"),
}
env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT="1")
out = open(W + "/mut_result.txt", "w", encoding="utf-8")
for k, (f, a, b) in M.items():
    p = os.path.join(T, f); orig = open(p, "rb").read()
    assert orig.count(a.encode()) == 1, k
    open(p, "wb").write(orig.replace(a.encode(), b.encode()))
    r = subprocess.run(["dotnet", "test", T + "/PriceOfBread.Tests/PriceOfBread.Tests.csproj", "-c", "Release", "--artifacts-path", ART,
                        "--filter", "FullyQualifiedName~FoodProcessingNoEngineerTests", "--logger", f"trx;LogFileName={W}/{k}.trx", "-v", "q", "-nologo"], capture_output=True, env=env)
    txt = r.stdout.decode("utf-8", "replace")
    import re
    trx = open(f"{W}/{k}.trx", encoding="utf-8").read()
    fails = sorted(set(re.findall(r'testName="[^"]*FoodProcessingNoEngineerTests\.(T\d)[^"]*"[^>]*outcome="Failed"', trx)))
    summ = [ln.strip() for ln in txt.splitlines() if "合計:" in ln]
    out.write(f"{k}\t{f}\trc={r.returncode}\t赤={','.join(fails) or '-'}\t{summ[-1] if summ else 'ビルド失敗?'}\n"); out.flush()
    open(p, "wb").write(orig)
out.close()
open(W + "/mut.done", "w").write("rc=0 \n")
