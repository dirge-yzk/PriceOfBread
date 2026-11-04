#!/usr/bin/env bash
# (840) c68hl 生を読む（読むだけ）：an_h2 -v5 の出力と比の表を作る。
#   bash run840.sh an <名>...   ＝D:/PoB_work/r840_work/an/<名>.txt
#   bash run840.sh ratio <出力> <名>...  ＝比の表 1 枚
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
# (845) 合流が scripts/instruments/r840/ へ写した：作業フォルダは R840_WORK で替える・台本はこのフォルダから読む
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); export SD
S=$(cd "$SD/.." && pwd)
W=${R840_WORK:-D:/PoB_work/r840_work}
K=D:/PoB_work
dir_of() {
  case "$1" in
    n839a) echo $K/keep_839/n1 ;; n839b) echo $K/keep_839/n2 ;;
    n826a) echo $K/keep_826/n1 ;; n826b) echo $K/keep_826/n2 ;;
    n797a) echo $K/keep_797/n1 ;; n797b) echo $K/keep_797/n2 ;;
    a839) echo $K/keep_839/a11 ;; a826) echo $K/keep_826/a11 ;; a797) echo $K/keep_797/a11 ;;
    b839) echo $K/keep_839/base_ins ;; b832) echo $K/keep_832/base_ins ;; b826) echo $K/keep_826/base_ins ;;
    abDW0) echo $K/keep_839_rej/abDW0_md_10/ins ;; c67svB) echo $K/keep_839_rej/c67svB_10/ins ;;
    mM) echo $K/keep_839_rej/mM_mddw_10/ins ;; mS) echo $K/keep_839_rej/mS_mddw_s11/ins ;;
    c65mdB) echo $K/keep_826_rej/c65mdB_10/ins ;; c66dwB) echo $K/keep_832_rej/c66dwB_10/ins ;;
    mS832) echo $K/keep_832_rej/mS_s11/ins ;;
    *) echo "名が無い $1" >&2; return 1 ;;
  esac
}
mode=$1; shift
if [ "$mode" = an ]; then
  for n in "$@"; do
    d=$(dir_of $n) || exit 2
    python -X utf8 -I $S/hol/an_h2.py $d -v5 > $W/an/$n.txt || exit 3
    echo "an $n $(head -3 $W/an/$n.txt | tr '\n' ' ')"
  done
elif [ "$mode" = ratio ]; then
  o=$1; shift; a=()
  for n in "$@"; do d=$(dir_of $n) || exit 2; a+=("$n=$d"); done
  python -X utf8 -I $SD/ratio840.py "${a[@]}" > $o || exit 3
  echo "ratio $o $(wc -l < $o)"
fi
