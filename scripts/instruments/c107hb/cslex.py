# C# の字句の走査（注の位置だけを返す）。文字列・逐語・補間・文字リテラルの中の // は注に数えない。


class LexError(Exception):
    pass


def find_comments(src):
    """src の中の注を (開始, 終了, 種類) の列で返す。種類は 'L'（行）か 'B'（塊）。終了は含まない位置。"""
    out = []
    n = len(src)

    def line_end(i):
        j = src.find('\n', i)
        if j < 0:
            j = n
        if j > i and src[j - 1] == '\r':
            j -= 1
        return j

    def skip_string(i, verbatim, interp):
        # i は開きの " の次。閉じの " の次の位置を返す。
        while i < n:
            c = src[i]
            if c == '"':
                if verbatim and i + 1 < n and src[i + 1] == '"':
                    i += 2
                    continue
                return i + 1
            if c == '\\' and not verbatim:
                i += 2
                continue
            if interp and c == '{':
                if i + 1 < n and src[i + 1] == '{':
                    i += 2
                    continue
                i = lex_code(i + 1, True)
                continue
            if interp and c == '}' and i + 1 < n and src[i + 1] == '}':
                i += 2
                continue
            if c == '\n' and not verbatim:
                raise LexError('改行を含む通常の文字列 @%d' % i)
            i += 1
        raise LexError('文字列が閉じない')

    def lex_code(i, in_interp):
        depth = 0
        paren = 0
        while i < n:
            c = src[i]
            if c == '/' and i + 1 < n and src[i + 1] == '/':
                e = line_end(i)
                out.append((i, e, 'L'))
                i = e
                continue
            if c == '/' and i + 1 < n and src[i + 1] == '*':
                e = src.find('*/', i + 2)
                if e < 0:
                    raise LexError('塊の注が閉じない')
                out.append((i, e + 2, 'B'))
                i = e + 2
                continue
            if c == '"':
                k = i
                pre = ''
                while k > 0 and src[k - 1] in '$@':
                    k -= 1
                    pre += src[k]
                if src.startswith('"""', i):
                    raise LexError('生の文字列は未対応 @%d' % i)
                i = skip_string(i + 1, '@' in pre, '$' in pre)
                continue
            if c == "'":
                if i + 1 < n and src[i + 1] == '\\':
                    e = src.find("'", i + 3)
                    i = e + 1
                else:
                    i += 3
                continue
            if in_interp:
                if c == '(' or c == '[':
                    paren += 1
                elif c == ')' or c == ']':
                    paren -= 1
                elif c == '{':
                    depth += 1
                elif c == '}':
                    if depth == 0:
                        return i + 1
                    depth -= 1
                elif (c == ':' and depth == 0 and paren == 0
                      and not (i + 1 < n and src[i + 1] == ':')
                      and not (i > 0 and src[i - 1] == ':')):
                    e = src.find('}', i)
                    return e + 1
            i += 1
        if in_interp:
            raise LexError('補間が閉じない')
        return i

    lex_code(0, False)
    return out
