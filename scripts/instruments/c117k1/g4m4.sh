WK=D:/PoB_work/c117k1_work/g4m4; mkdir -p $WK/tmp; export TEMP=$WK/tmp TMP=$WK/tmp; A=$WK/art; O=$WK/m4.txt
cd D:/PoB_work/wt_c117k1_g4
echo "# (1179) M4（ImpQ += value）を盤面 36 と 24 で 差分 $(git diff --stat | tail -1)" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build.txt 2>&1 || { echo BUILDFAIL >> $O; exit 1; }
for q in 36 24; do for f in 0 1; do
  rm -f $WK/m.txt
  K1G4Q=$q K1G4OUT=$WK/m.txt POB_COEF="SecessionInclusionOnce=$f" dotnet test PriceOfBread.Tests -c Release --no-build --artifacts-path $A --filter "FullyQualifiedName~TradeInstrumentTests.G4" > $WK/r_$q_$f.txt 2>&1; rc=$?
  echo "M4 q=$q f=$f rc=$rc $(cat $WK/m.txt 2>/dev/null | tr '\n' ' ') $(grep -m1 -A1 'エラー メッセージ' $WK/r_$q_$f.txt | tail -1 | cut -c1-120)" >> $O
done; done
echo M4DONE >> $O
