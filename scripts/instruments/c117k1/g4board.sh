#!/usr/bin/env bash
# (1179) c117k1 追記 2 の手順：G4 の盤面（四半期の数）の選び直し。使い捨ての木（試験だけ環境変数 K1G4Q で四半期の数を読む写し）で回す。
#   bash g4board.sh <使い捨ての木> <作業フォルダ（D:/…）> <候補の数...>   出力 <作業フォルダ>/g4_board.txt
T=${1:?木}; WK=${2:?作業}; shift 2; mkdir -p $WK/tmp; export TEMP=$WK/tmp TMP=$WK/tmp
cd "$T"; A=$WK/art; O=$WK/g4_board.txt
echo "# (1179) G4 の盤面の選び直し 木=$T HEAD=$(git rev-parse --short HEAD) 差分 $(git diff --stat | tail -1) $(date '+%F %T')" > $O
dotnet build PriceOfBread.Tests -c Release --artifacts-path $A -v q -nologo > $WK/build.txt 2>&1 || { echo BUILDFAIL >> $O; exit 1; }
for q in "$@"; do
  for f in 0 1; do for s in "" "SupplySlope=0.50000001" "SupplySlope=0.49999999"; do
    co="SecessionInclusionOnce=$f"; [ -n "$s" ] && co="$co;$s"
    rm -f $WK/m.txt
    K1G4Q=$q K1G4OUT=$WK/m.txt POB_COEF="$co" dotnet test PriceOfBread.Tests -c Release --no-build --artifacts-path $A --filter "FullyQualifiedName~TradeInstrumentTests.G4" > $WK/r.txt 2>&1; rc=$?
    echo "q=$q $co rc=$rc $(cat $WK/m.txt 2>/dev/null | tr '\n' ' ')" >> $O
  done; done
done
echo G4DONE >> $O
