# (1367) c144gr 読む：graph_raw.json から表を作る（数え方の定義は graph.py の頭と同じ）
import json, os, re, sys, collections, itertools
T = chr(9); NL = chr(10)
ROOT = sys.argv[1] if len(sys.argv) > 1 else 'D:/PoB_work/r1367_work/src/PriceOfBread.SimCore'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/r1367_work/out'
g = json.load(open(os.path.join(OUT, 'graph_raw.json'), encoding='utf-8'))
W = [w for w in g['writers'] if not w['instr']]
R = [r for r in g['reads'] if not r['instr']]
STATE = g['state']; COMPUTED = g['computed']
MEMBERS = [m for m in g['members'] if not m[0].startswith('Instruments/')]
key = lambda m: f'{m[0]}:{m[1]}'
MEM_BY_NAME = collections.defaultdict(list)
for m in MEMBERS: MEM_BY_NAME[m[3]].append(m)
MREADS = g['mreads']; MCALLS = g['mcalls']

CORE = ['Pop.Militancy', 'Colony.Militancy', 'World.Legitimacy', 'Power.WarExhaustion', 'Pop.Consciousness',
        'Pop.Satisfaction', 'Politics.Discontent()', 'World.DynasticMandate']
# 導いた値のうち欄ではなくメソッドで出る物（閉包はここで止めて節点に置き換える）
METHOD_NODES = {('Politics.cs', 'Discontent'): 'Politics.Discontent()', ('FactionSnapshot.cs', 'DiscontentOf'): 'Politics.Discontent()',
                ('PowerAffinity.cs', 'Legitimacy'): 'PowerAffinity.Legitimacy()'}
for nm, cls in COMPUTED.items():
    pass
STOP_COMPUTED = {'FoodFill': 'Pop.FoodFill()'}   # Pop の計算のプロパティ（欄の FoodFill と同名＝受け手の型で分けた）
GENERIC_MAX_DEFS = 2
SKIPPED = collections.Counter()

def node_of_member(m):
    return METHOD_NODES.get((m[0], m[3]))

STATE_CLASSES = {'World', 'Pop', 'Region', 'Power', 'Colony', 'Producer', 'Market', 'Diplomacy', 'War', 'FactionState'}
def is_state(nd):
    if nd.endswith('()'): return True
    return nd.split('.')[0] in STATE_CLASSES
def is_coef(nd): return nd.split('.')[0] == 'Coefficients'
MAXDEPTH = 5; MAXLINES = 120
CLASSNAMES = {m[4] for m in MEMBERS}
RECV_HINT = {'w': 'World', 'world': 'World', 'World': 'World', 'aw': 'World', 'ow': 'World', 'Diplomacy': 'Diplomacy', 'diplomacy': 'Diplomacy'}
def resolve(call):
    """`受け手.名` は受け手がクラス名（静的の呼び出し）か既知の受け手の名ならそのクラスの定義だけへ。分からない受け手は同名の全ての定義。"""
    if '.' in call:
        recv, nm = call.split('.', 1)
        defs = MEM_BY_NAME.get(nm, [])
        cls = recv if recv in CLASSNAMES else RECV_HINT.get(recv)
        if cls:
            d2 = [m for m in defs if m[4] == cls]
            return nm, d2
        if recv[:1].isupper() and recv not in STATE:   # SimCore の外の静的クラス（Math・Array ほか）
            return nm, []
        return nm, defs
    return call, MEM_BY_NAME.get(call, [])
def mlines(m):
    s_ = open(os.path.join(ROOT, m[0]), 'rb').read().decode('utf-8-sig', 'replace') if m[0] not in _SRCC else _SRCC[m[0]]
    _SRCC[m[0]] = s_
    return s_.count('\n', m[1], m[2])
_SRCC = {}
BIG = {key(m) for m in MEMBERS if mlines(m) > MAXLINES}
CL = {}
def closure(k, depth=MAXDEPTH, stack=()):
    """メソッド k の本体が読む状態の欄（呼び出しを depth 段まで〔5〕・120 行を超えるメソッドの中へは入らない・節点のメソッドで止める）"""
    ck = (k, depth)
    if ck in CL: return CL[ck]
    if k in stack: return {}
    acc = {nd: {'(直接)'} for nd in MREADS.get(k, []) if is_state(nd)}
    if depth > 0:
        for c_ in MCALLS.get(k, []):
            nm, defs = resolve(c_)
            if nm in STATE and nm in COMPUTED: continue
            if len(defs) > GENERIC_MAX_DEFS: SKIPPED[nm] += 1; continue
            for m2 in defs:
                nd = node_of_member(m2)
                if nd: acc.setdefault(nd, set()).add(nm); continue
                if key(m2) in BIG: SKIPPED['(大)' + nm] += 1; continue
                for nd2 in closure(key(m2), depth - 1, stack + (k,)):
                    acc.setdefault(nd2, set()).add(nm)
    CL[ck] = acc
    return acc
