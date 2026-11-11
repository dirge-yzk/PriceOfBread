# (899) 合流：生きた文書（実装ログ・00一覧・CURRENT・README）へ (893)〜(899) を書く。バイトで読み書きする（CR の数と BOM を保つ）。
#   python -X utf8 docs899.py <木の根> <作業フォルダ>
import sys, os
root, M = sys.argv[1:3]
def rd(p): return open(p, 'rb').read()
def wr(p, b): open(p, 'wb').write(b)
def one(b, old, new):
    assert b.count(old) == 1, (old[:80], b.count(old))
    return b.replace(old, new)
fill = {k: rd(os.path.join(M, f)).decode('utf-8').strip() for k, f in [('SUITE', 'fill_suite.txt'), ('STAT', 'fill_stat.txt'), ('SELF', 'fill_self.txt'), ('CLEAN', 'fill_clean.txt'), ('SUITE1', 'fill_suite1.txt')]}
def sub(s):
    for k, v in fill.items(): s = s.replace('<<' + k + '>>', v)
    assert '<<' not in s, s[s.index('<<'):s.index('<<') + 40]
    return s
# 実装ログ：末尾へ足す
L = os.path.join(root, 'docs/202609/99_実装ログ_20260903.md')
b = rd(L)
log = sub(rd(os.path.join(M, 'log899.md')).decode('utf-8'))
if not b.endswith(b'\n'): b += b'\n'
wr(L, b + ('\n' + log.lstrip('\n')).encode('utf-8'))
# 00一覧
F = os.path.join(root, 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md')
b = rd(F).decode('utf-8')
rows = sub(rd(os.path.join(M, 'list_rows899.md')).decode('utf-8')).rstrip('\n') + '\n'
old_title = '| **次の波（波76〜・(892) の推奨・AI・作者の承認は未確認）** |'
new_title = '| **次の波（波76〜・(892) の推奨・AI・作者の承認は未確認）**（2026-11-06 ★(899)：波76 で ⓪（c76lg・S15）・①（c76lb）・②（c76ls）・③（c76cs＝旗 1 のフルスイートで予言に無い赤＝腕なし・3 回の規則に数えない）・④（c76db）・設計の段（c76ds）を消化＝下の〈次の波（波77〜）〉へ） |'
b = one(b, old_title, new_title)
i = b.index(new_title); j = b.index('\n', i) + 1
b = b[:j] + rows + b[j:]
q = rd(os.path.join(M, 'q899.md')).decode('utf-8').rstrip('\n') + '\n'
k = b.index('| **★(892) 作者に聞く物（波75 の合流')
b = b[:k] + q + b[k:]
ad_old = '・恒等 2（c75ce〔cederule_s〕・c75cp〔crownpc_s〕）・読む 1（c75lg〔機能の台帳・S16〕）・設計の材料 1（c75ds）・計画 0**（★(892)）。'
ad_new = ad_old + ' → **波76 挙動 0（c76cs〔王権と議会 (1)・旗 1 のフルスイートで予言に無い赤 3 本＝腕 B・S11 なし＝3 回の規則に数えない〕）・恒等 2（c76lb〔landbase_s〕・c76ls〔器 Producer.LandShares と landshare_s〕）・読む 2（c76lg〔台帳・S15〕・c76db〔案 B の材料〕）・設計の材料 1（c76ds）・計画 0**（★(899)）。'
b = one(b, ad_old, ad_new)
wr(F, b.encode('utf-8'))
# CURRENT
C = os.path.join(root, 'docs/CURRENT.md')
b = rd(C)
head = '| ★ **最終更新（main へ合流済み）** |'.encode('utf-8')
i = b.index(head); j = b.index(b'\n', i) + 1
oldrow = b[i:j]
cur = rd(os.path.join(M, 'cur899.md')).decode('utf-8').rstrip('\n') + '\n'
prev = oldrow.replace(head, '| 前の更新（(892)） |'.encode('utf-8'), 1)
b = b[:i] + cur.encode('utf-8') + prev + b[j:]
wr(C, b)
# README
R = os.path.join(root, 'docs/README.md')
b = rd(R).decode('utf-8')
r_old = '・**★(892) 合流〔波75〕も挙動の線を採らず基線は m873 のまま＝計器の生 INSRAW だけ `D:/PoB_work/keep_892/base_ins`**'
r_new = '・**★(899) 合流〔波76〕も挙動の線を採らず基線は m873 のまま＝計器の生 INSRAW だけ `D:/PoB_work/keep_899/base_ins`**〔合わせた木 ea7fcad2＝BASE_COMMIT・SOURCE-HASH e55bd8213a483380・landbase_s・landbasereg_s・landshare_s が増えた・cearn_s と flow_s は経路の行番号だけ違う＝ident の既知の揺れ・keep_892/base_ins とバイトで同じ 1050 本はハードリンク・TESTS_NONFULL 2376〕・その前の' + r_old.replace('・**★(892)', '**★(892)')
b = one(b, r_old, r_new)
star = rd(os.path.join(M, 'readme899.md')).decode('utf-8').rstrip('\n') + '\n'
k = b.index('- ★(892)：**計っていない予言を')
b = b[:k] + star + b[k:]
wr(R, b.encode('utf-8'))
print('ok')
