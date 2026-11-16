cd D:/PoB_work/r1182_work
for a in M:base_ins N1:n1 N2:n2; do t=${a%%:*}; d=${a#*:}
python -X utf8 -I r1182/s02_freq_all.py D:/PoB_work/keep_1178/$d $t > out_s02all_$t.txt 2>&1
python -X utf8 -I r1182/s02_freq_old.py D:/PoB_work/keep_1178/$d 10 > out_s02old_$t.txt 2>&1
done
echo done > s02.done
