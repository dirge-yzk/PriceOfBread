# (1367) c144gr 読む：書き手→読み手の図（設計思想⑪・直さない）
# 作り方：正規表現（Python）。Roslyn は使わない。限界は out/README_limits.txt に書く。
#
# 数え方（wr の線と同じ定義・台本の頭に書く）
#   書き手＝SimCore（Instruments を除く・Display も除く）の代入・複合代入・Toward の結果の代入。
#           wr 互換の数（wr_dot）＝`.名前` の直後の = += -= *= /=（== と => を除く・コメントを除く）を
#           SimCore 直下＋Instruments で数えた物（WriterRatchetTests.CountWritersIn と同じ正規表現）。
#           台本の主の数（writers）＝Instruments を除き、`.名前` と、持ち主のクラスの中の裸の `名前` と、
#           オブジェクト初期化子の `名前 =` を数え、受け手の型で同名の別の欄（Pop と Colony の Militancy など）を分ける。
#   入力＝書き手の目標の式の項＝右辺に現れる状態の欄の名（Toward は第 2 引数の目標と第 3 引数の率・自分自身は除く）。
#           右辺の局所変数は同じメソッドの中の宣言と代入へ最大 6 段まで展開する。
#           右辺のメソッド呼び出しは、名前で引いたメソッドの本体が読む追跡の値（推移の閉包）を入力に足す＝〈経由〉と名乗る。
#   出力＝SimCore（Instruments・Display を除く）の読む式の場所。App・Runner・試験は数えない。AI の判断（SelfPlay.cs・AiRootSupply.cs）は数え、印を付ける。
import re, os, sys, json, collections

ROOT = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/r1367_work/src/PriceOfBread.SimCore'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/r1367_work/out'
os.makedirs(OUT, exist_ok=True)
AI_FILES = {'SelfPlay.cs', 'AiRootSupply.cs'}

# ---------- 前処理：コメントと文字列を空白にする（行と位置を保つ） ----------
def strip(src):
    out = list(src); i = 0; n = len(src)
    def blank(a, b):
        for k in range(a, b):
            if out[k] != '\n': out[k] = ' '
    while i < n:
        ch = src[i]
        if src.startswith('//', i):
            j = src.find('\n', i); j = n if j < 0 else j; blank(i, j); i = j
        elif src.startswith('/*', i):
            j = src.find('*/', i + 2); j = n if j < 0 else j + 2; blank(i, j); i = j
        elif src.startswith('"""', i) or src.startswith('$"""', i):
            s = i + (4 if src[i] == '$' else 3); j = src.find('"""', s); j = n if j < 0 else j + 3; blank(i, j); i = j
        elif ch == '"' or (ch in '@$' and i + 1 < n and src[i + 1] in '"@$'):
            k = i; verb = False
            while k < n and src[k] in '@$': verb = verb or src[k] == '@'; k += 1
            if k >= n or src[k] != '"': i += 1; continue
            k += 1
            while k < n:
                if verb:
                    if src[k] == '"':
                        if k + 1 < n and src[k + 1] == '"': k += 2; continue
                        k += 1; break
                else:
                    if src[k] == '\\': k += 2; continue
                    if src[k] == '"': k += 1; break
                    if src[k] == '\n': break
                k += 1
            blank(i, k); i = k
        elif ch == "'":
            m = re.match(r"'(\\.|[^'\\\n]){1,8}'", src[i:i + 12])
            if m: blank(i, i + m.end()); i += m.end()
            else: i += 1
        else:
            i += 1
    return ''.join(out)

def match_close(s, i, o, c):
    d = 0
    for k in range(i, len(s)):
        if s[k] == o: d += 1
        elif s[k] == c:
            d -= 1
            if d == 0: return k
    return len(s) - 1

FILES = {}
for dp, dns, fns in os.walk(ROOT):
    rel = os.path.relpath(dp, ROOT).replace('\\', '/')
    if any(x in rel.split('/') for x in ('obj', 'bin', 'Display')): continue
    for f in fns:
        if f.endswith('.cs'):
            p = os.path.join(dp, f)
            raw = open(p, 'rb').read().decode('utf-8-sig', 'replace')
            key = (rel + '/' + f) if rel != '.' else f
            FILES[key] = (raw, strip(raw))

