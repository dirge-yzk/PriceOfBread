# (1114) c107hc の門 1・2・5 と、行ごとの読み手の語の差・宣言の名の列（ソース）。
# python gates.py <木> <元の rev> [mut]   ＝ <木> の SimCore と docs/history を、<元の rev> の SimCore と比べる。
import os, re, subprocess, sys
from collections import Counter
here = os.path.dirname(os.path.abspath(__file__))
tree, rev = sys.argv[1], sys.argv[2]
sys.argv = ['x', 'x', os.path.join(tree, 'PriceOfBread.SimCore')]
sys.path.insert(0, here)
import cmove

sim = os.path.join(tree, 'PriceOfBread.SimCore'); hist = os.path.join(tree, 'docs', 'history')
fail = 0


def orig(f):
    return subprocess.check_output(['git', '-C', tree, 'show', f'{rev}:PriceOfBread.SimCore/{f}'])


def say(ok, msg):
    global fail
    if not ok: fail += 1
    print(('OK  ' if ok else 'RED ') + msg)


tmp = os.path.join(os.environ.get('GATE_TMP', 'D:/PoB_work/c107hc_work'), 'gate_restore')
os.makedirs(tmp, exist_ok=True)
for f in cmove.PARTS:
    o = orig(f)
    nb = open(os.path.join(sim, f), 'rb').read()
    ents = cmove.read_history(open(os.path.join(hist, f + '.md'), 'rb').read())
    try:
        rb = cmove.restore(nb.decode('utf-8'), ents).encode('utf-8')
    except Exception as e:
        rb = b''; print('   restore 例外', repr(e))
    say(rb == o, f'門1 履歴＋残り＝元（{f}・BOM {"有" if o[:3] == b"\xef\xbb\xbf" else "無"}・CR {o.count(b"\r")}）')
    ta = cmove.tokens(o.decode('utf-8')); tb = cmove.tokens(nb.decode('utf-8'))
    say(ta == tb, f'門2 注を除いた字句の列が一致（{f}・{len(ta)} 字句）')
    # 名の列（宣言の順）
    def names(t):
        return re.findall(r"public\s+[\w<>\[\],?]+(?:\s*<[^>]*>)?\s+(\w+)\s*(?:=|;|\()", cmove.strip_comments(t))
    say(names(o.decode('utf-8')) == names(nb.decode('utf-8')), f'名の列（ソース・宣言の順）が一致（{f}・{len(names(nb.decode("utf-8")))} 件）')
    # 門5：元に行末の注があった public の行は、新しい版でも行末の注を持ち 120 字までか
    olines, ooffs, oinfo, _sp = cmove.analyze(o.decode('utf-8'))
    ohas = []   # 元の public の行ごとに、注（行末か行頭）があったか
    for i, l in enumerate(olines):
        if oinfo[i][0] and re.match(r'\s*public', l):
            has = bool(oinfo[i][1])
            k = i - 1
            while k >= 0 and (not oinfo[k][0]) and olines[k].strip() == '': k -= 1
            if k >= 0 and (not oinfo[k][0]) and oinfo[k][1]: has = True
            ohas.append(has)
    nlines, _o2, ninfo, _s2 = cmove.analyze(nb.decode('utf-8'))
    nl = [l.rstrip() for i, l in enumerate(nlines) if ninfo[i][0] and re.match(r'\s*public', l)]
    miss = 0; long_ = 0; nonote = 0
    for l, has in zip(nl, ohas):
        sp = cmove.lex(l)
        if not sp:
            if has: miss += 1
            else: nonote += 1
            continue
        txt = cmove.body(l[sp[0][0]:sp[0][1]])
        if len(txt) > 120 and not re.search(r"\[PORT:P\d+\]", txt): long_ += 1
    say(miss == 0 and long_ == 0 and len(ohas) == len(nl), f'門5 元に注があった public の行は全て短い注を持つ・public {len(nl)} 本・120 字超 {long_}・注の欠け {miss}・元から注なし {nonote}（{f}）')
    # 行ごとの読み手：// より前の英数字の語の数の差
    def idc(t):
        c = Counter()
        for l in t.split('\n'): c.update(re.findall(r'[A-Za-z_][A-Za-z0-9_]*', l.split('//')[0]))
        return c
    ca, cb = idc(o.decode('utf-8')), idc(nb.decode('utf-8'))
    d = sorted(k for k in set(ca) | set(cb) if ca[k] != cb[k])
    print(f'   参考 行ごとの読み手の語の差 {len(d)} 語（{f}）：' + ','.join(d)[:300])
print('RED 合計', fail) if fail else print('全て OK')
sys.exit(1 if fail else 0)
