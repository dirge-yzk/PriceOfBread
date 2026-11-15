#!/usr/bin/env bash
# (1171) 合流の腕の組（arm1171.sh を呼ぶ・表の名簿をここに固定）
#   bash arms1171.sh <組 M|S11|S11z|p5|p10|N1|N2> <コミット>
S=D:/PoB_work/wt_m1171/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1171/arm1171.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1171="$A11,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s" bash $S/m1171/arm1171.sh mS11 1 10 11 1 - $H ;;
  S11z) TABLES1171="cpop_s,nation_s,admrule_s,rule_s,soldiervac_s" bash $S/m1171/arm1171.sh mS11z 1 10 11 1 SoldierVacancyFromZero=0 $H ;;
  p5)   bash $S/m1171/arm1171.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1171/arm1171.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1171="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,admrule_s" bash $S/m1171/arm1171.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
  N2)   TABLES1171="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,admrule_s" bash $S/m1171/arm1171.sh mN2 1 10 1 0 SupplySlope=0.49999999 $H ;;
esac
