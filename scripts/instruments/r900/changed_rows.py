# 読むだけ：AI のプレイテストの前に確かめ直す行を選ぶ。（(886) c75lg の台本を (893) c76lg が写して係数名の拾いを広げた・(900) c77lg が 12 列の行を読むようにした）
#  行の〈最後に確かめたコミット〉から、
#   ①持ち主のファイルが変わった行（git diff --name-only <コミット>＝作業ツリーとの差＝未コミットも含む）
#   ②その行が読む係数の宣言が Coefficients.cs の差分の行（+／−）にある行
#     係数の名＝錨が Coefficients.cs の行ならその名（(886) と同じ）＋錨の行（木 <最後に確かめたコミット> のその行）に出る c.X・Coef.X・coef.X の X（(893) で足した）
#     宣言の行＝差分の行で、その名の直後に = か { か ; が来る行（注の中の名は拾わない）
#  を出す。最後に確かめたコミットが〈—〉の行は数だけ出す。
# 使い方: python -X utf8 changed_rows.py <repo か worktree> <台帳 md>
import sys, subprocess, re, collections
tree, md = sys.argv[1:3]
def git(*a): return subprocess.run(['git', '-C', tree, *a], capture_output=True, text=True, encoding='utf-8', errors='replace').stdout
rows = []
for l in open(md, 'rb').read().decode('utf-8').split('\n'):
    if not re.match(r'^\| S\d\d-', l): continue
    c = [x.strip() for x in l.strip().strip('|').split('|')]
    if len(c) not in (11, 12): continue   # (900) 12 列目〈直す／消す／残す〉を足した台帳も読む（r893 の版は 11 列だけ＝12 列の台帳で行が 0 になる）
    rows.append(dict(id=c[0], anchor=c[4], owner=c[5], last=c[7]))
unver = sum(1 for r in rows if r['last'] in ('—', ''))
by_commit = collections.defaultdict(list)
for r in rows:
    if r['last'] not in ('—', ''): by_commit[r['last']].append(r)
files = git('ls-tree', '-r', '--name-only', 'HEAD').split('\n')
byname = collections.defaultdict(list)
for p in files:
    if p: byname[p.split('/')[-1]].append(p)
def resolve(fn):
    c = [p for p in byname.get(fn, []) if p.startswith('PriceOfBread.SimCore/')] or byname.get(fn, [])
    return c[0] if c else None
showcache = {}
def line_at(commit, fn, ln):
    p = resolve(fn) if '/' not in fn else fn
    if p is None: return ''
    k = (commit, p)
    if k not in showcache:
        showcache[k] = subprocess.run(['git', '-C', tree, 'show', f'{commit}:{p}'], capture_output=True).stdout.decode('utf-8-sig', 'replace').split('\n')
    ls = showcache[k]
    return ls[ln - 1] if 1 <= ln <= len(ls) else ''
COEF = re.compile(r'\b(?:c|Coef|coef)\.([A-Z]\w*)')
out = []
for commit, rs in by_commit.items():
    names = {p.split('/')[-1] for p in git('diff', '--name-only', commit).split('\n') if p}
    if any(n.startswith('Coefficients.') and n.endswith('.cs') for n in names): names.add('Coefficients.cs')   # (1106) 部品の変更は Coefficients.cs の持ち主へ
    coef_diff = [l[1:] for l in git('diff', '-U0', commit, '--', 'PriceOfBread.SimCore/Coefficients*.cs').split('\n') if l[:1] in '+-' and not l.startswith(('+++', '---'))]
    for r in rs:
        owners = set(r['owner'].split()) if r['owner'] not in ('—', '') else set()
        hit = sorted(owners & names)
        coefnames = []
        for a in r['anchor'].split(';'):
            a = a.strip(); m = re.match(r'^([^:]+):(\d+):(.+)$', a)
            if not m: continue
            fn, ln, tok = m.group(1), int(m.group(2)), m.group(3)
            if fn.startswith('Coefficients') and fn.endswith('.cs'): coefnames.append(tok.strip())
            else: coefnames += COEF.findall(line_at(commit, fn, ln))
        coefnames = sorted(set(coefnames))
        chit = [n for n in coefnames if any(re.search(r'\b' + re.escape(n) + r'\b\s*(=|\{|;)', l) for l in coef_diff)]
        if hit or chit: out.append((r['id'], commit, ' '.join(hit), ' '.join(chit)))
print(f'確かめ直す行 {len(out)}（最後に確かめたコミットのある行 {len(rows) - unver}・未確かめの行 {unver}）')
for o in out: print('\t'.join(o))
