#!/usr/bin/env bash
# (1226) 合流の腕の組（m1198/arms1198.sh の写し・arm1226.sh を呼ぶ）
#   bash arms1226.sh <組 M|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝(1191) ④ の推奨を引き継ぐ）
#   M は表を絞る（木 T は世界を変えない線 hs だけを足した＝全表の生は腕 Z の ident の生を keep_1226/base_ins にする・M の生は共通の表で腕 Z の生とバイト一致を確かめる）。
#   S11・N1 は m1198 と同じ表＋ca が足した satneeds_s・warpolity_s・dep_s（keep_1198/a11・n1 と共通の表で突き合わせる）。
S=D:/PoB_work/wt_m1226/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d ''); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '')
GATE="fiscal_s,landsale_s,migration_s,nation_s,pxmonth_s,region_s,scalars_s,secession_s,stock_s,wars_s,mintcap_s,rulepower_s,ideology_s,casualty_s"
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
CA="satneeds_s,warpolity_s,dep_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1226/arm1226.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1226="$A11,$CA,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s" bash $S/m1226/arm1226.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1226/arm1226.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1226/arm1226.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1226="$NUL,$CA" bash $S/m1226/arm1226.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