sys.setrecursionlimit(50000)

def node_of_member(m):
    return METHOD_NODES.get((m[0], m[3]))

def resolved_calls(calls):
    out = []
    for c_ in calls:
        nm, defs = resolve(c_)
        if nm in STATE and nm in COMPUTED: continue
        if defs: out.append((nm, defs))
    return out

def writer_inputs(w):
    """直接の項（局所の展開を含む）・制御（囲む if の条件）の項・呼び出し経由の欄。係数は別に数える。"""
    ins = {nd: {'直接'} for nd in w['in_direct'] if is_state(nd)}
    for nd in w.get('in_ctrl', []):
        if is_state(nd): ins.setdefault(nd, set()).add('制御')
    for lab, cl in (('経由:', w['calls']), ('制御経由:', w.get('ctrl_calls', []))):
        for nm, defs in resolved_calls(cl):
            if len(defs) > GENERIC_MAX_DEFS: SKIPPED[nm] += 1; continue
            for m2 in defs:
                nd = node_of_member(m2)
                if nd: ins.setdefault(nd, set()).add(lab + nm); continue
                if key(m2) in BIG: SKIPPED['(大)' + nm] += 1; continue
                for nd2 in closure(key(m2), MAXDEPTH - 1):
                    ins.setdefault(nd2, set()).add(lab + nm)
    ins.pop(w['node'], None)
    return ins

# ---- 全ての欄の入力・出力 ----
INPUTS = collections.defaultdict(lambda: collections.defaultdict(set))   # node -> src -> {how@site}
COEFS = collections.defaultdict(set); CALLTERMS = collections.defaultdict(set)
WRITERS = collections.defaultdict(list)
for w in W:
    if w['node'].startswith('?.'): continue
    WRITERS[w['node']].append(w)
    COEFS[w['node']] |= {x for x in w['in_direct'] if is_coef(x)}
    CALLTERMS[w['node']] |= {nm for nm, d in resolved_calls(w['calls'])}
    for src_, hows in writer_inputs(w).items():
        if src_.startswith('?.'): continue
        for h in hows: INPUTS[w['node']][src_].add(f"{h}@{w['file']}:{w['line']}")
for m in MEMBERS:
    nd = node_of_member(m)
    if not nd: continue
    WRITERS[nd].append(dict(node=nd, file=m[0], line=None, form='method', kind='method', op='def', rhs='(メソッドの本体)', member=m[3]))
    for src_, hows in closure(key(m), MAXDEPTH).items():
        if src_ != nd and not src_.startswith('?.'):
            for h in hows: INPUTS[nd][src_].add(('直接' if h == '(直接)' else '経由:' + h) + f'@{m[0]}:{m[3]}')
    CALLTERMS[nd] |= {nm for nm, d in resolved_calls(MCALLS.get(key(m), []))}
for m in MEMBERS:
    if m[3] == 'FoodFill' and m[5] == 'prop' and m[4] == 'Pop':
        WRITERS['Pop.FoodFill()'].append(dict(node='Pop.FoodFill()', file=m[0], line=None, form='computed', kind='prop', op='=>', rhs='FoodMet/FoodNeed', member='FoodFill'))
        for src_ in MREADS.get(key(m), []): INPUTS['Pop.FoodFill()'][src_].add(f'直接@{m[0]}:FoodFill')

OUTS = collections.defaultdict(list)
for r in R:
    if r['self_rw'] or r['node'].startswith('?.'): continue
    OUTS[r['node']].append(r)
# メソッドの節点の出力＝呼び出しの場所
SRC = {}
def src(f):
    if f not in SRC: SRC[f] = open(os.path.join(ROOT, f), 'rb').read().decode('utf-8-sig', 'replace')
    return SRC[f]
