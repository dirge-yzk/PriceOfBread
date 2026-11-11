# (828) c66dw：cearn_s の経路の名（<ファイル>.cs:<行>:<向き>:<区分>）が、行を足しただけで変わった（呼び出し位置）ことを確かめる読み手（読むだけ）。
#   基準の木と腕の木の差のある .cs について、変わらない行どうしの行番号の対応（difflib）を作り、基準の表の経路の名の行番号を腕の木の番号へ写してから、全ての行をバイトで比べる。
#   使い方：python -I -X utf8 cearn_remap.py <基準の ins> <腕の ins> <リポジトリ> <基準のコミット> <腕のコミット>
import sys, os, re, glob, difflib, subprocess
base, arm, repo, c0, c1 = sys.argv[1:6]
files = subprocess.run(['git', '-C', repo, 'diff', '--name-only', c0, c1, '--', '*.cs'], capture_output=True, text=True, encoding='utf-8').stdout.split()
maps = {}
for f in files:
    if not f.startswith('PriceOfBread.SimCore/'): continue
    a = subprocess.run(['git', '-C', repo, 'show', f'{c0}:{f}'], capture_output=True, text=True, encoding='utf-8').stdout.split('\n')
    b = subprocess.run(['git', '-C', repo, 'show', f'{c1}:{f}'], capture_output=True, text=True, encoding='utf-8').stdout.split('\n')
    m = {}
    for blk in difflib.SequenceMatcher(None, a, b, autojunk=False).get_matching_blocks():
        for k in range(blk.size): m[blk.a + k + 1] = blk.b + k + 1
    maps[os.path.basename(f)] = m
print('写す .cs：', {k: sum(1 for x, y in v.items() if x != y) for k, v in maps.items()}, '（行番号が動いた行の数）')
pat = re.compile(r'([A-Za-z0-9_]+\.cs):(\d+)')
unmapped = 0
def remap(s):
    global unmapped
    def r(mo):
        global unmapped
        fn, ln = mo.group(1), int(mo.group(2))
        if fn not in maps: return mo.group(0)
        if ln not in maps[fn]: unmapped += 1; return mo.group(0) + '?'
        return f'{fn}:{maps[fn][ln]}'
    return pat.sub(r, s)
tot = same = moved = diff = 0
for p in sorted(glob.glob(os.path.join(base, 'cearn_s*.tsv'))):
    q = os.path.join(arm, os.path.basename(p))
    la = open(p, 'rb').read().decode('utf-8').split('\n'); lb = open(q, 'rb').read().decode('utf-8').split('\n')
    if len(la) != len(lb): print('行の数が違う', os.path.basename(p), len(la), len(lb)); diff += 1; continue
    for x, y in zip(la, lb):
        tot += 1
        if x == y: same += 1; continue
        if remap(x) == y: moved += 1; continue
        diff += 1
        if diff <= 5: print('違う', os.path.basename(p), repr(x[:200]), repr(y[:200]))
print(f'行 {tot}・そのまま一致 {same}・行番号を写して一致 {moved}・写しても違う {diff}・写せない番号 {unmapped}')
print('判定：' + ('呼び出し位置だけの違い' if diff == 0 and unmapped == 0 else '呼び出し位置の外の違いあり'))
