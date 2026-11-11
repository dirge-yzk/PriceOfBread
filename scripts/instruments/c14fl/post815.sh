#!/usr/bin/env bash
# (815) c14fl 腕 B の後の読み（読むだけ）：門と止める線と記録（rec815.sh）・〈壊す〉①（home_break）・門 A′（nullgate）・NG（ng.sh）・.cost を並べる。
#   使い方: bash post815.sh（Bash の run_in_background で）。腕 B の生 D:/PoB_work/c14fl_work/armB/out_c14flB_10/ins。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_c14fl; S=scripts/instruments; W=D:/PoB_work/c14fl_work; O=measurements/m815_20261104
B=D:/PoB_work/c14fl_work/armB/out_c14flB_10/ins; Z=D:/PoB_work/c14fl_work/flag0_cp   # 旗 0 の cpwd_s・cpworld_s（ident c14flz の腕 b から写した 20 本＝門の比べる側）
for x in "" .cost .manifest; do cp $W/armB/b10_c14flB_10.txt$x measurements/b10_20261104_c14flB_ins$x; done
bash $S/c14fl/rec815.sh $B $O/rec_B.txt c14flB 1-10 $Z
python -X utf8 -I $S/brk693/home_break.py c14flB=$B base=D:/PoB_work/keep_813/base_ins n1=D:/PoB_work/keep_797/n1 n2=D:/PoB_work/keep_797/n2 > $O/homebreak_B.txt 2>&1
python -X utf8 $S/tools/nullgate.py measurements/b10_20261104_c14flB_ins measurements/b10_20261103_m797_ins measurements/b10_20261103_m797_n1_ins measurements/b10_20261103_m797_n2_ins > $O/nullgate_B.txt 2>&1
bash $S/ng.sh $B c14flB D:/PoB_work/c14fl_work/ngB rd:measurements/ng797_20261103:m797 > $O/ng_B.txt 2>&1
echo POSTDONE >> $O/rec_B.txt
