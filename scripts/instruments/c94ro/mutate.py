# (1013) 突然変異の台本：使い捨ての木（引数 1）に 1 つずつ変異を当て、計器の試験を回して全文を保存する。
import subprocess, sys, os
tree = sys.argv[1]; outdir = sys.argv[2]; art = sys.argv[3]
F = os.path.join(tree, 'PriceOfBread.SimCore', 'InstrumentsRecipeout.cs')
MUT = {
 'Mu1_投入の値段を国の平均にする': ('inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? r.Markets[inp.GoodId].Price : 0.0',
                         'inp.GoodId >= 0 && inp.GoodId < r.Markets.Length ? System.Linq.Enumerable.Average(w.Regions, rr => rr.Markets[inp.GoodId].Price) : 0.0'),
 'Mu2_outputSumをCapacityにする': ('a.N++; a.O += o;', 'a.N++; a.O += p.Capacity;'),
 'Mu3_農業労働の投入も割る': ('selfAb = self / System.Math.Pow(rule, expo);', 'selfAb = self / System.Math.Pow(rule, expo); inputs /= System.Math.Pow(rule, expo);'),
 'Mu4_大鉱床を4.0だけで割る': ('if (p.SiteFound && Catalog.Sites[site].Kind == Catalog.SiteKind.Deposit) rule *= c.GoldRushEndowment;', ''),
 'Mu5_級の規則値をProductivityから決める': ('        // 自己費用（参照価格の単位→州の名目）', '        if (!double.IsNaN(endow)) rule = endow;\n        // 自己費用（参照価格の単位→州の名目）'),
 'Mu6_希少鉱物の総量保存を使わない': ('else if (inRes) { cls = "E"; rule = Simulation.ScarceEndowment(g, c, shared)', 'else if (inRes) { cls = "E"; rule = Simulation.ResourceEndowment(g)'),
}
only = sys.argv[4:] 
orig = open(F, 'rb').read().decode('utf-8')
os.makedirs(outdir, exist_ok=True)
summary = []
for name, (a, b) in MUT.items():
    if only and not any(name.startswith(o) for o in only): continue
    assert orig.count(a) == 1, name
    open(F, 'wb').write(orig.replace(a, b).encode('utf-8'))
    log = os.path.join(outdir, name + '.log')
    with open(log, 'wb') as fh:
        subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~RecipeoutInstrumentTests'], stdout=fh, stderr=subprocess.STDOUT)
    open(F, 'wb').write(orig.encode('utf-8'))
    txt = open(log, 'rb').read().decode('utf-8', 'replace')
    keep = [l for l in txt.splitlines() if ('成功!' in l or '失敗!' in l or l.strip().startswith('失敗 ') or 'error CS' in l)]
    summary.append('## ' + name + '\n' + '\n'.join(keep) + '\n')
open(os.path.join(outdir, 'summary.txt'), 'wb').write('\n'.join(summary).encode('utf-8'))
