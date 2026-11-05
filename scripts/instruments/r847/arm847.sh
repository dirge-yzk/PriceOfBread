#!/usr/bin/env bash
# (847) c69hl 1 本の腕に型 T1・T2・T3 を当てる（記録として並べる＝この波は止める線にしない・事前登録 m847 §1・§3）
#   bash arm847.sh <名> <腕の計器の出力（region_s のあるフォルダ・例 <出力>/ins）> [<基線の名＝an/<名>.txt・既定 b839>] [<盤面の世代の集合・既定 n839a+n839b>]
#   出力＝<作業>/arm/<名>.an.txt（an_h2 -v5）・<名>.ratios.tsv（ヌル腕 9 組の行＋腕の行）・<名>.loo.txt（一つ抜き 9 組と腕の T1・T2・T3）
#   出力のフォルダは R847_OUT（既定 <作業>/arm）。台本の場所 S は既定でこのフォルダの 1 つ上（無ければ main の scripts/instruments・POB_INSTR で替える）。
#   作業フォルダは R847_WORK（既定 D:/PoB_work/r847_work＝an/<ヌル腕 9 組と基線>.txt・ratios.tsv がある所）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
S0=$(cd "$SD/.." && pwd); [ -f "$S0/r840/holclass840.py" ] || S0=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments
S=${POB_INSTR:-$S0}
W=${R847_WORK:-D:/PoB_work/r847_work}
A=${1:?名}; D=${2:?計器の出力}; B=${3:-b839}; BD=${4:-n839a+n839b}
NULLG=n839a,n839b,n826a,n826b,n797a,n797b,a839,a826,a797
O=${R847_OUT:-$W/arm}; mkdir -p $O $O/an
python -X utf8 -I $S/hol/an_h2.py $D -v5 > $O/an/$A.txt || exit 3
for g in ${NULLG//,/ } $B b826 b839; do cp $W/an/$g.txt $O/an/$g.txt; done
python -X utf8 -I - $W/ratios.tsv $O/$A.ratios.tsv "$NULLG" <<'EOF' || exit 4
import sys
src, out, gs = sys.argv[1], sys.argv[2], set(sys.argv[3].split(','))
L = open(src, 'rb').read().split(b'\n')
open(out, 'wb').write(b'\n'.join([L[0]] + [l for l in L[1:] if l and l.split(b'\t')[0].decode() in gs]) + b'\n')
EOF
python -X utf8 -I $S/r840/ratio840.py "$A=$D" | tail -n +2 >> $O/$A.ratios.tsv || exit 5
python -X utf8 -I $SD/loo847.py --hc $S/r840/holclass840.py --ratios $O/$A.ratios.tsv --an $O/an --groups $NULLG \
  --base $A:$B,n826a:b826,n826b:b826,n839a:b839,n839b:b839,a839:a826 --board $A:$BD,n839a:n839a+n839b,n839b:n839a+n839b,n826a:n826a+n826b,n826b:n826a+n826b,n797a:n797a+n797b,n797b:n797a+n797b,a826:a826,a797:a797,a839:a839 \
  --arms $A > $O/$A.loo.txt || exit 6
grep -E '^## |^'"$A"$'\t' $O/$A.loo.txt
