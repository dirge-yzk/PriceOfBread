# (505) 線 ir の突然変異の組。使い方：検証する木をチェックアウトした専用の worktree を MUT_ROOT に渡して `python scripts/instruments/ir505/mut.py [M01 M02 ...]`。
#   各回：1 か所を書き換え → dotnet build PriceOfBread.Tests -c Release → IndustryReadersTests を回す → 元に戻す。全部「失敗」が出れば合格（生き残りは番人の穴）。
#   ⚠M16（旧の旗 SanitationFromGoodsOnly の oldDev）は旗の削除（追記 1-0 ⑤）で無くなった。M14・M15 は既存の重みの掛け方の取り違えに置き換えた。
#   出力は cp932 で化ける環境がある（`iconv -f cp932 -t utf-8`）。事前登録 measurements/ir505_20261023_prereg.txt [8] の突然変異の再現用。
import subprocess, sys, os, re
root = os.environ.get("MUT_ROOT", "D:/PoB_work/ir_505/wt_mut")
muts = [
 ("M01 u の分母から 1 を外す", "PriceOfBread.SimCore/EconomyStep.cs", "return g > 0 ? g / (1.0 + ms) : 0.0;", "return g > 0 ? g / ms : 0.0;", 1),
 ("M02 差の向きを逆にする", "PriceOfBread.SimCore/EconomyStep.cs", "double g = m2 / w - ms;", "double g = ms - m2 / w;", 1),
 ("M03 倒産を数える", "PriceOfBread.SimCore/EconomyStep.cs", "if (!FactoryEquipment.Eligible(s) || s.Bankrupt || !(Discovery.Gate(region, s) > 0)) continue;", "if (!FactoryEquipment.Eligible(s) || !(Discovery.Gate(region, s) > 0)) continue;", 1),
 ("M04 発見の門が閉じた個体も数える", "PriceOfBread.SimCore/EconomyStep.cs", "if (!FactoryEquipment.Eligible(s) || s.Bankrupt || !(Discovery.Gate(region, s) > 0)) continue;", "if (!FactoryEquipment.Eligible(s) || s.Bankrupt) continue;", 1),
 ("M05 戻りが旧の係数×u でなく u だけ", "PriceOfBread.SimCore/EconomyStep.cs", "return (techLev, u, coef.IndProductivityGain * u * (1.0 + coef.IndTechLeverage * techLev), hurdle);", "return (techLev, u, u * (1.0 + coef.IndTechLeverage * techLev), hurdle);", 1),
 ("M06 戻りが在庫を読む", "PriceOfBread.SimCore/EconomyStep.cs", "double u = MarginGap(region, cost);", "double u = MarginGap(region, cost) / (1.0 + region.ProductiveIndustrialization);", 1),
 ("M07 台帳の足し忘れ", "PriceOfBread.SimCore/EconomyStep.cs", "if (cost is not null) cost[CostLedger.InvestReturnScans] += ps.Count;", "", 1),
 ("M08 適格の判定で上限が残る", "PriceOfBread.SimCore/World.cs", "|| RegionCapOff);", "|| !RegionCapOff);", 1),
 ("M09 按分の重みが上限の余地を読み続ける", "PriceOfBread.SimCore/World.cs", "double head = RegionCapOff ? 1.0 :", "double head = false ? 1.0 :", 1),
 ("M10 巨大建造物の効きが上限に切られる", "PriceOfBread.SimCore/Megaprojects.cs", "FactoryEquipment.CapOffOn(c, false) ? add :", "false ? add :", 1),
 ("M11 国家建設のヘッドルームが旗で外れない", "PriceOfBread.SimCore/EconomyStep.cs", "double stateHeadroomUnits = capOff ? double.PositiveInfinity :", "double stateHeadroomUnits = false ? double.PositiveInfinity :", 1),
 ("M12 助走でも旗を読む", "PriceOfBread.SimCore/FactoryEquipment.cs", "internal static bool MarginOn(Coefficients c, bool warmFrozen) => c.InvestReturnFromMargin > 0 && On(c, warmFrozen);", "internal static bool MarginOn(Coefficients c, bool warmFrozen) => c.InvestReturnFromMargin > 0 && c.RecipeEquipment > 0;", 1),
 ("M13 設備の規則が無い木でも旗を読む", "PriceOfBread.SimCore/FactoryEquipment.cs", "internal static bool MarginOn(Coefficients c, bool warmFrozen) => c.InvestReturnFromMargin > 0 && On(c, warmFrozen);", "internal static bool MarginOn(Coefficients c, bool warmFrozen) => c.InvestReturnFromMargin > 0 && !warmFrozen;", 1),
 ("M17 AI の信号が旧の式のまま", "PriceOfBread.SimCore/World.cs", "TechProductivity(r), marginOn, cost);", "TechProductivity(r), false, cost);", 1),
 ("M18 配りが利幅に比例でなく等分（G-M）", "PriceOfBread.SimCore/FactoryEquipment.cs", "double w = sumW > 1e-12 ? (s.SmoothedMargin > 0 ? s.SmoothedMargin / sumW : 0.0) : 1.0 / nCand;", "double w = 1.0 / nCand;", 1),
 ("M19 配りの割合 φ が半分（G-M）", "PriceOfBread.SimCore/FactoryEquipment.cs", "double phi = points / (ind + points);", "double phi = 0.5 * points / (ind + points);", 1),
 ("M20 計器 ir_s の guard が増加を数える", "PriceOfBread.SimCore/InstrumentsIndustry.cs", "if (!(after > before + 1e-12)) a.GuardNon++;", "a.GuardNon++;", 1),
]
sel = sys.argv[1:]
for name, path, old, new, nth in muts:
    if sel and name.split()[0] not in sel:
        continue
    p = os.path.join(root, path)
    with open(p, 'rb') as f:
        orig = f.read()
    s = orig.decode('utf-8')
    idx = -1
    for _ in range(nth):
        idx = s.find(old, idx + 1)
    assert idx >= 0, (name, 'not found')
    s2 = s[:idx] + new + s[idx + len(old):]
    with open(p, 'wb') as f:
        f.write(s2.encode('utf-8'))
    try:
        b = subprocess.run(["dotnet", "build", "PriceOfBread.Tests", "-c", "Release", "--nologo", "-v", "q"], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
        if "0 エラー" not in b.stdout:
            print(name, "ビルド失敗", b.stdout[-300:], flush=True)
        else:
            t = subprocess.run(["dotnet", "test", "PriceOfBread.Tests", "-c", "Release", "--no-build", "--nologo", "--filter", "FullyQualifiedName~IndustryReadersTests"], cwd=root, capture_output=True, text=True, encoding='utf-8', errors='replace')
            m = re.search(r"(成功|失敗)!.*", t.stdout)
            fails = re.findall(r"失敗 (\S+)", t.stdout)
            print(name, "→", m.group(0)[:80] if m else t.stdout[-200:], "|", ",".join(f[:60] for f in fails[:6]), flush=True)
    finally:
        with open(p, 'wb') as f:
            f.write(orig)
print("MUT-DONE", flush=True)
