# 読むだけ：機能の台帳の門（両側）を確かめる。
#  門1＝全ての錨（file:line:名）が、木 <commit> のそのファイルのその行に名を含む（引けない錨 0）
#  門2＝判定を埋めた行（未確かめ・この波の線が埋める 以外）は判定の型と根拠を持ち、根拠が 9-26 の記号だけではない
#  門3＝確かめた系統（最後に確かめたコミットのある行の系統）の行の数と、書き手の表（2-1）の行の数を並べる
# 使い方: python -X utf8 ledger_check.py <repo> <commit> <台帳 md>
import sys, subprocess, re, collections
repo, commit, md = sys.argv[1:4]
tracked = [p for p in subprocess.run(['git', '-C', repo, 'ls-tree', '-r', '--name-only', commit], capture_output=True, text=True).stdout.split('\n') if p]
byname = collections.defaultdict(list)
for p in tracked: byname[p.split('/')[-1]].append(p)
cache = {}
def lines_of(path):
    if path not in cache:
        cache[path] = subprocess.run(['git', '-C', repo, 'show', f'{commit}:{path}'], capture_output=True).stdout.decode('utf-8-sig', errors='replace').split('\n')
    return cache[path]
def resolve(fn):
    c = [p for p in byname.get(fn, []) if p.startswith('PriceOfBread.SimCore/')] or byname.get(fn, [])
    return c[0] if len(c) >= 1 else None
rows = []
for l in open(md, 'rb').read().decode('utf-8').split('\n'):
    if not re.match(r'^\| S\d\d-', l): continue
    cells = [c.strip() for c in l.strip().strip('|').split('|')]
    if len(cells) != 11: rows.append(dict(bad=l)); continue
    rows.append(dict(id=cells[0], sys=cells[1], anchor=cells[4], kind=cells[6], last=cells[7], judge=cells[8], basis=cells[9]))
bad_shape = [r for r in rows if 'bad' in r]
rows = [r for r in rows if 'bad' not in r]
n_anchor = 0; fails = []; no_anchor = 0
for r in rows:
    if r['anchor'] in ('—', ''): no_anchor += 1; continue
    for a in r['anchor'].split(';'):
        a = a.strip()
        if not a: continue
        m = re.match(r'^([^:]+):(\d+):(.+)$', a)
        if not m: fails.append((r['id'], a, '形が違う')); continue
        fn, ln, tok = m.group(1), int(m.group(2)), m.group(3)
        path = resolve(fn) if '/' not in fn else fn
        n_anchor += 1
        if path is None: fails.append((r['id'], a, 'ファイルが木に無い')); continue
        ls = lines_of(path)
        if ln < 1 or ln > len(ls) or tok not in ls[ln - 1]: fails.append((r['id'], a, '行に名が無い')); continue
judged = [r for r in rows if not (r['judge'] == '未確かめ' or r['judge'].startswith('この波の線が埋める'))]
def rest(s): return re.sub(r'；?消えた係数 [^；]*', '', re.sub(r'文書の記号 .（9-26・写さない）', '', s)).strip(' ；')
weak = [r for r in judged if r['kind'] in ('—', '') or not r['basis'] or r['basis'] in ('—',) or not rest(r['basis'])]
checked = [r for r in rows if r['last'] not in ('—', '')]
sys_checked = collections.Counter(r['sys'] for r in checked)
judge_count = collections.Counter(r['judge'] for r in checked)
print(f'行 {len(rows)}・形の崩れた行 {len(bad_shape)}・錨のある行 {len(rows) - no_anchor}・錨の無い行 {no_anchor}・錨 {n_anchor}・引けない錨 {len(fails)}')
for f in fails: print('  引けない:', *f)
print(f'判定を埋めた行 {len(judged)}・型か根拠の欠けた行（9-26 の記号だけの根拠を含む） {len(weak)}')
for r in weak: print('  欠け:', r['id'])
print('確かめた行（最後に確かめたコミットあり）', dict(sys_checked), dict(judge_count))
print('門', '通る' if not fails and not weak and not bad_shape else '落ちる')
sys.exit(0 if not fails and not weak and not bad_shape else 1)
