#!/usr/bin/env bash
# (840) c68hl 突然変異（事前登録 m840 §7）：holclass840.py の写しを 1 か所ずつ壊し、両側の門（小さな盤面）と恒等の門（全ての腕）で数える
#   bash mut840.sh  ＝D:/PoB_work/r840_work/mut/<MU>/ と mut/table.txt
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
# (845) 合流が scripts/instruments/r840/ へ写した：作業フォルダは R840_WORK で替える・台本はこのフォルダから読む
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); export SD
S=$(cd "$SD/.." && pwd)
W=${R840_WORK:-D:/PoB_work/r840_work}
cd $W
fixrun() {   # <holclass840 の道> <出力>
  python -X utf8 -I $1 fix/an_arm.txt fix/an_base.txt --ratios fix/ratios.tsv --null nA,nB --self arm > $2
  python -X utf8 -I $SD/fixgate840.py $2 | cut -f2,3
}
mkdir -p mut
: > mut/table.txt
for mu in MU0 MU1 MU2 MU3; do
  d=mut/$mu; mkdir -p $d
  python -X utf8 -I - "$mu" "$d/holclass840.py" <<'EOF'
import sys, os
mu, out = sys.argv[1], sys.argv[2]
b = open(os.environ['SD'] + '/holclass840.py', 'rb').read()
rep = {'MU0': None,
       'MU1': (b'NULL_MIN = min ', b'NULL_MIN = max '),
       'MU2': (b"NULLS = [x for x in null_s.split(',') if x and x != self_s]", b'NULLS = []'),
       'MU3': (b'STOP_DIV = 10 ', b'STOP_DIV = 4 ')}[mu]
if rep:
    assert b.count(rep[0]) == 1, (mu, b.count(rep[0]))
    b = b.replace(rep[0], rep[1])
open(out, 'wb').write(b)
EOF
  fx=$(fixrun $d/holclass840.py $d/fix_out.txt)
  bash $SD/all840.sh $d/holclass840.py $d/all > /dev/null
  ng=$(grep -c $'\t不一致\t' $d/all/gate.txt); ok=$(grep -c $'\t一致\t' $d/all/gate.txt)
  ii=$(grep -h '^## 区分' $d/all/new_*.txt | grep -o '線が原因（新） [0-9]*' | awk '{s+=$2} END {print s+0}')
  printf '%s\t両側の門 %s\t恒等の門 一致 %s・不一致 %s\t線が原因（新）の合計 %s\n' "$mu" "$fx" "$ok" "$ng" "$ii" >> mut/table.txt
done
cat mut/table.txt