CALL_RX = {'Politics.Discontent()': re.compile(r'(?:\bDiscontentOf|\bPolitics\.Discontent|(?<![\w.])Discontent)\s*\('),
           'PowerAffinity.Legitimacy()': re.compile(r'\bPowerAffinity\.Legitimacy\s*\(|(?<![\w.])Legitimacy\s*\(')}
for nd, rx in CALL_RX.items():
    for m in MEMBERS:
        pass
    for f in sorted({m[0] for m in MEMBERS}):
        lines = src(f).split('\n')
        for i, ln in enumerate(lines, 1):
            code = ln.split('//')[0]
            if re.search(r'(internal|public|private)\s+static\s+double\s+(Discontent|DiscontentOf|Legitimacy)\s*\(', code): continue
            if nd == 'PowerAffinity.Legitimacy()' and f != 'PowerAffinity.cs' and 'PowerAffinity.Legitimacy' not in code: continue
            for _ in rx.finditer(code):
                OUTS[nd].append(dict(node=nd, file=f, line=i, member='?', ai=f in ('SelfPlay.cs', 'AiRootSupply.cs'), into=[]))

# ---- wr の正規表現そのまま（SimCore 直下＋Instruments・コメントの後ろを除く） ----
def wr_count(member):
    rx = re.compile(r'\.' + member + r'(\[[^\]]*\])?\s*(\+=|-=|\*=|/=|=(?![=>]))')
    n = 0
    for d in (ROOT, os.path.join(ROOT, 'Instruments')):
        for fn in os.listdir(d):
            if not fn.endswith('.cs'): continue
            for line in open(os.path.join(d, fn), 'rb').read().decode('utf-8-sig', 'replace').split('\n'):
                c = line.find('//'); code = line[:c] if c >= 0 else line
                n += len(rx.findall(code))
    return n

def short(n): return n
def nodeinfo(nd):
    ws = [w for w in WRITERS.get(nd, []) if w['kind'] != 'init']; ins = INPUTS.get(nd, {}); outs = OUTS.get(nd, [])
    kinds = collections.Counter(w['kind'] for w in WRITERS.get(nd, []))
    direct = sorted(s for s, h in ins.items() if any(x.startswith('直接') or x.startswith('制御@') for x in h))
    via = sorted(s for s, h in ins.items() if not any(x.startswith('直接') or x.startswith('制御@') for x in h))
    return dict(writers=len(ws), kinds=dict(kinds), inputs=len(direct), in_direct=direct, in_via=via, n_via=len(via),
                coefs=len(COEFS.get(nd, ())), calls=sorted(CALLTERMS.get(nd, ())), outputs=len(outs),
                out_ai=sum(1 for r in outs if r.get('ai')), out_files=len({r['file'] for r in outs}))

# ---- 候補の選び方：核に入るか核から出る（直接か経由）欄で、書き手 1 以上・入力 3 以上。入力＋出力の多い順に 12 ----
EXCL_CLASS = {'Coefficients', 'Good', 'NationConfig', 'Calendar'}
NUMERIC = {'double', 'float', 'decimal', 'MoneyPerMonth', 'MoneyPerYear', 'MoneyStock', 'RatePerMonth', 'RatePerQuarter', 'RatePerYear', 'RatioToGdpYear', 'RealGdpPerMonth', 'Share'}
EXCL_WEIGHT = {'Pop.Size', 'World.Latitude', 'World.Longitude'}   # 重みと人口の在庫・地理の定数（導いた値ではない）は候補から外す〈推〉
TYPES = {}
def field_type(nd):
    if nd in TYPES: return TYPES[nd]
    cls, nm = nd.split('.')[0], nd.split('.')[-1].rstrip('()')
    t = '?'
    rx = re.compile(r'(?:public|internal|private|protected)[^;=(){}\n]*?\b([\w<>\[\],.?]+)\s+' + re.escape(nm) + r'\s*(?:[;={])')
    for f in sorted({m[0] for m in MEMBERS}):
        s_ = src(f)
        for mm in re.finditer(r'\b(?:class|struct|record)\s+' + re.escape(cls) + r'\b', s_):
            m2 = rx.search(s_, mm.end())
            if m2: t = m2.group(1).rstrip('?'); break
        if t != '?': break
    TYPES[nd] = t
    return t
def ok(nd):
    c = nd.split('.')[0]
    if c in EXCL_CLASS or nd.startswith('?.') or nd in EXCL_WEIGHT or not is_state(nd): return False
    return nd.endswith('()') or field_type(nd) in NUMERIC
