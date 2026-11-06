# (905) 合流：生きた文書（実装ログ・00一覧・CURRENT・README）へ (900)〜(905) を書く（(899) の docs899.py の型・バイトで読み書きする＝CR の数と BOM を保つ）。
#   python -X utf8 docs905.py <木の根> <作業フォルダ（log905.md・list_rows905.md・q905.md・cur905.md・readme905.md）>
import sys, os
root, M = sys.argv[1:3]
def rd(p): return open(p, 'rb').read()
def wr(p, b): open(p, 'wb').write(b)
def one(b, old, new):
    assert b.count(old) == 1, (old[:80], b.count(old))
    return b.replace(old, new)
txt = lambda f: rd(os.path.join(M, f)).decode('utf-8')
# 実装ログ：末尾へ足す
L = os.path.join(root, 'docs/202609/99_実装ログ_20260903.md')
b = rd(L)
if not b.endswith(b'\n'): b += b'\n'
wr(L, b + ('\n' + txt('log905.md').lstrip('\n')).encode('utf-8'))
# 00一覧
F = os.path.join(root, 'docs/202609/00_未着手_部分実装_差し戻し_一覧_20260906.md')
b = rd(F).decode('utf-8')
old_title = '| **次の波（波77〜・(899) の推奨・AI・作者の承認は未確認）** |'
new_title = '| **次の波（波77〜・(899) の推奨・AI・作者の承認は未確認）**（2026-11-06 ★(905)：波77 で ⓪（c77lg・S19）・①（c77cs＝採った）・②（c77ce＝S11 の gdp_zero10 で採らない）・③④（c77sm）・作者の指摘 (a)（c77cw＝S11 の〈100 倍〉の〈それ以外〉で採らない）を回した＝下の〈次の波（波78〜）〉の行へ引き継いだ・この行は残す） |'
b = one(b, old_title, new_title)
i = b.index(new_title); j = b.index('\n', i) + 1
b = b[:j] + txt('list_rows905.md').rstrip('\n') + '\n' + b[j:]
k = b.index('| **★(899) 作者に聞く物（波76 の合流')
b = b[:k] + txt('q905.md').rstrip('\n') + '\n' + b[k:]
ad_old = '・読む 2（c76lg〔台帳・S15〕・c76db〔案 B の材料〕）・設計の材料 1（c76ds）・計画 0**（★(899)）。'
ad_new = ad_old + ' → **波77 挙動 1/3（c77cs〔王権と議会 (1)・採った〕・c77ce〔割譲 C1・S11 の gdp_zero10〕と c77cw〔内戦の軍需・S11 の〈100 倍〉の〈それ以外〉〕は採らない＝どちらも腕つきの 1 回目・止めた型は新しいヌル腕も出す）・恒等 1（c77sm〔小物の束〕）・読む 1（c77lg〔台帳・S19〕）・設計の材料 0・計画 0**（★(905)）。'
b = one(b, ad_old, ad_new)
wr(F, b.encode('utf-8'))
# CURRENT
C = os.path.join(root, 'docs/CURRENT.md')
b = rd(C)
head = '| ★ **最終更新（main へ合流済み）** |'.encode('utf-8')
i = b.index(head); j = b.index(b'\n', i) + 1
oldrow = b[i:j]
prev = oldrow.replace(head, '| 前の更新（(899)） |'.encode('utf-8'), 1)
b = b[:i] + (txt('cur905.md').rstrip('\n') + '\n').encode('utf-8') + prev + b[j:]
wr(C, b)
# README
R = os.path.join(root, 'docs/README.md')
b = rd(R).decode('utf-8')
r_old = '（今は `measurements/b5_20261106_m873`＝2026-11-06 (873) の合流〔'
r_new = '（今は `measurements/b5_20261106_m905`＝2026-11-06 (905) の合流〔測った木 `fc9aeb4c`・SOURCE-HASH `aaecd9e3b5cce1b8`・c77cs〔王権と議会 (1)・旗 SummonByInterestShare 既定 1〕で軌道が変わった・恒等の線 c77sm と旗 0 の器 c77cw・c77ce を入れた・旗 0 の腕 Z〔ident m905z〕は m873 と全行一致〕・10種の基線は `b10_20261106_m905`〔exact 47.2・near 86.9・計器つき `b10_20261106_m905_ins`・NG 93〔`rd:measurements/ng905_20261106:m905`〕・推定 212.4 秒＝名目の余り 約 36.1 秒・ヌル腕 `b10_20261106_m905_n1_ins`・`_n2_ins`〔生 `keep_905/n1`・`n2`＝全ての表〕・種 11〜20 `b10s11_20261106_m905_ins`〔生 `keep_905/a11`＝POB_CEARN=0〕・計器の生 INSRAW `D:/PoB_work/keep_905/base_ins`（全ての表）・BASE_COMMIT fc9aeb4c・TESTS_NONFULL 2391〕。旧の基線 `measurements/b5_20261106_m873`＝2026-11-06 (873) の合流〔'
b = one(b, r_old, r_new)
k = b.index('- ★(899)：**挙動の線の事前登録の')
b = b[:k] + txt('readme905.md').rstrip('\n') + '\n' + b[k:]
wr(R, b.encode('utf-8'))
print('ok')
