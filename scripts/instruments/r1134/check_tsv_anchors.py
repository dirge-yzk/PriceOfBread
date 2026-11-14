# (1134) TSV の錨（file:line:名）の行に名があるかを数える（読むだけ）
import re, os, sys
sys.stdout.reconfigure(newline='\n')
SIM = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore'; cache = {}
def L(f):
    if f not in cache:
        p = os.path.join(SIM, f); cache[f] = open(p, encoding='utf-8-sig').read().split('\n') if os.path.exists(p) else None
    return cache[f]
bad = n = 0
for l in open(sys.argv[1], encoding='utf-8').read().split('\n')[1:]:
    if not l: continue
    c = l.split('\t')
    for a in c[4].split(';'):
        m = re.match(r'([^:]+\.cs):(\d+):(.+)$', a)
        if not m: continue
        n += 1; f, k, nm = m.group(1), int(m.group(2)), m.group(3); key = re.split(r'[(\s.]', nm)[-1] if '.' in nm else re.split(r'[(\s]', nm)[0]
        LL = L(f)
        if LL is None or k > len(LL) or key not in LL[k - 1]: bad += 1; print('NG', c[0], a)
print('anchors', n, 'NG', bad)
