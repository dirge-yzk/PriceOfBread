#!/usr/bin/env bash
# (933) 合流：採った挙動の線の腕の生（帰属を読み終えた）を keepsum に縮めてから消す。
#   bash shrinkline933.sh   （c81cw の腕 B・S11 は keepsum を作ってから・c81la は線の keepsum がある＝そのまま消す）
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
S=D:/PoB_work/wt_m933/scripts/instruments; K=D:/PoB_work/m933/keepsum
mkdir -p $K D:/PoB_work/c81cw_work/keep_s1
# 次の波の読む線 ⑥（反乱の側の国が建国から GDP 0 を続ける型）の材料＝c81cw の腕 B の種 1（共和制フランス）の 3 表だけ残す（約 0.3GB）
for t in nation region cearn; do cp D:/PoB_work/c81cw_work/out_c81cwB_10/ins/${t}_s1.tsv D:/PoB_work/c81cw_work/keep_s1/ || exit 4; done
for a in c81cwB_10 c81cwS_10_s11; do
  python -X utf8 -I $S/tools/keepsum.py D:/PoB_work/c81cw_work/out_$a/ins $K/keepsum_$a.tsv || exit 3
  rm -rf D:/PoB_work/c81cw_work/out_$a && echo "消した c81cw_work/out_$a"
done
for d in out_c81laB_10 out_c81laS_10_s11 mut909 mut923; do
  rm -rf D:/PoB_work/c81la_work/$d && echo "消した c81la_work/$d"
done
for d in src_ia src_ib src_it out_ia_5 out_ib_10 art_it; do rm -rf D:/PoB_work/ident_m933z/$d; done
python -c "import shutil;print('D: free', round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
echo SHRINKLINEDONE
