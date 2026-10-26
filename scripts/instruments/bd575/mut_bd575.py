# (575) 線 bd：番人 `BuildingLifeUnitsTests` の突然変異（読むだけ・木は写しの上で壊す＝作業の木は触らない）。
# 使い方：python -X utf8 mut_bd575.py <作業の木> <写し先（空でよい）> <artifacts-path> <出力ログ> [M3,M5,... 走らせる変異だけ]
#   写し先に作業の木を写し、変異を 1 つずつ当ててテストをビルドし、`BuildingLifeUnitsTests`・武器の強さの試験を回して赤になることを確かめる（赤＝殺した）。
#   基準（変異なし）が緑であることを最初に確かめる。生き残った変異は SURVIVED・当たらなかった変異は NOCHANGE と書く。
import os, shutil, subprocess, sys, re
src, dst, art, logp = sys.argv[1:5]
ONLY = set(sys.argv[5].split(',')) if len(sys.argv) > 5 else None
def run(cmd):
    return subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', errors='replace')
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
if os.path.exists(dst): shutil.rmtree(dst)
ig = shutil.ignore_patterns('bin', 'obj', '.git', 'docs', 'measurements', 'uishot', '.vs')
shutil.copytree(src, dst, ignore=ig)
core = os.path.join(dst, 'PriceOfBread.SimCore')
sim, coef = [os.path.join(core, n) for n in ('Simulation.cs', 'Coefficients.cs')]
orig = {p: rd(p) for p in (sim, coef)}
muts = [
 ("M1 掛け算の生産性を掛けない", sim, lambda s: s.replace("            p.Productivity *= k;\n            var scaled = new List<Input>(p.Inputs.Count);\n            foreach (var i in p.Inputs) scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)", "            var scaled = new List<Input>(p.Inputs.Count);\n            foreach (var i in p.Inputs) scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)")),
 ("M2 投入を k で割らず掛ける", sim, lambda s: s.replace("scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)", "scaled.Add(new Input(i.GoodId, i.QtyPerOutput * k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)")),
 ("M3 k を逆数にする（基準÷寿命）", sim, lambda s: s.replace("double k = years / baseYears;", "double k = baseYears / years;")),
 ("M4 段の掛け算が武器の関数を通って門の印を財の印で上書きする", sim, lambda s: s.replace("            if (k > 0) ScaleUnitsPerInput(p, k);\n        }\n    }", "            if (k > 0) ToWeaponStrength(p, k);\n        }\n    }")),
 ("M5 煉瓦造と鋳鉄の骨組みの寿命を取り違える", sim, lambda s: s.replace('p.RecipeName == "煉瓦造") k = Ratio(c.BuildingLifeYearsBrick', 'p.RecipeName == "煉瓦造") k = Ratio(c.BuildingLifeYearsCastIron')),
 ("M6 住居の鉄筋コンクリートに掛けない", sim, lambda s: s.replace('else if (p.GoodId == Catalog.HousingId && p.RecipeName == "鉄筋コンクリート")', 'else if (false)')),
 ("M7 木造（既定のレシピ）にも煉瓦造の k を掛ける", sim, lambda s: s.replace('if (p.RecipeName == "煉瓦造") k = Ratio(c.BuildingLifeYearsBrick', 'if (p.RecipeName == "煉瓦造" || p.RecipeName.Length == 0) k = Ratio(c.BuildingLifeYearsBrick')),
 ("M8 旗 0 でも掛ける（旗の守りを外す）", sim, lambda s: s.replace("        if (!(c.BuildingLifeEnabled > 0)) return;\n        double Ratio", "        double Ratio")),
 ("M9 無限の寿命の守りを外す", sim, lambda s: s.replace("return k > 0 && !double.IsInfinity(k) ? k : 0.0;", "return k > 0 ? k : 0.0;")),
 ("M10 住居の鉄筋コンクリートが建造物の寿命を読む", sim, lambda s: s.replace("k = Ratio(c.HousingLifeYearsConcrete, c.HousingLifeYearsWood);", "k = Ratio(c.BuildingLifeYearsConcrete, c.HousingLifeYearsWood);")),
 ("M11 自己費用も k で割る", sim, lambda s: s.replace("            p.Productivity *= k;\n            var scaled = new List<Input>(p.Inputs.Count);\n            foreach (var i in p.Inputs) scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));", "            p.Productivity *= k; p.MarginalCost /= k;\n            var scaled = new List<Input>(p.Inputs.Count);\n            foreach (var i in p.Inputs) scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));")),
 ("M12 労働の投入は割らない", sim, lambda s: s.replace("scaled.Add(new Input(i.GoodId, i.QtyPerOutput / k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)", "scaled.Add(new Input(i.GoodId, i.GoodId == Catalog.LaborId ? i.QtyPerOutput : i.QtyPerOutput / k));\n            p.Inputs = scaled;\n        }\n    }\n\n    /// <summary>★(575)")),
 ("M13 構築の末尾の呼びを外す", sim, lambda s: s.replace("        ApplyBuildingLife(producers, coef);   // ★(575)", "        // ApplyBuildingLife(producers, coef);   // ★(575)")),
 ("M14 木造の寿命を基準にせず煉瓦造の寿命で割る", sim, lambda s: s.replace("if (p.RecipeName == \"鋼の骨組み\") k = Ratio(c.BuildingLifeYearsSteel, c.BuildingLifeYearsWood);", "if (p.RecipeName == \"鋼の骨組み\") k = Ratio(c.BuildingLifeYearsSteel, c.BuildingLifeYearsBrick);").replace('else if (p.RecipeName == "鋼の骨組み") k = Ratio(c.BuildingLifeYearsSteel, c.BuildingLifeYearsWood);', 'else if (p.RecipeName == "鋼の骨組み") k = Ratio(c.BuildingLifeYearsSteel, c.BuildingLifeYearsBrick);')),
 ("M15 旗の既定を 1 にする", coef, lambda s: s.replace("public double BuildingLifeEnabled = 0;", "public double BuildingLifeEnabled = 1;")),
 ("M16 武器の強さが掛け算を呼ばない（切り出しの取りこぼし）", sim, lambda s: s.replace("        ScaleUnitsPerInput(p, k);\n        p.OpenedByRow", "        p.OpenedByRow")),
]
tests = os.path.join(dst, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')
dll = os.path.join(art, 'bin', 'PriceOfBread.Tests', 'release', 'PriceOfBread.Tests.dll')
FILTER = 'FullyQualifiedName~BuildingLifeUnitsTests|FullyQualifiedName~WeaponParallelTests'
def build_and_test(label):
    b = run(['dotnet', 'build', tests, '-c', 'Release', '--artifacts-path', art, '-v', 'q'])
    if b.returncode != 0: return 'BUILDFAIL', (b.stdout + b.stderr)[-600:]
    t = run(['dotnet', 'test', dll, '--filter', FILTER])
    out = t.stdout + t.stderr
    m = re.search(r'(成功|失敗)!\s+-失敗:\s*(\d+)、合格:\s*(\d+)', out)
    return ('GREEN' if t.returncode == 0 else 'RED'), (m.group(0) if m else out[-300:])
with open(logp, 'w', encoding='utf-8') as lg:
    st, info = build_and_test('base'); lg.write(f'BASE {st} {info}\n'); lg.flush()
    for name, path, f in muts:
        if ONLY is not None and name.split(' ')[0] not in ONLY: continue
        for p, o in orig.items(): wr(p, o)
        cur = rd(path); new = f(cur)
        if new == cur: lg.write(f'{name}: NOCHANGE（変異が当たらなかった）\n'); lg.flush(); continue
        wr(path, new)
        st, info = build_and_test(name)
        lg.write(f'{name}: {"KILLED" if st == "RED" else ("SURVIVED" if st == "GREEN" else st)} {info}\n'); lg.flush()
    for p, o in orig.items(): wr(p, o)
    lg.write('MUTDONE\n')