def is_instr(f): return f.startswith('Instruments/')

LINESTART = {}
def lineno(f, pos):
    ls = LINESTART.get(f)
    if ls is None:
        s = FILES[f][1]; ls = [0] + [m.end() for m in re.finditer('\n', s)]; LINESTART[f] = ls
    import bisect
    return bisect.bisect_right(ls, pos)

def line_text(f, ln):
    return FILES[f][0].split('\n')[ln - 1].strip()

# ---------- クラスとメンバーの範囲 ----------
KW = set('if for foreach while switch using catch lock return new else await throw case in is as out ref typeof sizeof nameof default checked unchecked fixed when get set init var base this where'.split())
MOD = r'(?:(?:public|private|internal|protected|static|override|virtual|sealed|readonly|unsafe|async|new|extern|abstract|partial|required|const)\s+)+'
TYPE = r'(?:\([^()]*\)|[\w.]+(?:<[^;{}()]*?>)?)(?:\[[\s,]*\])*\??'
RX_METHOD = re.compile(MOD + r'(?:' + TYPE + r'\s+)?(\w+)\s*(?:<[^;{}()=]*?>)?\s*\(')
RX_PROP_EB = re.compile(MOD + TYPE + r'\s+(\w+)\s*=>')
RX_PROP_BL = re.compile(MOD + TYPE + r'\s+(\w+)\s*\{\s*(?:get|set|init|internal|private|protected)')
RX_FIELD = re.compile(MOD + TYPE + r'\s+(\w+)\s*(?:=(?![=>])|;)')
RX_CLASS = re.compile(r'\b(class|struct|record|interface)\s+(\w+)')

CLASSES = []   # (f, a, b, name)
MEMBERS = []   # (f, a, b, name, cls, kind)  kind=method|prop|ctor
STATE = collections.defaultdict(set)  # name -> set of classes (fields / auto props)
COMPUTED = collections.defaultdict(set)  # name -> classes (computed props)
for f, (raw, s) in FILES.items():
    for m in RX_CLASS.finditer(s):
        j = s.find('{', m.end()); k = s.find(';', m.end())
        if j < 0 or (0 <= k < j): continue
        CLASSES.append((f, m.start(), match_close(s, j, '{', '}'), m.group(2)))
def class_at(f, pos):
    best = None
    for (ff, a, b, n) in CLASSES:
        if ff == f and a <= pos <= b and (best is None or a > best[0]): best = (a, n)
    return best[1] if best else '?'
for f, (raw, s) in FILES.items():
    taken = []
    for m in RX_METHOD.finditer(s):
        name = m.group(1)
        if name in KW: continue
        po = m.end() - 1; pc = match_close(s, po, '(', ')')
        rest = s[pc + 1:pc + 400]
        mm = re.match(r'\s*(?:where[^{;=]*|:\s*(?:base|this)\s*\([^)]*\))?\s*(\{|=>)', rest)
        if not mm: continue
        bstart = pc + 1 + mm.start(1)
        if mm.group(1) == '{': bend = match_close(s, bstart, '{', '}')
        else:
            bend = bstart
            d = 0
            while bend < len(s):
                c = s[bend]
                if c in '({[': d += 1
                elif c in ')}]': d -= 1
                elif c == ';' and d <= 0: break
                bend += 1
        cls = class_at(f, m.start())
        kind = 'ctor' if name == cls else 'method'
        MEMBERS.append((f, m.start(), bend, name, cls, kind))
    for m in RX_PROP_EB.finditer(s):
        name = m.group(1)
        e = m.end(); d = 0
        while e < len(s):
            c = s[e]
            if c in '({[': d += 1
            elif c in ')}]': d -= 1
            elif c == ';' and d <= 0: break
            e += 1
        cls = class_at(f, m.start()); MEMBERS.append((f, m.start(), e, name, cls, 'prop')); COMPUTED[name].add(cls)
    for m in RX_PROP_BL.finditer(s):
        name = m.group(1); j = s.find('{', m.end(1))
        k = match_close(s, j, '{', '}')
        body = s[j:k + 1]
        cls = class_at(f, m.start())
        if re.search(r'\bget\s*(\{|=>)', body):
            MEMBERS.append((f, m.start(), k, name, cls, 'prop')); COMPUTED[name].add(cls)
        elif not is_instr(f):
            STATE[name].add(cls)
    for m in RX_FIELD.finditer(s):
        name = m.group(1)
        if name in KW: continue
        cls = class_at(f, m.start())
        # メソッドの中の宣言（局所）を除く：クラスの直下の深さだけ
        inside = any(ff == f and a < m.start() < b and k_ != 'prop' for (ff, a, b, n, c, k_) in MEMBERS if ff == f)
        if not inside and not is_instr(f): STATE[name].add(cls)

