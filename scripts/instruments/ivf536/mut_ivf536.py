import subprocess, shutil, os, sys, io
WT = 'D:/PoB_work/wt_ivf'
OUT = 'D:/PoB_work/ivf_536b/mut/mut.log'
ART = 'D:/PoB_work/ivf_536b/art_mut'
muts = [
 ('M1 tie-breaks-to-era', 'PriceOfBread.SimCore/EconomyStep.cs', 'return era.Cost(r) < steel.Cost(r) ? era : steel;', 'return era.Cost(r) <= steel.Cost(r) ? era : steel;'),
 ('M2 flag-ignored', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (!(c.ConstructionEraBundle > 0)) return steel;\n', '\n'),
 ('M3 private-demand-quantity-mixed', 'PriceOfBread.SimCore/EconomyStep.cs', 'region.ConstructionCarryover, bundle.G1, bundle.Q1 * construction);', 'region.ConstructionCarryover, bundle.G1, bundle.Q0 * construction);'),
 ('M4 private-settlement-steel-ids', 'PriceOfBread.SimCore/EconomyStep.cs', '''        if (cap && construction > 0)
        {
            int sg = bundle.G0, mg = bundle.G1, cg = bundle.G2, lg = bundle.G3;''', '''        if (cap && construction > 0)
        {
            int sg = coef.SteelGoodId, mg = coef.MachineGoodId, cg = coef.CementGoodId, lg = coef.LumberGoodId;'''),
 ('M5 factory-build-steel-demand', 'PriceOfBread.SimCore/EconomyStep.cs', 'investDemand[bundle.G0] += bundle.Q0 * u;', 'investDemand[coef.SteelGoodId] += bundle.Q0 * u;'),
 ('M6 invest-weight-steel-set', 'PriceOfBread.SimCore/World.cs', 'var bun = EconomyStep.BundleOf(r, co);', 'var bun = EconomyStep.ConsBundle.Steel(co);'),
 ('M7 f1-inverted-tier', 'PriceOfBread.SimCore/Simulation.cs', 'if (exColonies && r.Tier != RegionTier.Colonial) { mainSav', 'if (exColonies && r.Tier == RegionTier.Colonial) { mainSav'),
 ('M8 f1-base-not-applied', 'PriceOfBread.SimCore/Simulation.cs', 'if (exColonies && mainPop > 1e-9) { natSav = mainSav; natEarn = mainEarn; natPop = mainPop; }', 'if (exColonies && mainPop > 1e-9) { }'),
 ('M9 f3-iron-quantity', 'PriceOfBread.SimCore/Simulation.cs', 'new Input(Catalog.IronId, 2.52)', 'new Input(Catalog.IronId, 1.514)'),
 ('M10 f3-adds-when-off', 'PriceOfBread.SimCore/Simulation.cs', 'if (coef.MachineCastIronRecipe > 0)\n        {\n            Producer? machineDefault', 'if (coef.MachineCastIronRecipe >= 0)\n        {\n            Producer? machineDefault'),
 ('M11 state-settlement-steel-ids', 'PriceOfBread.SimCore/EconomyStep.cs', '''        if (cap && stateConstruction > 0)
        {
            int sg = bundle.G0, mg = bundle.G1, cg = bundle.G2, lg = bundle.G3;''', '''        if (cap && stateConstruction > 0)
        {
            int sg = coef.SteelGoodId, mg = coef.MachineGoodId, cg = coef.CementGoodId, lg = coef.LumberGoodId;'''),
 ('M12 carry-decay-removed', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (coef.ConstructionEraBundle > 0)\n                {\n                    void Decay', 'if (coef.ConstructionEraBundle > 1e9)\n                {\n                    void Decay'),
 ('M13 carry-decay-sets-swapped', 'PriceOfBread.SimCore/EconomyStep.cs', 'if (bundle.Era) { Decay(coef.SteelGoodId)', 'if (!bundle.Era) { Decay(coef.SteelGoodId)'),
]
log = io.open(OUT, 'w', encoding='utf-8')
def run(cmd):
    return subprocess.run(cmd, shell=True, capture_output=True, text=True, encoding='utf-8', errors='replace')
only = sys.argv[1:]  # 名前の先頭で絞る
for name, f, old, new in muts:
    if only and not any(name.startswith(o) for o in only): continue
    p = os.path.join(WT, f)
    data = open(p, 'rb').read().decode('utf-8')
    if data.count(old) != 1:
        log.write(f'{name}: SKIP (old count {data.count(old)})\n'); log.flush(); continue
    bak = p + '.mutbak'
    shutil.copyfile(p, bak)
    try:
        open(p, 'wb').write(data.replace(old, new, 1).encode('utf-8'))
        b = run(f'cd {WT} && dotnet build PriceOfBread.Tests -c Release --artifacts-path {ART} -v q -nologo')
        if ' error ' in b.stdout:
            log.write(f'{name}: BUILD-ERROR\n{b.stdout[-600:]}\n'); log.flush(); continue
        t = run(f'cd {WT} && dotnet test PriceOfBread.Tests -c Release --artifacts-path {ART} --no-build --filter "FullyQualifiedName~IvfInitialValuesTests" -v n -nologo')
        fails = [l.strip() for l in t.stdout.splitlines() if l.strip().startswith('失敗 PriceOfBread')]
        log.write(f'{name}: {"RED (" + str(len(fails)) + ")" if fails else "SURVIVED"}\n')
        for l in fails: log.write('   ' + l[:160] + '\n')
        log.flush()
    finally:
        shutil.copyfile(bak, p); os.remove(bak)
log.write('MUTDONE\n'); log.close()
