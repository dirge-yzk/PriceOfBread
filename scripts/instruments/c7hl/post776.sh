#!/usr/bin/env bash
# (776) c7hl：腕 B（計器つき 10 種）の終了後の読み。使い方：bash post776.sh <腕の作業フォルダ=D:/PoB_work/c7hl_arm> <腕の名=c7hlB> <日付=20261102>
#   成績表・manifest・cost を measurements/ へ写し、nullgate・ng.sh・ngset192・an_h2.py（空洞化）を回して出力を measurements/m776_<日付>_arm_*.txt へ置く。
set -u
export PYTHONUTF8=1 PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W="${1:-D:/PoB_work/c7hl_arm}"; ARM="${2:-c7hlB}"; D="${3:-20261102}"
R="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"; M="$R/measurements"; O="$W/out_${ARM}_10"
cp "$W/b10_${ARM}_10.txt" "$M/b10_${D}_${ARM}_ins"
cp "$W/b10_${ARM}_10.txt.manifest" "$M/b10_${D}_${ARM}_ins.manifest"
[ -f "$W/b10_${ARM}_10.txt.cost" ] && cp "$W/b10_${ARM}_10.txt.cost" "$M/b10_${D}_${ARM}_ins.cost"
echo "== nullgate"; python "$R/scripts/instruments/tools/nullgate.py" "$M/b10_${D}_${ARM}_ins" "$M/b10_20261102_m773_ins" "$M/b10_20261102_m773_n1_ins" "$M/b10_20261102_m773_n2_ins" | tee "$M/m776_${D}_arm_nullgate.txt"
echo "== hol"; python "$R/scripts/instruments/hol/an_h2.py" "$O/ins" -v4 | tee "$M/m776_${D}_arm_hol.txt" | tail -40
echo "== ng"; mkdir -p "$W/ng"; bash "$R/scripts/instruments/ng.sh" "$O/ins" "$ARM" "D:/PoB_work/c7hl_arm/ng" "rd:$(cd "$M" && pwd -W)/ng773_20261102:m773" | tee "$M/m776_${D}_arm_ng.txt" | tail -30
