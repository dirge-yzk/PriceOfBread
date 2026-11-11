# (949) 突然変異：judge949.py・gdp_fall949.py の写しを壊し、selftest949.py が赤になる（rc 1）ことを確かめる。使い方: python -X utf8 -I mut949.py <作業フォルダ>
import os, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__)); W = sys.argv[1]; os.makedirs(W, exist_ok=True)
J = open(os.path.join(HERE, 'judge949.py'), 'rb').read().decode(); F = open(os.path.join(HERE, 'gdp_fall949.py'), 'rb').read().decode()
MUT = [('Mu1 判定の < を ≦（腕 > 参照 を 腕 >= 参照）', 'judge', 'stop = me > ref', 'stop = me >= ref'),
       ('Mu2 ヌル腕の件数を 0 にする', 'judge', 'ref = max(n[BASIS] for n in ns)', 'ref = 0'),
       ('Mu3 下がりの区分を上がりに混ぜる（比の向きを逆）', 'fall', 'elif g * FALL <= prev[u]:', 'elif g >= prev[u] * FALL:')]
allred = True
for name, kind, a, b in MUT:
    src = J if kind == 'judge' else F
    assert src.count(a) == 1, name
    p = os.path.join(W, f'mut_{kind}_{name[:3]}.py'); open(p, 'wb').write(src.replace(a, b).encode())
    r = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(HERE, 'selftest949.py'), '--' + kind, p, '--work', os.path.join(W, 'w')], capture_output=True, encoding='utf-8')
    reds = [l[2:].split(' ')[0] for l in r.stdout.split('\n') if l.startswith('赤 ')]
    print(f'{name}\trc={r.returncode}\t赤の門 {",".join(reds) or "無し"}'); allred &= (r.returncode == 1)
print('全ての突然変異で赤' if allred else '緑のままの突然変異あり'); sys.exit(0 if allred else 1)
