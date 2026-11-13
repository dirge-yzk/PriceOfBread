# (1114) c107hc：Coefficients の部品の注を、今の意味と単位の短い注へ縮め、経緯を docs/history/<部品の名>.md へ写す。
# 使い方：python cmove.py forward <SimCore の dir> <history の dir>   ＝ 注を縮める（部品 10 本を書き換える）
#         python cmove.py restore <SimCore の dir> <history の dir> <出力 dir> ＝ 新＋履歴から元の字を作る（門 1）
#         python cmove.py tokens <ファイル>  ＝ 注を除いた字句の列を出す（門 2）
# バイトで読み書きする（BOM と CR の混在を保つ）。
import os, re, sys, json

PARTS = ["Coefficients.cs"] + [f for f in sorted(os.listdir(sys.argv[2])) if re.match(r"Coefficients\.p\d\d_.*\.cs$", f)] if len(sys.argv) > 2 and os.path.isdir(sys.argv[2]) else []


def lex(t):
    """コメントの区間 [(start, end, kind)] を返す。kind は 'line' か 'block'。文字列・文字は飛ばす。"""
    n = len(t); i = 0; out = []
    while i < n:
        c = t[i]
        if c == '/' and i + 1 < n and t[i + 1] == '/':
            j = t.find('\n', i)
            j = n if j < 0 else j
            if j > i and t[j - 1] == '\r':
                j -= 1
            out.append((i, j, 'line')); i = j; continue
        if c == '/' and i + 1 < n and t[i + 1] == '*':
            j = t.find('*/', i + 2)
            j = n if j < 0 else j + 2
            out.append((i, j, 'block')); i = j; continue
        if c == '"' or (c in '$@' and i + 1 < n and t[i + 1] in '"$@'):
            k = i
            verb = False; interp = False
            while k < n and t[k] in '$@':
                verb |= t[k] == '@'; interp |= t[k] == '$'; k += 1
            if k < n and t[k] == '"':
                k += 1
                depth = 0
                while k < n:
                    ch = t[k]
                    if verb:
                        if ch == '"':
                            if k + 1 < n and t[k + 1] == '"': k += 2; continue
                            k += 1; break
                    else:
                        if ch == '\\': k += 2; continue
                        if ch == '"' and depth == 0: k += 1; break
                    if interp and not verb:
                        if ch == '{':
                            if k + 1 < n and t[k + 1] == '{': k += 2; continue
                            depth += 1
                        elif ch == '}':
                            if k + 1 < n and t[k + 1] == '}': k += 2; continue
                            depth = max(0, depth - 1)
                    k += 1
                i = k; continue
            i += 1; continue
        if c == "'":
            k = i + 1
            if k < n and t[k] == '\\': k += 2
            else: k += 1
            while k < n and t[k] != "'" and k - i < 12: k += 1
            i = k + 1; continue
        i += 1
    return out


def strip_comments(t):
    sp = lex(t); out = []; p = 0
    for s, e, _ in sp:
        out.append(t[p:s]); out.append(' '); p = e
    out.append(t[p:])
    return ''.join(out)


def tokens(t):
    return re.findall(r"\w+|[^\w\s]", strip_comments(t))


MARK = re.compile(r"^\s*([★⚠✔✓※◎△〔【~#]|\(\d{2,4}\)|\[PORT)")
HIST = re.compile(r"20\d\d-\d\d-\d\d|20\d\d年|\(\d{3,4}\)|点火|墓標|焼き込み|作者|腕|旗|裁定|差し戻|採用|撤回|実測|棚卸し|ラウンド|回で|機能\d")
HEADER = re.compile(r"^\s*//\s*(---|===|━|###)")


def body(text):
    if text.startswith('///'): return re.sub(r"<[^>]*>", "", text[3:]).strip()
    if text.startswith('//'): return text[2:].strip()
    if text.startswith('/*'): return text[2:-2].strip() if text.endswith('*/') else text[2:].strip()
    return text.strip()


