# 門 4 の dep_s：基準の dep_s の callerLine を、前のコミット→後の木の行の対応（difflib・行の注を除いたコードの字で一致）で写して腕の dep_s と全行比べる（前例 m843・★(933)）。
# 使い方：python -X utf8 -I dep_remap.py <リポジトリ> <前のコミット> <基準の表のフォルダ> <腕の表のフォルダ>
import sys, os, re, glob, subprocess, difflib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from csscan import scan

repo, base, bdir, adir = sys.argv[1:5]
FILES = ['PriceOfBread.SimCore/Diplomacy.cs', 'PriceOfBread.SimCore/MultiNationSim.cs']


def code_lines(text):
    cs = scan(text)
    # 行ごとに注を除く（行をまたぐ /* */ は空白に）
    out = list(text)
    for k, s, e in cs:
        for i in range(s, e):
            if out[i] != '\n':
                out[i] = ' '
    return [l.strip() for l in ''.join(out).split('\n')]


maps = {}
basetext = {}
for f in FILES:
    a = subprocess.run(['git', '-C', repo, 'show', '%s:%s' % (base, f)], capture_output=True, check=True).stdout.decode('utf-8-sig')
    b = open(os.path.join(repo, f), 'rb').read().decode('utf-8-sig')
    la, lb = code_lines(a), code_lines(b)
    m = {}
    for blk in difflib.SequenceMatcher(None, la, lb, autojunk=False).get_matching_blocks():
        for k in range(blk.size):
            m[blk.a + k + 1] = blk.b + k + 1
    maps[f] = (m, la, lb)

bad = 0
rows_total = 0
remapped = 0
for p in sorted(glob.glob(os.path.join(bdir, 'dep_s[0-9]*.tsv'))):
    q = os.path.join(adir, os.path.basename(p))
    if not os.path.exists(q):
        print('腕に無い', q)
        bad += 1
        continue
    ra = open(p, 'rb').read().decode('utf-8').split('\n')
    rb = open(q, 'rb').read().decode('utf-8').split('\n')
    if len(ra) != len(rb):
        print('行数が違う', os.path.basename(p), len(ra), len(rb))
        bad += 1
        continue
    hdr = ra[0].split('\t')
    ci = hdr.index('callerLine')
    for i, (x, y) in enumerate(zip(ra, rb)):
        rows_total += 1
        if i == 0:
            if x != y:
                print('見出しが違う', p)
                bad += 1
            continue
        c = x.split('\t')
        if len(c) <= ci or not c[ci].isdigit():
            if x != y:
                bad += 1
                print('違う行', os.path.basename(p), i)
            continue
        ln = int(c[ci])
        cands = [f for f in FILES if ln in maps[f][0] and 'SetDependency' in maps[f][1][ln - 1]]
        # 呼び出しの 1 行に SetDependency が無い（複数行の呼び出し）場合は両方の候補の写像が一致する物だけ
        if not cands:
            cands = [f for f in FILES if ln in maps[f][0]]
        news = set(maps[f][0][ln] for f in cands)
        if len(news) != 1:
            print('写せない', os.path.basename(p), i, ln)
            bad += 1
            continue
        nw = news.pop()
        assert all(maps[f][1][ln - 1] == maps[f][2][nw - 1] for f in cands), (ln, nw)
        c[ci] = str(nw)
        if '\t'.join(c) != y:
            bad += 1
            print('違う行', os.path.basename(p), i, ln)
        elif c[ci] != str(ln):
            remapped += 1
print('rows=%d remapped=%d bad=%d' % (rows_total, remapped, bad))
sys.exit(1 if bad else 0)
