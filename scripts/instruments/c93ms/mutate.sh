#!/usr/bin/env bash
# (1007) 突然変異 1 本：現物の木を D:/PoB_work/c93ms_work/mut_<名> へ写し、InstrumentsMerch.cs を 1 か所だけ書き換えて MerchInstrumentTests を回す。
# 使い方：bash mutate.sh <名> '<元の文字列>' '<新しい文字列>'
set -u
NAME="$1"; A="$2"; B="$3"
SRC=${SRC:-/d/PoB_work/wt_c93ms}; MUTW=${MUTW:-/d/PoB_work/c93ms_work}; DST=$MUTW/mut_$NAME   # (1011) 元の木と作業フォルダを環境変数で替えられる
rm -rf "$DST"; mkdir -p "$DST"
(cd "$SRC" && tar cf - --exclude=.git --exclude=measurements --exclude=docs --exclude=bin --exclude=obj .) | (cd "$DST" && tar xf -)
F="$DST/PriceOfBread.SimCore/InstrumentsMerch.cs"
python - "$F" "$A" "$B" <<'P'
import sys
p,a,b=sys.argv[1:4]
d=open(p,'rb').read(); a=a.encode(); b=b.encode()
assert d.count(a)==1, d.count(a)
open(p,'wb').write(d.replace(a,b))
P
cd "$DST" && dotnet test PriceOfBread.Tests -c Release --artifacts-path $MUTW/mutart_$NAME --filter "FullyQualifiedName~MerchInstrumentTests" > "$MUTW/mut_$NAME.log" 2>&1
grep -E "^\s+Failed |Failed!|Passed!|error|失敗|合格:|成功!" "$MUTW/mut_$NAME.log" | head -20   # (1011) 日本語の出力も拾う・全文は mut_<名>.log
