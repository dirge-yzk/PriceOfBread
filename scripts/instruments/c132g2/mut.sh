#!/usr/bin/env bash
# 使い方: mut.sh M1  （wt_c132g2_mut で突然変異を当て、隔離ビルドして該当の試験を走らせ、結果を 1 行で出す）
M=$1; WT=/d/PoB_work/wt_c132g2_mut; D=/d/PoB_work/c132g2_work/mut; PD=D:/PoB_work/c132g2_work/mut; PW=D:/PoB_work/wt_c132g2_mut
cd $WT && git checkout -q -- . && git reset -q --hard c132g2 2>/dev/null
FILE=$(python -X utf8 -c "ns={};exec(open('$PD/$M.py','rb').read().decode('utf-8'),ns);print(ns['FILE'])")
FILTER=$(python -X utf8 -c "ns={};exec(open('$PD/$M.py','rb').read().decode('utf-8'),ns);print(ns['FILTER'])")
python -X utf8 D:/PoB_work/c132g2_work/rep.py $PW/$FILE $PD/$M.py || { echo "$M: 変異が当たらない"; exit 3; }
dotnet build PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c132g2_mutart -v q > $D/$M.build.txt 2>&1 || { echo "$M: ビルド失敗"; tail -5 $D/$M.build.txt; git checkout -q -- .; exit 4; }
F=$(echo "$FILTER" | sed 's/|/|FullyQualifiedName~/g')
dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c132g2_mutart --no-build --filter "FullyQualifiedName~$F" > $D/$M.test.txt 2>&1
echo "$M: $(grep -E '^(失敗!|成功!|Failed!|Passed!)' $D/$M.test.txt | head -1) :: $(grep -E '^  失敗 ' $D/$M.test.txt | head -3 | cut -c1-110 | tr '\n' ';')"
git checkout -q -- .
