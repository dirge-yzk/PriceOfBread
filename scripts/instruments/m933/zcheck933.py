# (933) 合流：腕 Z（ident m933z）の〈判定: 不一致〉の中身を、ident_check.py と別の台本で数え直す（読むだけ）。
#   ident_check.py は cearn_s の経路の列（outTop1Route・outTop2Route・inTop1Route）の〈ファイル:行〉を基準の木の行から測る木の行へ写すが、
#   写す道（cearn_same_but_location）は〈経路の外の列はバイトで一致〉を要る＝--known-extra の列（c81sm の heirDebtSettledPerQuarter）と同時には通らない。
#   この台本：基準（INSRAW）と腕の cearn_s を列ごとに比べ、違う欄を〈宣言の列〉〈経路の列で、行の写し（基準の木→測る木・difflib の変わらない行）で一致〉〈その他〉に分ける。
#   python -X utf8 -I zcheck933.py <基準の ins> <腕の ins> <repo> <基準のコミット> <測る木のコミット> <宣言の列,...>
import sys, os, re, subprocess, difflib, collections, glob
base, arm, repo, bc, tc, decl = sys.argv[1:7]
DECL = set(decl.split(','))
ROUTE = ('outTop1Route', 'outTop2Route', 'inTop1Route')
RE = re.compile(r'^([A-Za-z0-9_]+\.cs):(\d+)(.*)$', re.S)
maps = {}
def gshow(c, p):
    r = subprocess.run(['git', '-C', repo, 'show', f'{c}:PriceOfBread.SimCore/{p}'], capture_output=True)
    return r.stdout if r.returncode == 0 else None
def lmap(fn):
    if fn not in maps:
        a, b = gshow(bc, fn), gshow(tc, fn)
        if a is None or b is None:
            maps[fn] = {}
        elif a == b:
            maps[fn] = None
        else:
            la = [x.rstrip(b'\r') for x in a.split(b'\n')]; lb = [x.rstrip(b'\r') for x in b.split(b'\n')]
            m = {}
            for blk in difflib.SequenceMatcher(None, la, lb, autojunk=False).get_matching_blocks():
                for k in range(blk.size):
                    m[blk.a + k + 1] = blk.b + k + 1
            maps[fn] = m
    return maps[fn]
def remap(s):
    mo = RE.match(s)
    if not mo:
        return s
    m = lmap(mo.group(1))
    if m is None:
        return s
    ln = int(mo.group(2))
    return f'{mo.group(1)}:{m[ln]}{mo.group(3)}' if ln in m else s + '?'
tot = collections.Counter(); moved = collections.Counter(); other = []
files = sorted(glob.glob(os.path.join(base, 'cearn_s*.tsv')))
for pa in files:
    pb = os.path.join(arm, os.path.basename(pa))
    fa, fb = open(pa, encoding='utf-8'), open(pb, encoding='utf-8')
    ha = fa.readline().rstrip('\n').split('\t'); hb = fb.readline().rstrip('\n').split('\t')
    if ha != hb:
        other.append((os.path.basename(pa), '見出しが違う')); continue
    nr = 0
    for la, lb in zip(fa, fb):
        nr += 1
        if la == lb:
            continue
        ra = la.rstrip('\n').split('\t'); rb = lb.rstrip('\n').split('\t')
        for i, (x, y) in enumerate(zip(ra, rb)):
            if x == y:
                continue
            c = ha[i]
            if c in DECL:
                tot['宣言の列 ' + c] += 1
            elif c in ROUTE and remap(x) == y:
                tot['経路の列（行の写しで一致） ' + c] += 1; moved[(x, y)] += 1
            else:
                other.append((os.path.basename(pa), nr, c, x, y))
    if fa.readline() or fb.readline():
        other.append((os.path.basename(pa), '行の数が違う'))
print(f'# zcheck933 基準 {base}・腕 {arm}・基準の木 {bc}・測る木 {tc}・宣言 {sorted(DECL)}・表 {len(files)} 本')
for k, v in sorted(tot.items()):
    print(f'{k}\t{v}')
for (x, y), v in moved.most_common():
    print(f'経路の写し\t{x}\t→\t{y}\t{v}')
print(f'その他の違い\t{len(other)}')
for o in other[:20]:
    print('その他', *o, sep='\t')
print('判定（この台本）：' + ('違いは宣言の列と経路の行の写しだけ' if not other else '説明できない違いがある'))