MEMBERS_BY_FILE = collections.defaultdict(list)
for mem in MEMBERS: MEMBERS_BY_FILE[mem[0]].append(mem)
def member_at(f, pos):
    best = None
    for mem in MEMBERS_BY_FILE[f]:
        if mem[1] <= pos <= mem[2] and (best is None or mem[1] > best[1]): best = mem
    return best
def outer_member_at(f, pos):
    best = None
    for mem in MEMBERS_BY_FILE[f]:
        if mem[1] <= pos <= mem[2] and (best is None or mem[1] < best[1]): best = mem
    return best

# ---------- 受け手の型の推定（同名の欄を分ける） ----------
NAME_HINT = {'w': 'World', 'world': 'World', 'World': 'World', 'W': 'World', 'pop': 'Pop', 'cp': 'Colony', 'col': 'Colony', 'colony': 'Colony',
             'region': 'Region', 'r': 'Region', 'reg': 'Region', 'cfg': 'NationConfig', 'config': 'NationConfig', 'nc': 'NationConfig'}
def infer_type(f, pos, recv):
    s = FILES[f][1]
    if recv in ('', 'this'): return class_at(f, pos)
    last = re.split(r'[.\]]', recv.rstrip(')'))[-1] if recv else ''
    last = re.sub(r'\[.*', '', recv.split('.')[-1])
    if last in ('World',): return 'World'
    mem = outer_member_at(f, pos)
    a = mem[1] if mem else max(0, pos - 6000)
    seg = s[a:pos]
    cands = []
    for mm in re.finditer(r'foreach\s*\(\s*(?:var|(\w+))\s+' + re.escape(last) + r'\s+in\s+([^\n]*?)\)\s*(?:\n|\{|$|\w)', seg):
        t = mm.group(1); expr = mm.group(2)
        if t: cands.append(t); continue
        if re.search(r'[Cc]olon', expr): cands.append('Colony')
        elif re.search(r'Pops|pops|[Rr]ebels|[Mm]ass', expr): cands.append('Pop')
        elif re.search(r'Powers|Nations|powers|Wars', expr): cands.append('Power')
        elif re.search(r'Regions', expr): cands.append('Region')
        else: cands.append('?')
    for mm in re.finditer(r'\b([A-Z]\w*)\??\s+' + re.escape(last) + r'\s*[,)=;]', seg):
        cands.append(mm.group(1))
    for mm in re.finditer(r'\bvar\s+' + re.escape(last) + r'\s*=\s*([^;]*);', seg):
        expr = mm.group(1)
        if re.search(r'new\s+(\w+)', expr): cands.append(re.search(r'new\s+(\w+)', expr).group(1))
        elif re.search(r'[Cc]olon', expr): cands.append('Colony')
        elif re.search(r'Pops', expr): cands.append('Pop')
        elif re.search(r'\.World\b|World\(', expr): cands.append('World')
    if cands: return cands[-1]
    if last in NAME_HINT: return NAME_HINT[last]
    if last == 'p': return 'Power' if f.startswith('Diplomacy') else 'Pop'
    if last in ('urban', 'rural', 'home', 'src', 'dst', 'origin', 'dest', 'rg', 'prov'): return 'Region'
    if last in ('aw', 'bw', 'ow', 'tw', 'vw', 'lw', 'hw', 'nw', 'mw', 'sw', 'pw'): return 'World'
    if 'col' not in last.lower(): return 'Pop~'
    return '?'

