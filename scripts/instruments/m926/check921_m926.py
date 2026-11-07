# (921) c78gz の台本の盤面の足し：〈同名で別の識別子〉と〈優先〉の盤面 fixture/board921 を、台本の写し（使い捨て・D:/PoB_work/c80sm_work/mut_gz921）の突然変異で落とす。
#   使い方: python -X utf8 -I check921.py   （出力＝各行 緑／赤）。期待は盤面の作り（k同名X は U1／U2 が各 3 行の 0＝識別子ごとなら 4 行に届かず 0 件・名前ごとに混ぜると 6 行 → 1 件）から書いた。
import os, shutil, subprocess, sys
HERE = 'D:/PoB_work/wt_m926/scripts/instruments/c80sm'; G = os.path.join(HERE, '..', 'c78gz'); BOARD = os.path.join(G, 'fixture', 'board921')
W = 'D:/PoB_work/m926/mut_gz921'
def run(d, script, *a):
    return subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(d, script), *a], capture_output=True, encoding='utf-8').stdout
def mutated(script, a, b):
    shutil.rmtree(W, ignore_errors=True); shutil.copytree(G, W)
    p = os.path.join(W, script); s = open(p, 'rb').read().decode('utf-8'); assert s.count(a) == 1, (script, s.count(a))
    open(p, 'wb').write(s.replace(a, b).encode('utf-8')); return W
res = []
def ok(n, c): res.append(bool(c)); print('緑' if c else '赤', n)
z = run(G, 'gdp_zero906.py', '--founding', f'FIX={BOARD}'); j = run(G, 'gdp_jump906.py', '--founding', '--rows', f'FIX={BOARD}')
ok('元の台本：同名で別の識別子の盤面は 建国から続く 0 を 0 件', '）0・国×四半期 0' in z)
ok('元の台本：優先の盤面は 建国の直後 1・分母の痩せ 0', '建国の直後〔≦4 四半期〕1・' in j and '分母の痩せ 0・' in j)
d = mutated('gdp_zero906.py', "(row[inn], row[iu]) if FOUNDING else row[inn]", "(row[inn], '') if FOUNDING else row[inn]")
zm = run(d, 'gdp_zero906.py', '--founding', f'FIX={BOARD}')
ok('突然変異（--founding を名前ごとに戻す）：同名の盤面で 1 件以上に増える＝盤面が突然変異を殺す', '）0・国×四半期 0' not in zm and 'k同名X' in zm)
d = mutated('gdp_jump906.py', "if q - first[u] <= K: found.append(row)\n                        elif FOUNDING", "if thin(rp): thinr.append(row)\n                        elif q - first[u] <= K: found.append(row)\n                        elif FOUNDING")
jm = run(d, 'gdp_jump906.py', '--founding', '--rows', f'FIX={BOARD}')
ok('突然変異（痩せを建国の直後より先に判定）：優先の盤面で 建国の直後が 0 になる', '建国の直後〔≦4 四半期〕0・' in jm)
shutil.rmtree(W, ignore_errors=True)
sys.exit(0 if all(res) else 1)
