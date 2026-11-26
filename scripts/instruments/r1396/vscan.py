# (1396) c148dm：PriceOfBread.Tests/ValueScan.cs（870ce5520・(1387) rk の後）の数え方の写し（Python・読むだけ）。
#   書き手＝ドットつき＋ドットなし（CountAllWriters）・入力＝CountInputs・出力＝CountReads（値）／CountCalls（関数・宣言を引く）。
#   r1389 の vscan.py はドットなしの書き手を数えない＝(1373) の後の ValueScan.cs と合わない（正統性 11 と 12）。ここで足した。
#   使い方: python vscan.py <SimCore のフォルダ> <詳細の出力> 名前...（頭に () を付けると関数の呼び出し）
import sys, os, re
sim, detail = sys.argv[1], sys.argv[2]
names = sys.argv[3:]
TOK = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:[^"\\\n]|\\.)*"|\'(?:[^\'\\\n]|\\.)+\'', re.S)
PATH = re.compile(r'(?<![\w.])[A-Za-z_]\w*(?:\s*\.\s*[A-Za-z_]\w*)*')
OPS = r'(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))'
NEWTAIL = re.compile(r'\bnew\b[\w.<>,\[\]\s]*(\((?:[^()]|\([^()]*\))*\))?\s*$', re.S)
KW = set('new true false null int double long float bool var default this base is as and or not in out ref checked unchecked typeof sizeof nameof string char byte short uint ulong decimal object when if else return'.split())


def strip(t):
    def rep(m):
        v = m.group(0)
        if v[0] == '/':
            return '\n' * v.count('\n') if v[1] == '*' else ''
        return '""' if v[0] == '"' else "' '"
    return TOK.sub(rep, t)


files = []
for root, ds, fs in os.walk(sim):
    for f in fs:
        if not f.endswith('.cs'):
            continue
        p = os.path.join(root, f); rel = os.path.relpath(p, sim).replace('\\', '/')
        if rel.startswith(('Instruments/', 'obj/', 'bin/')):
            continue
        files.append((rel, p))
files.sort()
texts = {rel: strip(open(p, encoding='utf-8-sig').read()) for rel, p in files}


def lineof(t, i):
    return t.count('\n', 0, i) + 1


def rhs_at(t, j):
    d = 0; k = j
    while k < len(t):
        ch = t[k]
        if ch in '([{':
            d += 1
        elif ch in ')]}':
            d -= 1
            if d < 0:
                break
        elif ch == ';' and d == 0:
            break
        k += 1
    return t[j:k]


def terms(rhs, member):
    for m in PATH.finditer(rhs):
        segs = [s.strip() for s in m.group(0).split('.')]
        if rhs[m.end():].lstrip().startswith('('):
            segs = segs[:-1]
        if not segs:
            continue
        root = segs[0]
        if root in KW or root[0].isupper():
            continue
        if root in ('c', 'Coef') or 'Coef' in segs:
            continue
        if len(segs) == 1:
            if len(root) >= 3:
                yield 'local:' + root
        elif segs[-1] != member:
            yield segs[-1]


def bare_writers(t, member):
    for m in re.finditer(r'(?<![\w.])' + re.escape(member) + r'\s*(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))', t):
        i = m.start() - 1
        while i >= 0 and t[i].isspace():
            i -= 1
        if i < 0:
            yield m; continue
        ch = t[i]
        if ch in ';}):':
            yield m; continue
        if ch == '{':
            if not NEWTAIL.search(t[:i]):
                yield m
            continue
        if i >= 3 and t[i - 3:i + 1] == 'else' and (i == 3 or not t[i - 4].isalnum()):
            yield m


out = open(detail, 'w', encoding='utf-8')
for nm in names:
    if nm.startswith('()'):
        fn = nm[2:]; allr = re.compile(r'\b' + re.escape(fn) + r'\s*\('); dec = re.compile(r'\b(static|double|float|int|bool)\s+' + re.escape(fn) + r'\s*\(')
        n = 0
        for rel, t in texts.items():
            k = len(allr.findall(t)) - len(dec.findall(t)); n += k
            for m in allr.finditer(t):
                out.write(f'{fn}\tcall-or-decl\t{rel}:{lineof(t, m.start())}\n')
        print(f'{fn}: calls {n}'); continue
    wr = re.compile(r'\.' + re.escape(nm) + OPS); rd = re.compile(r'\.' + re.escape(nm) + r'\b(?!' + OPS + ')')
    W = R = 0; inp = set()
    for rel, t in texts.items():
        sites = [(m, t) for m in wr.finditer(t)] + [(m, t) for m in bare_writers(t, nm)]
        for m, tt in sites:
            W += 1; r = rhs_at(tt, m.end()); ts = set(terms(r, nm)); inp |= ts
            out.write(f'{nm}\twrite\t{rel}:{lineof(tt, m.start())}\t{" | ".join(sorted(ts))}\n')
        for m in rd.finditer(t):
            R += 1; out.write(f'{nm}\tread\t{rel}:{lineof(t, m.start())}\n')
    print(f'{nm}: writers {W} inputs {len(inp)} outputs {R}')
out.close()
