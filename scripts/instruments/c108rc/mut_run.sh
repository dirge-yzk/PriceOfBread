#!/usr/bin/env bash
# (1117) c108rc 突然変異の腕（事前登録 §3）：使い捨ての木で 1 本ずつ当て、2 種・80 四半期の計器つきの短い走りで門の台本を読む（対照 CTL＝突然変異なしの旗 1 の木）。
# 使い方: bash mut_run.sh <元の木（旗 1）> <作業フォルダ> <c5_series.tsv> <突然変異...>
# 出力：<作業フォルダ>/<M>/ins（門の読む表だけ）・<作業フォルダ>/gates_<M>.txt。木の写しとビルドは走りの後に消す。
SRCT="$1"; W="$2"; C5="$3"; shift 3
export TEMP=D:/PoB_work/c108rc_work/tmp TMP=D:/PoB_work/c108rc_work/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$W"
for M in "$@"; do
  T="$W/tree_$M"; rm -rf "$T" && mkdir -p "$T"
  tar -cf - -C "$SRCT" --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs --exclude=PriceOfBread.App --exclude=PriceOfBread.Tests --exclude=PriceOfBread.Tests.Ui . | tar -xf - -C "$T"
  if [ "$M" != "CTL" ]; then python -X utf8 -I "$S/mutate.py" "$T" "$M" > "$W/mut_$M.log" 2>&1 || { echo "$M MUTFAIL" >> "$W/summary.txt"; continue; }; fi
  dotnet build "$T/PriceOfBread.Runner/PriceOfBread.Runner.csproj" -c Release --disable-build-servers > "$W/build_$M.log" 2>&1 || { echo "$M BUILDFAIL" >> "$W/summary.txt"; continue; }
  O="$W/$M/ins"; rm -rf "$W/$M"; mkdir -p "$O"
  POB_INSTRUMENTS="$O" POB_INSTRUMENTS_TABLES="rulepower_s,rulepoweralloc_s,nation_s" POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" \
    dotnet "$T/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" selfplay-batch 2 - 0 1 80 > "$W/run_$M.log" 2>&1
  echo "$M run rc=$?" >> "$W/summary.txt"
  python -X utf8 -I "$S/gates_rc.py" "$O" "$C5" --seeds 1-2 > "$W/gates_$M.txt" 2>&1
  rm -rf "$T"
done
echo "MUTEND $(date +%T)" >> "$W/summary.txt"
