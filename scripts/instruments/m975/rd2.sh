cd D:/PoB_work/wt_m975 || exit 9
W=D:/PoB_work/m975_work
bash scripts/instruments/m975/read975.sh n2 $W/out_n2_10 $W/b10_n2_10.txt 1 '' nogate
bash scripts/instruments/m975/read975.sh mS $W/out_mS_10_s11 $W/b10_mS_10_s11.txt 11 newnulls
R=$W/read/mM; RULE=pair NULLS="$W/read/n1/stop906.txt $W/read/n2/stop906.txt" bash scripts/instruments/c84gz/stoplines949.sh $W/out_mM_10/ins $R/stop949_newnulls.txt 1 > $R/stop949_newnulls.log 2>&1; echo "mM stop949new rc=$?"
echo RD2DONE
