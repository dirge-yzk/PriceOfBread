# 門 1・門 2（c107ha・(1112)）。
# 使い方：python -X utf8 -I check_notes.py <リポジトリ> <由来のコミット> <履歴の置き場> <ファイル一覧のファイル>
# 門 1：新しいファイル＋履歴の文書から元のバイト列を組み直し、元と一致すること（全文が逐語で 1 回ずつ・BOM と改行も同じ）。
#       残った 1 文が元の注の部分の文であること（警告として数える）。
# 門 2：注を除いた字句の列（空白を除く）と前処理の指令の行が前後で一致すること。
import sys, os, re, subprocess
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from csscan import scan, strip_comments

HEAD = re.compile(r'^## §(\d+)　形=(\w+)　元の行=(\d+)-(\d+)　次のメンバー=(.*)$')
REF = re.compile(r'〔履歴 ([^#〕]+)#(\d+)〕')


def parse_hist(path):
    secs = {}
    lines = open(path, 'rb').read().decode('utf-8').split('\n')
    i = 0
    while i < len(lines):
        m = HEAD.match(lines[i])
        if m:
            n = int(m.group(1))
            assert lines[i + 1] == '`````text', (path, n)
            j = i + 2
            body = []
            while lines[j] != '`````':
                body.append(lines[j])
                j += 1
            assert n not in secs, (path, n, '重複')
            secs[n] = (m.group(2), body, m.group(5))
            i = j
        i += 1
    return secs


def rebuild(newtext, base, secs, nl):
    cs = scan(newtext)
    used = set()
    edits = []  # (start, end, replacement)
    starts = [0]
    for m in re.finditer('\n', newtext):
        starts.append(m.end())
    import bisect

    def lstart(pos):
        return starts[bisect.bisect_right(starts, pos) - 1]

    def lend(pos):
        e = newtext.find('\n', pos)
        e = len(newtext) if e < 0 else e
        return e - 1 if e > 0 and newtext[e - 1] == '\r' else e
    for k, s, e in cs:
        txt = newtext[s:e]
        m = REF.search(txt)
        if not m:
            continue
        assert m.group(1) == base, (base, m.group(0))
        n = int(m.group(2))
        assert n in secs and n not in used, (base, n, '参照が履歴に無いか二重')
        used.add(n)
        form, body, _ = secs[n]
        orig = nl.join(body)
        if form in ('full', 'doc1', 'blockfull'):
            edits.append((lstart(s), lend(s), orig))
        elif form == 'doc3':
            a = lstart(s)
            pstart = lstart(a - 1)
            eend = lend(newtext.index(nl, s) + len(nl) if nl == '\n' else newtext.index('\n', s) + 1)
            edits.append((pstart, eend, orig))
        else:
            edits.append((s, e, orig))
    out = []
    p = 0
    for s, e, r in sorted(edits):
        out.append(newtext[p:s])
        out.append(r)
        p = e
    out.append(newtext[p:])
    return ''.join(out), used


def main():
    repo, commit, histdir, listfile = sys.argv[1:5]
    files = open(listfile, encoding='utf-8').read().split()
    bad = 0
    warn = 0
    for f in files:
        orig = subprocess.run(['git', '-C', os.environ.get('ORIG_REPO', repo), 'show', '%s:%s' % (commit, f)], capture_output=True, check=True).stdout
        new = open(os.path.join(repo, f), 'rb').read()
        base = os.path.basename(f)
        ob = orig.startswith(b'\xef\xbb\xbf')
        nb = new.startswith(b'\xef\xbb\xbf')
        if ob != nb:
            print('BOM 不一致', f)
            bad += 1
        ot = (orig[3:] if ob else orig).decode('utf-8')
        nt = (new[3:] if nb else new).decode('utf-8')
        nl = '\r\n' if '\r\n' in ot else '\n'
        if nl == '\r\n' and nt.count('\n') != nt.count('\r\n'):
            print('改行が混ざった', f)
            bad += 1
        if nl == '\n' and '\r' in nt and '\r' not in ot:
            print('CR が増えた', f)
            bad += 1
        hp = os.path.join(histdir, base + '.md')
        secs = parse_hist(hp) if os.path.exists(hp) else {}
        rb, used = rebuild(nt, base, secs, nl)
        if used != set(secs):
            print('履歴の節に使われない物', f, sorted(set(secs) - used)[:5])
            bad += 1
        if rb != ot:
            print('門1 赤：組み直しが元と違う', f)
            bad += 1
        # 残りの 1 文が元の注の部分か
        for k, s, e in scan(nt):
            m = REF.search(nt[s:e])
            if m:
                n = int(m.group(2))
                sent = nt[s:e][:m.start()]
                sent = re.sub(r'^/+\s?|\s*\*?/?$', '', sent).strip()
                sent = re.sub(r'^/+\s?', '', sent).strip()
                form, body, nm = secs[n]
                joined = ' '.join(re.sub(r'^\s*/+\s?', '', x) for x in body)
                if sent and sent != nm and sent not in joined and sent.replace('＆', '&') not in joined:
                    warn += 1
        # 門 2
        if strip_comments(ot) != strip_comments(nt):
            print('門2 赤：注を除いた字句が違う', f)
            bad += 1
        op = [l.strip() for l in ot.split('\n') if l.lstrip().startswith('#')]
        npp = [l.strip() for l in nt.split('\n') if l.lstrip().startswith('#')]
        if op != npp:
            print('門2 赤：前処理の指令が違う', f)
            bad += 1
    print('files=%d bad=%d warn_sentence_not_substring=%d' % (len(files), bad, warn))
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
