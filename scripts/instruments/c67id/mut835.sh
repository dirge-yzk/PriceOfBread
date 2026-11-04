#!/usr/bin/env bash
# (835) c67id 突然変異の駆動（mut828.sh の写し・mutate835.py の Mu7 Mu8）（使い捨ての木）：各突然変異で単位試験（CapitalCouponWriteDownTests・FiscalTests の規則 C）と、ARM に挙げた物は計器つき 1 種（種 1）を回し、gate828.py で門を読む。
#   使い方：bash mut828.sh <作業フォルダ D:/…> <元の木 D:/…> <単位の突然変異 例 "F1 Mu1 Mu2 Mu3 Mu5 Mu6"> <腕の突然変異 例 "F1 Mu1 Mu3 Mu5 Mu6">
#   出力：<作業フォルダ>/mutation_unit.txt・mutation_gate.txt。腕は batch.sh の枠（arms.sh）を使う＝1 本ずつ。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
W=${1:?作業フォルダ}; T=${2:?元の木}; UNIT=${3:?単位}; ARMS=${4:-}
S=$T/scripts/instruments
mkdir -p $W; : > $W/mutation_unit.txt; : > $W/mutation_gate.txt
[ "$UNIT" = none ] && UNIT=""   # 単位を回さない（腕だけ）
for mu in $UNIT; do
  SRC=$W/u_$mu; rm -rf $SRC && mkdir -p $SRC
  tar -cf - -C $T --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $SRC
  python -I $S/c67id/mutate835.py $mu $SRC >> $W/mutation_unit.txt 2>&1 || { echo "EDITFAIL $mu" >> $W/mutation_unit.txt; continue; }
  dotnet test $SRC/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $W/art_$mu --disable-build-servers \
    --filter "FullyQualifiedName~CapitalCouponWriteDownTests|FullyQualifiedName~FiscalTests.外資の配当と逃避" > $W/unit_$mu.log 2>&1
  echo "== $mu rc=$?" >> $W/mutation_unit.txt
  grep -E "^\s+失敗 |成功!|失敗!" $W/unit_$mu.log >> $W/mutation_unit.txt
  rm -rf $W/art_$mu $SRC
done
for mu in $ARMS; do
  export POB_WORK=$W
  bash $S/batch.sh prep m$mu dir:$T plain "$S/c67id/mutate835.py $mu" > $W/prep_$mu.log 2>&1 || { echo "PREPFAIL $mu" >> $W/mutation_gate.txt; continue; }
  INS=1 bash $S/batch.sh run m$mu 1 > $W/run_$mu.log 2>&1
  echo "== $mu rc=$?" >> $W/mutation_gate.txt
  python -I -X utf8 $S/c66dw/gate828.py $W/out_m${mu}_1/ins 1 >> $W/mutation_gate.txt 2>&1
  python -I -X utf8 $S/c66dw/paper_dw.py $W/out_m${mu}_1/ins 2>/dev/null | head -1 >> $W/mutation_gate.txt
  rm -rf $W/src_m$mu
done
echo MUTDONE >> $W/mutation_gate.txt
