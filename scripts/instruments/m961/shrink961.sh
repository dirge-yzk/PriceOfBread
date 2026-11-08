#!/usr/bin/env bash
# (961) 合流：外した木の腕（合わせた木 hs＋kp＋sl＋lt の M・N1・N2・S11）と外した線 c85lt の腕 B・S11 の生を keep_961_rej へ縮めて移す（shrink892.py を 1 字も変えずに呼ぶ・shrink905.sh の型）。
#   残す表＝門（kp の paper952・lt の gate958・tab937・paper951）と止める線（stoplines906・949・an_h2・gdp_zero10・home_break）の読み手が開く表。全ての表の要約は keepsum_<腕>_all.tsv。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
S=D:/PoB_work/wt_m961/scripts/instruments
R=D:/PoB_work/keep_961_rej; W=D:/PoB_work/m961_work
KEEP=nation_s,pxmonth_s,tnation_s,cessp_s,migration_s,region_s,cederule_s,rule_s,admrule_s,secession_s,landfix_s,stock_s,inheritintent_s,wars_s
mkdir -p $R
for a in "combo_mM_10 $W/out_mM_10/ins" "combo_n1_10 $W/out_n1_10/ins" "combo_n2_10 $W/out_n2_10/ins" "combo_mS_10_s11 $W/out_mS_10_s11/ins" "c85ltB_10 D:/PoB_work/c85lt_work/arm_c85ltB/out_c85ltB_10/ins" "c85ltS_10_s11 D:/PoB_work/c85lt_work/arm_c85ltS/out_c85ltS_10_s11/ins"; do
  set -- $a
  echo "== $1 $2"
  python -X utf8 -I $S/m892/shrink892.py $2 $R/$1 $W/keepsum_$1_all.tsv $KEEP || exit 1
done
du -sh $R
echo SHRINKDONE
