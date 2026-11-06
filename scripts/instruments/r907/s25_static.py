# (907) c78lg 読むだけ：文書 §3-8 外交の見出しごとに、本文の識別子（括弧とバッククォートの英字の名）を木で引き、
#  宣言の行（Coefficients.cs が先）・既定の値・SimCore の読み手の数（Coefficients.cs と Instruments*.cs と宣言の行を除く）を表にする。
# 使い方: python -X utf8 s25_static.py <repo> <木> <doc_s25.txt> <開始の行番号> <出力 tsv>
import sys, re, subprocess, collections
repo, tree, doc, start, out = sys.argv[1:6]
start = int(start)
L = open(doc, 'rb').read().decode('utf-8').split('\n')
heads = [(i, l) for i, l in enumerate(L) if l.startswith('#### ')]
IDS = re.compile(r'(?<![A-Za-z0-9_])([A-Z][A-Za-z0-9]{3,}(?:\.[A-Z][A-Za-z0-9]+)?)(?![A-Za-z0-9_])')
SKIP = {'SimCore', 'Coefficients', 'World', 'Nation', 'Diplomacy', 'MultiNationSim', 'SelfPlay', 'Politics', 'Government'}
cache = {}
def grep(name):
    if name in cache: return cache[name]
    r = subprocess.run(['git', '-C', repo, 'grep', '-n', '-w', name, tree, '--', 'PriceOfBread.SimCore'], capture_output=True).stdout.decode('utf-8', 'replace')
    hits = []
    for l in r.split('\n'):
        if not l: continue
        p = l.split(':', 3)
        if len(p) < 4: continue
        hits.append((p[1].split('/')[-1], int(p[2]), p[3].strip()))
    cache[name] = hits
    return hits
def decl(hits, name):
    for f, n, t in hits:
        if f == 'Coefficients.cs' and re.search(r'\b' + re.escape(name) + r'\s*(=|\{|;)', t) and 'public' in t:
            m = re.search(re.escape(name) + r'\s*(?:\{[^}]*\})?\s*=\s*([^;]+);', t)
            return f'{f}:{n}', (m.group(1).strip() if m else '?')
    for f, n, t in hits:
        if re.search(r'(class|struct|enum|record|static|public|private|internal)[^=(]*\b' + re.escape(name) + r'\b', t):
            return f'{f}:{n}', ''
    return '', ''
rows = []
for k, (i, h) in enumerate(heads):
    e = heads[k + 1][0] if k + 1 < len(heads) else len(L)
    body = '\n'.join(L[i:e])
    names = []
    for m in IDS.finditer(body):
        nm = m.group(1).split('.')[-1]
        if nm in SKIP or nm in names: continue
        if not re.search(r'[a-z]', nm) or not re.search(r'[A-Z].*[A-Z]', nm) and len(nm) < 6: continue
        names.append(nm)
    for nm in names:
        hits = grep(nm)
        d, v = decl(hits, nm)
        rd = [x for x in hits if x[0] != 'Coefficients.cs' and not x[0].startswith('Instruments')]
        files = collections.Counter(x[0] for x in rd)
        rows.append([str(start + i), h[5:60], nm, d, v, str(len(rd)), ' '.join(f'{f}:{c}' for f, c in files.most_common(4)), ';'.join(f'{f}:{n}' for f, n, t in rd[:3])])
    if not names: rows.append([str(start + i), h[5:60], '—', '', '', '', '', ''])
open(out, 'wb').write(('\n'.join('\t'.join(r) for r in [['docL', 'head', 'name', 'decl', 'value', 'readers', 'files', 'first']] + rows) + '\n').encode('utf-8'))
print('見出し', len(heads), '行', len(rows), '名', len(cache), '木に無い名', sum(1 for v in cache.values() if not v))