def make_note(bodies, port=None):
    sents = []
    for b in bodies:
        b = re.sub(r"\s+", " ", b).strip()
        cur = ''; depth = 0
        for ch in b:
            cur += ch
            if ch in '（(〔【「[': depth += 1
            elif ch in '）)〕】」]': depth = max(0, depth - 1)
            elif ch == '。' and depth == 0:
                if cur.strip(): sents.append(cur.strip())
                cur = ''
        if cur.strip(): sents.append(cur.strip())
    pick = None
    weak = re.compile(r"^([01]で|同上|上と同じ)")
    for s in sents:
        if not MARK.match(s) and not HIST.search(s) and len(s) >= 10 and not weak.match(s): pick = s; break
    if pick is None:
        for s in sents:
            if not MARK.match(s) and len(s) >= 10 and not weak.match(s): pick = s; break
    if pick is None:
        for s in sents:
            if not MARK.match(s): pick = s; break
    if pick is None and sents:
        pick = re.sub(r"^([★⚠✔✓※◎△~#\s]|\(\d{2,4}\)|〔[^〕]*〕|【[^】]*】|[【〔])+", "", sents[0]).strip() or sents[0]
    if pick is None: pick = ''
    pick = pick.replace('**', '').replace('★', '').strip()
    if HIST.search(pick[:60]) and '：' in pick:
        rest = pick.split('：', 1)[1].strip()
        if len(rest) >= 10 and not HIST.match(rest): pick = rest
    pick = pick.rstrip('。') if len(pick) <= 120 else pick
    if len(pick) > 120:
        cut = max(pick.rfind(x, 40, 118) for x in "、；：＝，")
        pick = pick[:cut] if cut > 0 else pick[:119] + "…"
        pick = pick.rstrip('、；：＝，')
        for o_, c_ in ('（）', '(）', '〔〕', '【】', '「」'):
            if pick.count(o_[0]) > pick.count(c_[-1]):
                cut2 = pick[:pick.rfind(o_[0])].rstrip('、；：＝，… ')
                if len(cut2) >= 8: pick = cut2
    if port and port not in pick: pick += " " + port
    return pick


DECL = re.compile(r"^\s*(public|internal|private|protected|static|\[)\b|^\s*(public|internal|private|protected|static)")
NAME = re.compile(r"(\w+)\s*(?:=|\(|;|\{|$)")


def code_name(code):
    c = code.strip()
    m = re.search(r"(\w+)\s*(?:=[^=]|\(|;)", c)
    return m.group(1) if m else (re.findall(r"\w+", c)[-1] if re.findall(r"\w+", c) else "-")


def split_lines(t):
    parts = t.split('\n')
    return [p + '\n' for p in parts[:-1]] + ([parts[-1]] if parts[-1] != '' else [])


def analyze(t):
    """行ごとの情報と、論理行（コード行）の並びを返す。"""
    sp = lex(t)
    lines = split_lines(t)
    offs = []; o = 0
    for l in lines:
        offs.append(o); o += len(l)
    # 各文字がコメントかを、区間から行ごとに引く
    import bisect
    starts = [s for s, _, _ in sp]
    def spans_in(a, b):
        res = []
        k = bisect.bisect_left(starts, a) - 1
        k = max(k, 0)
        while k < len(sp) and sp[k][0] < b:
            if sp[k][1] > a: res.append(sp[k])
            k += 1
        return res
    info = []
    for i, l in enumerate(lines):
        a = offs[i]; b = a + len(l)
        ss = spans_in(a, b)
        cov = [False] * len(l)
        for s, e, _ in ss:
            for x in range(max(s, a) - a, min(e, b) - a): cov[x] = True
        code = any((not cov[x]) and (not l[x].isspace()) for x in range(len(l)))
        info.append((code, ss, cov))
    return lines, offs, info, sp


