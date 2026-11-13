# c106dc 門：python -X utf8 -I scripts/instruments/c106dc/check.py [--tree D:/PoB_work/wt_c106dc]
import sys, re, os, subprocess
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import common
from common import *
from split import *
import make_all
TREE = REPO
if '--tree' in sys.argv:
    TREE = sys.argv[sys.argv.index('--tree') + 1]
ok = True


def verdict(name, cond, note=''):
    global ok
    print(('緑 ' if cond else '赤 ') + name + ((' ' + note) if note else ''))
    if not cond:
        ok = False


def rd(p):
    return open(TREE + '/' + p, 'rb').read()


def strip_add(data, key):
    idx, b = make_all.ADD[key]
    pos = 0
    for _ in range(idx):
        pos = data.index(b'\n', pos) + 1
    if data[pos:pos + len(b)] != b:
        return None
    return data[:pos] + data[pos + len(b):]


def lines_of(data):
    ls = data.split(b'\n')
    assert ls[-1] == b''
    return ls[:-1]


def merge(dest, pieces):
    # dest は split の行の表（最後は空要素）。pieces: 区分 -> 行の並び。元の行の順に戻す
    its = {k: iter(v) for k, v in pieces.items()}
    out = []
    for dd in dest[:-1]:
        x = next(its[dd], None)
        if x is None:
            return None
        out.append(x)
    for it in its.values():
        if next(it, None) is not None:
            return None
    return b''.join(x + b'\n' for x in out)


# --- 実装ログ ---
L, d = split_log(base(F_LOG))
keep = rd(F_LOG)
a9 = strip_add(rd(A_LOG9), 'log9_arc')
a10 = strip_add(rd(A_LOG10), 'log10_arc')
r = None if a9 is None or a10 is None else merge(d, {'keep': lines_of(keep), '09': lines_of(a9), '10': lines_of(a10)})
verdict('実装ログ：書庫 2 本＋残り＝元（バイト一致）', r == base(F_LOG))


def heads(data):
    return [m.group(1) for m in re.finditer(rb'(?m)^## (20\d\d-\d\d)-\d\d', data)]


verdict('実装ログ：残りの日付つき見出しは全て 2026-11（359 回）', set(heads(keep)) == {b'2026-11'} and len(heads(keep)) == 359)
verdict('実装ログ：書庫 09 は 2026-09 のみ（150）・書庫 10 は 2026-10 のみ（642）',
        set(heads(a9)) == {b'2026-09'} and set(heads(a10)) == {b'2026-10'} and len(heads(a9)) == 150 and len(heads(a10)) == 642)
verdict('実装ログ：頭の注（先頭 6 行）が元のまま', keep.split(b'\n')[:6] == base(F_LOG).split(b'\n')[:6])

# --- 00一覧 ---
L, d = split_one(base(F_ONE))
keep = strip_add(rd(F_ONE), 'one_keep')
arc = strip_add(rd(A_ONE), 'one_arc')
r = None if keep is None or arc is None else merge(d, {'keep': lines_of(keep), 'arc': lines_of(arc)})
verdict('00一覧：書庫＋残り＝元（バイト一致）', r == base(F_ONE))
rows = lines_of(arc) if arc is not None else []


def lastdate(l):
    ds = re.findall(rb'2026-(\d\d)-(\d\d)', l)
    return ds[-1] if ds else None


STRUCK = rb'^\s*\|?\s*~~'
verdict('00一覧：書庫の行は全て打消しで始まり最後の日付が 10-13 より前（73 行）',
        len(rows) == 73 and all(re.match(STRUCK, l) and lastdate(l) and lastdate(l) < (b'10', b'13') for l in rows))
rest = [l for l in lines_of(keep) if re.match(STRUCK, l)] if keep is not None else []
verdict('00一覧：残りの打消し行は日付が 10-13 以後か日付なし（22 行）',
        len(rest) == 22 and all((lastdate(l) is None) or lastdate(l) >= (b'10', b'13') for l in rest))

# --- CURRENT ---
L = lines(base(F_CUR))
dest = ['keep' if i < make_all.CUR_KEEP_LINES else 'arc' for i in range(len(L))]
keep = strip_add(rd(F_CUR), 'cur_keep')
arc = strip_add(rd(A_CUR), 'cur_arc')
r = None if keep is None or arc is None else merge(dest, {'keep': lines_of(keep), 'arc': lines_of(arc)})
verdict('CURRENT：書庫＋残り＝元（バイト一致）', r == base(F_CUR))
cur_chars = len(rd(F_CUR).decode('utf-8'))
verdict('CURRENT：短い版は 2 万字以内', cur_chars <= 20000, '%d 字' % cur_chars)

# --- 改行 ---
def cr(b):
    return b.count(b'\r')


