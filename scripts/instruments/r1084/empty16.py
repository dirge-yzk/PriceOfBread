# (1084) 台帳の 12 列目〈直す／消す／残す〉が空の行を数える（全体と §4 以降）
import sys
p = 'C:/Users/dirge/Documents/PriceOfBread/docs/機能の台帳.md'
L = open(p, 'rb').read().decode('utf-8').split('\n')
out = open('D:/PoB_work/r1084_work/empty_rows.tsv', 'w', encoding='utf-8', newline='\n')
out.write('台帳の行\t節\tid\t系統\t機能\t錨\t判定の型\t最後に確かめた\t判定\t根拠(先頭160字)\t残りタスク\t12列目\n')
sec = ''
n_all = 0
for i, s in enumerate(L, 1):
    if s.startswith('#'):
        sec = s.strip('# ').split('（')[0][:30]
    if not s.startswith('| ') or s.startswith('| 行 ') or s.startswith('|---'):
        continue
    c = [x.strip() for x in s.strip().strip('|').split('|')]
    if len(c) != 12:
        continue
    if c[0] in ('行',):
        continue
    v = c[11]
    if v in ('', '—', '-', '―') :
        n_all += 1
        out.write('\t'.join([str(i), sec, c[0], c[1], c[2][:40], c[4][:60], c[6], c[7], c[8], c[9][:160], c[10][:60], v]) + '\n')
out.close()
print('empty_or_dash', n_all)