def transform(t, report):
    lines, offs, info, sp = analyze(t)
    out = []          # 新しい行
    entries = []      # 履歴
    gap = []          # 直前の非コード行 [(idx, line)]
    codeord = 0
    i = 0
    n = len(lines)
    def flush_gap_plain():
        for _, l in gap: out.append(l)
    while i < n:
        code, ss, cov = info[i]
        if not code:
            gap.append((i, lines[i])); i += 1; continue
        l = lines[i]
        # 行の中でコメントが始まる前の区間・コード行の途中で始まるブロックの検出
        starts_here = [s for s in ss if s[0] >= offs[i]]
        mid = [s for s in ss if s[0] < offs[i]]
        if mid:
            report.append(f"bail: 行 {i+1} が前の行から続くブロックの途中にコードを持つ"); raise RuntimeError("bail mid")
        region = None; endi = i
        if starts_here:
            rs = starts_here[0][0]; re_ = starts_here[0][1]
            chain = [starts_here[0]]
            for s in starts_here[1:]:
                if t[re_:s[0]].strip() == '':
                    chain.append(s); re_ = s[1]
                else:
                    break
            # 区間の後ろにコードがあるか
            rest_end = t.find('\n', re_); rest_end = len(t) if rest_end < 0 else rest_end
            if t[re_:rest_end].strip() != '':
                # コメントの後ろにコード（行の途中のコメント）＝触らない
                report.append(f"skip-mid: 行 {i+1} は行の途中のコメント・そのまま残す")
                region = None
            else:
                # re_ の行（終端行）を求める
                # 正確な終端行
                endi = max(k for k in range(i, n) if offs[k] <= max(re_ - 1, rs))
                region = (rs, re_, chain)
        # gap の処理
        gap_has_comment = any(info[k][1] for k, _ in gap)
        newgap = []
        if gap_has_comment:
            for k, gl in gap:
                if not info[k][1] and gl.strip() == '': newgap.append(gl)
                elif info[k][1] and HEADER.match(gl.rstrip('\r\n')):
                    newgap.append(gl)
                # それ以外のコメント行は消える
        else:
            newgap = [gl for _, gl in gap]
        trail_raw = None; derived = False
        codeord += 1
        # 新しい行を作る
        endterm = lines[endi][len(lines[endi].rstrip('\r\n')):]
        if region:
            rs, re_, chain = region
            prefix = t[offs[i]:rs]
            raw = t[rs:re_]
            bodies = [body(t[s:e]) for s, e, _ in chain]
            pm = re.search(r"\[PORT:P\d+\]", raw)
            note = make_note(bodies, raw[pm.start():pm.end() + 90].splitlines()[0].rstrip() if pm else None)
            newreg = ("// " + note) if note else ""
            if newreg == raw:
                newline = l if endi == i else None
            else:
                trail_raw = raw
                newline = prefix.rstrip() + ("   " if prefix.strip() and not prefix.endswith((' ', '\t')) else prefix[len(prefix.rstrip()):]) + newreg + endterm if True else None
                # 元の空白（コードと注の間）を保つ
                newline = prefix + newreg + endterm
            if newline is None:
                newline = prefix + newreg + endterm
        else:
            codetxt = l.rstrip('\r\n')
            newline = l
            if gap_has_comment and DECL.match(codetxt) and '//' not in codetxt:
                bodies = []
                for k, gl in gap:
                    if info[k][1] and not HEADER.match(gl.rstrip('\r\n')):
                        for s, e, _ in info[k][1]:
                            bodies.append(body(t[s:e]) if True else '')
                # 行頭の連続ブロックのうち、コード行の直前の塊だけを使う
                blk = []
                for k, gl in reversed(gap):
                    if info[k][1] and not HEADER.match(gl.rstrip('\r\n')):
                        for s, e, _ in reversed(info[k][1]): blk.insert(0, body(t[s:e]))
                    else:
                        break
                note = make_note(blk if blk else bodies)
                if note:
                    newline = codetxt.rstrip() + "   // " + note + l[len(codetxt):]
                    derived = True
        if gap_has_comment or trail_raw is not None or derived:
            entries.append({
                'ord': codeord, 'line': i + 1, 'name': code_name(strip_comments(t[offs[i]:offs[endi] + len(lines[endi])])),
                'anchor': strip_comments(t[offs[i]:offs[endi] + len(lines[endi])]).rstrip(),
                'gap': [gl for _, gl in gap] if gap_has_comment else None,
                'trail': trail_raw, 'derived': derived,
            })
        out.extend(newgap)
        out.append(newline)
        gap = []
        i = endi + 1
    # ファイル末尾の gap
    tail_has_comment = any(info[k][1] for k, _ in gap)
    if tail_has_comment:
        entries.append({'ord': codeord + 1, 'line': n, 'name': '(末尾)', 'anchor': '', 'gap': [gl for _, gl in gap], 'trail': None, 'derived': False})
        for k, gl in gap:
            if not info[k][1] and gl.strip() == '': out.append(gl)
    else:
        for _, gl in gap: out.append(gl)
    return ''.join(out), entries


