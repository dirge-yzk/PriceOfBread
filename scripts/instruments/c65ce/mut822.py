"""(822) 突然変異の台本（使い捨て）：worktree の計器に 1 つずつ変異を入れ、隔離ビルドで CearnInstrumentTests を回し、結果を表にして元へ戻す。
使い方: python -I mut822.py <worktree> <ビルドの出力> <結果の txt> [<変異の名前,...>]
ファイルはバイトで読み書きする（改行と BOM を保つ）。各変異の後に元のバイトへ書き戻す（git は使わない）。"""
import sys, os, subprocess, re
wt, art, outp = sys.argv[1], sys.argv[2], sys.argv[3]
S = os.path.join(wt, 'PriceOfBread.SimCore')
MUT = [
    ('M1', 'Instruments.cs', 'CearnSav(p, kind, delta);', '/* M1 */'),
    ('M2', 'InstrumentsCearn.cs', 'a.Flow[(int)CearnCol.OutMarket] += earned - delta;', 'a.Flow[(int)CearnCol.OutMarket] += 0;'),
    ('M3', 'InstrumentsCearn.cs', "cat == TreasuryCat.Tax ? CearnCol.OutTax : cat == TreasuryCat.Borrow ? CearnCol.OutBond", "cat == TreasuryCat.Tax ? CearnCol.OutBond : cat == TreasuryCat.Borrow ? CearnCol.OutTax"),
    ('M4', 'InstrumentsCearn.cs', 'int atFloor = lpm <= c.RegionalPriceLevelMin + 1e-9 ? 1 : 0;', 'int atFloor = lpm > c.RegionalPriceLevelMin + 1e-9 ? 1 : 0;'),
    ('M5', 'InstrumentsCearn.cs', 'foreach (var r in w.Regions)\n        {\n            var a = CearnAccs.GetValue(r, _ => new CearnAcc());\n            bool fresh',
           'for (int ri = 0; ri < w.Regions.Count; ri++)\n        {\n            var r = w.Regions[ri]; var a = CearnAccs.GetValue(w.Regions[(ri + 1) % w.Regions.Count], _ => new CearnAcc());\n            bool fresh'),
    ('M6', 'InstrumentsCearn.cs', 'fileName == "Fiscal.cs" ? CearnCol.OutDues : fileName == "Colonial.cs" ? CearnCol.OutColonialRent', 'fileName == "Fiscal.cs" ? CearnCol.OutColonialRent : fileName == "Colonial.cs" ? CearnCol.OutDues'),
    ('M7', 'InstrumentsCearn.cs', 'a.Flow[(int)col] += isIn ? delta : -delta;', 'a.Flow[(int)col] += delta;'),
    ('M8', 'InstrumentsCearn.cs', 'return (mp > 1e-9 ? ms / mp : all, all);', 'return (all, all);'),   # 追記：本土の物差しを全ての州の物差しへ取り違える
]
only = set(sys.argv[4].split(',')) if len(sys.argv) > 4 else None   # 名前の集合（省略＝全て）
res = []
for name, f, old, new in MUT:
    if only is not None and name not in only: continue
    p = os.path.join(S, f)
    b0 = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    if b0.count(o) != 1:
        res.append('%s\t%s\t変異の場所が 1 本でない（%d）\n' % (name, f, b0.count(o))); continue
    open(p, 'wb').write(b0.replace(o, n))
    try:
        bl = subprocess.run(['dotnet', 'build', os.path.join(wt, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--disable-build-servers'],
                            capture_output=True, text=True, encoding='utf-8', errors='replace')
        if bl.returncode != 0:
            res.append('%s\t%s\tビルドの失敗\n' % (name, f)); continue
        t = subprocess.run(['dotnet', 'test', os.path.join(wt, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~CearnInstrumentTests', '--logger', 'console;verbosity=normal'],
                           capture_output=True, text=True, encoding='utf-8', errors='replace')
        failed = sorted(set(re.findall(r'CearnInstrumentTests\.(\S+) \[FAIL\]', t.stdout)))
        passed = sorted(set(re.findall(r'(?:成功|Passed)\s+PriceOfBread\.Tests\.CearnInstrumentTests\.(\S+)', t.stdout)))
        summ = [l for l in t.stdout.splitlines() if '合計:' in l or 'Total tests' in l]
        res.append('%s\t%s\t赤=%s\t緑=%s\t%s\n' % (name, f, ','.join(failed) or '-', ','.join(passed) or '-', (summ[-1].strip() if summ else '')))
    finally:
        open(p, 'wb').write(b0)
with open(outp, 'w', encoding='utf-8', newline='\n') as o:
    o.write('(822) 突然変異の結果（CearnInstrumentTests・使い捨ての変異を worktree に入れて隔離ビルド D:/PoB_work/c65ce_mut_art で回し、元のバイトへ戻した）\n')
    o.write(''.join(res))
print(''.join(res))
