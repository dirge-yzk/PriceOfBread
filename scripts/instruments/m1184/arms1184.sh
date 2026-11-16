#!/usr/bin/env bash
# (1184) 合流の腕の組（arm1184.sh を呼ぶ・m1178/arms1178.sh の型）
#   bash arms1184.sh <組 M|S11|S11z|At1|A0|p5|p10|N1|N2> <コミット>
#   挙動の線は c116hd（StockpilePanicWeight 0.5→0.0）だけ＝旗 0 は POB_COEF で StockpilePanicWeight=0.5。
#   アブレーション（★(1143)・t1 と在庫の目標を共有）：At1＝t1 だけ 0（TradeBuyBySupplyCurve=0・hd は 0.0 のまま）・A0＝t1 も hd も旧（両方 0）。
#   門の表 GT＝SMALL＋nationgood_s・armsfill_s・pop_s・warpair_s・tb_s・t1buy_s・cessp_s・sf_s・mintcap_s（hd と t1 の門と止める線の読み手が読む表）。
S=D:/PoB_work/wt_m1184/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
GT="$SMALL,nationgood_s,armsfill_s,pop_s,warpair_s,tb_s,t1buy_s,cessp_s,sf_s,mintcap_s"
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1184/arm1184.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1184="$A11,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s" bash $S/m1184/arm1184.sh mS11 1 10 11 1 - $H ;;
  S11z) TABLES1184="$GT" bash $S/m1184/arm1184.sh mS11z 1 10 11 1 "StockpilePanicWeight=0.5" $H ;;
  At1)  TABLES1184="$GT" bash $S/m1184/arm1184.sh mAt1 1 10 1 1 "TradeBuyBySupplyCurve=0" $H ;;
  A0)   TABLES1184="$GT" bash $S/m1184/arm1184.sh mA0 1 10 1 1 "TradeBuyBySupplyCurve=0;StockpilePanicWeight=0.5" $H ;;
  p5)   bash $S/m1184/arm1184.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1184/arm1184.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1184="$NUL" bash $S/m1184/arm1184.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
  N2)   TABLES1184="$NUL" bash $S/m1184/arm1184.sh mN2 1 10 1 0 SupplySlope=0.49999999 $H ;;
esac
