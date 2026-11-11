#!/usr/bin/env bash
# (932) c81ld: r898 の台本の写しを keep_919 の基線・n1・n2 へ当てる（読むだけ）
R=/d/PoB_work/r932_work/r932; K=/d/PoB_work/keep_919; O=/d/PoB_work/r932_work
D3="D:/PoB_work/keep_919/base_ins D:/PoB_work/keep_919/n1 D:/PoB_work/keep_919/n2"
for t in cornint rule class scalars fm nation; do for d in base_ins n1 n2; do printf "%s %s " $t $d; head -1 $K/$d/${t}_s1.tsv | md5sum | cut -c1-12; done; done > $O/headers932_out.txt
python -X utf8 -I -B $R/cornint932.py $D3 > $O/cornint932_out.txt 2>&1
python -X utf8 -I -B $R/paper932.py $D3 > $O/paper932_out.txt 2>&1
python -X utf8 -I -B $R/addon932.py $D3 > $O/addon932_out.txt 2>&1
python -X utf8 -I -B $R/rentshare932.py D:/PoB_work/keep_919/base_ins > $O/rentshare932_out.txt 2>&1
for d in base_ins n1 n2; do echo "== $d"; python -X utf8 -I -B $R/status932.py D:/PoB_work/keep_919/$d; done > $O/status932_out.txt 2>&1
