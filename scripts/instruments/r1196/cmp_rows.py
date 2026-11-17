# 読むだけ (1169) c115lg：11-12 の台帳の行（S01・S02）の錨が、読んだ木 OLD から今の木 NEW までに〈コードとして〉変わったかを数える。
#  changed_rows.py の限界（ファイル単位＝注の移し替え (1113) c107hb だけでほぼ全ての持ち主のファイルが拾われる）を埋める：
#   ①錨の行の字（注と空白を除く）が NEW の同じファイルにあるか（同じ字の行が複数なら全て）
#   ②錨の行を囲む関数（OLD の宣言の行から波括弧の対まで・注を除く）が NEW の同じ名の関数と字で同じか
#   ③錨と囲む関数の中に出る係数（c.X・coef.X・Coef.X・Coefficients.cs の錨の名）の宣言の値が OLD と NEW で同じか
#  NEW の錨の行番号を取り直す（①の行・複数なら OLD の関数の中の相対の位置に近い物）。
# 使い方: python -X utf8 -I cmp_rows.py <repo> <OLD> <NEW> <11-12 の tsv> <出力 tsv>
import sys, subprocess, re, collections
repo, OLD, NEW, tsv, out = sys.argv[1:6]
def git(*a): return subprocess.run(['git', '-C', repo, *a], capture_output=True).stdout
def ls(commit): return [p for p in git('ls-tree', '-r', '--name-only', commit).decode().split('\n') if p]
files = {OLD: ls(OLD), NEW: ls(NEW)}
def resolve(commit, fn):
    c = [p for p in files[commit] if p.split('/')[-1] == fn]
    s = [p for p in c if p.startswith('PriceOfBread.SimCore/')]
    return (s or c or [None])[0]
cache = {}
def lines(commit, path):
    k = (commit, path)
    if k not in cache: cache[k] = git('show', f'{commit}:{path}').decode('utf-8-sig', 'replace').split('\n')
    return cache[k]
def strip_comments(ls_):
    out_, inblk = [], False
    for l in ls_:
        s, i, res, instr = l, 0, '', None
        while i < len(s):
            if inblk:
                j = s.find('*/', i)
                if j < 0: i = len(s); break
                inblk = False; i = j + 2; continue
            ch = s[i]
            if instr:
                res += ch
                if ch == '\\' and instr == '"' : res += s[i+1:i+2]; i += 2; continue
                if ch == instr: instr = None
                i += 1; continue
            if s.startswith('//', i): break
            if s.startswith('/*', i): inblk = True; i += 2; continue
            if ch in '"\'': instr = ch
            res += ch; i += 1
        out_.append(re.sub(r'\s+', ' ', res).strip())
    return out_
scache = {}
def slines(commit, path):
    k = (commit, path)
    if k not in scache: scache[k] = strip_comments(lines(commit, path))
    return scache[k]
SIG = re.compile(r'^(?:\[[^\]]*\] )*(?:(?:public|private|internal|protected|static|override|virtual|readonly|sealed|unsafe|partial|extern|new|abstract|async) )+[^=;]*?\b([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*\(')
def enclosing(commit, path, ln):
    S = slines(commit, path)
    i = ln - 1
    if i >= len(S): return None   # (1196) 写しの直し：錨の行が OLD のファイルの外＝取り直せない側へ
    while i >= 0:
        m = SIG.match(S[i])
        if m and not S[i].endswith(';'):
            # 波括弧の対
            depth, started, j = 0, False, i
            while j < len(S):
                depth += S[j].count('{') - S[j].count('}')
                if '{' in S[j] or '=>' in S[j]: started = True
                if started and depth <= 0 and (j > i or S[j].endswith(';') or S[j].endswith('}')):
                    break
                j += 1
            if j >= ln - 1: return m.group(1), i, j
            # 錨が関数の外＝メンバーの宣言の行
            return None
        i -= 1
    return None
def body(commit, path, i, j): return [x for x in slines(commit, path)[i:j + 1] if x]
def find_method(commit, path, name, sig_old):
    S = slines(commit, path); hits = []
    for i, x in enumerate(S):
        m = SIG.match(x)
        if m and m.group(1) == name and not x.endswith(';'):
            e = enclosing(commit, path, i + 1)
            if e: hits.append(e)
    exact = [h for h in hits if S[h[1]] == sig_old]
    return (exact or hits)
