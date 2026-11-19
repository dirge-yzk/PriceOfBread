#!/usr/bin/env bash
# (1233) 合流の腕の組（m1226/arms1226.sh の写し・arm1233.sh を呼ぶ）
#   bash arms1233.sh <組 M|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝★(1191)）
#   M は全表（新しい INSRAW＝keep_1233/base_ins）。S11・N1 は m1226 と同じ表＋bk の paysale_s（G1・G2 と赤字の月を種 11〜20 でも読む）。
S=D:/PoB_work/wt_m1233/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d ''); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
BK="paysale_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1233/arm1233.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1233="$A11,$CA,$BK,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s" bash $S/m1233/arm1233.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1233/arm1233.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1233/arm1233.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1233="$NUL,$CA,$BK" bash $S/m1233/arm1233.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
