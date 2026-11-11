# (1037) c98lw 突然変異の台本：使い捨ての worktree の EconomyStep.cs に 1 本ずつ変異を入れ、器の試験 LaborWeightTests の赤と緑を書く（終わったら元に戻す）。
# 使い方: python mut_lw.py <使い捨ての木> <成果物のフォルダ> <出力の表>
import sys, subprocess, re
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
src = tree + '/PriceOfBread.SimCore/EconomyStep.cs'
orig = open(src, 'rb').read()
MUTS = [
    ('M1 m＝0 で固定', '(lwRole[lp] & 1) != 0 ? mPow : 0.0', '0.0'),
    ('M2 農民にも m', '(lwRole[lp] & 1) != 0 ? mPow : 0.0', 'mPow'),
    ('M3 子の重みに ChildWorkFactor を掛ける', 'lpop.AgeShare(10, 15) * wC * lpop.ChildLaborRate.Value', 'lpop.AgeShare(10, 15) * wC * coef.ChildWorkFactor * lpop.ChildLaborRate.Value'),
    ('M4 基準を毎月捕え直す', 'if (lpop.InitialWork < 0) lpop.InitialWork = work;', 'if (lpop.InitialWork < 0 || lwOn) lpop.InitialWork = work;'),
    ('M5 子の重みを手のまま', 'double wC = coef.ChildHandLaborWeight + (1.0 - coef.ChildHandLaborWeight) * m;', 'double wC = coef.ChildHandLaborWeight;'),
    ('M6 女の重みを手のまま', 'double wF = coef.FemaleHandLaborWeight + (1.0 - coef.FemaleHandLaborWeight) * m;', 'double wF = coef.FemaleHandLaborWeight;'),
    ('M8 m の分母を工場だけに', 'double den = region.Markets[Catalog.LaborId].LastDemand;',
     'double den = 0; foreach (var s0 in region.Producers) foreach (var i0 in s0.Inputs) if (i0.GoodId == Catalog.LaborId) den += s0.Output * EffectiveQty(s0, region, i0, coef);'),
    ('M9 技師にも重み（手）', 'bool lwW = lwOn && (lwRole[lp] & 3) != 0;', 'bool lwW = lwOn;'),
]
lines = ['| 変異 | ビルド | 赤の試験 | 緑の数 |']
for name, a, b in MUTS:
    ab, bb = a.encode(), b.encode()
    if orig.count(ab) != 1:
        lines.append(f'| {name} | 置き換えの元が {orig.count(ab)} 件＝回さない | - | - |'); continue
    open(src, 'wb').write(orig.replace(ab, bb))
    try:
        r = subprocess.run(['dotnet', 'build', tree + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '-v', 'q', '-nologo'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        if r.returncode != 0:
            lines.append(f'| {name} | 失敗 | - | - |'); continue
        t = subprocess.run(['dotnet', 'test', tree + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--no-build', '--filter', 'FullyQualifiedName~LaborWeightTests',
                            '--logger', 'console;verbosity=normal'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.LaborWeightTests\.(T\d[^ (\[]*)', t.stdout)))
        ok = len(re.findall(r'成功 PriceOfBread\.Tests\.LaborWeightTests\.', t.stdout))
        lines.append(f'| {name} | 通る | {"・".join(red) if red else "なし（緑のまま）"} | {ok} |')
    finally:
        open(src, 'wb').write(orig)
open(out, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('\n'.join(lines))
