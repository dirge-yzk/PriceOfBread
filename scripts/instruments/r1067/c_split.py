"""(1067) C の 3 の数え直し（読むだけ）。
(1) 止める線の事象を、今の読み手の区分（gdpjump833〈それ以外〉・hol 1/10〔例外の名簿の外〕・popjump）と旧の読み手 gdp_jump797〈それ以外〉（分母の痩せを分けない）に分けて、異なる事象の 1914 年より前／以後を数える。
(2) NG の一度でも出た異なる件のうち m1053 の 97 件に無い物の年。
数え方：measurements にある全ての出力（基線・ヌル腕・腕 B を含む）で一度でも出た異なる事象＝基線に対して新しい件ではない。
python -X utf8 c_split.py <リポジトリ> <出力>"""
import sys, os, re, glob, collections
repo, out = sys.argv[1], sys.argv[2]
g = {'__name__': 'x'}; exec(compile(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'c_stopyears.py'), encoding='utf-8').read().split('\nwith open(out')[0].replace("root, out = sys.argv[1], sys.argv[2]", "root, out = sys.argv[1] + '/measurements', None"), 'c_stopyears', 'exec'), g)
ev = g['ev']
d = {}
for e in ev: d.setdefault((e[1], e[2], e[3][1], e[4]), e)


def cls(k):
    if k[0] == 'gdpjump:gdpjump833' and k[1] == 'それ以外': return '今の読み手：gdpjump833 それ以外'
    if k[0] == 'hol1/10' and '例外' not in k[1]: return '今の読み手：hol 1/10（例外の名簿の外）'
    if k[0] == 'popjump': return '今の読み手：popjump'
    if k[0] == 'gdpjump:gdpjump' and k[1] == 'それ以外': return '旧の読み手：gdp_jump797 それ以外（分母の痩せを含む）'
    if 'それ以外' in k[1]: return '記録の読み手：906/1002 それ以外'
    return None


c = collections.Counter((cls(k), k[3] < 1914) for k in d if cls(k))
src = open(os.path.join(repo, 'scripts/instruments/m192/ngset192.py'), encoding='utf-8').read().split('\nA = load(sys.argv[1])')[0]
h = {}; exec(compile(src, 'ngset192', 'exec'), h)
base = set(h['ngs'](h['load'](f'rd:{repo}/measurements/ng1053_20261112:m1053')))
allc = {}
for dd in sorted(glob.glob(os.path.join(repo, 'measurements', 'ng*'))):
    if not os.path.isdir(dd): continue
    tags = collections.Counter(m.group(2) for f in os.listdir(dd) for m in [re.match(r'^([a-z0-9]+)_(.+)\.txt$', f)] if m)
    for t, n in tags.items():
        if n < 5: continue
        for k, v in h['ngs'](h['load'](f'rd:{dd}:{t}')).items():
            if k in allc: continue
            ys = [int(x) for y in list(k[1:]) + [v] for x in re.findall(r'(1[78]\d\d|19[0-5]\d)', str(y))]
            allc[k] = ys[0] if ys else None
nb = {k: y for k, y in allc.items() if k not in base}
with open(out, 'w', encoding='utf-8') as fo:
    fo.write('# 数え方：measurements の全ての出力（基線・ヌル腕・腕 B を含む）で一度でも出た異なる事象（基線に対して新しい件ではない）\n')
    for name in sorted({k[0] for k in c}):
        fo.write(f'{name}\t1914 年より前 {c[(name, True)]}\t以後 {c[(name, False)]}\n')
    cur = [n for n in {k[0] for k in c} if n.startswith('今の')]
    pre = sum(c[(n, True)] for n in cur); post = sum(c[(n, False)] for n in cur)
    fo.write(f'## 今の読み手の止める線の区分の合計：前 {pre}・以後 {post}（以後の割合 {post / max(pre + post, 1):.3f}）\n')
    fo.write(f'## NG：一度でも出た異なる件 {len(allc)}・うち m1053 の 97 件に無い物 {len(nb)}：1914 年より前 {sum(1 for y in nb.values() if y is not None and y < 1914)}・以後 {sum(1 for y in nb.values() if y is not None and y >= 1914)}・年なし {sum(1 for y in nb.values() if y is None)}\n')
print('ok')
