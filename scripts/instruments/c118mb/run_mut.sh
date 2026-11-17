#!/usr/bin/env bash
# (1187) c118mb 突然変異を使い捨ての木で 1 本ずつ（試験の束・E1/E2 の短い走り〔1 種×40 四半期・mintcap_s だけ〕）。
#   bash run_mut.sh <使い捨ての木（D:/…）> <作業フォルダ（D:/…）> <名...>
X=$1; W=$2; shift 2; S=$(cd "$(dirname "$0")" && pwd -W)
export TEMP=$W/tmp TMP=$W/tmp AVALONIA_TELEMETRY_OPTOUT=1 PYTHONUTF8=1
O=$W/mut; mkdir -p $O
for N in "$@"; do
  (cd $X && git checkout -q -- . ) || exit 1
  python -I $S/mut.py $X $N > $O/$N.apply.txt 2>&1 || { echo "$N APPLYFAIL" >> $O/summary.txt; continue; }
  (cd $X && dotnet build PriceOfBread.Tests -c Release --artifacts-path $W/art_mx -v q -nologo > $O/$N.build.txt 2>&1) || { echo "$N BUILDFAIL" >> $O/summary.txt; continue; }
  (cd $X && dotnet test PriceOfBread.Tests -c Release --artifacts-path $W/art_mx --no-build --filter "FullyQualifiedName~MintcapInstrumentTests|FullyQualifiedName~MintAtMarketTests|FullyQualifiedName~MintregInstrumentTests" > $O/$N.test.txt 2>&1)
  echo "$N 試験 $(grep -E '合計:' $O/$N.test.txt | tail -1 | tr -s ' ') 赤=[$(grep -E '^\s+失敗 ' $O/$N.test.txt | sed 's/.*Tests\.//; s/ \[.*//' | tr '\n' ' ')]" >> $O/summary.txt
  case " M2 M3 M5 " in *" $N "*)
    rm -rf $O/raw_$N $W/snap_mx; mkdir -p $O/raw_$N
    (cd $X && SEEDS=1 SNAP_DIR=$W/snap_mx POB_INSTRUMENTS=$O/raw_$N POB_INSTRUMENTS_TABLES=mintcap_s POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" bash measure.sh $O/b1_$N.txt selfplay-batch 1 - 0 1 40 > $O/$N.run.txt 2>&1)
    python -X utf8 -I $S/mb.py paper $O/raw_$N 1 1 $O/$N.json > $O/$N.paper.txt 2>&1
    echo "$N 短い走り（1 種×40 四半期）E1=$(grep '^E1 ' $O/$N.paper.txt | cut -d' ' -f2) E1rows=$(grep '^E1rows ' $O/$N.paper.txt | cut -d' ' -f2) E2=$(grep '^E2 ' $O/$N.paper.txt | cut -d' ' -f2) rows=$(grep '^rows ' $O/$N.paper.txt | cut -d' ' -f2)" >> $O/summary.txt ;;
  esac
done
(cd $X && git checkout -q -- . )
echo MUTDONE
