# (1245) 合流が写す形の試し（(1224) trial_ledger_s26.py の写しを S28 へ）。元の注：(1224) 合流が写す形の試し（(1217) trial_ledger_s13.py の写し・S28 へ）。元の注：(1217) 合流が写す形の試し（(1210) trial_ledger.py の写し）：docs/機能の台帳.md の写しへ S13 の 57 行を §2 の節として入れ、§3 の S06 の仮の行（S06-H）と §4-6 の 11-12 の S06 の行を外し、
#  §1 の S06 の状態を〈で確かめた〉へ替える＝ledger_check.py の門 1〜9 を通すための写し（リポジトリには書かない）。バイトで読み書きする（CR の数を保つ）。
# 使い方: python -X utf8 -I trial_ledger.py <台帳 md> <新しい tsv> <出力 md>
import sys
src, tsv, out = sys.argv[1:4]
b = open(src, 'rb').read()
nl = b'\r\n' if b.count(b'\r\n') > b.count(b'\n') // 2 else b'\n'
L = b.split(nl)
rows = open(tsv, 'rb').read().decode('utf-8').rstrip('\n').split('\n')
hdr, body = rows[0].split('\t'), [r.split('\t') for r in rows[1:]]
sec = ['## 2r. S28 環境と災害（(1245) c126s28 で確かめた・木 3cb2210a0・土台は 11-12 の紙の段〔7bb40c77・keep_1069〕・写し 9 行は 11-12 の判定・読み直し 24 行は keep_1233〔m1233〕とソース＝measurements/m1245_<日>/q/ledger_S28_2026-11-19.tsv）', '',
       '| ' + ' | '.join(hdr) + ' |', '|' + '---|' * len(hdr)] + ['| ' + ' | '.join(r) + ' |' for r in body] + ['']
new = []; removed_h = removed_r = 0; inserted = False; s1 = 0
for l in L:
    t = l.decode('utf-8', 'replace')
    import re
    if t.startswith('| S28-H'): removed_h += 1; continue
    if re.match(r'\| S28-\d\d \|', t): removed_r += 1; continue
    if t.startswith('| S28 | 環境と災害'):
        cells = t.split('|'); cells[-2] = ' (1224) c123rb で確かめた（木 81ad1e58d・§2q） '; t = '|'.join(cells); s1 += 1
        new.append(t.encode('utf-8')); continue
    if t.startswith('## 3. ') and not inserted:
        new += [x.encode('utf-8') for x in sec]; inserted = True
    new.append(l)
open(out, 'wb').write(nl.join(new))
print('仮の行を外した', removed_h, '・11-12 の行を外した', removed_r, '・§1 の行', s1, '・節', inserted)
