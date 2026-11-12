import sys, os, re, collections
repo = sys.argv[1]
src = open(os.path.join(repo, 'scripts/instruments/m192/ngset192.py'), encoding='utf-8').read().split('\nA = load(sys.argv[1])')[0]
g = {}; exec(compile(src, 'ngset192', 'exec'), g)
S = g['ngs'](g['load'](f'rd:{repo}/measurements/ng1053_20261112:m1053'))
c = collections.Counter()
for k, v in sorted(S.items()):
    ys = [int(m) for x in list(k[1:]) for m in re.findall(r'(1[78]\d\d|19[0-5]\d)', str(x))]
    win = ys[:2] if ys else []
    kind = k[0]
    if not ys: cls = 'none'
    elif ys[0] >= 1914: cls = 'post'
    elif len(ys) > 1 and ys[1] >= 1914 or (len(ys) == 1 and re.search(r'\d{4}-\d{2}\b', ' '.join(map(str, k))) and ys[0] >= 1905): cls = 'straddle'
    else: cls = 'pre'
    c[(kind, cls)] += 1
    if cls != 'pre': print(cls, kind, ' '.join(map(str, k[1:]))[:80])
print('## m1053 の NG 97 件の内訳（種類, 区分）:', dict(sorted(c.items())))
