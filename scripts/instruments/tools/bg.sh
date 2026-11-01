#!/usr/bin/env bash
# (728) 正規の待ちの台本（1/2）：長い物を走らせ、終わりに完了の印のファイルを書く。
#   呼び方（担当）：Bash を run_in_background: true で  bash scripts/instruments/tools/bg.sh <印のファイル> <命令...>
#     完了の通知が来るまで他の仕事をするか応答を終える。印は  rc=<終了コード> end=<時刻> の 1 行（走り始めに消す）。出力は <印のファイル>.log。
#   別の場所から印だけを待ちたいときは waitmark.sh（Monitor の命令にする）。時間で待つ命令（& と sleep など）は担当が書かない。
M="${1:?印のファイル}"; shift
rm -f "$M"; mkdir -p "$(dirname "$M")"
"$@" > "$M.log" 2>&1
RC=$?
echo "rc=$RC end=$(date '+%F %T')" > "$M.tmp" && mv "$M.tmp" "$M"
exit $RC
