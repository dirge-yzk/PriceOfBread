#!/usr/bin/env bash
# 越境の種の直し（㊴）を使い捨ての木で旗 1 と旗 0（係数 1.3）の両方で回す。
WK=D:/PoB_work/c152ms_work; T=D:/PoB_work/wt_c152ms_mut; export TEMP=$WK/tmp TMP=$WK/tmp
cd $T || exit 1
git checkout -q -- .
cp D:/PoB_work/wt_c152ms/PriceOfBread.Tests/InstrumentsTests.cs PriceOfBread.Tests/InstrumentsTests.cs
F="FullyQualifiedName~InstrumentsTests.州の人口と移動の表が揃い流れの出と入が合う"
for M in F1 F0; do
  if [ $M = F0 ]; then python -X utf8 -I -c "
p='PriceOfBread.SimCore/Coefficients.p08_audit_soldiers.cs';t=open(p,'rb').read();a=b'public double MilitarismStrengthBonus = 1.0; ';assert t.count(a)==1;open(p,'wb').write(t.replace(a,b'public double MilitarismStrengthBonus = 1.3; '))" || exit 2; fi
  dotnet build PriceOfBread.Tests -c Release --artifacts-path $WK/art_mut -v q -nologo > $WK/sbuild_$M.txt 2>&1 || { echo "$M BUILDFAIL"; continue; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path $WK/art_mut --no-build --filter "$F" --logger "console;verbosity=normal" --logger "trx;LogFileName=seed_$M.trx" --results-directory $WK/trx > $WK/stest_$M.txt 2>&1
  echo "$M rc=$? $(grep -E '合計:|成功:|失敗:' $WK/stest_$M.txt | tr '\n' ' ')"
done
git checkout -q -- .