def node_of(name, f, pos, recv):
    classes = STATE.get(name, set())
    if len(classes) == 1: return f'{next(iter(classes))}.{name}'
    if not classes: return None
    t = infer_type(f, pos, recv)
    if t == 'Pop~' and name not in ('Militancy', 'Literacy', 'Consciousness', 'Satisfaction'): t = '?'
    t = t.rstrip('~')
    if t in classes: return f'{t}.{name}'
    if t in COMPUTED.get(name, ()): return f'{t}.{name}()'
    return f'?.{name}'

# ---------- 書き手と読み手の走査 ----------
RX_ACCESS = re.compile(r'(?<![\w.])((?:[A-Za-z_]\w*(?:\[[^\]\[]*\])?(?:\(\))?\??\.)*)([A-Z]\w*)\b')
def stmt_end(s, i):
    d = 0
    while i < len(s):
        c = s[i]
        if c in '({[': d += 1
        elif c in ')}]':
            d -= 1
            if d < 0: return i
        elif c in ';,' and d <= 0:
            if c == ';' or d < 0: return i
            if c == ',' and d == 0:
                return i
        i += 1
    return i

ASSIGN = re.compile(r'\s*(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|\?\?=|=(?![=>]))')

writers = []   # dict
reads = []
for f, (raw, s) in FILES.items():
    for m in RX_ACCESS.finditer(s):
        recv = m.group(1)[:-1].rstrip('?') if m.group(1) else ''
        name = m.group(2)
        if name not in STATE: continue
        pos = m.start(2)
        after = s[m.end(2):m.end(2) + 40]
        am = ASSIGN.match(after)
        prev = s[max(0, m.start() - 60):m.start()]
        # 宣言（型＋名前）の行は除く
        if re.search(MOD + r'[\w<>\[\],.?() ]*$', prev) and not recv:
            mem = member_at(f, pos)
            if mem is None: continue
        mem = member_at(f, pos)
        if not recv and mem is None: continue
        if not recv:
            # 裸の名前：持ち主のクラスの中か、初期化子の中か
            cls = class_at(f, pos)
            pre = s[max(0, pos - 400):pos]
            init = bool(am) and re.search(r'new\s+\w+\s*(\([^)]*\))?\s*\{[^{}]*$', pre) is not None
            if not init and cls not in STATE[name]:
                continue
            kind_w = 'init' if init else 'bare'
            if init:
                t = re.search(r'new\s+(\w+)\s*(\([^)]*\))?\s*\{[^{}]*$', pre).group(1)
                node = f'{t}.{name}' if t in STATE[name] else node_of(name, f, pos, '')
            else:
                node = f'{cls}.{name}'
        else:
            # 静的な型の名前（Coefficients など）の後の欄は受け手の型で分ける
            kind_w = 'dot'
            node = node_of(name, f, pos, recv)
        if node is None: continue
        ln = lineno(f, pos)
        if am and not (s[m.end(2):m.end(2) + 3].lstrip().startswith('=>')):
            op = am.group(2)
            rs = m.end(2) + am.end()
            re_ = stmt_end(s, rs)
            writers.append(dict(node=node, file=f, line=ln, pos=pos, rhs_a=rs, rhs_b=re_, op=op, kind=kind_w, recv=recv,
                                member=(mem[3] if mem else '?'), instr=is_instr(f)))
            if op != '=':
                reads.append(dict(node=node, file=f, line=ln, pos=pos, member=(mem[3] if mem else '?'), instr=is_instr(f), self_rw=True))
        else:
            reads.append(dict(node=node, file=f, line=ln, pos=pos, member=(mem[3] if mem else '?'), instr=is_instr(f), self_rw=False))

