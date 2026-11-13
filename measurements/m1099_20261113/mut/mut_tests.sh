#!/usr/bin/env bash
# (1099) c105gp 突然変異 M1〜M5 を機構の試験（NonElectiveSeatsTests・PartyTests）で回す。木＝使い捨て D:/PoB_work/c105gp_work/mut（daea8a9f1）。
# 出力 D:/PoB_work/c105gp_work/mut_tests.txt。各変異の後に git checkout で戻す。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
T=D:/PoB_work/c105gp_work/mut
R=D:/PoB_work/c105gp_work/mut_tests.txt
A=D:/PoB_work/c105gp_work/mut_art
AP=D:/PoB_work/wt_c105gp/measurements/m1099_20261113/mut/mutapply.py
F="FullyQualifiedName~NonElectiveSeatsTests|FullyQualifiedName~PartyTests"
: > "$R"
cd "$T" || exit 2
run() {
  git checkout -q -- PriceOfBread.SimCore
  if [ "$1" != M_none ]; then python -X utf8 "$AP" "$1" "$T" || { echo "$1 APPLY-FAIL" >> "$R"; return; }; fi
  dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q > D:/PoB_work/c105gp_work/mut_build_$1.txt 2>&1 || { echo "$1 BUILD-FAIL" >> "$R"; return; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path $A --no-build --filter "$F" > D:/PoB_work/c105gp_work/mut_test_$1.txt 2>&1
  echo "$1 $(grep -E '失敗!|成功!' D:/PoB_work/c105gp_work/mut_test_$1.txt | tail -1)" >> "$R"
  grep -E "^\s+失敗 " D:/PoB_work/c105gp_work/mut_test_$1.txt | cut -c1-160 >> "$R"
}
for m in M_none M1 M2 M3 M4 M5; do run $m; done
git checkout -q -- PriceOfBread.SimCore
echo done >> "$R"
