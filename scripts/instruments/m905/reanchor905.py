# (905) 合流（(899) の reanchor899.py の写し・12 列の台帳〔(900) c77lg〕を読む・改名の対を渡せる＝<旧の名>=<新しい名> を 7 番目の引数に）：機能の台帳の錨（file:line:名）の行番号だけを、木 <old> から木 <new> へ写す（名は変えない・行の他の欄は変えない）。
#   python -X utf8 reanchor905.py <repo> <old> <new> <台帳 md 入力> <台帳 md 出力> <記録.tsv> [旧の名=新しい名,...]
#   写し方：old の行が new で同じ中身の行に対応するなら（difflib の一致の塊）その行へ。対応が無い（書き換えた・消した）なら、
#   new の写した位置の近く（±80 行）で名を含む行のうち一番近い物へ。どちらも無ければ写さずに記録へ〈写せない〉と書く。
#   old の行に名が無い錨（もとから引けない錨）は触らずに記録へ〈もとから引けない〉と書く。
import sys, subprocess, re, collections, difflib
repo, old, new, src, dst, rec = sys.argv[1:7]
REN = dict(x.split('=', 1) for x in sys.argv[7].split(',')) if len(sys.argv) > 7 and sys.argv[7] else {}

def tracked(c):
    return [p for p in subprocess.run(['git', '-C', repo, 'ls-tree', '-r', '--name-only', c], capture_output=True, text=True).stdout.split('\n') if p]
byname = collections.defaultdict(list)
for p in tracked(new): byname[p.split('/')[-1]].append(p)
def resolve(fn):
    c = [p for p in byname.get(fn, []) if p.startswith('PriceOfBread.SimCore/')] or byname.get(fn, [])
    return c[0] if c else None
cache = {}
def lines(c, path):
    k = (c, path)
    if k not in cache:
        cache[k] = subprocess.run(['git', '-C', repo, 'show', f'{c}:{path}'], capture_output=True).stdout.decode('utf-8-sig', errors='replace').split('\n')
    return cache[k]
maps = {}
def mapping(path):
    if path not in maps:
        a, b = lines(old, path), lines(new, path)
        m = {}
        if a == b:
            m = None
        else:
            for i, j, n in difflib.SequenceMatcher(None, a, b, autojunk=False).get_matching_blocks():
                for k in range(n): m[i + k] = j + k
        maps[path] = m
    return maps[path]

def remap(a):
    m = re.match(r'^([^:]+):(\d+):(.+)$', a)
    if not m: return a, '形が違う'
    fn, ln, tok = m.group(1), int(m.group(2)), m.group(3)
    path = resolve(fn) if '/' not in fn else fn
    if path is None: return a, 'ファイルが木に無い'
    la = lines(old, path)
    if not (1 <= ln <= len(la)) or tok not in la[ln - 1]: return a, 'もとから引けない'
    mp = mapping(path)
    if mp is None and tok not in REN: return a, '同じ'
    lb = lines(new, path)
    if tok in REN:
        # 改名：old の行が new で同じ中身の行に対応しない＝近くで新しい名を含む行を探し、錨の名も新しい名へ
        mp = dict(mp or {}); ntok = REN[tok]; fn0 = fn; tok0 = tok
        keys = sorted(mp); prev = [k for k in keys if k < ln - 1]
        guess = mp[prev[-1]] + (ln - 1 - prev[-1]) if prev else ln - 1
        for d in range(0, 81):
            for j in (guess - d, guess + d):
                if 0 <= j < len(lb) and ntok in lb[j]: return f'{fn0}:{j + 1}:{ntok}', '改名して写した'
        return a, '写せない'
    if (ln - 1) in mp and tok in lb[mp[ln - 1]]:
        nl = mp[ln - 1] + 1
        return f'{fn}:{nl}:{tok}', ('同じ' if nl == ln else '写した')
    # 近くの対応のある行から位置を推す
    keys = sorted(mp)
    guess = ln - 1
    prev = [k for k in keys if k < ln - 1]
    if prev: guess = mp[prev[-1]] + (ln - 1 - prev[-1])
    best = None
    for d in range(0, 81):
        for j in (guess - d, guess + d):
            if 0 <= j < len(lb) and tok in lb[j]: best = j; break
        if best is not None: break
    if best is None: return a, '写せない'
    return f'{fn}:{best + 1}:{tok}', '近くへ写した'

text = open(src, 'rb').read().decode('utf-8')
out = []; recs = []
for l in text.split('\n'):
    if re.match(r'^\| S\d\d-', l):
        cells = l.split('|')
        # cells[0]＝'' ・ cells[1..11]＝欄 ・ 最後＝''
        if len(cells) in (13, 14):   # 11 列（(899) まで）か 12 列（(900) から）
            anc = cells[5].strip()
            if anc not in ('—', ''):
                parts = []; moved = False
                for a in anc.split(';'):
                    a = a.strip()
                    if not a: continue
                    na, why = remap(a)
                    if why != '同じ': recs.append((cells[1].strip(), a, na, why))
                    if na != a: moved = True
                    parts.append(na)
                if moved:
                    cells[5] = ' ' + ';'.join(parts) + ' '
                    l = '|'.join(cells)
    out.append(l)
open(dst, 'wb').write('\n'.join(out).encode('utf-8'))
with open(rec, 'wb') as fh:
    fh.write('行\t前\t後\t写し方\n'.encode('utf-8'))
    for r in recs: fh.write(('\t'.join(r) + '\n').encode('utf-8'))
c = collections.Counter(r[3] for r in recs)
print('写した錨', dict(c))
