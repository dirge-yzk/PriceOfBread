# (933) 合流：README の基線の行と ★(933) の教訓を書く（バイトで読み書き）。
#   python -X utf8 readme933.py <README.md>
import sys
p = sys.argv[1]
b = open(p, 'rb').read().decode('utf-8')
def one(old, new):
    global b
    assert b.count(old) == 1, (old[:60], b.count(old))
    b = b.replace(old, new)
one('（今は `measurements/b5_20261107_m919`＝2026-11-07 (919) の合流〔',
    '（今は `measurements/b5_20261107_m933`＝2026-11-07 (933) の合流〔測った木 `1af3e5d7`・SOURCE-HASH `3325579fb3d8a8cc`・c81cw〔(929) 内戦の軍需・旗 CivilWarArmsDemandUnified 既定 1〕と c81la〔(930) B-2a の名簿 7 国の landOwnership のデータ〕で軌道が変わった・恒等の線 c81sm〔(927)〕・c81im〔(928)〕を入れた・腕 Z〔ident m933z〕は〈判定: 不一致〉＝道具の限界〔経路の行の写しと --known-extra の列が同じ cearn_s に重なる〕・独自の検算 zcheck933 で違いは宣言の列と Politics.cs:1958→1959 の経路の写しだけ〕・10種の基線は `b10_20261107_m933`〔exact 45・near 86・計器つき `b10_20261107_m933_ins`・NG 93〔`rd:measurements/ng933_20261107:m933`〕・推定 212.2 秒＝名目の余り 約 36.3 秒・ヌル腕 `b10_20261107_m933_n1_ins`・`_n2_ins`〔生 `keep_933/n1`・`n2`＝全ての表〕・種 11〜20 `b10s11_20261107_m933_ins`〔生 `keep_933/a11`＝POB_CEARN=0〕・計器の生 INSRAW `D:/PoB_work/keep_933/base_ins`（M の生・全ての表 1150 本・imp_s あり）・BASE_COMMIT 1af3e5d7・TESTS_NONFULL 2418〕。旧の基線 `measurements/b5_20261107_m919`＝2026-11-07 (919) の合流〔')
k = b.index('- ★(926)：**計画は D')
add = ('- ★(933)：**恒等の線を 2 本以上重ねた腕 Z の ident は、片方の線の〈--known-extra の列〉と、もう片方の線の〈金を動かすファイルの行の移動〔cearn_s の経路の名の :行〕〉が同じ cearn_s に重なると〈判定: 不一致〉を出す**（波81：c81sm の heirDebtSettledPerQuarter と c81im の Politics.cs の +1 行＝ident_check.py の cearn_same_but_location は経路の外の列のバイト一致を要る＝写しの道を通らない）。'
       '合流は道具を同じ合流で直さず、宣言も広げず、別の台本（`scripts/instruments/m933/zcheck933.py`）で違う欄を〈宣言の列〉〈経路の行の写しで一致〉〈その他〉に分けて数える（その他 0 なら記録の先頭に〈不一致・道具の限界〉と名乗って入れる）・道具の直しは次の波の恒等の線（この組み合わせを再現する試験つき）。'
       '**挙動の線が 2 本の合流のアブレーションは、他の線が恒等なら〈もう一方の線の腕 B〉と成績表が全行一致すると予言でき、素の 10 種（計器なし＝D: をほぼ使わない）で確かめられる**（波81：A-cw0 と c81la の腕 B・A-la0 と c81cw の腕 B が diff 0 行＝線の腕 B の生をアブレーションの生として読めた＝34GB と腕 2 本ぶんの D: を使わなかった）。'
       '**記録の量の足し算は門と別に読む**（c81cw の G3 は〈≧0.2〉の壊さない門で通ったが、弾薬の需要÷内戦の月は cw 単独 0.745・la 単独 1.048・M 1.335＝ヌル腕の幅 0.958〜1.122 の外で向きが逆＝内戦を起こす国の組み合わせに依る量・記録）。'
       '**前面の待ちを 2 本続けて 1 回の呼び出しに書かない**（合流が waitmark を 2 本連ねて 480 秒を超え、背景へ回された）。\n')
b = b[:k] + add + b[k:]
open(p, 'wb').write(b.encode('utf-8'))
print('ok')
