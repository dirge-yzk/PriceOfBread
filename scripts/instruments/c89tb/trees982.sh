#!/usr/bin/env bash
# (982) 直した試験を 3 つの木で回す（wt0＝c89tb 本体・wtH＝c87hd と合わせた木・wtD＝c82hd と合わせた木）。出力は <出力フォルダ>/<木の名>.log
OUT="${1:?出力フォルダ}"; mkdir -p "$OUT"
F="FullyQualifiedName~InstrumentsTests|FullyQualifiedName~RouteKmTests"
for t in wt_c89tb_c87hd wt_c89tb_c82hd; do
  dotnet test "D:/PoB_work/$t/PriceOfBread.Tests" -c Release --artifacts-path "D:/PoB_work/c89tb_art_$t" --filter "$F" > "$OUT/$t.log" 2>&1
  echo "$t rc=$?" >> "$OUT/rc.txt"
done
