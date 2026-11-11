#!/usr/bin/env bash
# (852) 合流：線 c69eh の腕 B2 の記録の読み手（採否に使わない）＝同じ盤面の新÷旧（ratio848.py）・資本家の在庫の新÷旧（cpopcap852.py）・c69hl の型 T1・T2（arm847.sh・記録として並べるだけ）。
#   bash recB2_852.sh <腕の ins> <出力フォルダ> [<名>=c69ehB]
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"; R="$(cd "$SP/../.." && pwd)"
I=${1:?腕の ins}; O=${2:?出力}; N=${3:-c69ehB}; mkdir -p $O
python -I D:/PoB_work/wt_c69eh/scripts/instruments/c69eh/ratio848.py $O/ratio848_$N.txt $I D:/PoB_work/keep_845/base_ins D:/PoB_work/keep_839/n1 D:/PoB_work/keep_839/n2 > /dev/null || echo "ratio848 rc=$?"
python -I -X utf8 $SP/m852/cpopcap852.py $O/cpopcap_$N.txt $I D:/PoB_work/keep_845/base_ins D:/PoB_work/keep_839/n1 D:/PoB_work/keep_839/n2 > /dev/null || echo "cpopcap rc=$?"
R847_WORK=$R/measurements/m847_20261105 R847_OUT=$O/hol847 bash $SP/r847/arm847.sh $N $I b839 n839a+n839b > $O/hol847_$N.log 2>&1 || echo "arm847 rc=$?"
echo RECDONE
