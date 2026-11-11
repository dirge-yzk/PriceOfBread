#!/usr/bin/env bash
# (1002) c92gf：stoplines1002.sh の終了コードの確かめ。使い方: bash rc1002.sh [<検査する stoplines の sh>]（既定は同じ場所の stoplines1002.sh）
#   合成：nation_s1.tsv だけで region_s1.tsv が無い ins フォルダ（gdp_jump1002.py が開けず rc≠0）。緑＝rc≠0 かつ STOPDONE が出ない。赤＝STOPDONE が出る（rc を見ずに出す旧の形）。
cd "$(dirname "$0")/../../.."; T=${1:-scripts/instruments/c92gf/stoplines1002.sh}
W=D:/PoB_work/c92gf_work/rcfix; rm -rf "$W"; mkdir -p "$W/ins"; cp scripts/instruments/c92gf/fixture/nation_s1.tsv "$W/ins/"
O=$(bash "$T" "$W/ins" "$W/out.txt" 1); rc=$?
case "$O" in *STOPDONE*) echo "赤 rc=$rc STOPDONE が出た"; exit 1;; *) [ "$rc" -ne 0 ] && { echo "緑 rc=$rc STOPDONE なし"; exit 0; } || { echo "赤 rc=0"; exit 1; };; esac