COEF = re.compile(r'\b(?:c|Coef|coef|Coefs)\.([A-Z]\w*)')
def coef_decl(commit):
    d = {}
    for p in files[commit]:
        fn = p.split('/')[-1]
        if not (p.startswith('PriceOfBread.SimCore/') and fn.startswith('Coefficients') and fn.endswith('.cs')): continue
        for k, x in enumerate(slines(commit, p), 1):
            m = re.match(r'^public (?:double|int|bool|float|long|string|[A-Z]\w*) ([A-Z]\w*)\s*(\{[^}]*\})?\s*=\s*(.+?);$', x)
            if m: d[m.group(1)] = (m.group(3), fn, k)
    return d
CO, CN = coef_decl(OLD), coef_decl(NEW)
rows = []
for l in open(tsv, 'rb').read().decode('utf-8').split('\n')[1:]:
    c = l.split('\t')
    if len(c) < 12: continue
    rows.append(c)
res = []
for c in rows:
    rid, anchors = c[0], c[4]
    notes, newanch, coefs = [], [], set()
    changed = False
    for a in anchors.split(';'):
        a = a.strip(); m = re.match(r'^([^:]+):(\d+):(.+)$', a)
        if not m: notes.append(f'形が違う {a[:30]}'); changed = True; continue
        fn, ln, tok = m.group(1), int(m.group(2)), m.group(3).strip()
        if fn == 'Coefficients.cs':
            name = re.match(r'([A-Za-z_]\w*)', tok).group(1)
            coefs.add(name)
            if name not in CN: notes.append(f'係数が消えた {name}'); changed = True; continue
            v, nf, nl = CN[name]
            newanch.append(f'{nf}:{nl}:{name}')
            continue
        po, pn = resolve(OLD, fn), resolve(NEW, fn)
        if po is None: notes.append(f'OLD に無い {fn}'); changed = True; continue
        if pn is None: notes.append(f'NEW に無い {fn}'); changed = True; continue
        so = slines(OLD, po)
        txt = so[ln - 1] if 0 < ln <= len(so) else ''
        coefs |= set(COEF.findall(txt))
        sn = slines(NEW, pn)
        enc = enclosing(OLD, po, ln)
        cand = [k + 1 for k, x in enumerate(sn) if txt and x == txt]
        if enc:
            name, i, j = enc
            bo = body(OLD, po, i, j)
            for x in bo: coefs |= set(COEF.findall(x))
            ms = find_method(NEW, pn, name, so[i])
            same = [h for h in ms if body(NEW, pn, h[1], h[2]) == bo]
            if same:
                h = same[0]; tgt = h[1] + 1 + (ln - 1 - i)
                inside = [k for k in cand if h[1] < k <= h[2] + 1]
                nl = min(inside, key=lambda k: abs(k - tgt)) if inside else tgt
            else:
                changed = True
                notes.append(f'関数 {name} の字が違う' if ms else f'関数 {name} が NEW に無い')
                inside = []
                for h in ms: inside += [k for k in cand if h[1] < k <= h[2] + 1]
                nl = inside[0] if inside else (cand[0] if cand else None)
        else:
            if not cand: changed = True; notes.append(f'行の字が NEW に無い（関数の外）')
            nl = cand[0] if cand else None
            if len(cand) > 1: notes.append(f'同じ字の行 {len(cand)}')
        if nl is None:
            notes.append(f'錨 {fn}:{ln} を取り直せない'); newanch.append(f'{fn}:?:{tok[:40]}')
        else:
            # 名＝元の字が行にあればそのまま・無ければ行の最初の識別子
            raw = lines(NEW, pn)[nl - 1]
            t2 = tok if tok in raw else None
            if t2 is None:
                ids = [w for w in re.findall(r'[A-Za-z_]\w{3,}', tok) if w in raw]
                t2 = ids[0] if ids else '?'
                if t2 == '?': notes.append(f'錨の名が行に無い {fn}:{nl}')
            newanch.append(f'{fn}:{nl}:{t2}')
    cdiff = []
    for n in sorted(coefs):
        o, nw = CO.get(n), CN.get(n)
        if o is None and nw is None: continue
        if o is None or nw is None or o[0] != nw[0]: cdiff.append(f'{n}:{o[0] if o else "無"}→{nw[0] if nw else "無"}')
    if cdiff: changed = True; notes.append('係数の値 ' + ' '.join(cdiff))
    res.append((rid, '変わった' if changed else '変わらない', ';'.join(newanch), '；'.join(notes), ' '.join(sorted(coefs))))
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    f.write('行\t判定\t新しい錨\t違い\t係数\n')
    for r in res: f.write('\t'.join(r) + '\n')
cnt = collections.Counter(r[1] for r in res)
print(dict(cnt))
