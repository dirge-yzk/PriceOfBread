# (1146) c112tt 門 5 を今の基準で読む。c107hc/gates.py の門 5 だけを、基準の rev（既定＝合流の頭 aa8ee7e51）と比べる。
# 旧は (1114) の元の rev と比べたので、その後に public の行が増減した 4 ファイルで zip の対応がずれて RED になった。
# 使い方：python gate5.py <木> [基準の rev]   ＝ <木> の SimCore の Coefficients*.cs を <基準の rev> の同じファイルと比べる。
import os, re, subprocess, sys
here = os.path.dirname(os.path.abspath(__file__))
tree = sys.argv[1]; rev = sys.argv[2] if len(sys.argv) > 2 else 'aa8ee7e51'
sim = os.path.join(tree, 'PriceOfBread.SimCore')
sys.argv = ['x', 'x', sim]
sys.path.insert(0, os.path.join(here, '..', 'c107hc'))
import cmove
fail = 0
for f in cmove.PARTS:
    o = subprocess.check_output(['git', '-C', tree, 'show', f'{rev}:PriceOfBread.SimCore/{f}'])
    nb = open(os.path.join(sim, f), 'rb').read()
    olines, _oo, oinfo, _sp = cmove.analyze(o.decode('utf-8'))
    ohas = []
    for i, l in enumerate(olines):
        if oinfo[i][0] and re.match(r'\s*public', l):
            has = bool(oinfo[i][1]); k = i - 1
            while k >= 0 and (not oinfo[k][0]) and olines[k].strip() == '': k -= 1
            if k >= 0 and (not oinfo[k][0]) and oinfo[k][1]: has = True
            ohas.append(has)
    nlines, _o2, ninfo, _s2 = cmove.analyze(nb.decode('utf-8'))
    nl = [l.rstrip() for i, l in enumerate(nlines) if ninfo[i][0] and re.match(r'\s*public', l)]
    miss = long_ = nonote = unbal = 0
    for l, has in zip(nl, ohas):
        sp = cmove.lex(l)
        if not sp:
            if has: miss += 1
            else: nonote += 1
            continue
        txt = cmove.body(l[sp[0][0]:sp[0][1]])
        if len(txt) > 120 and not re.search(r"\[PORT:P\d+\]", txt): long_ += 1
        if any(txt.count(a) != txt.count(b) for a, b in ('（）', '()', '〔〕', '【】', '「」', '[]')) or re.match(r'^[）)〕】」\]]', txt.strip()): unbal += 1
    ok = miss == 0 and long_ == 0 and unbal == 0 and len(ohas) == len(nl)
    if not ok: fail += 1
    print(('OK  ' if ok else 'RED ') + f'門5 括弧の崩れ {unbal}・public {len(nl)} 本（基準 {len(ohas)} 本）・120 字超 {long_}・注の欠け {miss}・元から注なし {nonote}（{f}）')
print('RED 合計', fail) if fail else print('全て OK')
sys.exit(1 if fail else 0)
