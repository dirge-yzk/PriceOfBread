# C# の注の位置を数える走査器（文字列・文字・前処理の行の中の // を注と読まない）
import re

def _skip_char(t, i):
    m = re.compile(r"'(\\[^']*|[^\\'])'").match(t, i)
    return m.end() if m else i + 1

def _skip_interp_expr(t, p):
    # p は { の次。対応する } の次の位置を返す
    depth = 1
    n = len(t)
    while p < n:
        c = t[p]
        if c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return p + 1
        elif c in '$@"':
            e = _try_string(t, p)
            if e:
                p = e
                continue
        elif c == "'":
            p = _skip_char(t, p)
            continue
        p += 1
    return n

def _try_string(t, i):
    j = i
    while j < len(t) and t[j] in '$@':
        j += 1
    if j >= len(t) or t[j] != '"':
        return 0
    pre = t[i:j]
    dollars = pre.count('$')
    verb = '@' in pre
    n = 0
    while j + n < len(t) and t[j + n] == '"':
        n += 1
    if not verb and n >= 3:
        end = t.find('"' * n, j + n)
        if end < 0:
            return len(t)
        k = end + n
        while k < len(t) and t[k] == '"':
            k += 1
        return k
    if not verb and n == 2:
        return j + 2
    p = j + 1
    L = len(t)
    while p < L:
        c = t[p]
        if verb:
            if c == '"':
                if p + 1 < L and t[p + 1] == '"':
                    p += 2
                    continue
                return p + 1
        else:
            if c == '\\':
                p += 2
                continue
            if c == '"':
                return p + 1
        if dollars:
            if c == '{':
                if p + 1 < L and t[p + 1] == '{':
                    p += 2
                    continue
                p = _skip_interp_expr(t, p + 1)
                continue
            if c == '}' and p + 1 < L and t[p + 1] == '}':
                p += 2
                continue
        p += 1
    return L

def scan(t):
    """注の一覧 [(kind, start, end)]。kind は 'line' か 'block'。end は改行を含まない。"""
    out = []
    i = 0
    n = len(t)
    bol = True
    while i < n:
        c = t[i]
        if c == '\n':
            bol = True
            i += 1
            continue
        if bol:
            if c in ' \t\r':
                i += 1
                continue
            if c == '#':
                e = t.find('\n', i)
                i = n if e < 0 else e
                continue
            bol = False
        if c == '/' and i + 1 < n and t[i + 1] == '/':
            e = t.find('\n', i)
            e = n if e < 0 else e
            end = e - 1 if e > i and t[e - 1] == '\r' else e
            out.append(('line', i, end))
            i = e
            continue
        if c == '/' and i + 1 < n and t[i + 1] == '*':
            e = t.find('*/', i + 2)
            e = n if e < 0 else e + 2
            out.append(('block', i, e))
            i = e
            continue
        if c in '$@"':
            e = _try_string(t, i)
            if e:
                i = e
                continue
        if c == "'":
            i = _skip_char(t, i)
            continue
        i += 1
    return out

def strip_comments(t):
    """注を除いた字（空白を全て除く）。"""
    cs = scan(t)
    parts = []
    p = 0
    for k, s, e in cs:
        parts.append(t[p:s])
        p = e
    parts.append(t[p:])
    return re.sub(r'\s+', '', ''.join(parts))

MARK = re.compile(
    r'20\d\d-\d\d(?:-\d\d)?|\(\d{3,4}\)|★\(\d+\)|波\d+'
    r'|(?<![A-Za-z0-9_])m\d{3,4}(?![A-Za-z0-9_])'
    r'|(?<![A-Za-z0-9_])c\d{2,3}[a-z]+(?![A-Za-z0-9_])'
    r'|旧＝|撤去|墓標|廃止|焼き込み|裁定|作者')