# ---------- メソッドごとの直接の読み（追跡の値）と呼び出し ----------
READS_IN_MEMBER = collections.defaultdict(set)
for r in reads:
    if r['instr'] or r['self_rw']: continue   # 複合代入（x += …）の左辺は書き手＝読みに数えない
    mem = member_at(r['file'], r['pos'])
    if mem: READS_IN_MEMBER[(mem[0], mem[1])].add(r['node'])
MEM_BY_NAME = collections.defaultdict(list)
for mem in MEMBERS:
    if not is_instr(mem[0]): MEM_BY_NAME[mem[3]].append(mem)
CALLS = {}
RX_CALL0 = re.compile(r'(?<![\w.])([A-Z]\w*)\s*\(|([A-Za-z_]\w*)\s*\.\s*([A-Z]\w*)\s*\(')
class _RC:
    """呼び出しの名。受け手つき（`World.Transfer(`）は `受け手.名` で返す（同名の別クラスのメソッドへ誤って解決しない）"""
    @staticmethod
    def findall(text):
        out = []
        for m in RX_CALL0.finditer(text):
            out.append(m.group(1) if m.group(1) else m.group(2) + '.' + m.group(3))
        return out
RX_CALL = _RC
for mem in MEMBERS:
    s = FILES[mem[0]][1]
    body = s[mem[1]:mem[2] + 1]
    names = set(RX_CALL.findall(body)) - {mem[3]}
    # 計算のプロパティの読み（括弧なし）
    for nm in re.findall(r'\.([A-Z]\w*)\b(?!\s*\()', body):
        if nm in COMPUTED: names.add(nm)
    CALLS[(mem[0], mem[1])] = names
CLOSURE = {}
def closure(key, stack=()):
    if key in CLOSURE: return CLOSURE[key]
    if key in stack: return set()
    acc = set(READS_IN_MEMBER.get(key, set()))
    for nm in CALLS.get(key, ()):
        for m2 in MEM_BY_NAME.get(nm, ()):
            acc |= closure((m2[0], m2[1]), stack + (key,))
    CLOSURE[key] = acc
    return acc
sys.setrecursionlimit(20000)
for mem in MEMBERS: closure((mem[0], mem[1]))

# ---------- 書き手の入力（局所の展開と呼び出しの閉包） ----------

IFBLOCKS = {}
def if_blocks(s):
    """ファイルの全ての if：(条件の始め, 条件の終わり, [(塊の始め, 塊の終わり)…])＝then と else の塊。1 回だけ作る。"""
    key_ = id(s)
    if key_ in IFBLOCKS: return IFBLOCKS[key_]
    out = []
    for m in re.finditer(r'\bif\s*\(', s):
        co = m.end() - 1; cc = match_close(s, co, '(', ')')
        k = cc + 1
        while k < len(s) and s[k] in ' \t\r\n': k += 1
        bend = match_close(s, k, '{', '}') if s[k:k + 1] == '{' else stmt_end(s, k)
        rng = [(k, bend)]
        k2 = bend + 1
        while k2 < len(s) and s[k2] in ' \t\r\n': k2 += 1
        if s.startswith('else', k2) and not re.match(r'else\s+if\b', s[k2:k2 + 12]):
            k3 = k2 + 4
            while k3 < len(s) and s[k3] in ' \t\r\n': k3 += 1
            eend = match_close(s, k3, '{', '}') if s[k3:k3 + 1] == '{' else stmt_end(s, k3)
            rng.append((k3, eend))
        out.append((co + 1, cc, rng))
    IFBLOCKS[key_] = out
    return out

def enclosing_conds(s, a, pos):
    """pos を囲む if の条件（then の塊か else の塊の中にいる時・メソッドの中だけ）。制御の入力として数える。"""
    return [(ca, s[ca:cc]) for (ca, cc, rng) in if_blocks(s) if a <= ca and cc < pos and any(x <= pos <= y for (x, y) in rng)]

