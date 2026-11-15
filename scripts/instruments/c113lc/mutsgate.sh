#!/usr/bin/env bash
# (1152) c113lc 突然変異の門の集計（短い走り・種 1〜2）。G-C0′＝F0 と比べる・G-C1a′ の床 0.4034・G-C2′＝0・xUnit の結果。
W=${1:-D:/PoB_work/c113lc_work/muts}; S=/d/PoB_work/wt_c113lc/scripts/instruments/c113lc
for n in F0 F1 M2 M3 M4 M5; do
  [ -d "$W/$n/ins" ] || continue
  echo "##### $n $(tr '\n' ' ' < $W/$n/rc.txt)"
  python -X utf8 -I $S/gates_lc.py $W/$n/ins 1 2 0.4034 | grep -v "^  種"
  [ $n = F0 ] || python -X utf8 -I $S/gc0.py $W/F0/ins $W/$n/ins 1 2 | tail -4
done
for n in M1 M3 M4 M5; do
  [ -f "$W/$n/xunit.txt" ] || continue
  echo "##### xUnit $n"; grep -E "失敗 |成功|失敗!|\[FAIL\]" "$W/$n/xunit.txt" | cut -c1-200 | head -8
done
