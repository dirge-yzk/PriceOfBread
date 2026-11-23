# (1330) c139sl 自己試験（合成の盤面）。期待は盤面の意図から手で書いた（台本の出力から写していない）。
#   使い方: python -X utf8 -I selftest1330.py [--dir <台本の置き場>]   緑なら rc=0・赤があれば rc=1
import os, subprocess, sys
A = sys.argv[1:]
HERE = os.path.dirname(os.path.abspath(__file__))
D = A[A.index('--dir') + 1] if '--dir' in A else HERE
FIX = os.path.join(HERE, 'fixture')
EXPECT = {
    'A': '建国の直後', 'B': '(i)人口の跳び', 'C': '(ii)0の区間の後', 'D': '(iii)産出が0の近く', 'E': 'どれにも当たらない',
    'F': 'どれにも当たらない', 'G': 'どれにも当たらない', 'H1': '(iii)産出が0の近く', 'H2': 'どれにも当たらない',
    'I1': '(i)人口の跳び', 'I2': 'どれにも当たらない', 'J': '(iii)産出が0の近く', 'K': '(i)人口の跳び', 'L': 'どれにも当たらない',
    'M': 'どれにも当たらない', 'N': 'どれにも当たらない', 'O': 'どれにも当たらない',
}
p = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(D, 'gdp_jump1330.py'), '--rows', f'M={FIX}'], capture_output=True, encoding='utf-8')
rows = {}
for l in p.stdout.split('\n'):
    if l.startswith('   行\t'):
        c = l.split('\t'); rows[c[2]] = c[4]
bad = 0
def ok(n, c, note=''):
    global bad
    print(('緑' if c else '赤'), n, note)
    if not c: bad += 1
for k, v in EXPECT.items(): ok(f'合成 {k}', rows.get(k) == v, f'期待 {v} 実 {rows.get(k)}')
ok('行の数 17', len(rows) == 17, f'実 {len(rows)}')
sm = [l for l in p.stdout.split('\n') if l.startswith('M\t種')]
ok('要約 建国 1・(i) 3・(ii) 1・(iii) 3・どれにも 9', bool(sm) and '建国の直後 1・(i)人口の跳び 3・(ii)0の区間の後 1・(iii)産出が0の近く 3・どれにも当たらない 9' in sm[0], sm[0] if sm else '(要約なし)')
sys.exit(1 if bad else 0)
