#!/usr/bin/env bash
# (1165) c115cr 短い走り（紙の旗 0・旗 1 の CTL・突然変異）：使い捨ての木に旗と突然変異を 1 本ずつ当て、計器つきで門の台本を読む（(1151) mut_run_cr.sh の型）。
# 使い方: bash mut_run_c115.sh <元の木> <作業フォルダ> <thr_c115.txt> <基準の生> <種の数> <四半期> <形 B|S|2> <M...>
#   M＝CTL0（旗 0・突然変異なし＝紙）／CTL（旗 1 だけ）／他（旗 1＋mutate_c115.py の突然変異）。
# 出力：<作業フォルダ>/<M>/ins（門の読む表だけ）・<作業フォルダ>/gates_<M>.txt。木の写しは走りの後に消す。
SRCT="$1"; W="$2"; THR="$3"; BASE="$4"; NS="$5"; NQ="$6"; FORM="$7"; shift 7
export TEMP=D:/PoB_work/c115cr_work/tmp TMP=D:/PoB_work/c115cr_work/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
S="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
mkdir -p "$W" "$TEMP"
SCALE=$(python -c "print($NS*$NQ/(10*668))")
for M in "$@"; do
  T="$W/tree_$M"; rm -rf "$T" && mkdir -p "$T"
  tar -cf - -C "$SRCT" --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs --exclude=PriceOfBread.App --exclude=PriceOfBread.Tests --exclude=PriceOfBread.Tests.Ui . | tar -xf - -C "$T"
  : > "$W/mut_$M.log"
  if [ "$M" != "CTL0" ]; then python -X utf8 -I "$S/mutate_c115.py" "$T" FLAG1 >> "$W/mut_$M.log" 2>&1 || { echo "$M FLAGFAIL" >> "$W/summary.txt"; continue; }; fi
  if [ "$M" != "CTL0" ] && [ "$M" != "CTL" ]; then python -X utf8 -I "$S/mutate_c115.py" "$T" "$M" >> "$W/mut_$M.log" 2>&1 || { echo "$M MUTFAIL" >> "$W/summary.txt"; continue; }; fi
  dotnet build "$T/PriceOfBread.Runner/PriceOfBread.Runner.csproj" -c Release --disable-build-servers > "$W/build_$M.log" 2>&1 || { echo "$M BUILDFAIL" >> "$W/summary.txt"; continue; }
  O="$W/$M/ins"; rm -rf "$W/$M"; mkdir -p "$O"
  POB_INSTRUMENTS="$O" POB_INSTRUMENTS_TABLES="rulepower_s,rulepoweralloc_s,rponset_s,nation_s" POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" \
    dotnet "$T/PriceOfBread.Runner/bin/Release/net10.0/PriceOfBread.Runner.dll" selfplay-batch "$NS" - 0 1 "$NQ" > "$W/run_$M.log" 2>&1
  echo "$M run rc=$?" >> "$W/summary.txt"
  python -X utf8 -I "$S/gates_c115.py" "$O" - "$THR" --base "$BASE" --win "$NQ" --form "$FORM" --seeds 1-"$NS" --min-scale "$SCALE" > "$W/gates_$M.txt" 2>&1
  rm -rf "$T"
done
echo "MUTEND $(date +%T)" >> "$W/summary.txt"