core_set = set(CORE)
conn = set()
def direct_srcs(nd): return set(INPUTS.get(nd, {}).keys())   # 2 回目の改め：直接と経由（3 段・120 行以下のメソッド）の両方
for nd in CORE:
    conn |= direct_srcs(nd)
for nd in list(INPUTS.keys()):
    if core_set & direct_srcs(nd): conn.add(nd)
conn -= core_set
scored = []
# 候補の規則（結果を見る前の 1 回目は〈入力＋出力の多い順〉で財政の予算の欄が並んだ＝局所の展開が長いメソッドの欄が上に来る＝目的に合わない。
# 2 回目＝〈核と核の間にある値〉：核を読む向きと核に読まれる向き（直接か経由）の数の和 deg が 2 以上。deg、出力、入力の順）
for nd in conn:
    if not ok(nd): continue
    i = nodeinfo(nd)
    if i['writers'] < 1: continue
    up = sorted(c for c in CORE if c in direct_srcs(nd))
    down = sorted(c for c in CORE if nd in direct_srcs(c))
    deg = len(up) + len(down)
    if deg >= 2:
        scored.append((deg, i['outputs'], i['inputs'], nd, up, down))
scored.sort(key=lambda x: (-x[0], -x[1], -x[2], x[3]))
FORCED = ['World.FoodFill', 'Region.FoodFill', 'Pop.FoodFill()', 'Pop.Literacy', 'Pop.LivingRef', 'Pop.FoodRef', 'World.FamineMemory', 'World.DefeatShock']
CAND = [x[3] for x in scored[:14]]
# 候補は名乗って固定する（呼び出しの段を 3 で選んだ時の上位 14＝核と核の間 deg≥2。段 5 で選び直すと海軍の欄〔FleetGauge ほか〕が経由の閉包で上がる＝選び方の揺れ〈推〉。下の順位の表は段 5 の参考）
CAND = ['Power.Military', 'Pop.Literacy', 'World.MeanSatisfactionPol', 'World.ParliamentControl', 'World.ReformistPower', 'World.Coercion',
        'World.RadicalPower', 'World.PriceLevel', 'Region.AdminFill', 'Pop.RemainingBudget', 'War.Score', 'World.FiscalFundingRatio',
        'World.CumulativeDevaluation', 'Power.NeedSea']
for nd in FORCED:
    if nd not in CAND: CAND.append(nd)
N = CORE + CAND

