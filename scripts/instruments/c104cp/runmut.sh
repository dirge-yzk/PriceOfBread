#!/usr/bin/env bash
# 使い捨ての木で PowerCenterTests（と PopPreferenceTests があれば）を回し、赤の名前を out へ
cd /d/PoB_work/c104cp_work/mut || exit 1
dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c104cp_mut_art --filter "FullyQualifiedName~PowerCenterTests|FullyQualifiedName~PopPreferenceTests" > /d/PoB_work/c104cp_work/mut_$1.log 2>&1
grep -E "^\s*(失敗|Failed) |成功!|失敗!|エラー|error CS" /d/PoB_work/c104cp_work/mut_$1.log | head -12 | cut -c1-200 > /d/PoB_work/c104cp_work/mut_$1.txt
echo done > /d/PoB_work/c104cp_work/mut_$1.mark
