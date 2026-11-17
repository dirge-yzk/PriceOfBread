#!/usr/bin/env bash
# (1191) 合流（m1184/arms1184.sh の写し・木と作業フォルダを wt_m1191・m1191_work へ）
# (1184) 合流の腕の組（arm1184.sh を呼ぶ・m1178/arms1178.sh の型）
#   bash arms1191.sh <組 M|S11|Asec|Amob|Arev|Aur|p5|p10|N1> <コミット>（ヌル腕は N1 の 1 本＝予言 §2）
S=D:/PoB_work/wt_m1191/scripts/instruments; CF=$S/ident/ident.cfg
SMALL=$(grep '^SMALL=' $CF | cut -d= -f2 | tr -d ''); BYPASS=$(grep '^BYPASS=' $CF | cut -d= -f2 | tr -d '')
# (1191) 挙動の線は c118nc（係数 3 本を 0 へ）と c118ur（係数 2 本）。アブレーション（記録）は nc の係数ごと＝止める線と nc・ur の門の読み手が読む表だけ（SMALL＋secession_s・rule_s・cessp_s・rponset_s）。
GA="$SMALL,secession_s,rule_s,cessp_s,rponset_s"
A11="$SMALL,$BYPASS,railspend_s,trrail_s,trnation_s,tport_s,dipcool_s,warband_s,warpair_s,disccontact_s"
NUL="$A11,rponset_s,rule_s,secession_s,cessp_s,nationgood_s,tb_s,cederule_s,armsfill_s,soldiervac_s,mintreg_s,mintcap_s,admrule_s,t1buy_s,pop_s,sf_s"
G=${1:?組}; H=${2:?コミット}
case $G in
  M)    bash $S/m1191/arm1191.sh mM 1 10 1 1 - $H ;;
  S11)  TABLES1191="$A11,cpop_s,admrule_s,rule_s,soldiervac_s,mintreg_s,mintcap_s,nationgood_s,armsfill_s,pop_s,tb_s,t1buy_s,secession_s,cessp_s,rulepower_s,sf_s,rponset_s" bash $S/m1191/arm1191.sh mS11 1 10 11 1 - $H ;;
  Asec) TABLES1191="$GA" bash $S/m1191/arm1191.sh mAsec 1 10 1 1 "SecessionConsciousnessWeight=1" $H ;;
  Amob) TABLES1191="$GA" bash $S/m1191/arm1191.sh mAmob 1 10 1 1 "NationBuildMobilizationWeight=0.5" $H ;;
  Arev) TABLES1191="$GA" bash $S/m1191/arm1191.sh mArev 1 10 1 1 "RevolutionConsciousness=0.25" $H ;;
  Aur)  TABLES1191="$GA" bash $S/m1191/arm1191.sh mAur 1 10 1 1 "UnificationAiPursueChance=0.7;UnificationAiWarGapYears=3" $H ;;
  p5)   bash $S/m1191/arm1191.sh mp5 0 5 1 1 - $H ;;
  p10)  bash $S/m1191/arm1191.sh mp10 0 10 1 1 - $H ;;
  N1)   TABLES1191="$NUL" bash $S/m1191/arm1191.sh mN1 1 10 1 0 SupplySlope=0.50000001 $H ;;
esac
