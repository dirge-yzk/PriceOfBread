#!/usr/bin/env bash
# (1321) 合流（波137・m1308/arms1308.sh の写し）の腕の組（arm1321.sh を呼ぶ）。表の組は (1308) と同じに、波137 の門と記録の表（W137）を足した。
#   W137＝sectreas_s（ak の門 G1）・subst_s（s3 の器）・popclose_s（sm の列）。
#   W135＝capcol_s（nc の門 G1・G2）・recipeout_s（k47 の門と史実の桁）・landshare_s（nc の記録）・treasledger_s（tl の器）。
#   bash arms1321.sh <組 M|S11|p5|N1|N2> <コミット>。M は全表（採れば新しい INSRAW＝keep_1321/base_ins）。
S=D:/PoB_work/wt_m1321/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d '\r'); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '\r')
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
BK="paysale2_s,capskip_s,port_s,sellr_s"
W127="fiscal_s,silverst_s,formation2_s,unifwar_s,tensionwhy_s"
W129="clock_s,factory_s,engineer_s,disc_s,devcap_s"
W130="popclose_s"
W132="urxpd_s,urxrt_s,arrears_s,labeltier_s"
W135="capcol_s,recipeout_s,landshare_s,treasledger_s"
W137="sectreas_s,subst_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1321/arm1321.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1321="$A11,$CA,$BK,$W127,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s,$W129,$W130,$W132,$W135,$W137" bash $S/m1321/arm1321.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1321/arm1321.sh mp5 0 5 1 1 - $H ;;
  N1)   TABLES1321="$NUL,$CA,$BK,$W127,$W129,$W130,$W132,$W135,$W137" bash $S/m1321/arm1321.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
  N2)   TABLES1321="$NUL,$CA,$BK,$W127,$W129,$W130,$W132,$W135,$W137" bash $S/m1321/arm1321.sh mN2 1 10 1 0 SupplySlope=0.49999999 $H ;;
esac
