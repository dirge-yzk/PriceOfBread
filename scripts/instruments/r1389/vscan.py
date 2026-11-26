# (1375) rw：PriceOfBread.Tests/ValueScan.cs の数え方の写し（Python・読むだけ）。書き手・入力・出力の数と、その場所（ファイル:行）を出す。
# 使い方: python vscan.py <SimCore のフォルダ> <出力の詳細ファイル> 名前...   （名前の頭に () を付けると関数の呼び出しとして数える）
import sys, os, re
sim, detail = sys.argv[1], sys.argv[2]
names = sys.argv[3:]
TOK = re.compile(r'//[^\n]*|/\*.*?\*/|"(?:[^"\\\n]|\\.)*"|\'(?:[^\'\\\n]|\\.)+\'', re.S)
PATH = re.compile(r'(?<![\w.])[A-Za-z_]\w*(?:\s*\.\s*[A-Za-z_]\w*)*')
OPS = r'(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))'
KW = set('new true false null int double long float bool var default this base is as and or not in out ref checked unchecked typeof sizeof nameof string char byte short uint ulong decimal object when if else return'.split())
def strip(t):
    def rep(m):
        v = m.group(0)
        if v[0] == '/': return '\n' * v.count('\n') if v[1] == '*' else ''
        return '""' if v[0] == '"' else "' '"
    return TOK.sub(rep, t)
files = []
for root, ds, fs in os.walk(sim):
    for f in fs:
        if not f.endswith('.cs'): continue
        p = os.path.join(root, f); rel = os.path.relpath(p, sim).replace('\\', '/')
        if rel.startswith(('Instruments/', 'obj/', 'bin/')): continue
        files.append((rel, p))
files.sort()
texts = {rel: strip(open(p, encoding='utf-8-sig').read()) for rel, p in files}
def lineof(t, i): return t.count('\n', 0, i) + 1
def rhs_at(t, j):
    d = 0; k = j
    while k < len(t):
        ch = t[k]
        if ch in '([{': d += 1
        elif ch in ')]}':
            d -= 1
            if d < 0: break
        elif ch == ';' and d == 0: break
        k += 1
    return t[j:k]
def terms(rhs, member):
    for m in PATH.finditer(rhs):
        segs = [s.strip() for s in m.group(0).split('.')]
        if rhs[m.end():].lstrip().startswith('('): segs = segs[:-1]
        if not segs: continue
        root = segs[0]
        if root in KW or root[0].isupper(): continue
        if root in ('c', 'Coef') or 'Coef' in segs: continue
        if len(segs) == 1:
            if len(root) >= 3: yield 'local:' + root
        elif segs[-1] != member: yield segs[-1]
out = open(detail, 'w', encoding='utf-8')
for nm in names:
    if nm.startswith('()'):
        fn = nm[2:]; allr = re.compile(r'\b' + re.escape(fn) + r'\s*\('); dec = re.compile(r'\b(static|double|float|int|bool)\s+' + re.escape(fn) + r'\s*\(')
        n = 0
        for rel, t in texts.items():
            decl = {lineof(t, m.start()) for m in dec.finditer(t)}
            for m in allr.finditer(t):
                ln = lineof(t, m.start())
                if ln in decl: decl.discard(ln); continue
                n += 1; out.write(f'{fn}\tcall\t{rel}:{ln}\n')
        print(f'{fn}: calls {n}'); continue
    wr = re.compile(r'\.' + re.escape(nm) + OPS); rd = re.compile(r'\.' + re.escape(nm) + r'\b(?!' + OPS + ')')
    W = R = 0; inp = set()
    for rel, t in texts.items():
        for m in wr.finditer(t):
            W += 1; r = rhs_at(t, m.end()); ts = sorted(set(terms(r, nm))); inp |= set(terms(r, nm))
            out.write(f'{nm}\twrite\t{rel}:{lineof(t, m.start())}\t{" | ".join(ts)}\n')
        for m in rd.finditer(t):
            R += 1; out.write(f'{nm}\tread\t{rel}:{lineof(t, m.start())}\n')
    print(f'{nm}: writers {W} inputs {len(inp)} outputs {R}  inputs={sorted(inp)}')
out.close()
