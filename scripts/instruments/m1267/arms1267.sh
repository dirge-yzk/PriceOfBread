#!/usr/bin/env bash
# (1267) 合流の腕の組（m1260/arms1260.sh の写し・arm1267.sh を呼ぶ）。S11 と N1 に al の門と記録の読み手の表 clock_s・factory_s・engineer_s・disc_s と df の申し送り devcap_s を足した（(1260)・(1265) の申し送り）。
#   bash arms1267.sh <組 M|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝★(1191)）
#   M は全表（採れば新しい INSRAW＝keep_1267/base_ins）。
S=D:/PoB_work/wt_m1267/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
BK="paysale2_s,capskip_s,port_s,sellr_s"
W127="fiscal_s,silverst_s,formation2_s,unifwar_s,tensionwhy_s"
W129="clock_s,factory_s,engineer_s,disc_s,devcap_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1267/arm1267.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1267="$A11,$CA,$BK,$W127,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s,$W129" bash $S/m1267/arm1267.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1267/arm1267.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1267/arm1267.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1267="$NUL,$CA,$BK,$W127,$W129" bash $S/m1267/arm1267.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
