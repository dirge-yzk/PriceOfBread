#!/usr/bin/env bash
# (964) c86lt 腕を起こす（(958) arm958.sh の写し・作業フォルダを c86lt_work へ・計器の国を明示・腕 A の旗 0 を prep の書き換えで渡す口を足した）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c86lt_work/armA.mark bash scripts/instruments/c86lt/arm964.sh c86ltA 5 1 0 scripts/instruments/c86lt/flag0_964.py
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c86lt_work/armB.mark bash scripts/instruments/c86lt/arm964.sh c86ltB 10 1 1
#   POB_CEARN=0 bash scripts/instruments/tools/bg.sh D:/PoB_work/c86lt_work/armS.mark bash scripts/instruments/c86lt/arm964.sh c86ltS 10 11 1
#   引数：<腕> <種の数> <種の起点> <INS 0|1> [prep の書き換えの台本（batch.sh prep の EDIT＝src の写しにだけ当たる）]
#   作業フォルダ D:/PoB_work/c86lt_work/arm_<腕>（POB_WORK）・木＝この台本のある木の HEAD（batch.sh prep commit:）。終了コードは batch.sh run のまま（0 完走・2 計器の国が空・3 ARMFULL・4 BATCHFAIL）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(cd "$SP/../../.." && pwd)"
ARM="${1:?腕}"; N="${2:-10}"; SB="${3:-1}"; INSF="${4:-1}"; ED="${5:-}"
export POB_WORK="D:/PoB_work/c86lt_work/arm_$ARM"; mkdir -p "$POB_WORK"
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
H=$(git -C "$REPO" rev-parse HEAD)
FREE=$(python -c "import shutil;print(round(shutil.disk_usage('D:/').free/1e9,1))")
echo "木 $H 腕 $ARM 種 $SB〜$((SB+N-1)) INS=$INSF 書き換え=${ED:-なし} POB_CEARN=${POB_CEARN:-（既定）} D:の空き ${FREE}GB 枠の空き $(bash "$REPO/scripts/instruments/tools/arms.sh" free)"
if [ -n "$ED" ]; then bash "$REPO/scripts/instruments/batch.sh" prep "$ARM" "commit:$H" plain "$REPO/$ED" || exit 1
else bash "$REPO/scripts/instruments/batch.sh" prep "$ARM" "commit:$H" plain || exit 1; fi
if [ "$INSF" = 1 ]; then INS=1 bash "$REPO/scripts/instruments/batch.sh" run "$ARM" "$N" "$SB"; else bash "$REPO/scripts/instruments/batch.sh" run "$ARM" "$N" "$SB"; fi