def expand(f, mem, text, text_a, before, depth, seen, spans, tag='d'):
    mk = (f, text_a, tag)
    if depth > 0 and mk in EXPMEMO:
        t_, c_, p_, sp_ = EXPMEMO[mk]; spans.extend(sp_); return t_, c_, p_
    sp0 = len(spans)
    r_ = _expand(f, mem, text, text_a, before, depth, seen, spans, tag)
    if depth > 0: EXPMEMO[mk] = r_ + (spans[sp0:],)
    return r_
EXPMEMO = {}
def _expand(f, mem, text, text_a, before, depth, seen, spans, tag='d'):
    """戻り値：terms＝{(名, 受け手, 'd'|'c')}（d＝式の項・c＝囲む if の条件の項）・calls＝{(名, 'd'|'c')}・params"""
    s = FILES[f][1]
    terms = set(); calls = set(); params = set()
    spans.append((text_a, text_a + len(text)))
    a = mem[1] if mem else 0
    region = s[a:before]
    for loc in set(re.findall(r'(?<![\w.])([a-z_]\w*)\b(?!\s*\()', text)) - seen - KW:
        decls = list(re.finditer(r'(?<![\w.])' + re.escape(loc) + r'\s*(\+=|-=|\*=|/=|=(?![=>]))', region))
        # 有効範囲：書き手の位置を囲む塊の中の最後の宣言（型名か var の直後の loc）から後の代入だけ（同じ名の別の塊の局所を拾わない）
        dstart = 0
        for dm in re.finditer(r'(?:\bvar|\bdouble|\bfloat|\bint|\blong|\bbool|\bdecimal|\b[A-Z][\w.]*(?:<[^;{}()]*?>)?(?:\[\])?\??)\s+' + re.escape(loc) + r'\s*(?:=(?![=>])|;|\bin\b)', region):
            bal = 0; okb = True
            for ch in region[dm.start():]:
                if ch == '{': bal += 1
                elif ch == '}':
                    bal -= 1
                    if bal < 0: okb = False; break
            if okb: dstart = dm.start()
        decls = [d for d in decls if d.start() >= dstart]
        # 組の分解（var (a, b, c) = 式;）
        if not decls:
            decls = list(re.finditer(r'\((?:[^()]*?[,(]\s*)?(?:var\s+|double\s+)?' + re.escape(loc) + r'\s*(?:,[^()]*)?\)\s*(=)(?![=>])', region))
        if not decls:
            if re.search(r'[\w>\]?]\s+' + re.escape(loc) + r'\s*[,)]', region[:400] if mem else ''): params.add(loc)
            continue
        if depth >= 6: continue
        for d in decls:
            ra = a + d.end(); rb = stmt_end(s, ra)
            t2, c2, p2 = expand(f, mem, s[ra:rb], ra, a + d.start(), depth + 1, seen | {loc}, spans, tag)
            terms |= t2; calls |= c2; params |= p2
    for mm in RX_ACCESS.finditer(text):
        nm = mm.group(2)
        if nm in STATE: terms.add((nm, mm.group(1)[:-1].rstrip('?') if mm.group(1) else '', tag))
    calls |= {(x, tag) for x in RX_CALL.findall(text)}
    for nm in re.findall(r'\.([A-Z]\w*)\b(?!\s*\()', text):
        if nm in COMPUTED: calls.add((nm, tag))
    # 囲む if の条件（制御の入力）：この式の位置について 1 回だけ（条件の中の局所も展開する）
    if depth <= 2:
        for (ca, ctext) in enclosing_conds(s, a, text_a):
            if (ca, 'cond') in seen: continue
            t2, c2, p2 = expand(f, mem, ctext, ca, ca, depth + 1, seen | {(ca, 'cond')}, [], 'c')
            terms |= t2; calls |= c2; params |= p2
    return terms, calls, params

def local_src(f, mem, before, loc):
    """局所変数 loc の最後の宣言の右辺（空白を除く）"""
    s = FILES[f][1]; a = mem[1] if mem else 0
    ds = list(re.finditer(r'(?<![\w.])' + re.escape(loc) + r'\s*=(?![=>])', s[a:before]))
    if not ds: return None
    ra = a + ds[-1].end(); return re.sub(r'\s+', '', s[ra:stmt_end(s, ra)])

