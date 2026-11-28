#!/usr/bin/env bash
# (1428) c153rg：突然変異 M1〜M4 を使い捨ての木で回し、単体の機構の試験（ReligionTensionRemovalTests）の赤と緑を数える（c152ms/mut.sh の写し）。
#   bash mut.sh <使い捨ての木（D:/…・HEAD の写し）> <作業フォルダ（D:/…）> <出力フォルダ（リポジトリの measurements/…）>
T=${1:?木}; WK=${2:?作業}; O=${3:?出力}; mkdir -p $WK/tmp "$O"
export TEMP=$WK/tmp TMP=$WK/tmp
P=PriceOfBread.SimCore/Coefficients.p05_port_insurance.cs
cd "$T" || exit 1
rep() { python -X utf8 -I - "$1" "$2" "$3" <<'PY'
import sys
p, a, b = sys.argv[1], sys.argv[2].encode('utf-8'), sys.argv[3].encode('utf-8')
t = open(p, 'rb').read(); n = t.count(a)
assert n == 1, f'{p}: {n} hits'
open(p, 'wb').write(t.replace(a, b))
PY
}
for M in M1 M2 M3 M4; do
  git checkout -q -- $P
  case $M in
    M1) rep $P "public double ReligionTensionPerMonth = 0; " "public double ReligionTensionPerMonth = 0.03; " ;;
    M2) rep $P "public double ReligionAffinityRelief = 0; " "public double ReligionAffinityRelief = 0.06; " ;;
    M3) rep $P "public double ProtectorTensionPerMonth = 0.02; " "public double ProtectorTensionPerMonth = 0; " ;;
    M4) rep $P "public double ReligionHeresyBonus = 0.22; " "public double ReligionHeresyBonus = 0; " ;;
  esac || { echo "$M REPFAIL"; continue; }
  git diff --stat -- $P | tail -1 > "$O/mut_$M.txt"
  dotnet build PriceOfBread.Tests -c Release --artifacts-path $WK/art_mut -v q -nologo > $WK/mbuild_$M.txt 2>&1 || { echo "$M BUILDFAIL" | tee -a "$O/mut_$M.txt"; continue; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path $WK/art_mut --no-build --filter "FullyQualifiedName~ReligionTensionRemovalTests" --logger "console;verbosity=normal" >$WK/mtest_$M.txt 2>&1
  echo "$M rc=$?" >> "$O/mut_$M.txt"
  grep -E "^\s+(失敗|成功|Failed|Passed) PriceOfBread|合計|失敗:|成功:" $WK/mtest_$M.txt | python -X utf8 -I -c "import sys,re;[print(re.sub(r'\[[0-9.]* m?s\]','',l),end='') for l in sys.stdin]" >> "$O/mut_$M.txt"
done
git checkout -q -- $P
echo MUTDONE
