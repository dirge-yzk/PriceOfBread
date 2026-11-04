#!/usr/bin/env bash
# (840) c68hl 全ての腕に旧（holclass839）と新（holclass840）を当て、恒等の門（gate840）を数える（読むだけ・事前登録 m840 §5・§7）
#   bash all840.sh <holclass840.py の道> <出力フォルダ>
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
# (845) 合流が scripts/instruments/r840/ へ写した：作業フォルダは R840_WORK で替える・台本はこのフォルダから読む
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); export SD
S=$(cd "$SD/.." && pwd)
W=${R840_WORK:-D:/PoB_work/r840_work}
HC=${1:?holclass840}; O=${2:?出力}
mkdir -p $O
NULL=n839a,n839b,n826a,n826b,n797a,n797b,a839,a826,a797
# 名  基線の an_h2 の名（- は無し）  記録 (b) の盤面の世代の集合
ARMS="abDW0:b832:n826a,n826b
c67svB:b832:n826a,n826b
mM:b832:n826a,n826b
mS:a826:a826
c65mdB:-:n797a,n797b
c66dwB:b826:n826a,n826b
mS832:a826:a826
b839:b832:n839a,n839b
a839:a826:a839
n839a:b839:n839a,n839b
n839b:b839:n839a,n839b
n826a:b826:n826a,n826b
n826b:b826:n826a,n826b
n797a:-:n797a,n797b
n797b:-:n797a,n797b
a826:-:a826
a797:-:a797"
: > $O/gate.txt
while IFS=: read -r a b bd; do
  bf=(); [ "$b" != "-" ] && bf=($W/an/$b.txt)
  python -X utf8 -I $S/m839/holclass839.py $W/an/$a.txt "${bf[@]}" > $O/old_$a.txt
  python -X utf8 -I $HC $W/an/$a.txt "${bf[@]}" --ratios $W/ratios.tsv --null $NULL --self $a --board $bd > $O/new_$a.txt
  python -X utf8 -I $SD/gate840.py $a $O/old_$a.txt $O/new_$a.txt >> $O/gate.txt
done <<< "$ARMS"
cat $O/gate.txt
