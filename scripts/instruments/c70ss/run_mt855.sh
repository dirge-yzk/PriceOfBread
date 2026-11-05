#!/usr/bin/env bash
# (855) c70ss（(849) run_mt.sh の写し・木と作業フォルダだけ c70ss）＝元は (849) 突然変異 M1〜M6 を使い捨ての写しの木の機構の試験で確かめる（D: の写し・リポジトリは書かない）
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
T=/d/PoB_work/wt_c70ss; M=/d/PoB_work/c70ss_work/mt; S=$M/src
rm -rf $S && mkdir -p $S
tar -cf - -C $T --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot --exclude=.vs . | tar -xf - -C $S
cp $S/PriceOfBread.SimCore/EconomyStep.cs $M/EconomyStep.orig.cs
F="FullyQualifiedName~SubsistenceStage2Tests|FullyQualifiedName~SubstInstrumentTests|FullyQualifiedName~UrbanFlowRealTests|FullyQualifiedName~FoodLedgerTests"
for m in M0 M1 M2 M3 M4 M5 M6; do
  cp $M/EconomyStep.orig.cs $S/PriceOfBread.SimCore/EconomyStep.cs
  [ $m = M0 ] || python $T/scripts/instruments/c69ss/mut849.py $m $S > $M/$m.mut.txt 2>&1 || { echo "MUTFAIL $m" > $M/$m.test.txt; continue; }
  dotnet build $S/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $M/art --disable-build-servers > $M/$m.build.txt 2>&1 || { echo "BUILDFAIL $m" > $M/$m.test.txt; continue; }
  dotnet test $S/PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --no-build --artifacts-path $M/art --filter "$F" > $M/$m.test.txt 2>&1
done
cp $M/EconomyStep.orig.cs $S/PriceOfBread.SimCore/EconomyStep.cs
