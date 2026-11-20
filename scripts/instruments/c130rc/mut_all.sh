#!/usr/bin/env bash
# (1270) c130rc 突然変異を使い捨ての木で 1 つずつ（元に戻してから当てる）。使い方: bash mut_all.sh <使い捨ての木> <出力ファイル> <台本の木>
T=${1:?木}; OUT=${2:?出力}; S=${3:?台本の木}
: > "$OUT"
for mu in none mu0 muopp musign mumax muskip; do
  (cd "$T" && git checkout -q -- PriceOfBread.SimCore/MultiNationSim.cs PriceOfBread.Tests/RecruitVacancyTests.cs)
  [ "$mu" != none ] && python -X utf8 -I "$S/scripts/instruments/c130rc/mutate.py" "$T" "$mu" >> "$OUT.detail" 2>&1
  (cd "$T" && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c130rc_work/mut_art --filter "FullyQualifiedName~RecruitVacancyTests" --logger "console;verbosity=normal") > "$OUT.$mu.txt" 2>&1
  echo "== $mu rc=$?" >> "$OUT"
  grep -E "^\s+(合格|失敗|成功|Passed|Failed) |T[1-4]_" "$OUT.$mu.txt" | grep -E "合格|失敗|Passed|Failed" | sed -E 's/\[[0-9 .a-z]+\]//' | sort -u >> "$OUT"
done
(cd "$T" && git checkout -q -- PriceOfBread.SimCore/MultiNationSim.cs PriceOfBread.Tests/RecruitVacancyTests.cs)
echo MUTDONE >> "$OUT"
