#!/usr/bin/env bash
# (820) c65op 腕 B（旗 1・計器つき 10 種・種 1〜10）。使い方: bash scripts/instruments/c65op/armB.sh（Bash の run_in_background で）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export POB_WORK=D:/PoB_work/c65op_work/armB POB_REPO=D:/PoB_work/wt_c65op PYTHONUTF8=1 AVALONIA_TELEMETRY_OPTOUT=1 MEASURE_CACHE=0
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
S=D:/PoB_work/wt_c65op/scripts/instruments
mkdir -p $POB_WORK
bash $S/batch.sh prep c65opB dir:D:/PoB_work/wt_c65op plain || exit 1
INS=1 bash $S/batch.sh run c65opB 10; RC=$?
echo "armB rc=$RC"
