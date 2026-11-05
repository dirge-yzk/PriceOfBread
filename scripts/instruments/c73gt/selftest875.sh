#!/usr/bin/env bash
# (875) c73gt 自己試験：(a) 固定の盤面・(d) 固定の盤面・(b) 判定の行のバイト一致と突然変異 M2。実の生は keep_873/base_ins だけ読む。
#   呼び方：bash scripts/instruments/tools/bg.sh D:/PoB_work/c73gt_work/self.mark bash scripts/instruments/c73gt/selftest875.sh
#   出力：measurements/m875_20261106/（fix_down875.txt・mut_hol875.txt・cmp875_base.txt・cmp875_M2.txt）と標準出力（VERDICT の行）。
export PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); R=$(cd "$SD/../../.." && pwd)
O=$R/measurements/m875_20261106; W=D:/PoB_work/c73gt_work; mkdir -p "$O" "$W"
PY="python -X utf8 -I"; C=$R/scripts/instruments/c73gt; NOGO=""
$PY $C/fix_down875.py $W/fix_down $O/fix_down875.txt > /dev/null 2>&1 || NOGO="$NOGO [a]"
$PY $C/mut_hol875.py $W/mut_hol $O/mut_hol875.txt > /dev/null 2>&1 || NOGO="$NOGO [d]"
REFOUT=$R/measurements/m873_20261106/armM/stop_mM.txt
INS=D:/PoB_work/keep_873/base_ins
bash $C/stoplines875.sh $INS $W/self_b.txt 1 > /dev/null 2>&1
$PY $C/cmp875.py $W/self_b.txt $REFOUT D:/PoB_work/m873/out_mM_10/ins $INS D:/PoB_work/m873/read/mM/stop_mM.txt $W/self_b.txt > $O/cmp875_base.txt 2>&1 || NOGO="$NOGO [b]"
# M2：写しの判定の行（gdp_zero10 の呼び名）を 1 字変える
$PY $C/mut_b875.py $C/stoplines875.sh $W/stoplines875_M2.sh
bash $W/stoplines875_M2.sh $INS $W/self_b2.txt 1 > /dev/null 2>&1
$PY $C/cmp875.py $W/self_b2.txt $REFOUT D:/PoB_work/m873/out_mM_10/ins $INS D:/PoB_work/m873/read/mM/stop_mM.txt $W/self_b2.txt > $O/cmp875_M2.txt 2>&1 && NOGO="$NOGO [M2が赤にならない]"
if [ -n "$NOGO" ]; then echo "VERDICT self875 fail$NOGO"; else echo "VERDICT self875 pass (a) (d) (b) と M2 が予言どおり"; fi
cat $O/cmp875_base.txt $O/cmp875_M2.txt
