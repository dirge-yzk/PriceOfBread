# (908) 突然変異の台本：木の中のファイルを 1 か所だけ壊し、フィルタの試験を回して赤か緑かを書き、必ずファイルを元に戻す。
#   python -I mutate908.py <木> <出力> <フィルタ> <名前>@@<ファイル>@@<旧>@@<新> ...   （旧は木のファイルにちょうど 1 回・文字列の中の \n は改行）
import sys, subprocess, os
tree, out, flt = sys.argv[1], sys.argv[2], sys.argv[3]
BS = chr(92)
res = []
for spec in sys.argv[4:]:
    name, rel, old, new = spec.split('@@', 3)
    p = os.path.join(tree, rel)
    orig = open(p, 'rb').read()
    ob, nb = old.replace(BS + 'n', chr(10)).encode(), new.replace(BS + 'n', chr(10)).encode()
    assert orig.count(ob) == 1, f'{name}: 旧が {orig.count(ob)} 回'
    try:
        open(p, 'wb').write(orig.replace(ob, nb))
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c80sm_art', '--filter', flt, '--nologo'],
                           capture_output=True, text=True, encoding='utf-8', errors='replace')
        txt = r.stdout + r.stderr
        failed = [l.strip()[:200] for l in txt.splitlines() if l.strip().startswith(('失敗 ', 'Failed '))]
        summ = [l.strip()[:200] for l in txt.splitlines() if '合計:' in l or 'error CS' in l][:2]
        res.append((name, 'RED' if r.returncode != 0 else 'GREEN', failed[:4], summ))
    finally:
        open(p, 'wb').write(orig)
with open(out, 'w', encoding='utf-8') as f:
    for name, st, failed, summ in res:
        f.write(f'== {name}: {st}\n')
        for l in failed + summ: f.write('   ' + l + '\n')
