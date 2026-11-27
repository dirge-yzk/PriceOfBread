#!/usr/bin/env bash
# (1413) 突然変異 4 本：使い捨ての木に 1 本ずつ当て、絞った試験（μ1・μ2・μ4）か statediff（μ3）で赤／違いを確かめる。出力 = $OUT
T=/d/PoB_work/wt_c151rb_mut; B=/d/PoB_work/wt_c151rb; OUT=/d/PoB_work/c151rb_work/mut; mkdir -p $OUT
H=$(dirname "$0")
for mu in mu1 mu2 mu4; do
  git -C $T checkout -q -- . 
  python $H/mut.py $mu $T > $OUT/$mu.log 2>&1
  (cd $T && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c151rb_mut_art --filter "FullyQualifiedName~RandomOnlyClimateEpidemicTests" >> $OUT/$mu.log 2>&1)
  echo "$mu: $(grep -E '失敗 PriceOfBread|成功!|失敗!|error CS' $OUT/$mu.log | cut -c1-200 | tr '\n' ' ')" >> $OUT/summary.txt
done
git -C $T checkout -q -- .
python $H/mut.py mu3 $T > $OUT/mu3.log 2>&1
bash $B/scripts/instruments/statediff/statediff.sh $B $T 1789 12 >> $OUT/mu3.log 2>&1
echo "mu3: $(tail -1 $OUT/mu3.log | cut -c1-250)" >> $OUT/summary.txt
git -C $T checkout -q -- .
