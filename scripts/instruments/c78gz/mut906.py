# (906) 突然変異：使い捨ての写し（D:/PoB_work/c78gz_work/mut_<名>）に 1 行だけ書き換え、selftest906 を回す。使い方: python -X utf8 -I mut906.py <名>
import os, shutil, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
M = {  # 名: (ファイル, 元, 後)
 'M1': ('gdp_zero906.py', "I0 = 0 if FOUNDING else 4", "I0 = 0"),
 'M2a': ('gdp_jump906.py', "ZMIN = 4 ", "ZMIN = 5 "),
 'M2b': ('gdp_jump906.py', "ZMIN = 4 ", "ZMIN = 3 "),
 'M2c': ('gdp_jump906.py', "WIN = 8 ", "WIN = 7 "),
 'M2d': ('gdp_jump906.py', "WIN = 8 ", "WIN = 9 "),
 'M2k-': ('gdp_jump906.py', "K = 4 ", "K = 3 "),
 'M2k+': ('gdp_jump906.py', "K = 4 ", "K = 5 "),
 'M3': ('gdp_jump906.py', "JUMP = 100.0", "JUMP = 10.0"),
 'M4': ('gdp_jump906.py', "if u not in first: first[u] = q", "if u not in first: first[u] = 0"),
}
n = sys.argv[1]; f, a, b = M[n]
dst = f'D:/PoB_work/c78gz_work/mut_{n}'
shutil.rmtree(dst, ignore_errors=True); shutil.copytree(HERE, dst)
p = os.path.join(dst, f); s = open(p, 'rb').read().decode('utf-8')
assert s.count(a) == 1, (n, s.count(a)); open(p, 'wb').write(s.replace(a, b).encode('utf-8'))
# 写しは repo の外＝fixture を元の場所の物にする
o = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(HERE, 'selftest906.py'), '--dir', dst, '--gates', (sys.argv[2] if len(sys.argv) > 2 else 'abcd')], capture_output=True, encoding='utf-8')
print(o.stdout)
