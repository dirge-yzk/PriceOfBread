#!/usr/bin/env bash
# (1152) c113lc の突然変異と対照。コミットから使い捨ての木を作り（git archive）、変異を当てて短い走りと xUnit（RailLocoGateWorldTests）を回す。
#   bash runmuts.sh <コミット> <作業フォルダ> <名:係数:変異:xunit ...>   係数は POB_COEF（; 区切り）・変異は空可・xunit は 1 で xUnit も回す
R=/d/PoB_work/wt_c113lc; S=/d/PoB_work/c113lc_work/scripts
export PATH="/c/Program Files/dotnet:/usr/bin:/c/Program Files/Git/usr/bin:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1
export TEMP=D:/PoB_work/c113lc_work/tmp TMP=D:/PoB_work/c113lc_work/tmp
H="${1:?コミット}"; W="${2:?作業フォルダ}"; shift 2
mkdir -p "$W"
for spec in "$@"; do
  IFS=: read -r NAME CO MU XU <<< "$spec"
  T="$W/tree_$NAME"; rm -rf "$T" "$W/$NAME"; mkdir -p "$T" "$W/$NAME"
  ( cd "$R" && git archive "$H" ) | tar -x -C "$T"
  if [ -n "$MU" ]; then python -X utf8 -I "$S/muts.py" "$T" "$MU" > "$W/$NAME/apply.txt" 2>&1 || { echo "$NAME apply-fail" >> "$W/runmuts.log"; rm -rf "$T"; continue; }; fi
  if [ -n "$CO" ]; then bash "$S/runshort.sh" "$T" "$W/$NAME" "$CO"; fi
  if [ "$XU" = 1 ]; then
    dotnet test "$T/PriceOfBread.Tests/PriceOfBread.Tests.csproj" -c Release --artifacts-path "$W/art_$NAME" -nologo --filter "FullyQualifiedName~RailLocoGateWorldTests" > "$W/$NAME/xunit.txt" 2>&1
    echo "xunit rc=$?" >> "$W/$NAME/rc.txt"
    rm -rf "$W/art_$NAME"
  fi
  echo "$NAME $(tr '\n' ' ' < "$W/$NAME/rc.txt" 2>/dev/null)" >> "$W/runmuts.log"
  rm -rf "$T"
done
