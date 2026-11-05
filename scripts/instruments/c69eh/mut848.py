# (848) c69eh 突然変異の台本（使い捨ての木で EmptyPopEstate.cs を書き換え、EmptyPopEstateTests を回して試験ごとの赤と緑を表にする）。
#   呼び方：python -I scripts/instruments/c69eh/mut848.py <使い捨ての木> <artifacts のフォルダ> <出力ファイル>
#   木は git worktree の写し（呼び手が作る）。元のファイルは最後に戻す。事前登録 measurements/m848_20261105_prereg_eh.txt §5 の E0〜E4。
import sys, os, subprocess, re
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
src = os.path.join(tree, 'PriceOfBread.SimCore', 'EmptyPopEstate.cs')
orig = open(src, 'rb').read()
M = {
    'E0 何もしない（常に早い return）': [(b'if (!(w.Coef.EmptyPopEstate > 0)) return;', b'return;')],
    'E1 人数の比でなく均等に配る': [(b'World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);', b'World.Transfer((w, payers), (w, heirs, heirs.ConvertAll(_ => 1.0)), sum, fromWeights: held);')],
    'E2 人の居る POP からも出す': [(b'if (p.Size > 1e-12) { live += p.Size; continue; }', b'if (p.Size > 1e-12) { live += p.Size; }')],
    'E3 別の州へ移す（次の州の受け手）': [(b'foreach (var p in r.Pops) if (p.Size > 1e-12) heirs.Add(p);', b'foreach (var p in w.Regions[(w.Regions.IndexOf(r) + 1) % w.Regions.Count].Pops) if (p.Size > 1e-12) heirs.Add(p);')],
    'E4 人数 0 の POP へも配る（重み 1）': [(b'World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);', b'World.Transfer((w, payers), (w, new List<Pop>(r.Pops), r.Pops.ConvertAll(p => p.Size > 1e-12 ? p.Size : 1.0)), sum, fromWeights: held);')],
}
L = ['(848) c69eh 突然変異（EmptyPopEstateTests・使い捨ての木 ' + tree + '）']
def run(label):
    r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art,
                        '--filter', 'FullyQualifiedName~EmptyPopEstateTests', '--logger', 'console;verbosity=normal'],
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    res = {}
    for line in r.stdout.splitlines():
        m = re.search(r'(成功|失敗|Passed|Failed)\s+PriceOfBread\.Tests\.EmptyPopEstateTests\.(\S+)', line)
        if m: res[m.group(2)] = '緑' if m.group(1) in ('成功', 'Passed') else '赤'
    L.append(f'== {label}（終了コード {r.returncode}）')
    if not res: L.append('  （試験の行が読めない＝ビルドの失敗か）'); L.extend('  ' + x for x in r.stdout.splitlines()[-15:])
    for k in sorted(res): L.append(f'  {res[k]}\t{k}')
    print('\n'.join(L[-(len(res) + 1):]), flush=True)
try:
    run('元の形（旗の既定のまま）')
    for name, reps in M.items():
        b = orig
        for a, c in reps:
            assert b.count(a) == 1, (name, a)
            b = b.replace(a, c)
        open(src, 'wb').write(b)
        run(name)
finally:
    open(src, 'wb').write(orig)
with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
