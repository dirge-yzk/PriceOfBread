# (1101) 台帳へ写す節の下書き（section_S08.md）と、合流の門を読むための台帳の写し（ledger_copy.md）を作る。台帳そのものには書かない。
# 呼び方：python -X utf8 -I mk_section.py <repo> <TSV>
import os, sys, re
repo, tsv = sys.argv[1], sys.argv[2]
W = os.path.dirname(os.path.abspath(__file__))
L = [x.split('\t') for x in open(tsv, encoding='utf-8').read().split('\n') if x]
head, body = L[0], L[1:]
cnt = {}
for r in body: cnt[r[8]] = cnt.get(r[8], 0) + 1
sec = ['## 2h. S08 輸送と地理（(1101) c105s08 で確かめた・木 fff0f1658・合流が写す）', '',
       '- 範囲：文書 docs/ゲーム概要と実装機能.md L1347〜L1532 の 38 見出し＋新しい行 3（S08-39 封鎖の危険の書き手・S08-40 運賃の受け手の偏り・S08-41 運河の総額が律速にならない）。§3 の S08 の仮の行はこの節への参照に畳む（id が同じ）。表は `measurements/m1101_<日付>/ledger_S08_2026-11-13.tsv`（合流が写す）。',
       '- 生（4 腕）：M＝D:/PoB_work/keep_1096/base_ins（種 1〜10）・N1／N2＝keep_1087/n1・n2・S11＝keep_1087/a11。',
       '- 判定の数（TSV の 9 列目）：' + '・'.join(f'{k} {v}' for k, v in sorted(cnt.items(), key=lambda kv: -kv[1])) + f'（{len(body)} 行）。',
       '', '| ' + ' | '.join(head) + ' |', '|' + '---|' * len(head)]
for r in body:
    assert all('|' not in c for c in r), r[0]
    sec.append('| ' + ' | '.join(r) + ' |')
sec.append('')
open(os.path.join(W, 'section_S08.md'), 'w', encoding='utf-8', newline='\n').write('\n'.join(sec) + '\n')
# 写し：§1 の S08 の状態・§3 の S08 の仮の行を参照へ畳む・§2h を ## 3. の前へ
src = open(os.path.join(repo, 'docs', '機能の台帳.md'), 'rb').read().decode('utf-8')
lines = src.split('\n'); out = []; i = 0; done3 = False; ins = False
while i < len(lines):
    l = lines[i]
    if l.startswith('| S08 | 輸送と地理 |'):
        c = l.split('|'); c[-2] = ' (1101) で確かめた '; l = '|'.join(c)
    if l.startswith('## 3. 他の系統') and not ins:
        out += sec; ins = True
    if l == '### S08 輸送と地理' and not done3:
        out += [l, '', '→ §2h（(1101) c105s08 で確かめた 41 行・id は同じ）。', '']
        i += 1
        while i < len(lines) and not lines[i].startswith('### '): i += 1
        done3 = True; continue
    out.append(l); i += 1
assert ins and done3
open(os.path.join(W, 'ledger_copy.md'), 'wb').write('\n'.join(out).encode('utf-8'))
print('section rows', len(body))
