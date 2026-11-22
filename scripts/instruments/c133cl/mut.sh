#!/usr/bin/env bash
# 使い方: mut.sh MA1  （wt_c133cl_mut の c133cl の木へ突然変異を当て、隔離ビルドして該当の試験を走らせ、結果を 1 行で出す）
M=$1; WT=/d/PoB_work/wt_c133cl_mut; D=/d/PoB_work/c133cl_work/mut; PD=D:/PoB_work/wt_c133cl/scripts/instruments/c133cl/mut; PW=D:/PoB_work/wt_c133cl_mut
mkdir -p $D
cd $WT && git checkout -q -- . && git reset -q --hard c133cl 2>/dev/null
FILE=$(python -X utf8 -c "ns={};exec(open('$PD/$M.py','rb').read().decode('utf-8'),ns);print(ns['FILE'])")
FILTER=$(python -X utf8 -c "ns={};exec(open('$PD/$M.py','rb').read().decode('utf-8'),ns);print(ns['FILTER'])")
python -X utf8 D:/PoB_work/wt_c133cl/scripts/instruments/c133cl/rep.py $PW/$FILE $PD/$M.py || { echo "$M: 変異が当たらない"; exit 3; }
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c133cl_mutart -v q > $D/$M.build.txt 2>&1 || { echo "$M: ビルド失敗"; tail -5 $D/$M.build.txt; git checkout -q -- .; exit 4; }
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c133cl_mutart --no-build --filter "FullyQualifiedName~$FILTER" --logger "console;verbosity=normal" > $D/$M.test.txt 2>&1
echo "$M: $(grep -E '^(テストの合計|     成功|     失敗)' $D/$M.test.txt | tr '\n' ' ') :: $(grep -E '^  失敗 ' $D/$M.test.txt | head -3 | cut -c1-110 | tr '\n' ';')"
git checkout -q -- .
