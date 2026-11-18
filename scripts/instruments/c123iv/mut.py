# 使い方: python -X utf8 -I mut.py <使い捨ての木> <出力フォルダ> <artifacts>
# (1223) c123iv の突然変異 μ0〜μ4：1 つずつ当てて InvestBudgetFromEarningsTests を回し、成功・失敗の行を mut_<名>.txt に書く（元のバイトへ戻してから次へ）。
import sys, subprocess, os
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
ES = "PriceOfBread.SimCore/EconomyStep.cs"; TS = "PriceOfBread.Tests/InvestBudgetFromEarningsTests.cs"
M = {
 "mu0": (TS, "var (p1, paid1, _, _) = Month(1.0, 1e6, 500.0);", "var (p1, paid1, _, _) = Month(0.0, 1e6, 500.0);"),
 "mu1": (ES, "double ivE = ivp.LastEarnedPerMonth.Value > 0 ? ivp.LastEarnedPerMonth.Value : 0.0;", "double ivE = ivp.EarnedPerMonth.Value > 0 ? ivp.EarnedPerMonth.Value : 0.0;"),
 "mu2": (ES, "double ivE = ivp.LastEarnedPerMonth.Value > 0 ? ivp.LastEarnedPerMonth.Value : 0.0;", "double ivE = ivp.LastEarnedPerMonth.Value;"),
 "mu3": (ES, "if (coef.InvestBudgetFromEarnings > 0)\n            {", "if (true)\n            {"),
 "mu4": (ES, "ivp.RemainingBudget -= investBudgetFromEarnings;", "ivp.RemainingBudget *= 1.0 - coef.IndInvestmentFrac.Value;"),
}
env = dict(os.environ)
for k, (f, a, b) in M.items():
    p = os.path.join(tree, f); orig = open(p, "rb").read()
    assert orig.count(a.encode()) == 1, (k, orig.count(a.encode()))
    open(p, "wb").write(orig.replace(a.encode(), b.encode()))
    if k == "mu3":   # 予算の行も旗を見ない形へ
        p2 = os.path.join(tree, ES); o2 = open(p2, "rb").read()
        a2 = b"coef.InvestBudgetFromEarnings > 0 ? investBudgetFromEarnings"
        assert o2.count(a2) == 1
        open(p2, "wb").write(o2.replace(a2, b"true ? investBudgetFromEarnings"))
    try:
        r = subprocess.run(["dotnet", "test", os.path.join(tree, "PriceOfBread.Tests/PriceOfBread.Tests.csproj"), "-c", "Release", "--artifacts-path", art,
                            "--filter", "FullyQualifiedName~InvestBudgetFromEarningsTests", "--logger", "console;verbosity=normal"], capture_output=True, text=True, encoding="utf-8", errors="replace", env=env)
        lines = [l for l in r.stdout.splitlines() if ("成功 " in l or "失敗 " in l or "error CS" in l or "合計数" in l or "     失敗:" in l or "     成功:" in l)]
        with open(os.path.join(out, f"mut_{k}.txt"), "w", encoding="utf-8", newline="\n") as o:
            o.write(f"(1223) c123iv 突然変異 {k}：{f}\n  旧：{a}\n  新：{b}\n終了コード {r.returncode}\n" + "\n".join(lines) + "\n")
    finally:
        open(p, "wb").write(orig)
        if k == "mu3": open(os.path.join(tree, ES), "wb").write(orig if f == ES else open(os.path.join(tree, ES), "rb").read())
print("done")
