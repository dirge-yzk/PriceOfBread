#!/usr/bin/env bash
# (546) 線 iwf 後半：腕 A・B を回す（計器つき 10 種）。使い方：bash run_arm.sh <A|B> <commit>
#   A＝旗 0（恒等の証明・POB_FIRST5_OUT で先頭 5 種の成績表も出す）／B＝POB_COEF=FamineGateIncomeExempt=1。A と B は別々の run_in_background の呼びで同時に回す。
#   作業フォルダ D:/PoB_work/iwf_546b（報告を書いたら消す）。木は commit:（git archive）。NATIONS は事前登録 [4] の 12 国。
ARM=$1; COMMIT=$2
export POB_WORK=D:/PoB_work/iwf_546b
export POB_REPO=D:/PoB_work/wt_iwf
export INS=1
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ,マラータ同盟,ハイデラバード,スペイン"
cd $POB_REPO
bash scripts/instruments/batch.sh prep $ARM commit:$COMMIT plain || exit 1
if [ "$ARM" = A ]; then
  : > $POB_WORK/coef_A.txt
  export POB_FIRST5_OUT=$POB_WORK/first5_A.txt
else
  echo "FamineGateIncomeExempt=1" > $POB_WORK/coef_B.txt
fi
bash scripts/instruments/batch.sh run $ARM 10
