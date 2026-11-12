"""(1067) C：NG の件の年の分布（読むだけ）。measurements/ng*/ の全てのフォルダを ngset192 の規則で集合にし、
各の件（セル）の年（キーか値の最初の 4 桁の年・窓は始まりの年）を 1914 年より前か後かで数える。
python -X utf8 c_ngyears.py <リポジトリ> <出力>"""
import sys, os, re, glob, collections
repo, out = sys.argv[1], sys.argv[2]
src = open(os.path.join(repo, 'scripts/instruments/m192/ngset192.py'), encoding='utf-8').read()
src = src.split('\nA = load(sys.argv[1])')[0]
g = {'__name__': 'ngset'}; exec(compile(src, 'ngset192', 'exec'), g)
load, ngs = g['load'], g['ngs']
KF = set(g['KINDS']) | {'mil180', 'lawmatch', 'gate'}


def year_of(key, val):
    for x in list(key[1:]) + [val]:
        m = re.search(r'(1[78]\d\d|19[0-5]\d)', str(x))
        if m: return int(m.group(1))
    return None


rows = []
allcells = {}
for d in sorted(glob.glob(os.path.join(repo, 'measurements', 'ng*'))):
    if not os.path.isdir(d): continue
    tags = collections.Counter()
    for f in os.listdir(d):
        m = re.match(r'^([a-z0-9]+)_(.+)\.txt$', f)
        if m and m.group(1) in KF: tags[m.group(2)] += 1
    for tag, n in tags.items():
        if n < 5: continue
        try: S = ngs(load(f'rd:{d}:{tag}'))
        except Exception as e: rows.append((os.path.basename(d), tag, 'ERR', str(e)[:60])); continue
        pre = post = none = 0
        for k, v in S.items():
            y = year_of(k, v)
            if y is None: none += 1
            elif y < 1914: pre += 1
            else: post += 1
            allcells.setdefault(k, y)
        rows.append((os.path.basename(d), tag, len(S), pre, post, none))
ok = [r for r in rows if r[2] != 'ERR']
with open(out, 'w', encoding='utf-8') as fo:
    fo.write('# フォルダ\tタグ\tNG の件\t1914 年より前\t1914 年以後\t年なし\n')
    for r in rows: fo.write('\t'.join(str(x) for x in r) + '\n')
    tot = sum(r[2] for r in ok); pre = sum(r[3] for r in ok); post = sum(r[4] for r in ok); no = sum(r[5] for r in ok)
    fo.write(f'## 合計（{len(ok)} 組・誤り {len(rows) - len(ok)}）：件 {tot}・前 {pre}・後 {post}・年なし {no}・後の割合 {post / max(tot, 1):.3f}\n')
    ys = collections.Counter(y for y in allcells.values() if y is not None)
    fo.write(f'## 一度でも出た異なる件 {len(allcells)}：1914 年より前 {sum(1 for y in allcells.values() if y is not None and y < 1914)}・以後 {sum(1 for y in allcells.values() if y is not None and y >= 1914)}・年なし {sum(1 for y in allcells.values() if y is None)}\n')
    dec = collections.Counter((y // 10) * 10 for y in allcells.values() if y is not None)
    fo.write('## 異なる件の年代（10 年刻み）: ' + '  '.join(f'{k}:{dec[k]}' for k in sorted(dec)) + '\n')
    byk = collections.Counter((k[0], 'pre' if (y is not None and y < 1914) else 'post' if y is not None else 'none') for k, y in allcells.items())
    fo.write('## 種類ごと（異なる件）: ' + '  '.join(f'{a}/{b}:{c}' for (a, b), c in sorted(byk.items())) + '\n')
print('done', len(rows))
