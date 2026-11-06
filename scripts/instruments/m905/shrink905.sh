#!/usr/bin/env bash
# (905) 合流：採らなかった腕（c77ce・c77cw の腕 B と S11）の生を keep_905_rej へ縮めて移す（shrink892.py を 1 字も変えずに呼ぶ）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments
R=D:/PoB_work/keep_905_rej; W=D:/PoB_work/m905
KEEP=nation_s,pxmonth_s,tnation_s,cessp_s,migration_s,region_s,cederule_s,rule_s,admrule_s,wars_s,cwmult_s
mkdir -p $R
for a in "c77ceB_10 D:/PoB_work/c77ce_work/bw/out_c77ceB_10/ins" "c77ceS_10_s11 D:/PoB_work/c77ce_work/bw/out_c77ceS_10_s11/ins" "c77cwB_10 D:/PoB_work/c77cw_work/armB/out_c77cwB_10/ins" "c77cwS_10_s11 D:/PoB_work/c77cw_work/armS/out_c77cwS_10_s11/ins"; do
  set -- $a
  echo "== $1 $2"
  python -X utf8 -I $S/m892/shrink892.py $2 $R/$1 $W/keepsum_$1_all.tsv $KEEP || exit 1
done
cp -r D:/PoB_work/c77cw_work/flag0_cwmult $R/c77cw_flag0_cwmult
du -sh $R
