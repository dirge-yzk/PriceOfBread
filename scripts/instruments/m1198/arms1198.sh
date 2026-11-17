#!/usr/bin/env bash
# (1198) 合流の腕の組（m1191/arms1191.sh の型・arm1198.sh を呼ぶ）
#   bash arms1198.sh <組 M|Mg|Zg|S11|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝(1191) ④ の推奨を引き継ぐ）
#   Mg・Zg＝D: が〈35GB＋全表〉に届かないときの絞った腕（門と止める線の読み手が開く表＋mintcap_s・rulepower_s・ideology_s・casualty_s）。
#   Zg は腕 Z の木（kf＋nd 段 A＋us）の計器つき 10 種を同じ表で回し、keep_1191/base_ins と表ごとの sha1 で突き合わせる（ident の計器つきの代わりではない＝測った表で一致）。
S=D:/PoB_work/wt_m1198/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d ''); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '')
GATE="fiscal_s,landsale_s,migration_s,nation_s,pxmonth_s,region_s,scalars_s,secession_s,stock_s,wars_s,mintcap_s,rulepower_s,ideology_s,casualty_s"
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1198/arm1198.sh mM 1 10 1 1 - $H ;;
  Mg)   TABLES1198="$GATE" bash $S/m1198/arm1198.sh mMg 1 10 1 1 - $H ;;
  Zg)   TABLES1198="$GATE" bash $S/m1198/arm1198.sh mZg 1 10 1 1 - $H ;;
  S11)  TABLES1198="$A11,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s" bash $S/m1198/arm1198.sh mS11 1 10 11 1 - $H ;;
  p5)   bash $S/m1198/arm1198.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1198/arm1198.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1198="$NUL" bash $S/m1198/arm1198.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
