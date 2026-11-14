# (1134) 台帳の S24 の行の錨（file:line:名）が木で引けるかを数え、引けない物は名の宣言の行を探す（読むだけ）
import sys, os, re
REPO = sys.argv[1]; ROWS = sys.argv[2]
sys.stdout.reconfigure(newline='\n')
SIM = os.path.join(REPO, 'PriceOfBread.SimCore')
cache = {}
def lines(fn):
    if fn not in cache:
        p = os.path.join(SIM, fn)
        cache[fn] = open(p, encoding='utf-8-sig').read().split('\n') if os.path.exists(p) else None
    return cache[fn]
coef_files = sorted(f for f in os.listdir(SIM) if f.startswith('Coefficients') and f.endswith('.cs'))
def find_decl(name):
    out = []
    for f in coef_files + ['Diplomacy.cs', 'Simulation.cs', 'MultiNationSim.cs', 'Landmass.cs', 'World.cs', 'Military.cs']:
        L = lines(f) or []
        for i, l in enumerate(L):
            if re.search(r'\b' + re.escape(name) + r'\b\s*(=|\{|;|\()', l) and ('public' in l or 'internal' in l or 'private' in l or 'class' in l):
                out.append(f'{f}:{i+1}:{name}'); break
        if out: break
    return out[0] if out else None
ok = bad = 0
for row in open(ROWS, encoding='utf-8'):
    c = [x.strip() for x in row.strip().strip('|').split('|')]
    rid, anc = c[0], c[4]
    res = []
    for a in anc.split(';'):
        a = a.strip()
        m = re.match(r'([^:]+\.cs):(\d+):(.+)$', a)
        if not m: res.append(f'{a}=＝錨なし'); continue
        fn, ln, nm = m.group(1), int(m.group(2)), m.group(3)
        L = lines(fn); key = re.split(r'[(\s]', nm)[0]
        hit = L is not None and ln - 1 < len(L) and key in L[ln - 1]
        if hit: ok += 1; res.append(f'{a}=OK')
        else:
            bad += 1; nd = find_decl(key); res.append(f'{a}=NG→{nd}')
    print(rid + '\t' + ' ; '.join(res))
print(f'# OK {ok}  NG {bad}')
