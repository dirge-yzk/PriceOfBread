# (550) 線 fb：番人 `BuildingGoodBundleTests` の突然変異（読むだけ・木は写しの上で壊す＝作業の木は触らない）。
# 使い方：python -X utf8 mut_fb550.py <作業の木> <写し先（空でよい）> <artifacts-path> <出力ログ> [M8,M13,... 走らせる変異だけ]
#   写し先に作業の木を写し、変異を 1 つずつ当ててテストをビルドし、`BuildingGoodBundleTests` を回して赤になることを確かめる（赤＝殺した）。
#   基準（変異なし）が緑であることを最初に確かめる。生き残った変異は出力に SURVIVED と書く。当たらなかった変異は NOCHANGE。
import os, shutil, subprocess, sys, re
src, dst, art, logp = sys.argv[1:5]
ONLY = set(sys.argv[5].split(',')) if len(sys.argv) > 5 else None   # 例 M8,M13,M14（番人を緩めた後に、その主張で殺していた変異だけ再走する）
def run(cmd):
    return subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', errors='replace')
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
if os.path.exists(dst): shutil.rmtree(dst)
ig = shutil.ignore_patterns('bin', 'obj', '.git', 'docs', 'measurements', 'uishot', '.vs')
shutil.copytree(src, dst, ignore=ig)
core = os.path.join(dst, 'PriceOfBread.SimCore')
eco, sim, cat, coef, val = [os.path.join(core, n) for n in ('EconomyStep.cs', 'Simulation.cs', 'Catalog.cs', 'Coefficients.cs', 'Validation.cs')]
orig = {p: rd(p) for p in (eco, sim, cat, coef, val)}
muts = [
 ("M1 BundleOf が旗を読まない（常に建造物の組）", eco, lambda s: s.replace("if (!(c.BuildingGoodBundle > 0)) return ConsBundle.Steel(c);", "if (false) return ConsBundle.Steel(c);")),
 ("M2 BundleRef が旗を読まない（常に建造物の組の参照）", eco, lambda s: s.replace("=> c.BuildingGoodBundle > 0\n            ?", "=> true\n            ?")),
 ("M3 Units の 2 枠が後ろの 2 枠も読む", eco, lambda s: s.replace("=> Slots == 2 ? Math.Min(e0 / Math.Max(floor, Q0), e1 / Math.Max(floor, Q1))", "=> Slots == 99 ? Math.Min(e0 / Math.Max(floor, Q0), e1 / Math.Max(floor, Q1))")),
 ("M4 繰越の二重積みの守りを外す", eco, lambda s: s.replace("if (bundle.Slots > 2)   // ★(550)", "if (true)   // ★(550)")),
 ("M5 旗 0 でも既定の建造物の作り手を置く", sim, lambda s: s.replace("if (d.Id == Catalog.BuildingId && !(coef.BuildingGoodBundle > 0)) continue;", "")),
 ("M6 鋼の骨組みの門を外す", sim, lambda s: s.replace('[new Input(Catalog.SteelId, 0.2), new Input(Catalog.ClayId, 0.45), new Input(Catalog.CoalId, 0.2)], "転炉鋼")', '[new Input(Catalog.SteelId, 0.2), new Input(Catalog.ClayId, 0.45), new Input(Catalog.CoalId, 0.2)], null)')),
 ("M7 鉄筋コンクリート造の門を外す", sim, lambda s: s.replace('new Input(Catalog.LimestoneId, 0.25)], "鉄筋コンクリート")', 'new Input(Catalog.LimestoneId, 0.25)], null)')),
 ("M8 建造物の量 ConsBuilding を 2.0 にする", coef, lambda s: s.replace("public double ConsBuilding = 2.3969;", "public double ConsBuilding = 2.0;")),
 ("M9 下流の無い財の検査の除外を外す", val, lambda s: s.replace("if (g.Id == Catalog.BuildingId && !(sim.Coef.BuildingGoodBundle > 0)) continue;", "")),
 ("M10 2 枠の費用が後ろの 2 枠も足す", eco, lambda s: s.replace("=> Slots == 2 ? Q0 * Math.Max(r.Markets[G0].Price, Eps) + Q1 * Math.Max(r.Markets[G1].Price, Eps)\n             :", "=> Slots == 99 ? Q0 * Math.Max(r.Markets[G0].Price, Eps) + Q1 * Math.Max(r.Markets[G1].Price, Eps)\n             :")),
 ("M11 木造の投入の製材を 1.1 にする", cat, lambda s: s.replace("[I(LumberId,1.0), I(LimestoneId,0.15)], NonTradeable: true, WearRatePerMonth: 0.00168", "[I(LumberId,1.1), I(LimestoneId,0.15)], NonTradeable: true, WearRatePerMonth: 0.00168")),
 ("M12 建造物の損耗を 0.005 にする", cat, lambda s: s.replace("NonTradeable: true, WearRatePerMonth: 0.00168", "NonTradeable: true, WearRatePerMonth: 0.005")),
 ("M13 鋳鉄の骨組みの行を表の先頭付近へ（末尾の規則）", cat, lambda s: s.replace('        new("鋳鉄の骨組み", BuildingId, "鋳鉄の骨組み", DiscoveryField.Process, 1790),', '').replace('new("蒸気機関", LaborId, "蒸気機関", DiscoveryField.Process, 1780),', 'new("鋳鉄の骨組み", BuildingId, "鋳鉄の骨組み", DiscoveryField.Process, 1790), new("蒸気機関", LaborId, "蒸気機関", DiscoveryField.Process, 1780),')),
 ("M14 AddBuilding の生産性を木造の 1.1 倍にする", sim, lambda s: s.replace("Productivity = woodDefault.Productivity, Output = 0.5", "Productivity = woodDefault.Productivity * 1.1, Output = 0.5")),
 ("M15 BundleOf の建造物の市場の守りを外す（市場が無くても建造物の組）", eco, lambda s: s.replace("if (!(Catalog.BuildingId < n && c.MachineGoodId < n)) return ConsBundle.Steel(c);", "")),
]
tests = os.path.join(dst, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')
dll = os.path.join(art, 'bin', 'PriceOfBread.Tests', 'release', 'PriceOfBread.Tests.dll')
def build_and_test(label):
    b = run(['dotnet', 'build', tests, '-c', 'Release', '--artifacts-path', art, '-v', 'q'])
    if b.returncode != 0: return 'BUILDFAIL', (b.stdout + b.stderr)[-600:]
    t = run(['dotnet', 'test', dll, '--filter', 'FullyQualifiedName~BuildingGoodBundleTests'])
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
