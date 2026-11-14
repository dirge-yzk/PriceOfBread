#!/usr/bin/env bash
# (1130) c110lo の突然変異と対照の短い走り（事前登録 §突然変異）。コミットから使い捨ての木を作り（git archive）、変異を当てて runshort.sh で回す。
#   bash runmuts.sh <コミット> <作業フォルダ> <名:旗:変異 ...>   例  F0:0:  F1:1:  M1:1:M1
#   出力 <作業フォルダ>/<名>/ins/railspend_s{1,2}.tsv・rc.txt。走り終えた木は消す（生の表は残す）。
R="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"; S="$R/scripts/instruments/c110lo"
H="${1:?コミット}"; W="${2:?作業フォルダ}"; shift 2
mkdir -p "$W"
for spec in "$@"; do
  IFS=: read -r NAME FLAG MU <<< "$spec"
  T="$W/tree_$NAME"; rm -rf "$T" "$W/$NAME"; mkdir -p "$T"
  ( cd "$R" && git archive "$H" ) | tar -x -C "$T"
  bash "$S/runshort.sh" "$T" "$W/$NAME" "$FLAG" $MU
  echo "$NAME $(cat "$W/$NAME/rc.txt")" >> "$W/runmuts.log"
  rm -rf "$T"
done
