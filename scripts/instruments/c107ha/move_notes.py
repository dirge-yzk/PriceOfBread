# 注を履歴の文書へ移す台本（c107ha・(1112)）。
# 使い方：python -X utf8 -I move_notes.py <リポジトリ> <履歴の置き場> <由来のコミット> <ファイル...> [--keep <字の表>]
# 規則：連続する行頭の注のかたまり・行末の注・/* */ を単位とし、経緯の印があれば全文を履歴へ逐語で写し、
# コードには印の無い最初の1文＋〔履歴 ファイル#番号〕だけ残す。
import sys, os, re, bisect
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from csscan import scan, MARK

KEYWORDS = {'if','for','foreach','while','return','var','else','switch','case','using','throw','yield','do','new','int','double','bool','string','long','float'}
MODS = r'(?:public|private|internal|protected|static|readonly|const|sealed|abstract|override|virtual|partial|async|unsafe|extern|new|ref|required)'


def member_name(line):
    s = line.strip()
    m = re.match(r'^(?:\[[^\]]*\]\s*)*(?:%s\s+)*[\w<>\[\],.?()\s]*?(\w+)\s*(?:\(|=>|=(?!=)|;|\{|$)' % MODS, s)
    if m:
        nm = m.group(1)
        if nm not in KEYWORDS:
            return nm
    m2 = re.search(r'(\w+)\s*(?:\(|=(?!=)|;)', s)
    if m2 and m2.group(1) not in KEYWORDS:
        return m2.group(1)
    ids = [x for x in re.findall(r'[A-Za-z_]\w{3,}', s) if x not in KEYWORDS]
    if ids:
        return max(ids, key=len)
    return '注'


def sentences(text):
    parts = re.split(r'(?<=[。！？])|(?<=\.)\s+', text)
    return [p.strip() for p in parts if p and p.strip()]


def pick(text, fallback):
    for s in sentences(text):
        if MARK.search(s) or '〔履歴' in s:
            continue
        if 2 <= len(s) <= 120:
            return s
    return fallback


def xml_clean(s):
    s = re.sub(r'<(?:see|paramref)\s+(?:cref|name)="([^"]*)"\s*/>', r'\1', s)
    s = re.sub(r'<[^>]*>', '', s)
    return s.replace('&', '＆').replace('<', '＜').replace('>', '＞')


