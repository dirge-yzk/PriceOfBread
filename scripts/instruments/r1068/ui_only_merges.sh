#!/usr/bin/env bash
# (1068) (a) main の first-parent の merge ごとに、入った枝の差分が〈画面だけ〉（App・SimCore/Display・Tests・docs・measurements・scripts・uishot）かを数える。読むだけ。
# 使い方: bash ui_only_merges.sh <リポジトリ> <頭> <いつから（git の --since）>
R=$1; H=$2; S=$3
git -C "$R" log --first-parent --merges --since="$S" --format='%h' "$H" | while read -r m; do
  p1=$(git -C "$R" rev-parse "$m^1"); p2=$(git -C "$R" rev-parse "$m^2"); b=$(git -C "$R" merge-base "$p1" "$p2")
  f=$(git -C "$R" diff --name-only "$b" "$p2")
  n=$(printf '%s\n' "$f" | grep -c .)
  eng=$(printf '%s\n' "$f" | grep -E '^PriceOfBread\.(SimCore|Runner)/' | grep -v '^PriceOfBread\.SimCore/Display/' | grep -c .)
  disp=$(printf '%s\n' "$f" | grep -c -E '^PriceOfBread\.(App|SimCore/Display)/')
  echo "$m files=$n engine_or_runner=$eng app_or_display=$disp"
done
