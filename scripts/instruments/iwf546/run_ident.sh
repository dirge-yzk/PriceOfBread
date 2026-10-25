#!/usr/bin/env bash
# 恒等の事故の検出（1種・200四半期・証明ではない）：基点の木・新しい木（旗 0・計器なし）・新しい木（旗 0・計器つき）の標準出力を cmp
export PATH="/c/Program Files/dotnet:$PATH"
W=/d/PoB_work/iwf_546
cd $W/base_src && dotnet build PriceOfBread.Runner/PriceOfBread.Runner.csproj -c Release --artifacts-path D:/PoB_work/iwf_546/art_base -v q > $W/ident_build_base.txt 2>&1
EB=$W/art_base/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
EN=$W/art_new/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.exe
unset POB_INSTRUMENTS POB_INSTRUMENTS_NATIONS POB_COEF
mkdir -p $W/ident
$EB selfplay 1 200 > $W/ident/base.txt 2>&1; echo "base done rc=$?" >> $W/ident/log.txt
$EN selfplay 1 200 > $W/ident/new_noins.txt 2>&1; echo "new_noins done rc=$?" >> $W/ident/log.txt
mkdir -p $W/ident/ins && POB_INSTRUMENTS=D:/PoB_work/iwf_546/ident/ins POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ,マラータ同盟,ハイデラバード,スペイン" $EN selfplay 1 200 > $W/ident/new_ins.txt 2>&1; echo "new_ins done rc=$?" >> $W/ident/log.txt
cmp $W/ident/base.txt $W/ident/new_noins.txt > $W/ident/cmp1.txt 2>&1; echo "cmp1 rc=$?" >> $W/ident/log.txt
cmp $W/ident/base.txt $W/ident/new_ins.txt > $W/ident/cmp2.txt 2>&1; echo "cmp2 rc=$?" >> $W/ident/log.txt
echo ALLDONE >> $W/ident/log.txt
