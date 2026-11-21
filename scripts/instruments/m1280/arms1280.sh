#!/usr/bin/env bash
# (1280) 合流（波131）の腕の組（m1274/arms1274.sh の写し・arm1280.sh を呼ぶ）。表は (1274) と同じ（pf の門と記録の capskip_s・port_s・sellr_s は BK・cr の門の region_s・secession_s・nation_s は A11/NUL に入っている）。
#   bash arms1274.sh <組 M|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝★(1191)）
#   M は全表（採れば新しい INSRAW＝keep_1274/base_ins）。
S=D:/PoB_work/wt_m1280/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
BK="paysale2_s,capskip_s,port_s,sellr_s"
W127="fiscal_s,silverst_s,formation2_s,unifwar_s,tensionwhy_s"
W129="clock_s,factory_s,engineer_s,disc_s,devcap_s"
W130="popclose_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1280/arm1280.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1280="$A11,$CA,$BK,$W127,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s,$W129,$W130" bash $S/m1280/arm1280.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1280/arm1280.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1280/arm1280.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1280="$NUL,$CA,$BK,$W127,$W129,$W130" bash $S/m1280/arm1280.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
