export PYTHONUTF8=1
S=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments
for M in D:/PoB_work/keep_899/base_ins D:/PoB_work/keep_873/n1 D:/PoB_work/keep_873/n2; do
  echo "#### $M"
  python -X utf8 -I $S/c11fl/pxjump.py M=$M
  python -X utf8 -I $S/r833/gdp_jump833.py --rows M=$M | head -6
  python -X utf8 -I $S/brk693/plvl_seeds.py $M
done
