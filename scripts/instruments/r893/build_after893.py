# (893) c76lg：今の docs/機能の台帳.md（木 731d3de1）に S15 の節を入れた台帳の下書きを作る（読むだけの線＝リポジトリは書かない・合流が写す）。
# 使い方: python -X utf8 build_after893.py <今の台帳> <S15 の節> <出力>
import sys
src, sec, out = sys.argv[1:4]
b = open(src, 'rb').read().decode('utf-8')
s = open(sec, 'rb').read().decode('utf-8')
assert '\r' not in b and '\r' not in s
def rep(a, c):
    global b
    assert b.count(a) == 1, (b.count(a), a[:60])
    b = b.replace(a, c)
# §0 の運用の行
rep('**門ごとに割ってあるのは確かめた系統だけ**（今は S16）', '**門ごとに割ってあるのは確かめた系統だけ**（今は S16・S15）')
rep('- 錨の確かめ：`python -X utf8 scripts/instruments/r886/ledger_check.py <repo> <木> docs/機能の台帳.md`＝各錨の行に名が含まれるか。',
    '- 錨の確かめ：`python -X utf8 scripts/instruments/r893/ledger_check.py <repo> <木> docs/機能の台帳.md`＝上側の門（各錨の行に名が含まれるか・判定を埋めた行の型と根拠）と下側の門（行が 1 本以上・§1 で〈で確かめた〉の系統ごとに行があり全ての行に最後に確かめたコミットがある＝仮の行が残らない・判定を埋めた行は錨と最後に確かめたコミットを持つ）。(893) で下側の門を足した（それまでは上側だけ）。')
rep('- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r886/changed_rows.py <repo か worktree> docs/機能の台帳.md`＝行の〈最後に確かめたコミット〉から、持ち主のファイルが変わった行（`git diff --name-only <コミット>`）と、錨の係数の宣言の行が変わった行（Coefficients.cs の差分にその名がある）だけを出す。',
    '- 確かめ直す行の選び方（AI のプレイテストの前）：`python -X utf8 scripts/instruments/r893/changed_rows.py <repo か worktree> docs/機能の台帳.md`＝行の〈最後に確かめたコミット〉から、持ち主のファイルが変わった行（`git diff --name-only <コミット>`）と、錨の係数の宣言の行が変わった行（錨が Coefficients.cs ならその名・他の錨ならその行の `c.X`・`Coef.X`・`coef.X` の X＝(893) で足した・Coefficients.cs の差分にその名の宣言〔名の直後に = か { か ;〕がある）だけを出す。')
rep('- 生の名：R1＝D:/PoB_work/keep_885/base_ins（合わせた木 aef07f2a の腕 Z・計器つき 10 種・盤面 m873 と全行一致）。',
    '- 生の名：R1＝D:/PoB_work/keep_885/base_ins（合わせた木 aef07f2a の腕 Z・計器つき 10 種・盤面 m873 と全行一致）。R1′＝D:/PoB_work/keep_892/base_ins（合わせた木 10fe1b48 の腕 Z・計器つき 10 種・m873 と全行一致・crownpc_s と cederule_s つき＝(893) の S15 が読んだ）。')
# §1 の S15 の行の状態
rep('| 19 | 94 | なし | 未確かめ（対象外の候補・次の 1 系統の候補） |', '| 19 | 94 | なし | (893) で確かめた（§2b・木 731d3de1） |')
# §2b を §3 の前に入れる
rep('\n## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）\n', '\n' + s.rstrip('\n') + '\n\n## 3. 他の系統の仮の行（見出し 1 件＝1 行・判定は未確かめ）\n')
# §3 の S15 の仮の行を置き換える
i = b.index('\n### S15 正統性と政体の遷移\n')
j = b.index('\n### S27 支配度と分離独立と領土の移転\n', i)
b = b[:i] + '\n### S15 正統性と政体の遷移\n\n- (893) c76lg が門ごとに割った＝§2b（仮の行 S15-H001〜S15-H032 は §2b の S15-01〜S15-45 へ置き換えた）。\n' + b[j:]
open(out, 'wb').write(b.encode('utf-8'))
print('書いた', out, len(b.split('\n')), '行')
