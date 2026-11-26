# (1378) c146bk 突然変異 M1〜M4 を使い捨ての木で 1 本ずつ当て、BankruptReentryShortageTests の赤と緑を表にする（バイトで読み書き）。
# 使い方: python mut_bk.py <使い捨ての木> <出力フォルダ> <隔離ビルドのフォルダ>
import sys, subprocess, os, re
T, OUT, ART = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
P = os.path.join(T, 'PriceOfBread.SimCore', 'EconomyStep.cs')
orig = open(P, 'rb').read()
NEW = b'? rm.LastShortage > 0   //'
OLD = b': rm.LastDemand > rm.LastProduced + rm.Stock;'
FLAG = b'bool shortage = coef.ReentryShortageSameMonth > 0'
muts = {
    'M0': (None, None),
    'M1': (NEW, b'? rm.LastDemand > rm.LastProduced + rm.Stock   //'),
    'M2': (NEW, b'? !(rm.LastShortage <= 0)   //'),
    'M3': (FLAG, b'bool shortage = true'),
    'M4': (NEW, b'? rm.LastShortage >= 0   //'),
}
rows = []
for k, (a, b) in muts.items():
    src = orig
    if a is not None:
        assert orig.count(a) == 1, k
        src = orig.replace(a, b)
    open(P, 'wb').write(src)
    log = os.path.join(OUT, f'{k}.txt')
    with open(log, 'wb') as f:
        subprocess.run(['dotnet', 'test', os.path.join(T, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', ART,
                        '--filter', 'FullyQualifiedName~BankruptReentryShortageTests'], stdout=f, stderr=subprocess.STDOUT)
    t = open(log, encoding='utf-8', errors='replace').read()
    fails = sorted(set(re.findall(r'BankruptReentryShortageTests\.(\S+) \[FAIL\]', t)))
    summ = re.findall(r'(失敗!|成功!)[^\n]*', t)
    rows.append(f'{k}\t{len(fails)}\t{",".join(fails) or "-"}\t{summ[-1] if summ else "ビルド失敗?"}')
open(P, 'wb').write(orig)
open(os.path.join(OUT, 'mut_table.tsv'), 'w', encoding='utf-8', newline='\n').write('mut\tred\tfailed\tsummary\n' + '\n'.join(rows) + '\n')
print('\n'.join(rows))
