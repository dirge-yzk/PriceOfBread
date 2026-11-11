# 読むだけ：AI のプレイテストの前に確かめ直す行を選ぶ。
#  行の〈最後に確かめたコミット〉から、持ち主のファイルが変わった行（git diff --name-only <コミット>＝作業ツリーとの差＝未コミットも含む）と、
#  錨が Coefficients.cs の行で、その名が Coefficients.cs の差分の行（+／−）に出る行を出す。最後に確かめたコミットが〈—〉の行は数だけ出す。
# 使い方: python -X utf8 changed_rows.py <repo か worktree> <台帳 md>
import sys, subprocess, re, collections
tree, md = sys.argv[1:3]
def git(*a): return subprocess.run(['git', '-C', tree, *a], capture_output=True, text=True, encoding='utf-8', errors='replace').stdout
rows = []
for l in open(md, 'rb').read().decode('utf-8').split('\n'):
    if not re.match(r'^\| S\d\d-', l): continue
    c = [x.strip() for x in l.strip().strip('|').split('|')]
    if len(c) != 11: continue
    rows.append(dict(id=c[0], anchor=c[4], owner=c[5], last=c[7]))
unver = sum(1 for r in rows if r['last'] in ('—', ''))
by_commit = collections.defaultdict(list)
for r in rows:
    if r['last'] not in ('—', ''): by_commit[r['last']].append(r)
out = []
for commit, rs in by_commit.items():
    names = {p.split('/')[-1] for p in git('diff', '--name-only', commit).split('\n') if p}
    coef_diff = [l[1:] for l in git('diff', '-U0', commit, '--', 'PriceOfBread.SimCore/Coefficients.cs').split('\n') if l[:1] in '+-' and not l.startswith(('+++', '---'))]
    for r in rs:
        owners = set(r['owner'].split()) if r['owner'] not in ('—', '') else set()
        hit = sorted(owners & names)
        coefnames = [a.split(':')[2] for a in r['anchor'].split(';') if a.count(':') >= 2 and a.split(':')[0] == 'Coefficients.cs']
        chit = [n for n in coefnames if any(re.search(r'\b' + re.escape(n) + r'\b', l) for l in coef_diff)]
        if hit or chit: out.append((r['id'], commit, ' '.join(hit), ' '.join(chit)))
print(f'確かめ直す行 {len(out)}（最後に確かめたコミットのある行 {len(rows) - unver}・未確かめの行 {unver}）')
for o in out: print('\t'.join(o))