KEEPWORD = re.compile(r'Keep|Decay|Retain|Persist|Smooth|Speed|Rate|Memory|Halflife|HalfLife|Fade|Relief|Damp|\(1(\.0)?-|0\.9\d*|Math\.Pow|Math\.Exp')
def classify(w, f, mem):
    s = FILES[f][1]
    name = w['node'].split('.')[-1]
    lhs = re.sub(r'\s+', '', (w['recv'] + '.' if w['recv'] else '') + name)
    rhs = re.sub(r'\s+', '', s[w['rhs_a']:w['rhs_b']])
    op = w['op']
    if 'Toward(' in rhs: return 'Toward'
    # 右辺が局所 1 つ（か Clamp01(局所)）なら、その局所の右辺へ 3 段まで置き換える（x = legClamped のような書き手を読む）
    for _ in range(3):
        mloc = re.fullmatch(r'(?:[\w.]*Clamp01\()?([a-z_]\w*)\)?', rhs)
        if not mloc: break
        s2 = local_src(f, mem, w['pos'], mloc.group(1))
        if not s2: break
        rhs = s2
    if op == '=' and re.search(r'\*\s*(?:Math\.Max\(0,)?\(?1(?:\.0)?-' + re.escape(lhs) + r'\)', rhs):
        return 'hand-approach(x+k*(1-x))'
    L = re.escape(lhs)
    selfref = re.search(r'(?<![\w.])' + L + r'(?![\w(])', rhs) is not None
    # (t - x) の x が自分か、自分を写した局所か
    def diff_to_self():
        for mm in re.finditer(r'\(([^()]*)-([A-Za-z_][\w.]*)\)', rhs):
            x = mm.group(2)
            if x == lhs or x.split('.')[-1] == name and x.endswith(name) and '.' in x: return True
            if '.' not in x:
                src = local_src(f, mem, w['pos'], x)
                if src is not None and (src == lhs or src.endswith('.' + name) or src == name): return True
        return False
    if op in ('+=', '-=') and diff_to_self(): return 'hand-blend(x+=k*(t-x))'
    if op == '-=' and selfref: return 'hand-decay(x-=x*k)'
    if op == '*=':
        return 'hand-decay(x*=k)' if KEEPWORD.search(rhs) or re.search(r'[A-Z]\w*', rhs) else 'scale(x*=lit)'
    if op == '/=': return 'scale(x/=)'
    if op == '=':
        if selfref:
            if re.fullmatch(r'(' + L + r'\*[^+\-]+|[^+\-]+\*' + L + r')', rhs): return 'hand-decay(x=x*k)'
            if re.search(r'1(\.0)?-', rhs) and re.search(L + r'\*|\*' + L, rhs): return 'hand-blend(x=x*(1-k)+t*k)'
            if re.search(r'(' + L + r'\*[^+\-]+|[^+\-]+\*' + L + r')[+\-]', rhs) or re.search(r'[+\-](' + L + r'\*[\w.()]+|[\w.()]+\*' + L + r')', rhs):
                return 'hand-leaky(x=x*keep+in)'
            if re.search(r'Math\.(Max|Min)\(', rhs): return 'self-clamp'
            return 'self-other'
        if diff_to_self(): return 'hand-blend(x=y+k*(t-y))'
    return ''

