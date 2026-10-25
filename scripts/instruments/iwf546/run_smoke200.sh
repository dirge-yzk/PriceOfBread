#!/usr/bin/env bash
# 煙試験（種 1・200 四半期・腕の外）：旗 0 と旗 1 の計器つきを順に（$1＝名前 $2＝POB_COEF）
EXE=/d/PoB_work/iwf_546/art_new/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
OUT=/d/PoB_work/iwf_546/s0/$1
rm -rf $OUT; mkdir -p $OUT
POB_INSTRUMENTS=D:/PoB_work/iwf_546/s0/$1 POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ,マラータ同盟,ハイデラバード,スペイン" POB_COEF="$2" $EXE selfplay 1 200 > $OUT.txt 2>&1
echo "done $1 rc=$?" >> /d/PoB_work/iwf_546/s0/log.txt
