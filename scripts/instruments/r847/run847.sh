#!/usr/bin/env bash
# (847)(853) c69hl 恒等の門 1 の作り直し：今ある生から an_h2 -v5 と ratio840 を作り直す（事前登録 m847 §5 の門 1）。(847) の反証役 ⑥ で台本が無かった物。
#   bash run847.sh <出力フォルダ> [名 ...]    名を渡さなければ 16 組（生が今ある物）の全て。
#   出力＝<出力>/an/<名>.txt（LF）・<出力>/ratios.tsv（見出し 1 行＋名ごとの行・LF）。比べるのは gate847.sh（measurements/m840_20261105/ の写しとバイトで）。
#   生の場所の表は下の MAP（名=計器の出力のフォルダ・D:/PoB_work の下）。消えた 3 組（b832・c66dwB・mS832）は写しを使う＝ここには無い。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
S0=$(cd "$SD/.." && pwd); S=${POB_INSTR:-$S0}
O=${1:?出力フォルダ}; shift
P=${R847_RAW:-D:/PoB_work}
MAP="n839a=keep_839/n1 n839b=keep_839/n2 n826a=keep_826/n1 n826b=keep_826/n2 n797a=keep_797/n1 n797b=keep_797/n2 a839=keep_839/a11 a826=keep_826/a11 a797=keep_797/a11 b839=keep_839/base_ins b826=keep_826/base_ins abDW0=keep_839_rej/abDW0_md_10/ins c67svB=keep_839_rej/c67svB_10/ins mM=keep_839_rej/mM_mddw_10/ins mS=keep_839_rej/mS_mddw_s11/ins c65mdB=keep_826_rej/c65mdB_10/ins"
NAMES="${*:-$(for kv in $MAP; do echo ${kv%%=*}; done)}"
mkdir -p "$O/an"
first=1
for n in $NAMES; do
  d=""; for kv in $MAP; do [ "${kv%%=*}" = "$n" ] && d="$P/${kv#*=}"; done
  [ -n "$d" ] || { echo "名が分からない: $n"; exit 2; }
  ls "$d"/region_s*.tsv > /dev/null 2>&1 || { echo "生が無い: $d"; exit 3; }   # 種 11〜20 の組は region_s11〜 なので glob
  python -X utf8 -I "$S/hol/an_h2.py" "$d" -v5 | tr -d '\r' > "$O/an/$n.txt" || exit 4
  if [ $first = 1 ]; then python -X utf8 -I "$S/r840/ratio840.py" "$n=$d" | tr -d '\r' > "$O/ratios.tsv" || exit 5; first=0
  else python -X utf8 -I "$S/r840/ratio840.py" "$n=$d" | tr -d '\r' | tail -n +2 >> "$O/ratios.tsv" || exit 5; fi
done
echo "作り直した: $NAMES"
