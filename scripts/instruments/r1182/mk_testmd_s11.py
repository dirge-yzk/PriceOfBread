# 読むだけ (1182)：台帳（木 b9570c8a7）の写しに S11 の 37 行を差し込んだ試しの md を作る（ledger_check の門 1〜9 を通すための試し・リポジトリには書かない）。
#  §3 の S11-H の仮の行 15 本と ### S11 の見出しを消し、§2 の新しい節（2k S11）を足す。§1 の S11 の状態を〈(1182) c117rs で確かめた〉へ。
# 使い方: python -X utf8 -I mk_testmd_s10.py <repo> <作業フォルダ>
import sys, os, subprocess, re
REPO, W = sys.argv[1:3]
md = subprocess.run(['git', '-C', REPO, 'show', 'c590d21ba:docs/機能の台帳.md'], capture_output=True).stdout.decode('utf-8')
nl = '\r\n' if '\r\n' in md else '\n'
L = md.split(nl)
ls = open(os.path.join(W, 'ledger_S11_2026-11-16.tsv'), encoding='utf-8').read().split('\n')
rows = ['| ' + ' | '.join(l.split('\t')) + ' |' for l in ls[1:] if l.strip()]
out = []; dropped = 0; droph = 0
for l in L:
    if re.match(r'^\| S11-H\d\d\d \|', l): dropped += 1; continue
    if l.startswith('### S11 '): droph += 1; continue
    m = re.match(r'^\| (S11) \|', l)
    if m:
        c = l.split('|'); c[-2] = ' (1182) c117rs で確かめた（木 c590d21ba・写しは 9b18137a） '; l = '|'.join(c)
    out.append(l)
i3 = out.index(next(x for x in out if x.startswith('## 3. ')))
hdr = '| ' + ' | '.join(ls[0].split('\t')) + ' |'
sep = '|' + '---|' * 12
ins = ['## 2l. S11 教育と識字と技術の伝わり（(1182) c117rs で確かめた・試し）', '', hdr, sep] + rows + ['']
out = out[:i3] + ins + out[i3:]
open(os.path.join(W, 'test_ledger.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('消した仮の行', dropped, '消した見出し', droph, '足した行', len(rows))
