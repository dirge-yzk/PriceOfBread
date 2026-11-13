# 注を消して履歴の文書へ移す。規則は measurements/m1113_*_prereg_hb.txt の [1]。
# 使い方: python -I strip.py <木の根> <docs/history のフォルダ> <ファイル名...>
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cslex import find_comments

KEEP = ('[PORT:',)


def keep(text):
    return any(k in text for k in KEEP)


def strip_source(src, keepfn=keep):
    """(新しい本文, 記録の列) を返す。記録は (元の行, 'W'|'T', 注の本文, 直後の行か同じ行のコード)。"""
    cm = find_comments(src)
    removed = []
    pos = 0
    head = ''      # 出力のうち確定した部分
    for (s, e, kind) in cm:
        text = src[s:e]
        if keepfn(text):
            continue
        cur = head + src[pos:s]
        ls = cur.rfind('\n') + 1
        prefix = cur[ls:]
        nl = src.find('\n', e)
        if nl < 0:
            rest = src[e:]
            nxt = len(src)
        else:
            rest = src[e:nl]
            nxt = nl + 1
        ln = src.count('\n', 0, s) + 1
        if prefix.strip() == '' and (kind == 'L' or rest.strip() == ''):
            head = cur[:ls]
            pos = nxt
            removed.append((ln, 'W', text, None))
        elif kind == 'L':
            head = cur.rstrip(' \t')
            pos = e
            removed.append((ln, 'T', text, prefix.strip()))
        else:
            after = src[e:e + 1]
            if cur and cur[-1] not in ' \t\n' and after and after not in ' \t\r\n':
                cur += ' '
            head = cur
            pos = e
            removed.append((ln, 'T', text, prefix.strip()))
    new = head + src[pos:]
    srclines = src.split('\n')
    recs = []
    for (ln, k, text, anchor) in removed:
        if k == 'W':
            j = ln - 1 + text.count('\n') + 1
            a = ''
            while j < len(srclines):
                t = srclines[j].strip()
                if t and not t.startswith('//') and not t.startswith('/*'):
                    a = t
                    break
                j += 1
            anchor = a
        recs.append((ln, k, text, anchor))
    return new, recs


def render(rel, recs):
    L = []
    L.append('# %s の注（1cd66aae5 の時点・注を履歴へ移した記録）' % rel)
    L.append('')
    L.append('見出しの数字は 1cd66aae5 の行番号。W＝行ぜんたいの注・T＝コードの後ろの注。')
    L.append('')
    i = 0
    n = len(recs)
    while i < n:
        ln, k, text, anchor = recs[i]
        if k == 'W':
            blk = [text.rstrip('\r')]
            start = ln
            end = ln + text.count('\n')
            j = i + 1
            while j < n and recs[j][1] == 'W' and recs[j][0] == end + 1:
                blk.append(recs[j][2].rstrip('\r'))
                end = recs[j][0] + recs[j][2].count('\n')
                j += 1
            L.append('## L%d-%d W 直後 `%s`' % (start, end, anchor[:90].replace('`', "'")))
            L.append('````')
            L.extend(blk)
            L.append('````')
            L.append('')
            i = j
        else:
            L.append('## L%d T `%s`' % (ln, anchor[:90].replace('`', "'")))
            L.append('````')
            L.append(text.rstrip('\r'))
            L.append('````')
            L.append('')
            i += 1
    return '\n'.join(L) + '\n'


def main():
    root, hist = sys.argv[1], sys.argv[2]
    os.makedirs(hist, exist_ok=True)
    tot_c = tot_n = 0
    for f in sys.argv[3:]:
        p = os.path.join(root, 'PriceOfBread.SimCore', f)
        src = open(p, 'rb').read().decode('utf-8')
        new, recs = strip_source(src)
        if not recs:
            continue
        open(p, 'wb').write(new.encode('utf-8'))
        md = render('PriceOfBread.SimCore/' + f, recs)
        open(os.path.join(hist, f[:-3] + '.md'), 'wb').write(md.encode('utf-8'))
        c = sum(len(r[2]) for r in recs)
        tot_c += c
        tot_n += len(recs)
        print('%s\t%d\t%d' % (f, len(recs), c))
    print('TOTAL\t%d\t%d' % (tot_n, tot_c))


if __name__ == '__main__':
    main()
