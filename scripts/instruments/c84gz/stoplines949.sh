#!/usr/bin/env bash
# (949) c84gz：stoplines906.sh の写し＋末尾に〈それ以外〉の判定の 1 節（judge949.py）と記録の節（下がり）を足した。stoplines906.sh は書き換えず、そのまま呼ぶ（本体の出力はバイトで同じ）。
#   使い方: [RULE=pair|strict] [NULLS="<n1 の stop 出力> <n2 の stop 出力>"] bash stoplines949.sh <ins フォルダ（D:/ の形）> <出力ファイル> [種の起点 1|11]
#   既定の RULE は pair・NULLS は m947 のヌル腕の出力（measurements/m947_20261108/null/）。新しい基線を作ったら NULLS をその基線のヌル腕の出力に替える。終了コード＝判定の rc（0 記録・1 止める・2 入力の誤り）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd)"
M=${1:?ins}; OUT=${2:?出力}; SB=${3:-1}
RULE=${RULE:-pair}
NULLS=${NULLS:-"$R/measurements/m947_20261108/null/stop906_n1.txt $R/measurements/m947_20261108/null/stop906_n2.txt"}
bash "$R/scripts/instruments/c78gz/stoplines906.sh" "$M" "$OUT" "$SB" > /dev/null || exit 4
NA=""; for n in $NULLS; do NA="$NA --null $n"; done
{
echo "== 判定（949）：100 倍の〈それ以外〉（rule=$RULE）"
python -X utf8 -I "$D0/judge949.py" --rule "$RULE" --arm "$OUT" $NA; echo "rc=$?"
echo "== 記録（949）：名目 GDP の下がり（1/100 以下・止める線にしない）"
python -X utf8 -I "$D0/gdp_fall949.py" --rows M="$M"
} > "$OUT.j949"
cat "$OUT.j949" >> "$OUT"; RC=$(grep -o '^rc=[0-9]*' "$OUT.j949" | tail -1 | cut -d= -f2); rm -f "$OUT.j949"
echo STOPDONE; exit "${RC:-2}"
