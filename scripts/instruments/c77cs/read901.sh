#!/usr/bin/env bash
# (901) c77cs 腕の読み（読むだけ・(896) read896.sh の写し・基準を keep_899/base_ins へ・門を gates901.py へ・an_h2 -v4・nullgate・ng を足した）。他の線の台本は 1 字も変えずに呼ぶ。
#   bash read901.sh b <ins> <batch の出力フォルダ>   種 1〜10
#   bash read901.sh s <ins> <batch の出力フォルダ>   種 11〜20（基準 keep_873/a11・幅は種 1〜10 の物＝近似）
#   出力：D:/PoB_work/c77cs_work/read/<b|s>/
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
R=C:/Users/dirge/Documents/PriceOfBread; S=$R/scripts/instruments; W=D:/PoB_work/wt_c77cs; C=$W/scripts/instruments/c77cs
MODE=${1:?b|s}; I=${2:?ins}; BO=${3:?batch の出力}; O=D:/PoB_work/c77cs_work/read/$MODE
mkdir -p $O
PY="python -X utf8 -I"
K=D:/PoB_work/keep_899/base_ins; N1=D:/PoB_work/keep_873/n1; N2=D:/PoB_work/keep_873/n2
case $MODE in
  b)
    bash $S/m866/stoplines866.sh $I $O/stop_b.txt 1 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_b.txt $R/measurements/m873_20261106/armM/stop_mM.txt > $O/holclass_b.txt 2>&1
    $PY $S/c74id/holown880.py $O/stop_b.txt $I $O/holown_b.txt --base $R/measurements/m873_20261106/armM/stop_mM.txt $K > $O/holown_b.log 2>&1
    $PY $S/c73gt/gdp_down875.py arm=$I base=$K N1=$N1 N2=$N2 > $O/gdpdown_b.txt 2>&1
    python -X utf8 $S/hol/an_h2.py $I -v4 > $O/hol_v4_b.txt 2>&1
    python -X utf8 $C/gates901.py $I $K $K $N1 $N2 1 $O/gates_b.txt > $O/gates_b.log 2>&1; echo "gates rc=$?"
    python -X utf8 $S/tools/nullgate.py $BO $R/measurements/b10_20261106_m873_ins $R/measurements/b10_20261106_m873_n1_ins $R/measurements/b10_20261106_m873_n2_ins > $O/nullgate_b.txt 2>&1
    (cd $W && bash $S/ng.sh $I c77csB D:/PoB_work/c77cs_work/read/b/ng rd:measurements/ng873_20261106:m873) > $O/ng_b.txt 2>&1   # rd: の中に C: のコロンを入れない＝相対パス
    ;;
  s)
    bash $S/m866/stoplines866.sh $I $O/stop_s.txt 11 > /dev/null
    $PY $S/m839/holclass839.py $O/stop_s.txt $R/measurements/m873_20261106/armS11/stop_mS.txt > $O/holclass_s.txt 2>&1
    $PY $S/c74id/holown880.py $O/stop_s.txt $I $O/holown_s.txt --base $R/measurements/m873_20261106/armS11/stop_mS.txt D:/PoB_work/keep_873/a11 > $O/holown_s.log 2>&1
    $PY $S/c73gt/gdp_down875.py arm=$I base=D:/PoB_work/keep_873/a11 > $O/gdpdown_s.txt 2>&1
    python -X utf8 $S/hol/an_h2.py $I -v4 > $O/hol_v4_s.txt 2>&1
    python -X utf8 $C/gates901.py $I D:/PoB_work/keep_873/a11 $K $N1 $N2 11 $O/gates_s.txt > $O/gates_s.log 2>&1; echo "gates rc=$?"
    ;;
esac
echo READDONE
