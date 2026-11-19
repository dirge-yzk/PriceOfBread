# (1210) 読むだけ：cmp_rows.py が〈変わった〉とした行ごとに、錨を囲む関数の中で、錨の行の前後 W 行（注と空白を除いた行で）に差の塊があるかを数える。
#  関数が大きい（EconomyStep.Run・MonthSettle ほか）と、錨と関係の無い直しで〈関数の字が違う〉になる＝錨の近くの差だけを拾って読み直しの要否を決める。
#  使い方: python -X utf8 -I anchor_hunks.py <repo> <OLD> <NEW> <11-12 の tsv> <cmp の tsv> <出力> [W=12]
import sys, os, difflib
repo, OLD, NEW, tsv, cmpt, out = sys.argv[1:7]; W = int(sys.argv[7]) if len(sys.argv) > 7 else 12
here = os.path.dirname(os.path.abspath(__file__))
src = open(os.path.join(here, 'cmp_rows.py'), 'rb').read().decode('utf-8')
src = src[:src.index('CO, CN = coef_decl(OLD)')]
sys.argv = ['x', repo, OLD, NEW, os.devnull, os.devnull]
g = {'__name__': 'cmp'}; exec(compile(src, 'cmp_rows_head', 'exec'), g)
import re
changed = {}
for l in open(cmpt, 'rb').read().decode('utf-8').split('\n')[1:]:
    x = l.split('\t')
    if len(x) > 1 and x[1] == '変わった': changed[x[0]] = x[3] if len(x) > 3 else ''
rows = {}
for l in open(tsv, 'rb').read().decode('utf-8').split('\n')[1:]:
    x = l.split('\t')
    if len(x) > 4: rows[x[0]] = x[4]
with open(out, 'w', encoding='utf-8', newline='\n') as o:
    o.write(f'# OLD={OLD} NEW={NEW} W=±{W} 行（注と空白を除いた行）\n行\t錨\t囲む関数\t近くの差の塊\t差の行（先頭の 3 行・字は 90 字まで）\n')
    for rid in sorted(changed):
        for a in rows.get(rid, '').split(';'):
            m = re.match(r'^([^:]+):(\d+):(.+)$', a.strip())
            if not m: continue
            fn, ln = m.group(1), int(m.group(2))
            if fn == 'Coefficients.cs':
                o.write(f'{rid}\t{a.strip()[:60]}\t（係数）\t—\t係数の宣言は cmp の〈係数〉の列で読む\n'); continue
            po, pn = g['resolve'](OLD, fn), g['resolve'](NEW, fn)
            if po is None or pn is None:
                o.write(f'{rid}\t{a.strip()[:60]}\t—\tファイルが無い\t\n'); continue
            enc = g['enclosing'](OLD, po, ln)
            if not enc:
                o.write(f'{rid}\t{a.strip()[:60]}\t（関数の外）\t—\t\n'); continue
            name, i, j = enc
            so = g['slines'](OLD, po)
            # 錨の行の、関数の中での位置（注と空白を除いた行での順番）
            bo_idx = [k for k in range(i, j + 1) if so[k]]
            pos = min(range(len(bo_idx)), key=lambda t: abs(bo_idx[t] - (ln - 1)))
            bo = [so[k] for k in bo_idx]
            ms = g['find_method'](NEW, pn, name, so[i])
            if not ms:
                o.write(f'{rid}\t{a.strip()[:60]}\t{name}\tNEW に無い\t\n'); continue
            h = ms[0]; bn = g['body'](NEW, pn, h[1], h[2])
            sm = difflib.SequenceMatcher(a=bo, b=bn, autojunk=False)
            near = []
            for tag, a1, a2, b1, b2 in sm.get_opcodes():
                if tag == 'equal': continue
                if a2 >= pos - W and a1 <= pos + W:
                    near.append(('-' + ' | -'.join(bo[a1:a2]) if a2 > a1 else '') + ((' +' + ' | +'.join(bn[b1:b2])) if b2 > b1 else ''))
            o.write(f'{rid}\t{a.strip()[:60]}\t{name}\t{len(near)}\t' + ' ／ '.join(s[:90] for s in near[:3]) + '\n')
print('ok', out)
