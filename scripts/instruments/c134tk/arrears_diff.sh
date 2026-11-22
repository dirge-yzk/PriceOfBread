#!/usr/bin/env bash
# (1295) c134tk：arrears_s の基準（keep_1294/base_ins）と腕（ident_c134tk）の差を、underLimit から導かれる列（underLimit・owes・predNew・arrOldMonth・arrNewMonth・cumOld・cumNew）を除いて種ごとに数える。
# 使い方：bash arrears_diff.sh <基準の ins> <腕の ins> <作業フォルダ>
A=$1; B=$2; W=$3
for s in 1 2 3 4 5 6 7 8 9 10; do
  cut -f1-11,13-16,18,24 "$A/arrears_s$s.tsv" | tail -n +2 | sort > "$W/a.k"; cut -f1-11,13-16,18,24 "$B/arrears_s$s.tsv" | tail -n +2 | sort > "$W/b.k"
  echo "s$s base_only=$(comm -23 "$W/a.k" "$W/b.k" | wc -l) arm_only=$(comm -13 "$W/a.k" "$W/b.k" | wc -l) base_only_predOld1=$(comm -23 "$W/a.k" "$W/b.k" | awk -F'\t' '$16==1' | wc -l) rows $(($(wc -l < "$A/arrears_s$s.tsv")-1))->$(($(wc -l < "$B/arrears_s$s.tsv")-1))"
done
