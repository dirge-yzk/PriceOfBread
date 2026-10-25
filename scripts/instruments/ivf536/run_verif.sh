#!/usr/bin/env bash
# ⚠墓標（(560) 合流・反証役 fb2〔bv〕低）：係数 ConstructionEraBundle は (555) fb2 で道ごと消した＝今の木でこの台本を回すと POB_COEF の〈存在しない係数名〉で止まる。当時の木（枝 ivf）で回す。
EXE=/d/PoB_work/ivf_536/art_new/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
OUT=/d/PoB_work/ivf_536/verif.txt
FLAGS="ConstructionEraBundle=1;RegionalPriceBaseExcludesColonies=1;MachineCastIronRecipe=1"
: > $OUT
echo "## savetest 1789 60 8 (旗 0)" >> $OUT
$EXE savetest 1789 60 8 >> $OUT 2>&1; echo "rc=$?" >> $OUT
echo "## savetest 1789 60 8 (旗 3 本)" >> $OUT
POB_COEF="$FLAGS" $EXE savetest 1789 60 8 >> $OUT 2>&1; echo "rc=$?" >> $OUT
echo "## leakhunt 1789 200 warm (旗 3 本)" >> $OUT
POB_COEF="$FLAGS" $EXE leakhunt 1789 200 warm >> $OUT 2>&1; echo "rc=$?" >> $OUT
echo VERIFDONE >> $OUT
