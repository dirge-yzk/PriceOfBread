# (1141) 台帳の写しに S25 の行を当て、門（scripts/instruments/r900/ledger_check.py）を読むための写しを作る（読むだけ・本物の台帳は書かない）
# S25-01〜77 は元の塊の位置へ、S25-V01・V02 は元の行（V の節）の位置へそれぞれ置き換える。
import os
W = 'D:/PoB_work/r1141_work'
src = 'C:/Users/dirge/Documents/PriceOfBread/docs/機能の台帳.md'
b = open(src, 'rb').read().decode('utf-8')
nl = '\r\n' if '\r\n' in b else '\n'
L = b.split(nl)
tsv = open(os.path.join(W, 'ledger_S25_2026-11-14.tsv'), 'rb').read().decode('utf-8').strip('\n').split('\n')
rows = {x.split('\t')[0]: '| ' + ' | '.join(x.split('\t')) + ' |' for x in tsv[1:]}
out = []; nrep = 0
for l in L:
    if l.startswith('| S25 | 外交 |'):
        c = l.split(' | '); c[-1] = '(1141) c111s25 で確かめた（木 03d192514） |'
        out.append(' | '.join(c)); continue
    if l.startswith('| S25-'):
        rid = l.split('|')[1].strip()
        out.append(rows.pop(rid)); nrep += 1; continue
    out.append(l)
assert not rows, rows.keys()
open(os.path.join(W, 'ledger_copy.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('replaced', nrep, 'rows in place; newline', repr(nl))
