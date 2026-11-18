# (1210) 合流が写す形の試し：docs/機能の台帳.md の写しへ S06 の 30 行を §2 の節として入れ、§3 の S06 の仮の行（S06-H）と §4-6 の 11-12 の S06 の行を外し、
#  §1 の S06 の状態を〈で確かめた〉へ替える＝ledger_check.py の門 1〜9 を通すための写し（リポジトリには書かない）。バイトで読み書きする（CR の数を保つ）。
# 使い方: python -X utf8 -I trial_ledger.py <台帳 md> <新しい tsv> <出力 md>
import sys
src, tsv, out = sys.argv[1:4]
b = open(src, 'rb').read()
nl = b'\r\n' if b.count(b'\r\n') > b.count(b'\n') // 2 else b'\n'
L = b.split(nl)
rows = open(tsv, 'rb').read().decode('utf-8').rstrip('\n').split('\n')
hdr, body = rows[0].split('\t'), [r.split('\t') for r in rows[1:]]
sec = ['## 2o. S06 お金の記帳・物価・GDP・損害（(1210) c121rn で確かめた・木 326e284e・写し 25 行は 7bb40c77 の 11-12 の判定・読み直し 5 行は keep_1198＝measurements/m1210_<日>/q3/ledger_S06_2026-11-18.tsv）', '',
       '| ' + ' | '.join(hdr) + ' |', '|' + '---|' * len(hdr)] + ['| ' + ' | '.join(r) + ' |' for r in body] + ['']
new = []; removed_h = removed_r = 0; inserted = False; s1 = 0
for l in L:
    t = l.decode('utf-8', 'replace')
    if t.startswith('| S06-H'): removed_h += 1; continue
    if t.startswith('| S06-') : removed_r += 1; continue
    if t.startswith('| S06 | お金の記帳'):
        cells = t.split('|'); cells[-2] = ' (1210) c121rn で確かめた（木 326e284e・§2o） '; t = '|'.join(cells); s1 += 1
        new.append(t.encode('utf-8')); continue
    if t.startswith('## 3. ') and not inserted:
        new += [x.encode('utf-8') for x in sec]; inserted = True
    new.append(l)
open(out, 'wb').write(nl.join(new))
print('仮の行を外した', removed_h, '・11-12 の行を外した', removed_r, '・§1 の行', s1, '・節', inserted)
