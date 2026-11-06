#!/usr/bin/env bash
# (887) c75ip 紙の段（事前登録 m887 [11]）：旗 0 の盤面 keep_885/base_ins とヌル腕 keep_873/n1・n2 に門の台本 gates887.py を当て、周辺の点検 periph887.py・ペルーの型 peru887.py を数える。
#   (876) の腕 B の生 keep_879_rej/c73ipB_10（旗 1・同じ盤面・種 1〜10）にも同じ台本を当てる＝〈旗 1 の実の生で正しい実装が通るか〉の記録（採否に使わない）。読むだけ。
#   bash paper887.sh <出力フォルダ>
export PYTHONUTF8=1
C="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; O=${1:?出力フォルダ}; mkdir -p "$O"
K=D:/PoB_work/keep_885/base_ins; N1=D:/PoB_work/keep_873/n1; N2=D:/PoB_work/keep_873/n2; B=D:/PoB_work/keep_879_rej/c73ipB_10/ins
python -I -X utf8 "$C/gates887.py" $K > "$O/paper_gates_M.txt" 2>&1; echo "gates M rc=$?"
python -I -X utf8 "$C/gates887.py" $N1 > "$O/paper_gates_n1.txt" 2>&1; echo "gates n1 rc=$?"
python -I -X utf8 "$C/gates887.py" $N2 > "$O/paper_gates_n2.txt" 2>&1; echo "gates n2 rc=$?"
python -I -X utf8 "$C/gates887.py" $B > "$O/record_gates_c73ipB.txt" 2>&1; echo "gates c73ipB rc=$?"
python -I -X utf8 "$C/peru887.py" $K > "$O/peru_M.txt" 2>&1; echo "peru M rc=$?"
python -I -X utf8 "$C/peru887.py" $N1 > "$O/peru_n1.txt" 2>&1; echo "peru n1 rc=$?"
python -I -X utf8 "$C/peru887.py" $N2 > "$O/peru_n2.txt" 2>&1; echo "peru n2 rc=$?"
python -I -X utf8 "$C/periph887.py" M=$K N1=$N1 N2=$N2 c73ipB=$B > "$O/periph_debt.txt" 2>&1; echo "periph rc=$?"
echo PAPERDONE