# ---- 表 T1：節点 ----
with open(os.path.join(OUT, 'T1_nodes.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write(T.join(['節点', '区分', 'wr互換の書き手(直下+計器)', '書き手(計器と初期化子を除く)', '書き手の種類(初期化子を含む)', '入力(状態の項・直接)の数', '入力(直接)', '係数の項の数', '呼び出しの項', '経由だけの入力の数', '経由だけの入力', '出力の数', 'うちAI', '出力のファイル数']) + NL)
    for nd in N:
        i = nodeinfo(nd); nm = nd.split('.')[-1].rstrip('()')
        wr = wr_count(nm) if not nd.endswith('()') else '-'
        fo.write(T.join(str(x) for x in [nd, '核' if nd in core_set else '候補', wr, i['writers'], i['kinds'], i['inputs'], ' '.join(i['in_direct']), i['coefs'], ' '.join(i['calls']), i['n_via'], ' '.join(i['in_via'])[:400], i['outputs'], i['out_ai'], i['out_files']]) + NL)
    fo.write(NL + '# 候補の選び方の順位（上位 40・核と核の間＝deg 2 以上・deg・出力・入力の順）' + NL)
    for (deg, o, i_, nd, up, down) in scored[:40]:
        fo.write(T.join(['#', nd, f'deg{deg}', f'出力{o}', f'入力{i_}', '核を読む:' + ' '.join(up), '核に読まれる:' + ' '.join(down)]) + NL)

# ---- 表 T2：辺（N の中） ----
EDGES = {}
for c in N:
    for a, hows in INPUTS.get(c, {}).items():
        if a in N and a != c: EDGES[(a, c)] = sorted(hows)
with open(os.path.join(OUT, 'T2_edges.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('元\t先\t道(直接@書き手の行 / 経由:メソッド@書き手の行)\n')
    for (a, c), hows in sorted(EDGES.items()):
        fo.write(f"{a}\t{c}\t{' | '.join(hows[:12])}{' …他' + str(len(hows) - 12) if len(hows) > 12 else ''}\n")

# ---- 表 T3：書き手（核と候補） ----
with open(os.path.join(OUT, 'T3_writers.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('節点\tファイル:行\tメソッド\t種類\t演算\t形\t入力(直接)\t経由の呼び出し\t右辺\n')
    for nd in N:
        for w in WRITERS.get(nd, []):
            if w['kind'] == 'init': continue
            ins = writer_inputs(w) if 'in_direct' in w else {}
            via = sorted({h for hs in ins.values() for h in hs if h.startswith('経由')})
            fo.write(f"{nd}\t{w['file']}:{w['line']}\t{w.get('member','')}\t{w['kind']}\t{w['op']}\t{w.get('form','')}\t{' '.join(w.get('in_direct', []))}\t{' '.join(via)[:200]}\t{w['rhs'][:200]}\n")
    fo.write('\n# 初期化子（new X { 名前 = … }）の書き手の数（構築＝wr は数えない）\n')
    for nd in N:
        n = sum(1 for w in WRITERS.get(nd, []) if w['kind'] == 'init')
        if n: fo.write(f'#\t{nd}\t{n}\n')

# ---- 表 T4：読み手（核の出力の場所） ----
with open(os.path.join(OUT, 'T4_readers_core.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('節点\tファイル:行\tメソッド\tAI\t流れ込む書き手(右辺か局所の展開)\n')
    for nd in CORE:
        for r in sorted(OUTS.get(nd, []), key=lambda r: (r['file'], r['line'])):
            fo.write(f"{nd}\t{r['file']}:{r['line']}\t{r.get('member','?')}\t{'AI' if r.get('ai') else ''}\t{' '.join(r.get('into', []))}\n")

# ---- 三角形・近道・輪 ----
adj = collections.defaultdict(set)
for (a, c) in EDGES: adj[a].add(c)
def alt_path(a, c, maxlen=5):
    # a→c の辺を使わない a⇝c の最長でない道を 1 本（長さ 2 以上）
    from collections import deque
    best = None
    stack = [(a, [a])]
    found = []
    def dfs(u, path):
        if len(path) > maxlen: return
        for v in adj[u]:
            if u == a and v == c: continue
            if v in path: continue
            if v == c: found.append(path + [v]); continue
            dfs(v, path + [v])
    dfs(a, [a])
    return sorted(found, key=len, reverse=True)
tri = []
for (a, c) in EDGES:
    for b in adj[a]:
        if b != c and (b, c) in EDGES: tri.append((a, b, c))
short = []
for (a, c), hows in EDGES.items():
    ps = alt_path(a, c)
    if ps: short.append((a, c, ps, hows))
def isd(a, c): return any(h.startswith('直接') or h.startswith('制御@') for h in EDGES[(a, c)])
with open(os.path.join(OUT, 'T5_triangles.tsv'), 'w', encoding='utf-8', newline=NL) as fo:
    fo.write(T.join(['A', 'B', 'C', '3 辺とも直接か', 'A→C の道（消す候補）', 'A→B の道', 'B→C の道', 'C の出力の数', 'C の出力のうち AI']) + NL)
    for (a, b_, c) in sorted(tri, key=lambda t: (not (isd(t[0], t[2]) and isd(t[0], t[1]) and isd(t[1], t[2])), t)):
        ic = nodeinfo(c)
        alld = '直接' if (isd(a, c) and isd(a, b_) and isd(b_, c)) else '経由を含む〈推〉'
        fo.write(T.join(str(x) for x in [a, b_, c, alld, ' | '.join(EDGES[(a, c)][:6]), ' | '.join(EDGES[(a, b_)][:3]), ' | '.join(EDGES[(b_, c)][:3]), ic['outputs'], ic['out_ai']]) + NL)
with open(os.path.join(OUT, 'T6_shortcuts.tsv'), 'w', encoding='utf-8', newline=NL) as fo:
    fo.write(T.join(['近道 A→C', '最長の代わりの道', '代わりの道の本数', 'A→C の道の種類(直接/経由)', '代わりの道の辺が全て直接か', 'A→C の書き手の行']) + NL)
    for (a, c, ps, hows) in sorted(short, key=lambda x: (-len(x[2][0]), x[0], x[1])):
        kinds = sorted({h.split('@')[0].split(':')[0] for h in hows})
        sites = sorted({h.split('@')[1] for h in hows})
        p0 = ps[0]
        alld = all(isd(p0[i], p0[i + 1]) for i in range(len(p0) - 1))
        fo.write(T.join([f'{a}→{c}', '→'.join(p0), str(len(ps)), ','.join(kinds), '直接' if alld else '経由を含む〈推〉', ' '.join(sites[:8])]) + NL)
# 輪（長さ 2〜4 の単純な輪）
cycles = set()
def cyc(start, u, path):
    if len(path) > 4: return
    for v in adj[u]:
        if v == start and len(path) >= 2:
            k = min(range(len(path)), key=lambda i: path[i]); cycles.add(tuple(path[k:] + path[:k]))
        elif v not in path and v > start: cyc(start, v, path + [v])
for s0 in N: cyc(s0, s0, [s0])
with open(os.path.join(OUT, 'T7_cycles.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('輪(長さ)\t輪\t各辺の道(先頭 2 本)\n')
    for cy in sorted(cycles, key=lambda c: (len(c), c)):
        segs = []
        for i in range(len(cy)):
            a, c = cy[i], cy[(i + 1) % len(cy)]
            segs.append(f"{a}→{c}: {' | '.join(EDGES[(a, c)][:2])}")
        fo.write(f"{len(cy)}\t{'→'.join(cy)}→{cy[0]}\t{' ;; '.join(segs)}\n")

# ---- 表 T8：Toward を通らない手書きの寄りと減衰（SimCore 全域・計器を除く） ----
with open(os.path.join(OUT, 'T8_hand_blend_decay.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('欄\tファイル:行\tメソッド\t形\t演算\t核か候補か\t右辺\n')
    rows = [w for w in W if w['form'].startswith('hand-')]
    for w in sorted(rows, key=lambda w: (w['file'], w['line'])):
        tag = '核' if w['node'] in core_set else ('候補' if w['node'] in N else '')
        fo.write(f"{w['node']}\t{w['file']}:{w['line']}\t{w['member']}\t{w['form']}\t{w['op']}\t{tag}\t{w['rhs'][:160]}\n")
    fo.write('\n# Toward を通る書き手（参考）\n')
    for w in sorted([w for w in W if w['form'] == 'Toward'], key=lambda w: (w['file'], w['line'])):
        fo.write(f"#\t{w['node']}\t{w['file']}:{w['line']}\n")
    fo.write('\n# 寄りか減衰かを機械で決められなかった自己参照（self-other）と定数倍（scale）の書き手（参考・目で読む）\n')
    for w in sorted([w for w in W if w['form'] in ('self-other', 'scale(x*=lit)')], key=lambda w: (w['file'], w['line'])):
        fo.write(f"#\t{w['form']}\t{w['node']}\t{w['file']}:{w['line']}\t{w['rhs'][:120]}\n")

# ---- 表 T9：手書きの /12 と /3（と暦の定数の掛け割り） ----
with open(os.path.join(OUT, 'T9_div12_div3.tsv'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write('種類\tファイル:行\tメソッド\t行\n')
    for d in sorted(g['divs'], key=lambda d: (d['div'], d['file'], d['line'])):
        fo.write(f"{d['div']}\t{d['file']}:{d['line']}\t{d['member']}\t{d['text']}\n")

with open(os.path.join(OUT, 'T0_summary.txt'), 'w', encoding='utf-8', newline='\n') as fo:
    fo.write(f"核 {len(CORE)}・候補 {len(CAND)}：{' '.join(CAND)}\n辺 {len(EDGES)}・三角形 {len(tri)}・近道（代わりの道あり）{len(short)}・輪 {len(cycles)}\n")
    fo.write(f"手書きの寄り・減衰 {sum(1 for w in W if w['form'].startswith('hand-'))}（核 {sum(1 for w in W if w['form'].startswith('hand-') and w['node'] in core_set)}・候補 {sum(1 for w in W if w['form'].startswith('hand-') and w['node'] in N and w['node'] not in core_set)}）・Toward {sum(1 for w in W if w['form']=='Toward')}\n")
    fo.write(f"/12・/3 と暦の定数：{collections.Counter(d['div'] for d in g['divs'])}\n")
    fo.write(f"名前で引けず飛ばした呼び出し（同名の定義が {GENERIC_MAX_DEFS} を超える）上位：{SKIPPED.most_common(25)}\n")
print(open(os.path.join(OUT, 'T0_summary.txt'), encoding='utf-8').read())
