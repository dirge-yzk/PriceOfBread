#!/usr/bin/env bash
# (1178) 合流の腕の組（arm1178.sh を呼ぶ・表の名簿をここに固定・m1171/arms1171.sh の型）
#   bash arms1178.sh <組 M|S11|S11z|At1|Ak1|Ahd|p5|p10|N1|N2> <コミット>
#   門の表 GT＝SMALL＋nationgood_s・armsfill_s・pop_s・warpair_s・tb_s・t1buy_s・secession_s・cessp_s・rulepower_s（t1・k1・hd の門と止める線の読み手が読む表）。
#   旗 0 は POB_COEF で置く（TradeBuyBySupplyCurve=0・SecessionInclusionOnce=0・StockpilePanicWeight=0.5）。
S=D:/PoB_work/wt_m1178/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
GT="$SMALL,nationgood_s,armsfill_s,pop_s,warpair_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s"
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,admrule_s,t1buy_s,pop_s"
Z0="TradeBuyBySupplyCurve=0;SecessionInclusionOnce=0;StockpilePanicWeight=0.5"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1178/arm1178.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1178="$A11,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s" bash $S/m1178/arm1178.sh mS11 1 10 11 1 - $H ;;
  S11z) TABLES1178="$GT" bash $S/m1178/arm1178.sh mS11z 1 10 11 1 "$Z0" $H ;;
  At1)  TABLES1178="$GT" bash $S/m1178/arm1178.sh mAt1 1 10 1 1 TradeBuyBySupplyCurve=0 $H ;;
  Ak1)  TABLES1178="$GT" bash $S/m1178/arm1178.sh mAk1 1 10 1 1 SecessionInclusionOnce=0 $H ;;
  Ahd)  TABLES1178="$GT" bash $S/m1178/arm1178.sh mAhd 1 10 1 1 StockpilePanicWeight=0.5 $H ;;
  p5)   bash $S/m1178/arm1178.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1178/arm1178.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1178="$NUL" bash $S/m1178/arm1178.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
  N2)   TABLES1178="$NUL" bash $S/m1178/arm1178.sh mN2 1 10 1 0 SupplySlope=0.49999999 $H ;;
esac
