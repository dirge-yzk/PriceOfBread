#!/usr/bin/env bash
# (1247) 合流の腕の組（m1233/arms1233.sh の写し・arm1247.sh を呼ぶ）
#   bash arms1247.sh <組 M|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝★(1191)）
#   M は全表（新しい INSRAW＝keep_1247/base_ins）。S11・N1 は m1233 と同じ表＋rk の paysale2_s（paysale_s の改名）＋pf の capskip_s・port_s・sellr_s（H3〜H5）。旧の注：S11・N1 は m1226 と同じ表＋bk の paysale_s（G1・G2 と赤字の月を種 11〜20 でも読む）。
S=D:/PoB_work/wt_m1247/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d ''); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
BK="paysale2_s,capskip_s,port_s,sellr_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1247/arm1247.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1247="$A11,$CA,$BK,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s" bash $S/m1247/arm1247.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1247/arm1247.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1247/arm1247.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1247="$NUL,$CA,$BK" bash $S/m1247/arm1247.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
