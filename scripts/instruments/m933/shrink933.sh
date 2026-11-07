#!/usr/bin/env bash
# (933) 合流：反証役が読み終えた線の生を〈keepsum〉へ縮めてから消す（読むだけの集計＋消す）。
#   bash shrink933.sh <タグ> <生のフォルダ（ins を含む out_*）の親> <消すサブフォルダ...>
#   keepsum は D:/PoB_work/m933/keepsum/keepsum_<タグ>.tsv へ（合流が measurements/m933_20261107/ へ写す）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
S=D:/PoB_work/wt_m933/scripts/instruments
TAG=${1:?タグ}; P=${2:?親}; shift 2
mkdir -p D:/PoB_work/m933/keepsum
if [ -d "$P/out_ib_10/ins" ]; then
  python -X utf8 -I $S/tools/keepsum.py "$P/out_ib_10/ins" D:/PoB_work/m933/keepsum/keepsum_$TAG.tsv || exit 3
fi
for d in "$@"; do
  case "$d" in out_*|src_*|art_*) rm -rf "$P/$d" && echo "消した $P/$d" ;; *) echo "消さない $d" ;; esac
done
python -c "import shutil;print('D: free', round(shutil.disk_usage('D:/').free/1e9,2),'GB')"
echo SHRINKDONE
