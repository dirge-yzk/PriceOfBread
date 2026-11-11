# (899) 合流：読む線・設計の材料の線の出力を木へ写す（バイトで読み、CRLF を LF にする・中身は変えない・反証役の訂正は末尾に足す）
#   python -X utf8 copy899.py <木の根>
import sys, os, glob
root = sys.argv[1]
W = 'D:/PoB_work'
M = W + '/m899'
pairs = []
def add(src, dst, tail=None): pairs.append((src, dst, tail))
for f in ['ledger_check.py', 'changed_rows.py', 'build_after893.py', 'mk_fixtures893.py', 'mutate893.py', 'patch_overview893.py', 'selftest893_all.sh', 'selftest_changed893.sh']:
    add(f'{W}/r893_work/tools/{f}', f'scripts/instruments/r893/{f}')
for p in sorted(glob.glob(f'{W}/r893_work/s15/*')):
    b = os.path.basename(p)
    if b == 'Coefficients_731d3de1.cs': continue   # 731d3de1 の Coefficients.cs の写し＝git show で戻せる
    add(p, f'measurements/m893_20261106/s15/{b}')
for p in sorted(glob.glob(f'{W}/r893_work/selftest/*.txt')):
    add(p, f'measurements/m893_20261106/selftest/{os.path.basename(p)}')
add(f'{W}/r893_work/plan.txt', 'measurements/m893_20261106/plan.txt')
add(f'{W}/r893_work/README.txt', 'measurements/m893_20261106/README_r893.txt')
for f in ['debt897.py', 'debt897b.py', 'debt897c.py', 'debt897d.py']:
    add(f'{W}/r897_work/{f}', f'scripts/instruments/r897/{f}')
for f in ['debt897.txt', 'out_M.txt', 'out_N.txt', 'out_b.txt', 'out_c.txt', 'out_d.txt', 'plan.txt']:
    add(f'{W}/r897_work/{f}', f'measurements/m897_20261106/{f}')
add(f'{W}/r897_work/20261106_人の居ないPOPの債務の扱い_案B_下書き.md', 'docs/202611/20261106/20261106_人の居ないPOPの債務の扱い_案B_下書き.md', f'{M}/fix_db899.md')
for f in ['addon898.py', 'cornint898.py', 'paper898.py', 'rentshare898.py', 'status898.py']:
    add(f'{W}/r898_work/r898/{f}', f'scripts/instruments/r898/{f}')
for f in ['addon898_out.txt', 'cornint898_out.txt', 'paper898_out.txt', 'rentshare898_out.txt', 'status898_out.txt']:
    add(f'{W}/r898_work/{f}', f'measurements/m898_20261106/{f}')
D = 'docs/202611/20261106/20261106_地主の政治の導き直しの段C-0′_盤面m873とC-1・D-0の素案'
add(f'{W}/r898_work/draft.md', D + '_下書き.md', f'{M}/fix_ds899.md')
add(f'{W}/r898_work/material.md', D + '_材料.md')
add(f'{M}/ledger_final899.md', 'docs/機能の台帳.md')
for src, dst, tail in pairs:
    b = open(src, 'rb').read().replace(b'\r\n', b'\n')
    if tail:
        if not b.endswith(b'\n'): b += b'\n'
        b += open(tail, 'rb').read().replace(b'\r\n', b'\n')
    d = os.path.join(root, dst)
    os.makedirs(os.path.dirname(d), exist_ok=True)
    if os.path.exists(d) and dst != 'docs/機能の台帳.md':
        raise SystemExit('既にある：' + dst)
    open(d, 'wb').write(b)
print('写した', len(pairs))
