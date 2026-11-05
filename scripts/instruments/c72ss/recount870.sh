export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
S=D:/PoB_work/wt_c72ss/scripts/instruments; O=D:/PoB_work/c72ss_work/read
bash $S/m866/stoplines866.sh D:/PoB_work/keep_859_rej/c70ssB_10/ins $O/stop_c70ssB_s1.txt 1
python -X utf8 -I $S/m839/holclass839.py $O/stop_c70ssB_s1.txt D:/PoB_work/wt_c72ss/measurements/m852_20261105/armM/stop_mM.txt > $O/holclass_c70ssB_s1.txt 2>&1
bash $S/m866/stoplines866.sh D:/PoB_work/keep_852/a11 $O/stop_a11_852.txt 11
echo RECOUNTDONE