def crlist(lines):
    return [k for k, x in enumerate(lines) if x.endswith('\r')]


def enc_text(raw_lines_with_term):
    """行（終端つき）を、履歴の本文（終端なし・LF）と CR の位置の表へ。"""
    parts = []; cr = []; last_nl = True
    for k, x in enumerate(raw_lines_with_term):
        y = x[:-1] if x.endswith('\n') else x
        if not x.endswith('\n') and k == len(raw_lines_with_term) - 1: last_nl = False
        if y.endswith('\r'): cr.append(k); y = y[:-1]
        parts.append(y)
    return parts, cr, last_nl


FENCE = "````"


def write_history(name, entries, nfields):
    L = []
    L.append(f"# {name} の注の履歴（(1114)・作者 2026-11-13〈注を履歴の文書へ移す〉）")
    L.append("")
    L.append("元のソースにあった注の全文。ソースには、係数ごとに〈今の意味と単位〉の短い注だけを残した。見出しは係数（宣言）の名前。")
    L.append("`行頭の注` は元の宣言の前にあった行（空行を含む・元のまま）、`行末の注` は元の宣言の行の末尾にあった注。")
    L.append("`CR` は元の行末が CRLF だった行の番号（0 始まり）。`派生` は元に行末の注が無く、行頭の注から短い注を作った印。")
    L.append("")
    for e in entries:
        L.append(f"## {e['name']}")
        L.append(f"- コード行 {e['ord']}・元の行 {e['line']}・派生 {'はい' if e['derived'] else 'いいえ'}")
        L.append("- 行の写し：`" + json.dumps(e['anchor'], ensure_ascii=False)[1:-1].replace('`', "\\u0060") + "`")
        if e['gap'] is not None:
            p, cr, nl = enc_text(e['gap'])
            L.append(f"- 行頭の注（CR {','.join(map(str, cr)) or '-'}・最終行の改行 {'有' if nl else '無'}）：")
            L.append(FENCE + "text"); L.extend(p); L.append(FENCE)
        if e['trail'] is not None:
            tl = e['trail'].split('\n')
            cr = [k for k, x in enumerate(tl) if x.endswith('\r')]
            tl = [x[:-1] if x.endswith('\r') else x for x in tl]
            L.append(f"- 行末の注（CR {','.join(map(str, cr)) or '-'}）：")
            L.append(FENCE + "text"); L.extend(tl); L.append(FENCE)
        L.append("")
    return '\n'.join(L).encode('utf-8')


def read_history(b):
    t = b.decode('utf-8').split('\n')
    entries = []; i = 0
    cur = None
    while i < len(t):
        s = t[i]
        if s.startswith('## ') and not s.startswith('###'):
            cur = {'name': s[3:], 'gap': None, 'trail': None}; entries.append(cur)
        elif cur is not None and s.startswith('- コード行 '):
            m = re.match(r"- コード行 (\d+)・元の行 (\d+)・派生 (はい|いいえ)", s)
            cur['ord'] = int(m.group(1)); cur['line'] = int(m.group(2)); cur['derived'] = m.group(3) == 'はい'
        elif cur is not None and s.startswith('- 行の写し：`'):
            cur['anchor'] = json.loads('"' + s[len('- 行の写し：`'):-1].replace("\\u0060", '`') + '"')
        elif cur is not None and (s.startswith('- 行頭の注（') or s.startswith('- 行末の注（')):
            m = re.match(r"- (行頭|行末)の注（CR ([-\d,]+)(?:・最終行の改行 (有|無))?）：", s)
            kind = m.group(1); cr = [] if m.group(2) == '-' else [int(x) for x in m.group(2).split(',')]
            nl = m.group(3) != '無'
            assert t[i + 1] == FENCE + "text"
            j = i + 2; body_ = []
            while t[j] != FENCE: body_.append(t[j]); j += 1
            if kind == '行頭':
                lines = [x + ('\r' if k in cr else '') + '\n' for k, x in enumerate(body_)]
                if not nl: lines[-1] = lines[-1][:-1]
                cur['gap'] = lines
            else:
                cur['trail'] = '\n'.join(x + ('\r' if k in cr else '') for k, x in enumerate(body_))
            i = j
        i += 1
    return entries


