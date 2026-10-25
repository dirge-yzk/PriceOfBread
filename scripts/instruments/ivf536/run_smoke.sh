#!/usr/bin/env bash
# ⚠墓標（(560) 合流・反証役 fb2〔bv〕低）：係数 ConstructionEraBundle は (555) fb2 で道ごと消した＝今の木でこの台本を回すと POB_COEF の〈存在しない係数名〉で止まる。当時の木（枝 ivf）で回す。
# 段0 の煙試験（種 1・40 四半期・腕の外）＝ivf_s を読む。旗 0／3 本とも 1／助走 40・80（旗 0）
EXE=/d/PoB_work/ivf_536/art_new/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
OUT=/d/PoB_work/ivf_536/smoke
NAT="イギリス,フランス,プロイセン,アメリカ,清,マラータ同盟,ロシア,日本,オスマン,オーストリア"
run() { # <名前> <POB_COEF> <seed> <quarters>
  mkdir -p $OUT/$1
  POB_INSTRUMENTS=$OUT/$1 POB_INSTRUMENTS_NATIONS="$NAT" POB_COEF="$2" $EXE selfplay $3 $4 > $OUT/$1.txt 2>&1
  echo "done $1 rc=$?" >> $OUT/log.txt
}
run f0_s1 "" 1 40
run f1_s1 "ConstructionEraBundle=1;RegionalPriceBaseExcludesColonies=1;MachineCastIronRecipe=1" 1 40
run w40_s1 "StartWarmupQuarters=40" 1 12
run w80_s1 "StartWarmupQuarters=80" 1 12
echo ALLDONE >> $OUT/log.txt
