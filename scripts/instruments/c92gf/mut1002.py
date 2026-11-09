# (996) c92gf 突然変異：使い捨ての写し（D:/PoB_work/c92gf_work/mut_<名>）に書き換えを入れ、selftest1002 を回す（赤が期待）。
#   使い方: python -X utf8 -I mut1002.py <M1..M7> [--real]   --real は実データのシャムと UK を gates1002f の G2・G3 の行で読む（任意）。
import os, shutil, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
F = 'gdp_jump1002.py'
M = {  # 名: [(元, 後)…]（元は 1 回だけ出る行）
 'M1': [("and math.isfinite(cr) and LO <= cr <= HI", "and math.isfinite(tot) and LO <= tot <= HI")],
 'M2': [("elif moved: kind", "elif False: kind")],
 'M3': [("LO = 0.1 ", "LO = 0.0 "), ("HI = 10.0\n", "HI = math.inf\n")],
 'M4': [("a = reg.get((e['pq'], e['pidx']), {})", "a = reg.get((e['pq'], e['cidx']), {})")],
 'M5': [("COLS = ('factoryOutputRefValuePerMonth', 'extractionOutputRefValuePerMonth')", "COLS = ('factoryOutputRefValuePerMonth',)")],
 'M6': [("moved = (set(a) != set(b)) and", "moved = True and")],
 'M7': [("and LO <= cr <= HI and ok3", "and LO <= cr <= HI")],   # (1002) 条件 (iii) を外す→P・Q が州の出入りへ逃げる
}
n = sys.argv[1]
dst = f'D:/PoB_work/c92gf_work/mut_{n}'
shutil.rmtree(dst, ignore_errors=True); shutil.copytree(HERE, dst)
p = os.path.join(dst, F); s = open(p, 'rb').read().decode('utf-8')
for a, b in M[n]:
    assert s.count(a) == 1, (n, a, s.count(a)); s = s.replace(a, b)
open(p, 'wb').write(s.encode('utf-8'))
o = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(HERE, 'selftest1002.py'), '--dir', dst], capture_output=True, encoding='utf-8')
print(o.stdout, end='')
print(f'{n} selftest rc={o.returncode}（赤＝1 が期待）')
