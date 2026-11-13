# (1116) 合流の予言の材料：基準の生（keep_1103/base_ins）の cearn_s・cpop_s の経路の名〈ファイル:行〉を、
#   ident_check.py の _cearn_map と同じ写像（difflib・行全体の一致・\r を除く）で BASE_COMMIT 62693feb6 → 合わせた木（各ファイルはそれを変えた枝の版）へ写し、写せない名を数える。
#   python -X utf8 routepre.py
import csv, os, re, subprocess, difflib, sys, collections
csv.field_size_limit(1 << 30)
R = 'C:/Users/dirge/Documents/PriceOfBread'; BASE = '62693feb6'; RAW = 'D:/PoB_work/keep_1103/base_ins'
BR = {b: f'origin/c107{b}' for b in ('di', 'ha', 'hb', 'hc')}
owner = {}
for b in BR:
    for ln in open(f'D:/PoB_work/m1116_work/tmp_c107{b}.txt', encoding='utf-8'):
        owner[ln.strip()] = BR[b]
def g(args):
    r = subprocess.run(['git', '-C', R] + args, capture_output=True); return r.stdout if r.returncode == 0 else None
paths = collections.defaultdict(list)
for ln in g(['ls-tree', '-r', '--name-only', BASE, '--', 'PriceOfBread.SimCore']).decode().splitlines():
    if ln.endswith('.cs'): paths[os.path.basename(ln)].append(ln)
RE = re.compile(r'^([A-Za-z0-9_]+\.cs):(\d+)(.*)$', re.S)
names = collections.Counter()
for f in sorted(os.listdir(RAW)):
    t = 'cearn' if f.startswith('cearn_s') else 'cpop' if f.startswith('cpop_s') else None
    if not t: continue
    cols = ('outTop1Route', 'outTop2Route', 'inTop1Route') if t == 'cearn' else ('outOtherTop1Route',)
    with open(os.path.join(RAW, f), encoding='utf-8', newline='') as fh:
        rd = csv.reader(fh, delimiter='\t'); h = next(rd); ix = [i for i, c in enumerate(h) if c in cols]
        for row in rd:
            for i in ix:
                if i < len(row):
                    mo = RE.match(row[i])
                    if mo: names[(t, mo.group(1), int(mo.group(2)))] += 1
maps = {}
import importlib.util
K = (lambda s: s)
if len(sys.argv) > 1 and sys.argv[1] == 'key':
    sp = importlib.util.spec_from_file_location('ic', 'D:/PoB_work/wt_m1116/scripts/instruments/ident/ident_check.py'); ic = importlib.util.module_from_spec(sp); sp.loader.exec_module(ic); K = ic._code_key
def mp(fn):
    if fn in maps: return maps[fn]
    ps = paths.get(fn, [])
    if len(ps) != 1: maps[fn] = ('nopath', None); return maps[fn]
    p = ps[0]; a = g(['show', f'{BASE}:{p}'])
    # 合わせた木の版：p を変えた枝（di は SimCore/Display を移す＝経路は Display に無い）
    br = owner.get(p); bb = g(['show', f'{br}:{p}']) if br else a
    if bb is None: maps[fn] = ('gone', None); return maps[fn]
    if a == bb: maps[fn] = ('same', None); return maps[fn]
    la = [K(x.rstrip(b'\r')) for x in a.split(b'\n')]; lb = [K(x.rstrip(b'\r')) for x in bb.split(b'\n')]
    m = {}
    for blk in difflib.SequenceMatcher(None, la, lb, autojunk=False).get_matching_blocks():
        for k in range(blk.size): m[blk.a + k + 1] = blk.b + k + 1
    maps[fn] = (br, m); return maps[fn]
out = collections.Counter(); bad = []
for (t, fn, ln), c in sorted(names.items()):
    st, m = mp(fn)
    if m is None: out[(t, st)] += c; continue
    if ln in m: out[(t, st + (':moved' if m[ln] != ln else ':same'))] += c
    else: out[(t, st + ':UNMAPPED')] += c; bad.append((t, fn, ln, c, st))
for k, v in sorted(out.items()): print(k, v)
print('distinct names', len(names), 'unmapped distinct', len(bad))
for b in bad: print('UNMAPPED', *b)
