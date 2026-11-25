#!/usr/bin/env bash
# (1358) 突然変異 M1〜M3：1 本ずつ SimCore の 1 行を壊し、隔離ビルドで TsregInstrumentTests（M3 は TradeInstrumentTests の T5 も）を回して赤を確かめ、直す。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c143sg_work/mut.mark bash scripts/instruments/c143sg/mutate.sh   （run_in_background で起こす）
cd D:/PoB_work/wt_c143sg_mut || exit 2   # 使い捨ての写し（git worktree add D:/PoB_work/wt_c143sg_mut --detach <c143sg の HEAD>）＝ident の木に触れない
F=PriceOfBread.SimCore/Instruments/InstrumentsTradeskip.cs
OUT=D:/PoB_work/c143sg_work/mut; mkdir -p $OUT
for M in M0 M1 M2 M3; do
  git checkout -q -- $F
  python -X utf8 -I - "$M" <<'PY'
import sys
m=sys.argv[1]; p='PriceOfBread.SimCore/Instruments/InstrumentsTradeskip.cs'
b=open(p,'rb').read().decode('utf8')
def rep(o,n):
    global b
    assert b.count(o)==1,(o,b.count(o)); b=b.replace(o,n)
if m=='M1': rep("{ Add(curBn, curG, TsCol.XCap); R(RgCol.XCap);","{ Add(curBn, curG, TsCol.XCap); if (regIx.TryGetValue((tr[sn].r, curG), out var mrr)) mrr.C[(int)RgCol.XCap]++;")
if m=='M2': rep("R(which == TsOutIdle ? RgCol.Idle : which == TsOutNoNeed ? RgCol.NoNeed : RgCol.Budget0); }","if (which != TsOutIdle) R(which == TsOutNoNeed ? RgCol.NoNeed : RgCol.Budget0); }")
if m=='M3': rep("if (!On || !first || nations.Count == 0) return null;","if (!first || nations.Count == 0) return null;")
open(p,'wb').write(b.encode('utf8'))
PY
  dotnet build PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c143sg_mutart -v q > $OUT/$M.build.txt 2>&1
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c143sg_mutart --no-build --filter "FullyQualifiedName~TsregInstrumentTests|FullyQualifiedName~TradeInstrumentTests.T5" > $OUT/$M.test.txt 2>&1
  echo "$M rc=$? $(grep -E '^(失敗|成功|Failed|Passed)!' $OUT/$M.test.txt | head -1)" >> $OUT/summary.txt
done
git checkout -q -- $F
