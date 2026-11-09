# (996) c92gf 自己試験（合成の盤面）。期待は盤面の意図から手で書いた（台本の出力から写していない）。
#   使い方: python -X utf8 -I selftest1002.py [--dir <台本の置き場>]   緑なら rc=0・赤があれば rc=1
import os, subprocess, sys
A = sys.argv[1:]
HERE = os.path.dirname(os.path.abspath(__file__))
D = A[A.index('--dir') + 1] if '--dir' in A else HERE
FIX = os.path.join(HERE, 'fixture')
EXPECT = {  # 国 -> (区分, 向き)
    'A': ('州の出入り', '上がり'), 'B': ('それ以外', '上がり'), 'C': ('それ以外', '上がり'), 'D': ('それ以外', '上がり'),
    'E': ('州の出入り', '上がり'), 'F': ('分母の痩せ', '上がり'), 'G': ('州の出入り', '上がり'), 'H': ('それ以外', '上がり'),
    'I': ('州の出入り', '下がり'), 'P': ('それ以外', '上がり'), 'Q': ('それ以外', '下がり'), 'R': ('それ以外', '上がり'), 'S': ('州の出入り', '上がり'),
}
p = subprocess.run([sys.executable, '-X', 'utf8', '-I', os.path.join(D, 'gdp_jump1002.py'), '--rows', f'M={FIX}'], capture_output=True, encoding='utf-8')
rows = {}
for l in p.stdout.split('\n'):
    if l.startswith('   行\t'):
        c = l.split('\t'); rows[c[2]] = (c[4], c[5][:3])
bad = 0
def ok(n, c, note=''):
    global bad
    print(('緑' if c else '赤'), n, note)
    if not c: bad += 1
for k, v in EXPECT.items(): ok(f'合成 {k}', rows.get(k) == v, f'期待 {v} 実 {rows.get(k)}')
ok('合成 K は建国の直後で区分の行に出ない', 'K' not in rows)
ok('行の数 13', len(rows) == 13, f'実 {len(rows)}')
sm = [l for l in p.stdout.split('\n') if l.startswith('M\t種')]
ok('要約の 建国の直後 1・州の出入り 4（上がり）・それ以外 5（B・C・D・H・P の上がり。Q は下がりで別の行）・分母の痩せ 1', bool(sm) and '建国の直後 1・' in sm[0] and '分母の痩せ 1・州の出入り 4・それ以外 5' in sm[0], sm[0][:120] if sm else '')
ok('〈それ以外〉≧100 は 6（B・C・D・H・P・Q）', '〈それ以外〉≧100（上がり・下がり） 6' in p.stdout)
sys.exit(1 if bad else 0)
