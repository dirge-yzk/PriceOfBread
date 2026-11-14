# (1127) 台帳の写しに S05 の行を当て、門（scripts/instruments/r900/ledger_check.py）を読むための写しを作る（読むだけ・本物の台帳は書かない）
import os, re
here = os.path.dirname(os.path.abspath(__file__))
src = 'C:/Users/dirge/Documents/PriceOfBread/docs/機能の台帳.md'
b = open(src, 'rb').read().decode('utf-8')
nl = '\r\n' if '\r\n' in b else '\n'
L = b.split(nl)
tsv = open(os.path.join(here, 'ledger_S05_2026-11-14.tsv'), 'rb').read().decode('utf-8').strip('\n').split('\n')
rows = ['| ' + ' | '.join(x.split('\t')) + ' |' for x in tsv[1:]]
out = []; done = False; nrep = 0
for i, l in enumerate(L):
    if l.startswith('| S05 | 投資と建設 |'):
        c = l.split(' | ')
        c[-1] = '(1127) c109s05 で確かめた（木 758f554ed） |'
        out.append(' | '.join(c)); continue
    if l.startswith('| S05-H'):
        nrep += 1
        if not done:
            out.extend(rows); done = True
        continue
    out.append(l)
open(os.path.join(here, 'ledger_copy.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('replaced', nrep, 'old rows with', len(rows), 'rows; newline', repr(nl))
