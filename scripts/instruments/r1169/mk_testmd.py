# 読むだけ (1169)：台帳（木 c3c4de36a）の写しに、この線の行を差し込んだ試しの md を作る（ledger_check の門 1〜9 を通すための試し・リポジトリには書かない）。
#  §3 の S01-H・S02-H の仮の行を消し、§2 の新しい節（2i S02・2j S01）を足す。S01-V・S22 の 9 行は同じ id の行を置き換える。§1 の S01・S02 の状態を〈(1169) で確かめた〉にする。
# 使い方: python -X utf8 -I mk_testmd.py <repo> <作業フォルダ>
import sys, os, subprocess, re
REPO, W = sys.argv[1:3]
md = subprocess.run(['git', '-C', REPO, 'show', 'c3c4de36a:docs/機能の台帳.md'], capture_output=True).stdout.decode('utf-8')
nl = '\r\n' if '\r\n' in md else '\n'
L = md.split(nl)
def tsv_rows(p):
    ls = open(os.path.join(W, p), encoding='utf-8').read().split('\n')[1:]
    return ['| ' + ' | '.join(l.split('\t')) + ' |' for l in ls if l.strip()]
repl = {}
for p in ('ledger_S01V_2026-11-16.tsv', 'ledger_S22u9_2026-11-16.tsv'):
    for r in tsv_rows(p): repl[r.split('|')[1].strip()] = r
out = []; dropped = 0
for l in L:
    m = re.match(r'^\| (S0[12]-H\d\d\d) \|', l)
    if m: dropped += 1; continue
    m = re.match(r'^\| (S\d\d-[A-Z]\d\d\d?) \|', l)
    if m and m.group(1) in repl: out.append(repl.pop(m.group(1))); continue
    m = re.match(r'^\| (S0[12]) \|', l)
    if m:
        c = l.split('|'); c[-2] = f' (1169) c115lg で確かめた（木 c3c4de36a・写しは 9b18137a） '; l = '|'.join(c)
    out.append(l)
assert not repl, repl
i4 = out.index(next(x for x in out if x.startswith('## 3. ')))
hdr = ['| 行 | 系統 | 機能 | 期待する動き（出典） | 錨 | 持ち主のファイル | 判定の型 | 最後に確かめた | 判定 | 根拠 | 残りタスク | 直す／消す／残す |', '|---|---|---|---|---|---|---|---|---|---|---|---|']
ins = ['## 2i. S02 値段と賃金の形成（(1169) c115lg で確かめた・試し）', ''] + hdr + tsv_rows('ledger_S02_2026-11-16.tsv') + ['', '## 2j. S01 財と製法と発見（(1169) c115lg で確かめた・試し）', ''] + hdr + tsv_rows('ledger_S01_2026-11-16.tsv') + ['']
out = out[:i4] + ins + out[i4:]
open(os.path.join(W, 'test_ledger.md'), 'wb').write(nl.join(out).encode('utf-8'))
print('消した仮の行', dropped, '・足した行', len(tsv_rows('ledger_S02_2026-11-16.tsv')) + len(tsv_rows('ledger_S01_2026-11-16.tsv')))
