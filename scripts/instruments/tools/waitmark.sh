#!/usr/bin/env bash
# (728) 正規の待ちの台本（2/2）：印のファイル（bg.sh・ident.sh が書く）ができるまで待ち、できたら中身を 1 行出して終わる。
#   呼び方（担当）：Monitor の命令に  bash scripts/instruments/tools/waitmark.sh <印のファイル> [最大秒=1500]  ＝待つ間の時間の扱いはこの 1 本に閉じ込める（担当の命令に sleep を書かない）。
#   最大秒を超えたら TIMEOUT を出して 124 で終わる（Monitor は 30 分まで＝再び呼ぶ）。印の rc が 0 以外なら FAILED を付けて出す。
M="${1:?印のファイル}"; MAXS="${2:-1500}"; T0=$SECONDS
while [ ! -f "$M" ]; do
  [ $((SECONDS - T0)) -ge "$MAXS" ] && { echo "TIMEOUT $M（${MAXS}秒）"; exit 124; }
  sleep 3
done
L=$(cat "$M"); case "$L" in rc=0\ *) echo "DONE $M $L" ;; *) echo "FAILED $M $L"; exit 1 ;; esac