verdict('改行：CR の数が元と同じ（実装ログ・00一覧・CURRENT）',
        sum(cr(rd(p)) for p in (F_LOG, A_LOG9, A_LOG10)) == cr(base(F_LOG))
        and sum(cr(rd(p)) for p in (F_ONE, A_ONE)) == cr(base(F_ONE))
        and sum(cr(rd(p)) for p in (F_CUR, A_CUR)) == cr(base(F_CUR)))
verdict('BOM：残りのファイルの先頭が元と同じ', all(rd(f)[:3] == base(f)[:3] for f in (F_LOG, F_ONE, F_CUR)))
verdict('改行：短い版の README・.ignore は CR なし', cr(rd('docs/README_線の担当.md')) == 0 and cr(rd('.ignore')) == 0)
out = subprocess.run(['git', '-C', TREE, '-c', 'core.quotepath=false', 'ls-files', '--eol', F_LOG, F_ONE, F_CUR, A_CUR, A_LOG9, A_LOG10, A_ONE,
                      'docs/README_線の担当.md', '.ignore', 'docs/README.md'], capture_output=True).stdout.decode('utf-8', 'replace')
print(out.rstrip())
verdict('git ls-files --eol：w/crlf（全行 CRLF）のファイルが無い', 'w/crlf' not in out)

# --- 短い README ---
s = rd('docs/README_線の担当.md').decode('utf-8')
verdict('担当向けの README：1〜2 万字', 10000 <= len(s) <= 20000, '%d 字' % len(s))
RD = base(F_RD).decode('utf-8').split('\n')
bad = 0
n = 0
for l in s.split('\n'):
    m = re.match(r'^- (.*) 〔元 (\S+) L(\d+)〕$', l)
    if not m:
        continue
    n += 1
    txt, ln = m.group(1), int(m.group(3))
    raw = RD[ln - 1].replace('\r', '').strip()
    t = txt[:-1] if txt.endswith('…') else txt
    if raw.startswith('|'):
        cells = [c.strip() for c in raw.strip('|').split('|')]
        head = cells[0]
        rest = t[len(head) + 1:] if t.startswith(head + '：') else None
        if rest is not None:
            okl = cells[1].startswith(rest) if len(cells) > 1 else rest == ''
        else:
            okl = cells[0].startswith(t)   # 最初の列の途中で切れた行
    else:
        okl = re.sub(r'^[-*]\s+', '', raw).startswith(t)
    if not okl:
        bad += 1
verdict('担当向けの README：全ての行が元の README の行の頭の抜粋（新しい文が無い）', bad == 0 and n > 100, '行 %d・不一致 %d' % (n, bad))
cur = rd('docs/README.md').split(b'\n')
old = base(F_RD).split(b'\n')
k = len(cur) - len(old)
verdict('README.md：元の行がそのまま残り、増えた行は先頭 12 行以内の挿入だけ',
        k >= 0 and any(cur[:i] + cur[i + k:] == old for i in range(0, 13)), '増 %d 行' % k)

# --- Python の規則（GoodIdGuardTests の PyRules の移し） ---
rules = [
    r"\b\w*(?:GoodId|[Gg]ood|[Gg]id)\w*\s*(?:==|!=)\s*['\"]?\d+",
    r"\b(?:g|id|gi|g2)\s*(?:==|!=)\s*['\"]?\d+",
    r"\b(?:Goods|Markets|RefPrices|Complexity|goods|markets|prices|stocks|ref_prices)\s*\[\s*\d+\s*\]",
    r"\bI\(\s*\d+\s*,", r"\bnew Input\(\s*\d+\s*,",
    r"\b(?:good|gid|good_id)\w*\s*=\s*\d+",
    r"\b(?:good|gid)\w*\s+in\s+[\(\[\{]\s*\d+",
    r"\bnew\s*(?:int)?\s*\[\s*\]\s*\{\s*\d+",
    r"\bGOODS?\w*\s*=\s*[\{\[\(]",
]
hits = 0
sd = TREE + '/scripts/instruments/c106dc'
for f in os.listdir(sd):
    if f.endswith('.py'):
        t = open(sd + '/' + f, 'rb').read().decode('utf-8')
        for rx in rules:
            hits += len(re.findall(rx, t))
verdict('台本：GoodIdGuardTests の Python の規則に当たる書き方が無い', hits == 0, '当たり %d' % hits)

# --- 差分の範囲 ---
names = subprocess.run(['git', '-C', TREE, '-c', 'core.quotepath=false', 'diff', '--name-only', BASE + '..HEAD'],
                       capture_output=True).stdout.decode('utf-8').split('\n')
names = [n for n in names if n]


def okp(n):
    return n == '.ignore' or n.startswith('docs/') or n.startswith('scripts/instruments/c106dc/') or n.startswith('measurements/m1107_')


verdict('差分の範囲：docs・.ignore・scripts/instruments/c106dc・measurements/m1107_* だけ', all(okp(n) for n in names), '%d ファイル' % len(names))
print('総合：' + ('全て緑' if ok else '赤あり'))
sys.exit(0 if ok else 1)
