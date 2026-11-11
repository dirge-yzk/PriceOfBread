# (1047) c99ov 突然変異の台本（c98lw/mut_lw.py の写し）：使い捨ての木の EconomyStep.cs に 1 本ずつ変異を入れ、器の試験 SellRTradeTests の赤と緑を書く（終わったら元に戻す）。
# 使い方: python mut_ov.py <使い捨ての木> <成果物のフォルダ> <出力の表>
import sys, subprocess, re
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
src = tree + '/PriceOfBread.SimCore/EconomyStep.cs'
orig = open(src, 'rb').read()
NEW = 'double sold = mkR.LastDemand + mkR.PrevExport - mkR.PrevImport;'
MUTS = [
    ('M1 PrevExport を LastExport に（常に 0）', NEW, 'double sold = mkR.LastDemand + mkR.LastExport - mkR.PrevImport;'),
    ('M2 −PrevImport を外す', NEW, 'double sold = mkR.LastDemand + mkR.PrevExport;'),
    ('M3 ＋PrevExport を外す', NEW, 'double sold = mkR.LastDemand - mkR.PrevImport;'),
    ('M4a 旗を読まない（常に旧）', 'if (coef.SellRCountsTrade > 0)', 'if (false)'),
    ('M4b 旗を読まない（常に新）', 'if (coef.SellRCountsTrade > 0)', 'if (true)'),
    ('M5 輸出の符号を逆に', NEW, 'double sold = mkR.LastDemand - mkR.PrevExport - mkR.PrevImport;'),
]
lines = ['| 変異 | ビルド | 赤の試験 | 緑の数 |']
for name, a, b in MUTS:
    ab, bb = a.encode(), b.encode()
    if orig.count(ab) != 1:
        lines.append(f'| {name} | 置き換えの元が {orig.count(ab)} 件＝回さない | - | - |'); continue
    open(src, 'wb').write(orig.replace(ab, bb))
    try:
        r = subprocess.run(['dotnet', 'build', tree + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '-v', 'q', '-nologo', '--disable-build-servers'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        if r.returncode != 0:
            lines.append(f'| {name} | 失敗 | - | - |'); continue
        t = subprocess.run(['dotnet', 'test', tree + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', art, '--no-build', '--filter', 'FullyQualifiedName~SellRTradeTests',
                            '--logger', 'console;verbosity=normal'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        red = sorted(set(re.findall(r'失敗 PriceOfBread\.Tests\.SellRTradeTests\.(T\d[^ (\[]*)', t.stdout)))
        ok = len(re.findall(r'成功 PriceOfBread\.Tests\.SellRTradeTests\.', t.stdout))
        lines.append(f'| {name} | 通る | {"・".join(red) if red else "なし（緑のまま）"} | {ok} |')
    finally:
        open(src, 'wb').write(orig)
open(out, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('\n'.join(lines))
