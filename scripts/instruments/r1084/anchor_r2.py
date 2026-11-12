# (1084) round2 の 9 系統の TSV の錨を、行の〈最後に確かめた〉の木と main decb50fa5 の両方で引く（読むだけ）
import subprocess, re, collections
REPO = 'C:/Users/dirge/Documents/PriceOfBread'
S = 'C:/Users/dirge/AppData/Local/Temp/claude/ee79ebbd2c0731cc/9b9a86e7-43dc-4b00-9074-443f5c41f431/scratchpad'
W = 'D:/PoB_work/r1084_work'
SYS = ['S03', 'S04', 'S06', 'S09', 'S13', 'S21', 'S26', 'S28', 'S29']
trees = {}
def tree(c):
    if c not in trees:
        r = subprocess.run(['git', '-C', REPO, 'ls-tree', '-r', '--name-only', c], capture_output=True, text=True)
        by = collections.defaultdict(list)
        for p in r.stdout.split('\n'):
            if p:
                by[p.split('/')[-1]].append(p)
        trees[c] = (r.returncode == 0, by, {})
    return trees[c]
def has(c, fn, ln, tok):
    ok, by, cache = tree(c)
    if not ok:
        return None
    cand = [p for p in by.get(fn, []) if p.startswith('PriceOfBread.SimCore/')] or by.get(fn, [])
    if not cand:
        return False
    p = cand[0]
    if p not in cache:
        cache[p] = subprocess.run(['git', '-C', REPO, 'show', f'{c}:{p}'], capture_output=True).stdout.decode('utf-8-sig', 'replace').split('\n')
    ls = cache[p]
    return 1 <= ln <= len(ls) and tok in ls[ln - 1]
out = open(W + '/round2_anchors.tsv', 'w', encoding='utf-8', newline='\n')
out.write('系統\t行の数\t最後に確かめた(種類)\t錨\t引けた(その木)\t引けた(decb50fa5)\t判定の列が語彙の外の行\n')
VOC = ('未確かめ', '働く', '一部だけ働く', '働かない', '文書が古い')
for sy in SYS:
    T = [t for t in open(f'{S}/ledger_{sy}_2026-11-12.tsv', 'rb').read().decode('utf-8').split('\n')[1:] if t.strip()]
    commits = collections.Counter()
    n = a = b = 0
    offvoc = 0
    for t in T:
        c = t.split('\t')
        commits[c[7]] += 1
        if c[8] not in VOC:
            offvoc += 1
        for x in c[4].split(';'):
            x = x.strip()
            m = re.match(r'^([^:]+):(\d+):(.+)$', x)
            if not m:
                continue
            fn, ln, tok = m.group(1).split('/')[-1], int(m.group(2)), m.group(3)
            n += 1
            r1 = has(c[7], fn, ln, tok) if re.match(r'^[0-9a-f]{7,}$', c[7]) else None
            if r1:
                a += 1
            if has('decb50fa5', fn, ln, tok):
                b += 1
    out.write(f'{sy}\t{len(T)}\t{dict(commits)}\t{n}\t{a}\t{b}\t{offvoc}\n')
out.close()
print(open(W + '/round2_anchors.tsv', encoding='utf-8').read())
