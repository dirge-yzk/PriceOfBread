#!/usr/bin/env bash
# (875) c73gt 基準とヌル腕に (a)(c)(d) を当てる（読むだけ・事前登録 [5] の 3）。keep_873 と keep_852 の base_ins・n1・n2・a11 だけを読む。c72ipB_10 などの腕は開かない。
#   呼び方：bash scripts/instruments/tools/bg.sh D:/PoB_work/c73gt_work/ref.mark bash scripts/instruments/c73gt/run875.sh
#   出力：measurements/m875_20261106/（down_*・down10_*・g5b_*・hol_*）
export PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); R=$(cd "$SD/../../.." && pwd)
O=$R/measurements/m875_20261106; mkdir -p "$O"
K3=D:/PoB_work/keep_873; K2=D:/PoB_work/keep_852
PY="python -X utf8 -I"; C=$R/scripts/instruments/c73gt
echo "== (a) 下向きの 100 倍（行つき）"
$PY $C/gdp_down875.py --rows m873_base=$K3/base_ins m873_n1=$K3/n1 m873_n2=$K3/n2 m873_a11=$K3/a11 m852_base=$K2/base_ins m852_n1=$K2/n1 m852_n2=$K2/n2 m852_a11=$K2/a11 > $O/down_ref875.txt 2>&1; echo "rc=$?"
echo "== (a) M1 閾値 1/10"
$PY $C/gdp_down875.py --down 10 m873_base=$K3/base_ins m873_n1=$K3/n1 m873_n2=$K3/n2 m873_a11=$K3/a11 m852_base=$K2/base_ins m852_n1=$K2/n1 m852_n2=$K2/n2 m852_a11=$K2/a11 > $O/down10_ref875.txt 2>&1; echo "rc=$?"
echo "== (c) G5b の分布：ヌル対ヌル"
$PY $C/g5bdist875.py null m873_ヌル対ヌル $K3/base_ins $K3/n1 $K3/n2 $O/g5b_null_m873.txt 1-10 --by-seed > /dev/null 2>&1; echo "rc=$?"
$PY $C/g5bdist875.py null m852_ヌル対ヌル $K2/base_ins $K2/n1 $K2/n2 $O/g5b_null_m852.txt 1-10 --by-seed > /dev/null 2>&1; echo "rc=$?"
echo "== (d) 割れる州の率（較正）867 と同じ入力"
M3=$R/measurements/m873_20261106
$PY $C/holsplit875.py --calib $M3/armM/stop_mM.txt $K3/base_ins $M3/null/stop_n1.txt $K3/n1 $M3/null/stop_n2.txt $K3/n2 $O/hol_calib_m873_875.txt 10 > /dev/null 2>&1; echo "rc=$?"
echo "== (d) 一つ抜き（1/4 の事象を腕に見立てる・m873）"
loo() { # <名> <腕an> <腕ins> <ヌルa an> <ヌルa ins> <ヌルb an> <ヌルb ins> <出力の接頭>
  $PY $C/holsplit875.py "$2" "$3" "$8_875.txt" --level 1/4 --null "$4" "$5" --null "$6" "$7" > /dev/null 2>&1; echo "$1 match rc=$?"
  $PY $C/holsplit875.py "$2" "$3" "$8_nomatch.txt" --level 1/4 --no-match --null "$4" "$5" --null "$6" "$7" > /dev/null 2>&1; echo "$1 nomatch rc=$?"
}
loo N1 $M3/null/stop_n1.txt $K3/n1 $M3/armM/stop_mM.txt $K3/base_ins $M3/null/stop_n2.txt $K3/n2 $O/hol_loo_m873_N1
loo N2 $M3/null/stop_n2.txt $K3/n2 $M3/armM/stop_mM.txt $K3/base_ins $M3/null/stop_n1.txt $K3/n1 $O/hol_loo_m873_N2
loo M $M3/armM/stop_mM.txt $K3/base_ins $M3/null/stop_n1.txt $K3/n1 $M3/null/stop_n2.txt $K3/n2 $O/hol_loo_m873_M
echo "== (c) 前例の腕 c70ssB_10（m852 の盤面・近似）と keep_852 の組・--check"
$PY $C/g5bdist875.py arm m852_c70ssB_10 D:/PoB_work/keep_859_rej/c70ssB_10/ins $K2/base_ins $K2/n1 $K2/n2 $O/g5b_arm_c70ssB_m852.txt 1-10 --by-seed --check > /dev/null 2>&1; echo "rc=$?"
echo REFDONE
