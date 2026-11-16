# 読むだけ (1176)：台帳（木 b9570c8a7）の写しに S10 の 76 行を差し込んだ試しの md を作る（ledger_check の門 1〜9 を通すための試し・リポジトリには書かない）。
#  §3 の S10-H の仮の行 47 本と ### S10 の見出しを消し、§2 の新しい節（2k S10）を足す。§1 の S10 の状態を〈(1176) c116rl で確かめた〉へ。
# 使い方: python -X utf8 -I mk_testmd_s10.py <repo> <作業フォルダ>
import sys, os, subprocess, re
REPO, W = sys.argv[1:3]
md = subprocess.run(['git', '-C', REPO, 'show', 'b9570c8a7:docs/機能の台帳.md'], capture_output=True).stdout.decode('utf-8')
nl = '\r\n' if '\r\n' in md else '\n'
L = md.split(nl)
ls = open(os.path.join(W, 'ledger_S10_2026-11-16.tsv'), encoding='utf-8').read().split('\n')
rows = ['| ' + ' | '.join(l.split('\t')) + ' |' for l in ls[1:] if l.strip()]
out = []; dropped = 0; droph = 0
for l in L:
    if re.match(r'^\| S10-H\d\d\d \|', l): dropped += 1; continue
    if l.startswith('### S10 '): droph += 1; continue
    m = re.match(r'^\| (S10) \|', l)
    if m:
        c = l.split('|'); c[-2] = ' (1176) c116rl で確かめた（木 b9570c8a7・写しは 9b18137a） '; l = '|'.join(c)
    out.append(l)
i3 = out.index(next(x for x in out if x.startswith('## 3. ')))
hdr = '| ' + ' | '.join(ls[0].split('\t')) + ' |'
sep = '|' + '---|' * 12
ins = ['## 2k. S10 人口動態と暮らし（(1176) c116rl で確かめた・試し）', '', hdr, sep] + rows + ['']
out = out[:i3] + ins + out[i3:]
open(os.path.join(W, 'test_ledger.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('消した仮の行', dropped, '消した見出し', droph, '足した行', len(rows))
