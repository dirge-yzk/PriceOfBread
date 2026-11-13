#!/usr/bin/env bash
# (1091) 突然変異 M1〜M3（使い捨ての木 D:/PoB_work/c104si_work/mut）。出力 D:/PoB_work/c104si_work/mut_result.txt
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
T=D:/PoB_work/c104si_work/mut
R=D:/PoB_work/c104si_work/mut_result.txt
: > "$R"
cd "$T" || exit 2
run() {  # 名前 絞り込み
  git checkout -q -- PriceOfBread.SimCore
  python /d/PoB_work/c104si_work/mutapply.py "$1" || { echo "$1 APPLY-FAIL" >> "$R"; return; }
  dotnet build PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c104si_work/mut_art -v q > D:/PoB_work/c104si_work/mut_build_$1.txt 2>&1 || { echo "$1 BUILD-FAIL" >> "$R"; return; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c104si_work/mut_art --no-build --filter "$2" > D:/PoB_work/c104si_work/mut_test_$1.txt 2>&1
  echo "$1 $(grep -E '失敗!|成功!' D:/PoB_work/c104si_work/mut_test_$1.txt | tail -1)" >> "$R"
  grep -E "^\s+失敗 " D:/PoB_work/c104si_work/mut_test_$1.txt | cut -c1-160 >> "$R"
}
run M0 "FullyQualifiedName~RulePowerAllocTests|FullyQualifiedName~MineCapInstrumentTests"
run M1 "FullyQualifiedName~RulePowerAllocTests|FullyQualifiedName~MineCapInstrumentTests"
run M2 "FullyQualifiedName~RulePowerAllocTests|FullyQualifiedName~MineCapInstrumentTests"
run M3 "FullyQualifiedName~RulePowerAllocTests|FullyQualifiedName~MineCapInstrumentTests"
git checkout -q -- PriceOfBread.SimCore
echo done >> "$R"
