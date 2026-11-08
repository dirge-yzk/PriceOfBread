#!/usr/bin/env bash
# (977) c88sm 走査の番人（㉘）→ フルスイート（FullLength 以外）→ FullLength → 一覧の数 → savetest・leakhunt。出力は D:/PoB_work/c88sm_work/suite/
W=D:/PoB_work/c88sm_work/suite; ART=D:/PoB_work/c88sm_art; mkdir -p $W; cd D:/PoB_work/wt_c88sm || exit 1
T="dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART"
$T --filter "FullyQualifiedName~IndustrializationReaderTests|FullyQualifiedName~UnitTypeTests|FullyQualifiedName~InstrumentsTests|FullyQualifiedName~WriterRatchetTests|FullyQualifiedName~DormantIdentityTests" > $W/scan.log 2>&1
$T --filter "Category!=FullLength" > $W/nonfull.log 2>&1
$T --filter "Category=FullLength" > $W/full.log 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART --no-build --list-tests --filter "Category!=FullLength" > $W/list_nonfull.log 2>&1
dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART --no-build --list-tests --filter "Category=FullLength" > $W/list_full.log 2>&1
POB_WORK=$W SAVELEAK_SRC=D:/PoB_work/wt_c88sm bash scripts/instruments/saveleak.sh
