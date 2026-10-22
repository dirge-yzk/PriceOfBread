# (495) 線 eq の突然変異の組（18 本）。使い方：専用の worktree〔検証する木をチェックアウトした場所〕を MUT_ROOT に渡して `python scripts/instruments/eq495/mut.py [M01 M02 ...]`。
#   各回：1 か所を書き換え → dotnet build PriceOfBread.Tests -c Release → FactoryEquipmentTests を回す → 元に戻す。全部「失敗」が出れば合格（生き残りは番人の穴）。
#   出力は cp932 で化ける環境がある（`iconv -f cp932 -t utf-8`）。事前登録 追記 3・4 と実装ログ (495) §5 の〈突然変異 18 本全て赤〉の再現用。
import subprocess, sys, os, re, time
root = os.environ.get("MUT_ROOT", "D:/PoB_work/wt_eq_mut")
muts = [
 ("M01 φ=g（割合にしない）", "PriceOfBread.SimCore/FactoryEquipment.cs", "double phi = points / (ind + points);", "double phi = points;", 1),
 ("M02 倒産も受け取る", "PriceOfBread.SimCore/FactoryEquipment.cs", "=> !s.Bankrupt && Discovery.Gate(r, s) > 0;", "=> Discovery.Gate(r, s) > 0;", 1),
 ("M04 廃棄が Scale から引かれない", "PriceOfBread.SimCore/FactoryEquipment.cs", "if (drop > 0) s.Scale -= drop;", "", 1),
 ("M05 自由な伸びが戻る", "PriceOfBread.SimCore/EconomyStep.cs", "if (!eqS) s.Scale += catSpeed * (scaleTarget - s.Scale);", "s.Scale += catSpeed * (scaleTarget - s.Scale);", 1),
 ("M06 indMult が旗で消えない", "PriceOfBread.SimCore/EconomyStep.cs", "double indMult = cap && !indOff", "double indMult = cap", 1),
 ("M07 倒産の重み除外なし", "PriceOfBread.SimCore/EconomyStep.cs", "if (eqOn && s.Bankrupt && FactoryEquipment.Eligible(s)) { prodW[pi] = 0; continue; }", "", 1),
 ("M08 完成で Scale を足さない", "PriceOfBread.SimCore/World.cs", "if (Coef.RecipeEquipment > 0 && FactoryEquipment.Eligible(p)) p.Scale += Coef.FactoryBuildScale;", "", 1),
 ("M09 全員赤字の等分をやめる", "PriceOfBread.SimCore/FactoryEquipment.cs", ": 1.0 / nCand;", ": 0.0;", 1),
 ("M10 基準が一棟の下限を持たない", "PriceOfBread.SimCore/FactoryEquipment.cs", "double basis = sumScale > c.FactoryBuildScale ? sumScale : c.FactoryBuildScale;", "double basis = sumScale;", 1),
 ("M11 賃金の天井が旗で消えない", "PriceOfBread.SimCore/MarketFlow.cs", "(coef.IndustrializationOff > 0 ? 0.0 : region.Industrialization)", "region.Industrialization", 1),
 ("M13 独占の解体が旗で消えない", "PriceOfBread.SimCore/EconomyStep.cs", "(c.IndustrializationOff > 0 ? 1.0 : Math.Max(", "(false ? 1.0 : Math.Max(", 1),
 ("M14 ヴィンテージが旗で止まらない", "PriceOfBread.SimCore/EconomyStep.cs", "&& !warmFrozen && !indOff)", "&& !warmFrozen)", 1),
 ("M15 機械化度が旗で消えない", "PriceOfBread.SimCore/EconomyStep.cs", "coef.LaborSaveRate > 0 && !indOff ?", "coef.LaborSaveRate > 0 ?", 1),
 ("M17 薄めない（尺度が増える）", "PriceOfBread.SimCore/FactoryEquipment.cs", "s.Scale = b + p * keep;", "s.Scale = b + p;", 1),
 ("M18 国営の持分も薄める", "PriceOfBread.SimCore/FactoryEquipment.cs", "s.Scale = b + p * keep;", "s.Scale = (b + p) * keep; s.BuiltScale = b * keep;", 1),
 ("M19 民間の持分の合計でなく全体で薄める", "PriceOfBread.SimCore/FactoryEquipment.cs", "double keep = priv / (priv + given);", "double keep = sumScale / (sumScale + given);", 1),
 ("M20 門が閉じた個体も受け取る", "PriceOfBread.SimCore/FactoryEquipment.cs", "=> !s.Bankrupt && Discovery.Gate(r, s) > 0;", "=> !s.Bankrupt;", 1),
 ("M16 倒産で Scale を消す", "PriceOfBread.SimCore/EconomyStep.cs", "s.Bankrupt = true; if (!eqS) s.Scale = 0;", "s.Bankrupt = true; s.Scale = 0;", 1),
]
sel = sys.argv[1:]
for name, path, old, new, nth in muts:
    if sel and name.split()[0] not in sel: continue
    p = os.path.join(root, path)
    with open(p, 'rb') as f: orig = f.read()
    s = orig.decode('utf-8')
    idx = -1
    for _ in range(nth):
        idx = s.find(old, idx + 1)
    assert idx >= 0, (name, 'not found')
    s2 = s[:idx] + new + s[idx + len(old):]
    with open(p, 'wb') as f: f.write(s2.encode('utf-8'))
    try:
        b = subprocess.run(["dotnet", "build", "PriceOfBread.Tests", "-c", "Release", "--nologo", "-v", "q"], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
        if "0 エラー" not in b.stdout:
            print(name, "ビルド失敗", b.stdout[-300:], flush=True)
        else:
            t = subprocess.run(["dotnet", "test", "PriceOfBread.Tests", "-c", "Release", "--no-build", "--nologo", "--filter", "FullyQualifiedName~FactoryEquipmentTests"], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
            m = re.search(r"(成功|失敗)!.*", t.stdout)
            fails = re.findall(r"失敗 (\S+)", t.stdout)
            print(name, "→", m.group(0)[:80] if m else t.stdout[-200:], "|", ",".join(f[:60] for f in fails[:6]), flush=True)
    finally:
        with open(p, 'wb') as f: f.write(orig)
print("MUT-DONE", flush=True)
