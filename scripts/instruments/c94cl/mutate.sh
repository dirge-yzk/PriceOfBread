#!/usr/bin/env bash
# (1012) c94cl 突然変異の台本（★(1011)：出力の全文を残し、日本語の出力で絞る）。使い方：bash mutate.sh <使い捨ての木> <出力フォルダ> <M の番号...>
# 木＝合わせたコミットの worktree（使い捨て）。各回：適用→試験（隔離ビルド）→全文を <出力>/<M>.log→絞った行を <出力>/summary.txt→木を元に戻す。
TREE="$1"; OUT="$2"; shift 2
SELF="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$OUT"
for M in "$@"; do
  git -C "$TREE" checkout -q -- PriceOfBread.SimCore PriceOfBread.Tests
  python "$SELF/mutations.py" "$TREE" "$M" > "$OUT/$M.log" 2>&1 || { echo "$M 適用失敗" >> "$OUT/summary.txt"; cat "$OUT/$M.log" >> "$OUT/summary.txt"; continue; }
  ( cd "$TREE" && dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c94cl_work/mut_art --filter "FullyQualifiedName~CorncfInstrumentTests|FullyQualifiedName~LordstatInstrumentTests" ) >> "$OUT/$M.log" 2>&1
  { echo "== $M"; grep -E "成功!|失敗!|^\s+失敗 |合格:" "$OUT/$M.log" | cut -c1-200; } >> "$OUT/summary.txt"
done
git -C "$TREE" checkout -q -- PriceOfBread.SimCore PriceOfBread.Tests
echo done > "$OUT/done.flag"