def restore(newt, entries):
    lines, offs, info, sp = analyze(newt)
    out = []; gap = []; codeord = 0; ei = 0; i = 0; n = len(lines)
    byord = {}
    for e in entries: byord[e['ord']] = e
    while i < n:
        code, ss, cov = info[i]
        if not code:
            gap.append(lines[i]); i += 1; continue
        starts_here = [s for s in ss if s[0] >= offs[i]]
        region = None; endi = i
        if starts_here:
            rs = starts_here[0][0]; re_ = starts_here[0][1]
            chain = [starts_here[0]]
            for s in starts_here[1:]:
                if newt[re_:s[0]].strip() == '': chain.append(s); re_ = s[1]
                else: break
            rest_end = newt.find('\n', re_); rest_end = len(newt) if rest_end < 0 else rest_end
            if newt[re_:rest_end].strip() == '':
                region = (rs, re_)
                endi = max(k for k in range(i, n) if offs[k] <= max(re_ - 1, rs))
        codeord += 1
        e = byord.get(codeord)
        line = ''.join(lines[i:endi + 1])
        if e is not None:
            assert strip_comments(line).rstrip() == e['anchor'], (codeord, e['name'])
            if e['gap'] is not None:
                out.extend(e['gap'])
            else:
                out.extend(gap)
            if e['trail'] is not None:
                rs, re_ = region
                a = offs[i]
                line = newt[a:rs] + e['trail'] + newt[re_:offs[endi] + len(lines[endi])]
            elif e['derived']:
                # 行末に足した注を外す
                rs, re_ = region
                a = offs[i]
                pre = newt[a:rs].rstrip(' \t')
                # 元は注なし＝行末の空白を持たない
                line = pre + newt[re_:offs[endi] + len(lines[endi])]
        else:
            out.extend(gap)
        out.append(line)
        gap = []; i = endi + 1
    if byord.get(codeord + 1) is not None and byord[codeord + 1]['gap'] is not None:
        out.extend(byord[codeord + 1]['gap'])
    else:
        out.extend(gap)
    return ''.join(out)


def rd(p):
    return open(p, 'rb').read().decode('utf-8')


def wr(p, s):
    open(p, 'wb').write(s.encode('utf-8'))


if __name__ == '__main__':
    cmd = sys.argv[1]
    if cmd == 'forward':
        sim, hist = sys.argv[2], sys.argv[3]
        os.makedirs(hist, exist_ok=True)
        for f in PARTS:
            t = rd(os.path.join(sim, f)); rep = []
            nt, ents = transform(t, rep)
            wr(os.path.join(sim, f), nt)
            open(os.path.join(hist, f + '.md'), 'wb').write(write_history(f[:-3], ents, 0))
            print(f, len(t), '->', len(nt), 'entries', len(ents), 'notes', len(rep))
            for r in rep: print('   ', r)
    elif cmd == 'restore':
        sim, hist, outd = sys.argv[2], sys.argv[3], sys.argv[4]
        os.makedirs(outd, exist_ok=True)
        for f in PARTS:
            nt = rd(os.path.join(sim, f))
            ents = read_history(open(os.path.join(hist, f + '.md'), 'rb').read())
            wr(os.path.join(outd, f), restore(nt, ents))
    elif cmd == 'tokens':
        print('\n'.join(tokens(rd(sys.argv[2]))))
