# (1068) (a) 〈報告の字を読む所〉の確かめ：Display の差分で変わった文字列のリテラルを、Display と App の外で誰が読むか。
# 使い方: python label_readers.py <diff のファイル> <木 main> <木 uf> <リポジトリの根>
# 限界（自己申告）: 日本語を含むリテラルの補間の外の断片だけ。.cs は引用符ごとの一致（"字"）・台本と成績表は 4 字以上か「年金」の素の一致（雑音を含む上界）。成績表は measurements/ の m1053 の名の付く .txt だけを見る。
import os, re, sys
diff, tm, tu, repo = sys.argv[1:5]
LIT = re.compile(r'"((?:[^"\\]|\\.)*)"')
JA = re.compile(r'[぀-ヿ一-鿿]{2,}')
removed, added = set(), set()
for line in open(diff, 'rb').read().decode('utf-8', 'replace').split('\n'):
    if line.startswith(('---', '+++')):
        continue
    if line.startswith(('-', '+')):
        for m in LIT.finditer(line):
            for piece in re.split(r'\{[^}]*\}', m.group(1)):
                if JA.search(piece) and len(piece.strip()) >= 2:
                    (removed if line[0] == '-' else added).add(piece.strip())
gone = removed - added
new = added - removed
print('消えた語', len(gone), '足された語', len(new))

def scan(root, sub, exts):
    base = os.path.join(root, sub)
    for d, _, fs in os.walk(base):
        if os.sep + 'obj' in d or os.sep + 'bin' in d:
            continue
        for f in fs:
            if f.endswith(exts):
                p = os.path.join(d, f)
                try:
                    yield p, open(p, 'rb').read().decode('utf-8', 'replace')
                except OSError:
                    pass

targets = []
for tree, tag in ((tm, 'main'), (tu, 'uf')):
    for sub in ('PriceOfBread.Tests', 'PriceOfBread.Runner', 'PriceOfBread.SimCore'):
        for p, t in scan(tree, sub, ('.cs',)):
            if os.sep + 'Display' + os.sep in p:
                continue
            targets.append((f'{tag}:{sub}:{os.path.basename(p)}', t))
for p, t in scan(repo, 'scripts', ('.py', '.sh', '.tsv', '.md')):
    targets.append(('scripts:' + os.path.relpath(p, repo), t))
mdir = os.path.join(repo, 'measurements')
for f in os.listdir(mdir):
    if 'm1053' in f and f.endswith('.txt'):
        targets.append(('meas:' + f, open(os.path.join(mdir, f), 'rb').read().decode('utf-8', 'replace')))
print('見た所', len(targets))
hits = {}
for w in sorted(gone | new):
    hs = [n for n, t in targets if (('"' + w + '"') in t if n.split(':')[0] in ('main', 'uf') else (len(w) >= 4 or w == '年金') and w in t)]
    if hs:
        hits[w] = hs
for w in sorted(gone):
    print('消えた', w, len(hits.get(w, [])), ' '.join(hits.get(w, [])[:6]))
print('---- 足された語で外に読み手がいる物')
for w in sorted(new):
    if w in hits:
        print('足された', w, len(hits[w]), ' '.join(hits[w][:6]))
