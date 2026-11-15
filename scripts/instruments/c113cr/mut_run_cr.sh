#!/usr/bin/env bash
# (1151) c113cr 突然変異の短い走り（事前登録 §3・(1123) mut_run.sh の型）：使い捨ての木に旗 1 と突然変異を 1 本ずつ当て、2 種×80 四半期の計器つきで門の台本を読む。
# 使い方: bash mut_run_cr.sh <元の木> <作業フォルダ> <c5_series.tsv> <thr_cr.txt> <突然変異...>（CTL＝旗 1 だけ）
# 出力：<作業フォルダ>/<M>/ins（門の読む表だけ）・<作業フォルダ>/gates_<M>.txt。木の写しは走りの後に消す（ビルドの出力は木の中）。
SRCT="$1"; W="$2"; C5="$3"; THR="$4"; shift 4
export TEMP=D:/PoB_work/c113cr_work/tmp TMP=D:/PoB_work/c113cr_work/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$W" "$TEMP"
for M in "$@"; do
  T="$W/tree_$M"; rm -rf "$T" && mkdir -p "$T"
  tar -cf - -C "$SRCT" --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs --exclude=PriceOfBread.App --exclude=PriceOfBread.Tests --exclude=PriceOfBread.Tests.Ui . | tar -xf - -C "$T"
  python -X utf8 -I "$S/mutate_cr.py" "$T" FLAG1 > "$W/mut_$M.log" 2>&1 || { echo "$M FLAGFAIL" >> "$W/summary.txt"; continue; }
  if [ "$M" != "CTL" ]; then python -X utf8 -I "$S/mutate_cr.py" "$T" "$M" >> "$W/mut_$M.log" 2>&1 || { echo "$M MUTFAIL" >> "$W/summary.txt"; continue; }; fi
  dotnet build "$T/PriceOfBread.Runner/PriceOfBread.Runner.csproj" -c Release --disable-build-servers > "$W/build_$M.log" 2>&1 || { echo "$M BUILDFAIL" >> "$W/summary.txt"; continue; }
  O="$W/$M/ins"; rm -rf "$W/$M"; mkdir -p "$O"
  POB_INSTRUMENTS="$O" POB_INSTRUMENTS_TABLES="rulepower_s,rulepoweralloc_s,rponset_s,nation_s" POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" \
    dotnet "$T/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" selfplay-batch 2 - 0 1 80 > "$W/run_$M.log" 2>&1
  echo "$M run rc=$?" >> "$W/summary.txt"
  python -X utf8 -I "$S/gates_cr.py" "$O" "$C5" "$THR" --seeds 1-2 --min-scale 0.024 > "$W/gates_$M.txt" 2>&1
  rm -rf "$T"
done
echo "MUTEND $(date +%T)" >> "$W/summary.txt"