writers_in = []
for w in writers:
    f = w['file']; s = FILES[f][1]
    mem = member_at(f, w['pos'])
    rhs = s[w['rhs_a']:w['rhs_b']]
    w['rhs'] = ' '.join(rhs.split())[:300]
    w['toward'] = 'Toward(' in rhs
    w['form'] = classify(w, f, mem)
    spans = []
    terms, calls, params = expand(f, mem, rhs, w['rhs_a'], w['pos'], 0, set(), spans)
    ctrl = set()
    w['spans'] = spans
    direct = set()
    for (nm, recv, tg) in terms:
        nd = node_of(nm, f, w['pos'], recv) if recv else (f'{class_at(f, w["pos"])}.{nm}' if class_at(f, w['pos']) in STATE[nm] else node_of(nm, f, w['pos'], ''))
        if nd and nd != w['node']: (direct if tg == 'd' else ctrl).add(nd)
    ctrl -= direct
    w['in_direct'] = sorted(direct); w['in_ctrl'] = sorted(ctrl)
    w['calls'] = sorted({x for (x, tg) in calls if tg == 'd'}); w['ctrl_calls'] = sorted({x for (x, tg) in calls if tg == 'c'} - set(w['calls'])); w['params'] = sorted(params)

# 読みがどの書き手の右辺（局所の展開を含む）に入るか
SPANS = collections.defaultdict(list)
for wi, w in enumerate(writers):
    for (a, b_) in w['spans']: SPANS[w['file']].append((a, b_, wi))
for r in reads:
    r['into'] = sorted({writers[wi]['node'] + '@' + f"{writers[wi]['file']}:{writers[wi]['line']}" for (a, b_, wi) in SPANS[r['file']] if a <= r['pos'] < b_ and writers[wi]['node'] != r['node']})
    r['ai'] = r['file'] in AI_FILES

# 手書きの /12 と /3
divs = []
RX_DIV = re.compile(r'/\s*(12|3)(?:\.0*)?[dDfFmM]?(?![\w.])')
for f, (raw, s) in FILES.items():
    if is_instr(f): continue
    for m in RX_DIV.finditer(s):
        ln = lineno(f, m.start()); mem = member_at(f, m.start())
        divs.append(dict(file=f, line=ln, div='/' + m.group(1), member=(mem[3] if mem else '?'), text=line_text(f, ln)[:160]))
RX_CONV = re.compile(r'[*]\s*(12|3|4)(?:\.0*)?[dDfFmM]?(?![\w.])|/\s*4(?:\.0*)?[dDfFmM]?(?![\w.])')
RX_CAL = re.compile(r'(?<![\w])(?:Calendar\.)?(MonthsPerYear|MonthsPerQuarter|QuartersPerYear)\b')
for f, (raw, s) in FILES.items():
    if is_instr(f) or f in ('Calendar.cs',): continue
    for m in RX_CONV.finditer(s):
        ln = lineno(f, m.start()); mem = member_at(f, m.start())
        divs.append(dict(file=f, line=ln, div=re.sub(r'\s+', '', m.group(0)), member=(mem[3] if mem else '?'), text=line_text(f, ln)[:160]))
    for m in RX_CAL.finditer(s):
        pre = re.sub(r'\(\s*(double|int|float)\s*\)', '', s[max(0, m.start() - 30):m.start()]).rstrip()
        post = s[m.end():m.end() + 6].lstrip()
        op = (pre[-1:] if pre[-1:] in '*/%' else '') + '|' + (post[:1] if post[:1] in '*/%' else '')
        if op == '|': continue
        kind = 'cal%' if '%' in op else 'cal*/'
        ln = lineno(f, m.start()); mem = member_at(f, m.start())
        divs.append(dict(file=f, line=ln, div=kind + ':' + m.group(1), member=(mem[3] if mem else '?'), text=line_text(f, ln)[:160]))
json.dump(dict(writers=writers, reads=reads, divs=divs,
               members=[list(m) for m in MEMBERS], state={k: sorted(v) for k, v in STATE.items()},
               computed={k: sorted(v) for k, v in COMPUTED.items()},
               mreads={f'{k[0]}:{k[1]}': sorted(v) for k, v in READS_IN_MEMBER.items()},
               mcalls={f'{k[0]}:{k[1]}': sorted(v) for k, v in CALLS.items()}),
          open(os.path.join(OUT, 'graph_raw.json'), 'w', encoding='utf-8'), ensure_ascii=False)
print('files', len(FILES), 'members', len(MEMBERS), 'state names', len(STATE), 'writers', len(writers), 'reads', len(reads), 'divs', len(divs))
