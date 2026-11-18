# 読むだけ (1203)：台帳（木 c68e09bfb）の写しに S21 の 35 行を差し込んだ試しの md を作る（ledger_check の門 1〜9 を通すための試し・リポジトリには書かない）。
#  §3 の S21-H の仮の行 20 本と ### S21 の見出し、§4-6 の ### 4-6-S21 の見出しと 11-12 の 32 行を消し、§2 の新しい節（2n S21）を足す。§1 の S21 の状態を〈(1203) c120rh で確かめた〉へ。
#  （(1182) mk_testmd_s11.py の形を写した）
# 使い方: python -X utf8 -I mk_testmd_s21.py <repo> <作業フォルダ>
import sys, os, subprocess, re
REPO, W = sys.argv[1:3]
md = subprocess.run(['git', '-C', REPO, 'show', 'c68e09bfb:docs/機能の台帳.md'], capture_output=True).stdout.decode('utf-8')
nl = '\r\n' if '\r\n' in md else '\n'
L = md.split(nl)
ls = open(os.path.join(W, 'q3', 'ledger_S21_2026-11-18.tsv'), encoding='utf-8').read().split('\n')
rows = ['| ' + ' | '.join(l.split('\t')) + ' |' for l in ls[1:] if l.strip()]
out = []; dropped = 0; droph = 0; dropold = 0
for l in L:
    if re.match(r'^\| S21-H\d\d\d \|', l): dropped += 1; continue
    if re.match(r'^\| S21-\d\d \|', l): dropold += 1; continue
    if l.startswith('### S21 ') or l.startswith('### 4-6-S21'): droph += 1; continue
    m = re.match(r'^\| (S21) \|', l)
    if m:
        c = l.split('|'); c[-2] = ' (1203) c120rh で確かめた（木 c68e09bf・写しは 7bb40c77） '; l = '|'.join(c)
    out.append(l)
i3 = out.index(next(x for x in out if x.startswith('## 3. ')))
hdr = '| ' + ' | '.join(ls[0].split('\t')) + ' |'
sep = '|' + '---|' * 12
ins = ['## 2n. S21 国債と破産（(1203) c120rh で確かめた・試し）', '', hdr, sep] + rows + ['']
out = out[:i3] + ins + out[i3:]
open(os.path.join(W, 'q3', 'test_ledger.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('消した仮の行', dropped, '消した 11-12 の行', dropold, '消した見出し', droph, '足した行', len(rows))
