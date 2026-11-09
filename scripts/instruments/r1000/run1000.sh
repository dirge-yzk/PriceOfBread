#!/usr/bin/env bash
# (1000) c91ld：r932 の台本の写しと landlord1000.py を盤面 m988 の生（基線 keep_994/base_ins・ヌル腕 keep_988/n1・n2）へ当てる（読むだけ）
# 使い方：bash run1000.sh <出力のフォルダ>
O="${1:?出力のフォルダ}"; mkdir -p "$O"
R=$(cd "$(dirname "$0")" && pwd)
B=D:/PoB_work/keep_994/base_ins; N1=D:/PoB_work/keep_988/n1; N2=D:/PoB_work/keep_988/n2
D3="$B $N1 $N2"
for t in cornint rule class scalars fm nation landshare; do for d in $B $N1 $N2; do printf "%s %s " $t $d; head -1 $d/${t}_s1.tsv | md5sum | cut -c1-12; done; done > "$O/headers1000_out.txt"
python -X utf8 -I -B "$R/cornint1000.py" $D3 > "$O/cornint1000_out.txt" 2>&1
python -X utf8 -I -B "$R/paper1000.py" $D3 > "$O/paper1000_out.txt" 2>&1
python -X utf8 -I -B "$R/addon1000.py" $D3 > "$O/addon1000_out.txt" 2>&1
python -X utf8 -I -B "$R/rentshare1000.py" $B > "$O/rentshare1000_out.txt" 2>&1
for d in $B $N1 $N2; do echo "== $d"; python -X utf8 -I -B "$R/status1000.py" $d; done > "$O/status1000_out.txt" 2>&1
python -X utf8 -I -B "$R/landlord1000.py" $B "$O/landlord1000_out.txt"
python -X utf8 -I -B "$R/landlord1000.py" $N1 "$O/landlord1000_n1_out.txt"
python -X utf8 -I -B "$R/cost1000.py" $B "$O/cost1000_out.txt"
echo ok
