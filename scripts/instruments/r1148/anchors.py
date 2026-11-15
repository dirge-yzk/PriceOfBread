# (1148) 台帳の S26 の行の錨（file:line:名）が木で引けるかを数え、引けない物は同じファイルで名の最も近い行、無ければ全域の宣言を探す（読むだけ）
import sys, os, re
REPO = sys.argv[1]; ROWS = sys.argv[2]
sys.stdout.reconfigure(newline='\n')
SIM = os.path.join(REPO, 'PriceOfBread.SimCore')
files = {}
for root, _, fs in os.walk(SIM):
    if os.sep + 'bin' in root or os.sep + 'obj' in root: continue
    for f in fs:
        if f.endswith('.cs'): files.setdefault(f, os.path.join(root, f))
cache = {}
def lines(fn):
    if fn not in cache:
        p = files.get(os.path.basename(fn))
        cache[fn] = open(p, encoding='utf-8-sig').read().split('\n') if p else None
    return cache[fn]
def nearest(fn, ln, key):
    L = lines(fn)
    if not L: return None
    best = None
    for i, l in enumerate(L):
        if key in l:
            d = abs(i + 1 - ln)
            if best is None or d < best[0]: best = (d, i + 1)
    return f'{fn}:{best[1]}:{key}' if best else None
def decl(key):
    for f in sorted(files):
        L = lines(f)
        for i, l in enumerate(L):
            if re.search(r'\b' + re.escape(key) + r'\b', l) and re.search(r'(public|internal|private|static|const)', l):
                return f'{f}:{i+1}:{key}'
    return None
ok = bad = 0
for row in open(ROWS, encoding='utf-8'):
    n, r = row.rstrip('\n').split('\t', 1)
    c = [x.strip() for x in r.split('|')]
    rid, anc = c[1], c[5]
    res = []
    for a in anc.split(';'):
        a = a.strip()
        m = re.match(r'([^:]+\.cs):(\d+)(?::(.+))?$', a)
        if not m: res.append(f'{a}=錨なし'); continue
        fn, ln, nm = m.group(1), int(m.group(2)), (m.group(3) or '')
        key = re.split(r'[(\s=]', nm.strip())[0] if nm else ''
        L = lines(fn)
        hit = L is not None and ln - 1 < len(L) and key and key in L[ln - 1]
        if hit: ok += 1; res.append(f'{a}=OK')
        else:
            bad += 1
            nd = (nearest(fn, ln, key) if key else None) or (decl(key) if key else None)
            res.append(f'{a}=NG→{nd}')
    print(rid + '\t' + ' ; '.join(res))
print(f'# OK {ok}  NG {bad}')
