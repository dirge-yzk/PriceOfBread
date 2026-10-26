# (573) 線 sm5 lit の突然変異の台本：EconomyStep.cs の lit の換算を 1 か所ずつ戻し、StateIndConstructionScaleTests が赤になることを確かめる。
# 使い方：python mut_lit.py <worktree> <artifacts-path>  （各変異ごとに元のバイトへ戻す。途中で落ちても finally で戻す）
import subprocess, sys, os
wt, art = sys.argv[1], sys.argv[2]
path = os.path.join(wt, 'PriceOfBread.SimCore', 'EconomyStep.cs')
orig = open(path, 'rb').read()
text = orig.decode('utf-8')
MUTS = [
    ('M1 余地の割り算を定数に戻す', 'Math.Max(Eps, stateIndPerConstruction));', 'Math.Max(Eps, coef.IndPerConstruction));'),
    ('M2 足しの積を定数に戻す', 'double stateIndGain = built * stateIndPerConstruction;', 'double stateIndGain = built * coef.IndPerConstruction;'),
    ('M3 旗を無視する', 'stateIndPerConstruction = coef.StateIndPerConstructionScaled > 0 ? privateIndPerConstruction : coef.IndPerConstruction;', 'stateIndPerConstruction = coef.IndPerConstruction;'),
    ('M4 旗 0 でも私的な換算', 'stateIndPerConstruction = coef.StateIndPerConstructionScaled > 0 ? privateIndPerConstruction : coef.IndPerConstruction;', 'stateIndPerConstruction = privateIndPerConstruction;'),
]
res = []
try:
    for name, old, new in MUTS:
        assert text.count(old) == 1, (name, text.count(old))
        open(path, 'wb').write(text.replace(old, new).encode('utf-8'))
        p = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--filter', 'FullyQualifiedName~StateIndConstructionScaleTests'],
                           cwd=wt, capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = p.stdout
        red = p.returncode != 0 and ('失敗' in out or 'Failed' in out)
        res.append((name, 'RED' if red else ('BUILDFAIL?' if 'error' in out.lower() and '合格' not in out else 'GREEN(生き残り)')))
        print(res[-1], flush=True)
finally:
    open(path, 'wb').write(orig)
print('RESTORED', open(path, 'rb').read() == orig)
