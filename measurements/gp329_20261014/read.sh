#!/usr/bin/env bash
export PATH="/c/Program Files/dotnet:/usr/bin:$PATH" PYTHONUTF8=1
cd /d/PoB_work/wt_gp
W=/d/PoB_work/gp_329
for a in gpA gpB gpN1 gpN2; do
  bash scripts/instruments/ng.sh $W/out_${a}_10/ins $a D:/PoB_work/gp_329/ng "rd:measurements/ng314_20261014:r314" > $W/ng_$a.log 2>&1
  python scripts/instruments/hol/an_h2.py $W/out_${a}_10/ins -v4 > $W/hol_$a.txt 2>&1
done
python scripts/instruments/gp/an_gp.py $W/out_gpA_10/ins $W/out_gpB_10/ins $W/out_gpN1_10/ins $W/out_gpN2_10/ins > $W/an_gp.txt 2>&1
echo READDONE
