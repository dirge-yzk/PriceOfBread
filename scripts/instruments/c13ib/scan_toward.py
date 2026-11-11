# (812) c13ib 時間 doc 1-7 の形 A（x += k*(t-x)）の座標の走査（読むだけ）。
# 使い方：python scan_toward.py <SimCore のフォルダ> [--write-plan <出力 tsv>]
# 形 A の 2 つの書き方だけを拾う（どちらも IEEE で x + k*(t-x) と同じ値）：
#   X += K * (T - X);        → Toward(X, T, K)       （K は * を含んでよい：a*b*(t-x) は (a*b)*(t-x)）
#   X += (T - X) * K;        → Toward(X, T, K)       （K は * を含まない単項だけ：(t-x)*a*b は ((t-x)*a)*b で別の丸め）
# 拾わない物：X が型つき（Rate・Money など）／T に X が残る（引く側が X そのものでない）／行に複数の文。
import re, sys, os

root = sys.argv[1]
out = None
if '--write-plan' in sys.argv:
    out = sys.argv[sys.argv.index('--write-plan') + 1]

# X：識別子と . と [ ] だけ（呼び出しや算術を含む左辺は拾わない）
LHS = r'(?P<x>[A-Za-z_][A-Za-z0-9_\.\[\]]*)'
A = re.compile(r'^(?P<ind>\s*)(?P<pre>(?:else\s+|if\s*\([^;{}]*\)\s*)?)' + LHS + r'\s*\+=\s*(?P<k>[^;=]+?)\s*\*\s*\((?P<t>[^;]+?)\s*-\s*(?P=x)\s*\)\s*;(?P<tail>.*)$')
B = re.compile(r'^(?P<ind>\s*)(?P<pre>(?:else\s+|if\s*\([^;{}]*\)\s*)?)' + LHS + r'\s*\+=\s*\((?P<t>[^;]+?)\s*-\s*(?P=x)\s*\)\s*\*\s*(?P<k>[A-Za-z_][A-Za-z0-9_\.]*)\s*;(?P<tail>.*)$')

rows = []
for dp, dn, fn in os.walk(root):
    if '\\obj' in dp or '/obj' in dp or '\\bin' in dp or '/bin' in dp:
        continue
    for f in fn:
        if not f.endswith('.cs'):
            continue
        p = os.path.join(dp, f)
        with open(p, 'rb') as fh:
            data = fh.read().decode('utf-8')
        for i, line in enumerate(data.split('\n'), 1):
            s = line.rstrip('\r')
            # 行の先頭が文のとき（コメントや複数文の行は除く）
            code = s.split('//')[0] if '//' in s else s
            if code.count(';') != 1:
                continue
            m = A.match(code)
            form = 'A'
            if not m:
                m = B.match(code)
                form = 'B'
            if not m:
                continue
            rows.append((os.path.relpath(p, root).replace('\\', '/'), i, form, m.group('x'), m.group('k').strip(), m.group('t').strip(), s.strip()[:160]))

rows.sort()
for r in rows:
    print('\t'.join(str(c) for c in r))
print('# total', len(rows), file=sys.stderr)
if out:
    with open(out, 'wb') as fh:
        fh.write(('\n'.join('\t'.join(str(c) for c in r) for r in rows) + '\n').encode('utf-8'))