def process(text, fname, keep_pats):
    cs = scan(text)
    starts = [0]
    for m in re.finditer('\n', text):
        starts.append(m.end())

    def lineno(pos):
        return bisect.bisect_right(starts, pos) - 1

    def line_end(i):
        e = text.find('\n', starts[i])
        e = len(text) if e < 0 else e
        return e - 1 if e > starts[i] and text[e - 1] == '\r' else e

    full = {}
    units = []
    for k, s, e in cs:
        ln = lineno(s)
        pre = text[starts[ln]:s]
        if k == 'line':
            body = text[s:e]
            if pre.strip() == '':
                full[ln] = (s, e, body, pre)
            else:
                units.append(('trail', s, e, {'text': body, 'line': ln}))
        else:
            ln2 = lineno(e - 1)
            post = text[e:line_end(ln2)]
            if pre.strip() == '' and post.strip() == '':
                units.append(('blockfull', starts[ln], line_end(ln2), {'text': text[s:e], 'line': ln, 'line2': ln2, 'indent': pre}))
            else:
                units.append(('inline', s, e, {'text': text[s:e], 'line': ln}))
    lns = sorted(full)

    def isdoc(l):
        return full[l][2].startswith('///') and not full[l][2].startswith('////')

    def isport(l):
        return '[PORT:' in full[l][2]

    i = 0
    while i < len(lns):
        if isport(lns[i]):
            i += 1
            continue
        j = i
        while j + 1 < len(lns) and lns[j + 1] == lns[j] + 1 and not isport(lns[j + 1]) and isdoc(lns[j + 1]) == isdoc(lns[i]):
            j += 1
        a, b = lns[i], lns[j]
        body = [text[starts[l]:line_end(l)] for l in range(a, b + 1)]
        units.append(('full', starts[a], line_end(b), {'lines': body, 'line': a, 'line2': b, 'indent': full[a][3], 'doc': isdoc(a)}))
        i = j + 1
    units.sort(key=lambda u: u[1])
    out = []
    pos = 0
    hist = []
    nl = '\r\n' if '\r\n' in text else '\n'
    base = os.path.basename(fname)

    def next_code(idx):
        for l in range(idx, min(len(starts), idx + 40)):
            t = text[starts[l]:line_end(l)].strip()
            if t and not t.startswith('//') and not t.startswith('/*') and not t.startswith('*'):
                return t
        return ''

    for kind, s, e, info in units:
        if kind == 'full':
            rawtext = '\n'.join(info['lines'])
            plain = ' '.join(re.sub(r'^/+\s?', '', x.strip()) for x in info['lines'])
        else:
            rawtext = info['text']
            if kind == 'trail':
                plain = re.sub(r'^/+\s?', '', rawtext)
            else:
                plain = re.sub(r'\s+', ' ', re.sub(r'^/\*+|\*+/$', '', rawtext)).strip()
        if '〔履歴' in rawtext or not MARK.search(rawtext) or any(p in rawtext for p in keep_pats):
            continue
        n = len(hist) + 1
        if kind in ('full', 'blockfull'):
            code = next_code(info['line2'] + 1)
        else:
            code = text[starts[info['line']]:s].strip() or next_code(info['line'] + 1)
        nm = member_name(code) if code else '注'
        ref = '〔履歴 %s#%d〕' % (base, n)
        first = pick(plain, nm)
        if kind == 'full' and info['doc']:
            line = xml_clean(first)
            if '<summary>' in rawtext:
                ind = info['indent']
                rep = nl.join([ind + '/// <summary>', ind + '/// ' + line + ref, ind + '/// </summary>'])
                form = 'doc3'
            else:
                rep = info['indent'] + '/// ' + line + ref
                form = 'doc1'
            orig_lines = info['lines']
        elif kind == 'full':
            rep = info['indent'] + '// ' + first + ref
            form = 'full'
            orig_lines = info['lines']
        elif kind == 'blockfull':
            rep = info['indent'] + '/* ' + first.replace('*/', '') + ref + ' */'
            form = 'blockfull'
            orig_lines = text[s:e].split('\n')
        elif kind == 'trail':
            rep = '// ' + first + ref
            form = 'trail'
            orig_lines = [rawtext]
        else:
            rep = '/* ' + first.replace('*/', '') + ref + ' */'
            form = 'inline'
            orig_lines = rawtext.split('\n')
        out.append(text[pos:s])
        out.append(rep)
        pos = e
        lo = info['line'] + 1
        hi = info.get('line2', info['line']) + 1
        hist.append((n, form, lo, hi, nm, orig_lines))
    out.append(text[pos:])
    return ''.join(out), hist


def hist_text(base, commit, hist):
    L = ['# %s の注の履歴' % base, '',
         '由来：%s の %s から、経緯の印のある注を逐語で移した（台本 scripts/instruments/c107ha/move_notes.py）。コードには印の無い最初の 1 文と〔履歴 %s#番号〕だけが残る。' % (commit, base, base), '']
    for n, form, lo, hi, nm, orig in hist:
        L.append('## §%d　形=%s　元の行=%d-%d　次のメンバー=%s' % (n, form, lo, hi, nm))
        L.append('`````text')
        L.extend(x.rstrip('\r') for x in orig)
        L.append('`````')
        L.append('')
    return '\n'.join(L)


def main():
    args = sys.argv[1:]
    keep = []
    if '--keep' in args:
        k = args.index('--keep')
        keep = [x for x in open(args[k + 1], encoding='utf-8').read().split('\n') if x]
        args = args[:k] + args[k + 2:]
    repo, histdir, commit = args[:3]
    tot = 0
    for f in args[3:]:
        p = os.path.join(repo, f)
        raw = open(p, 'rb').read()
        bom = raw.startswith(b'\xef\xbb\xbf')
        text = (raw[3:] if bom else raw).decode('utf-8')
        new, hist = process(text, f, keep)
        if not hist:
            continue
        nb = new.encode('utf-8')
        if bom:
            nb = b'\xef\xbb\xbf' + nb
        open(p, 'wb').write(nb)
        base = os.path.basename(f)
        open(os.path.join(histdir, base + '.md'), 'wb').write(hist_text(base, commit, hist).encode('utf-8'))
        moved = sum(len('\n'.join(h[5])) for h in hist)
        tot += moved
        print('%s\t%d\t%d' % (f, len(hist), moved))
    print('TOTAL\t%d' % tot)


if __name__ == '__main__':
    main()
