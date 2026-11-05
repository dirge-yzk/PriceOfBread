#!/usr/bin/env bash
# (847) c69hl 突然変異（事前登録 m847 §6）：holclass840.py と loo847.py の写しを 1 か所ずつ壊し、
#   両側の門（盤 A・盤 B・盤 L）・恒等の門 2（17 組の holclass839 との和）・実の生の一つ抜き（空振り・選んだ型）・恒等の門 3 で数える。
#   bash mut847.sh  ＝<作業>/mut/<MU>/ と mut/table.txt（MUS="MU9" MUTAG=_mu9 で一部だけ回せる＝mut/table_mu9.txt）（作業フォルダは R847_WORK で替える・既定 D:/PoB_work/r847_work）
#   前提：<作業>/fix/（mkfix847 の盤面と an_h2 -v5 と比の表）・<作業>/an/<名>.txt（19 組）・<作業>/ratios.tsv（19 組）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd); export SD
S0=$(cd "$SD/.." && pwd); [ -f "$S0/r840/holclass840.py" ] || S0=C:/Users/dirge/Documents/PriceOfBread/scripts/instruments
S=${POB_INSTR:-$S0}; export S
W=${R847_WORK:-D:/PoB_work/r847_work}
cd $W || exit 2
NULLG=n839a,n839b,n826a,n826b,n797a,n797b,a839,a826,a797
BASE=abDW0:b832,c67svB:b832,mM:b832,b839:b832,mS:a826,a839:a826,mS832:a826,c66dwB:b826,n826a:b826,n826b:b826,n839a:b839,n839b:b839,c65mdB:-,n797a:-,n797b:-,a826:-,a797:-
BOARD=abDW0:n826a+n826b,c67svB:n826a+n826b,mM:n826a+n826b,mS:a826,c65mdB:n797a+n797b,c66dwB:n826a+n826b,mS832:a826,b839:n839a+n839b,a839:a839,n839a:n839a+n839b,n839b:n839a+n839b,n826a:n826a+n826b,n826b:n826a+n826b,n797a:n797a+n797b,n797b:n797a+n797b,a826:a826,a797:a797
ARMS=abDW0,c65mdB,c67svB,mM,mS,b839,a839,c66dwB,mS832
mkdir -p mut
T=mut/table${MUTAG:-}.txt   # (853) MUS＝回す変異の一部・MUTAG＝表の名の後ろ（部分の実行で全体の表を消さない）
: > $T
for mu in ${MUS:-MU0 MU1 MU2 MU3 MU4 MU5 MU6 MU7 MU8 MU9 MU10}; do
  d=mut/$mu; mkdir -p $d
  python -X utf8 -I - "$mu" "$d" <<'EOF' || exit 3
import sys, os
mu, out = sys.argv[1], sys.argv[2]
src = {'holclass840.py': os.environ['S'] + '/r840/holclass840.py', 'loo847.py': os.environ['SD'] + '/loo847.py'}
NUL = b"NULLS = [x for x in null_s.split(',') if x and x != self_s]"
rep = {'MU0': None,
       'MU1': ('holclass840.py', b'NULL_MIN = min ', b'NULL_MIN = max '),
       'MU2': ('holclass840.py', NUL, b'NULLS = []'),
       'MU3': ('holclass840.py', b'STOP_DIV = 10 ', b'STOP_DIV = 4 '),
       'MU4': ('holclass840.py', NUL, b"NULLS = [x for x in null_s.split(',') if x]"),
       'MU5': ('holclass840.py', b'mn, nn = pop_min(same, lambda st: [s], reg)', b'mn, nn = pop_min(same, lambda st: sorted(SEEDS.get(st, ())), reg)'),
       'MU6': ('holclass840.py', b'inside = nn > 0 and ra >= mn', b'inside = nn == 0 or ra >= mn'),
       'MU7': ('loo847.py', b'R = [g for g in GROUPS if g != k]', b'R = list(GROUPS)'),
       'MU8': ('loo847.py', b'def t2(c, thr): return c > thr', b"def t2(c, thr): return c > float('inf')"),
       # (853) MU9：腕の基線（--base の割り当て＝元からの弱さの比べ先）を別の腕の物にずらす（割り当ての写し間違い）
       'MU9': ('loo847.py', b"if BASE.get(a, '-') != '-': cmd.append(f'{AN}/{BASE[a]}.txt')", b"if BASE.get(a, '-') != '-': _nb = [k for k, v in BASE.items() if v != '-']; cmd.append(f'{AN}/{BASE[_nb[(_nb.index(a) + 1) % len(_nb)]]}.txt')"),
       # (853) MU10：腕の盤面（--board の割り当て）をずらす。盤面は記録 (b) の世代にだけ使う欄で件数に効かない＝門の色（盤A・盤B・盤L・恒等の門）は変わらない。拾うのは別の欄の〈割り当ての不一致〉（結果は 12/17〔measurements/m853_20261105/mut847_mu9_mu10.txt〕）。(860) 注を結果に合わせた（実行する式は 1 字も変えない）
       'MU10': ('loo847.py', b"if a in BOARD: cmd += ['--board', BOARD[a]]", b"if a in BOARD: cmd += ['--board', BOARD[list(BOARD)[(list(BOARD).index(a) + 1) % len(BOARD)]]]")}[mu]
for name, p in src.items():
    b = open(p, 'rb').read()
    if rep and rep[0] == name:
        assert b.count(rep[1]) == 1, (mu, name, b.count(rep[1]))
        b = b.replace(rep[1], rep[2])
    open(os.path.join(out, name), 'wb').write(b)
EOF
  HC=$d/holclass840.py; LO=$d/loo847.py
  python -X utf8 -I $HC fix/an/arm.txt fix/an/base.txt --ratios fix/ratios.tsv --null nA,nB,nC --self arm > $d/fixA.txt
  python -X utf8 -I $HC fix/an/nA.txt --ratios fix/ratios.tsv --null nA,nB,nC --self nA > $d/fixB.txt
  python -X utf8 -I $LO --hc $HC --ratios fix/ratios.tsv --an fix/an --groups nA,nB,nC --base arm:base --arms arm > $d/fixL.txt
  fa=$(python -X utf8 -I $SD/fixgate847.py A $d/fixA.txt | cut -f2,3)
  fb=$(python -X utf8 -I $SD/fixgate847.py B $d/fixB.txt | cut -f2,3)
  fl=$(python -X utf8 -I $SD/fixgate847.py L $d/fixL.txt | cut -f2,3)
  R840_WORK=$W bash $S/r840/all840.sh $HC $d/all > /dev/null
  ng=$(grep -c $'\t不一致\t' $d/all/gate.txt); ok=$(grep -c $'\t一致\t' $d/all/gate.txt)
  python -X utf8 -I $LO --hc $HC --ratios ratios.tsv --an an --groups $NULLG --base $BASE --board $BOARD --arms $ARMS --check $d/all > $d/loo.txt
  asg=$(python -X utf8 -I - "$d/loo.txt" "$BASE" "$BOARD" <<'EOF2'
import sys, re
t = open(sys.argv[1], encoding='utf-8').read()
B = dict(x.split(':', 1) for x in sys.argv[2].split(',') if x); D = {k: v.replace('+', ',') for k, v in (x.split(':', 1) for x in sys.argv[3].split(',') if x)}
bad = n = 0
for m in re.finditer(r'^## 割り当て (\S+) 基線 (\S+) 盤面 (\S*)$', t, re.M):
    a, bs, dd = m.groups(); n += 1
    if bs != B.get(a, '-') or dd != D.get(a, ''): bad += 1
print(f'{bad}/{n}')
EOF2
)
  fp=$(grep '^## 空振り' $d/loo.txt | cut -c4-); pk=$(grep '^## 選んだ型' $d/loo.txt | cut -c4-); g3=$(grep '^## 恒等の門 3' $d/loo.txt | cut -c4-)
  ii=$(grep -h '^## 区分' $d/all/new_*.txt | grep -o '線が原因（新） [0-9]*' | awk '{s+=$2} END {print s+0}')
  printf '%s\t盤A %s\t盤B %s\t盤L %s\t恒等の門2 一致 %s・不一致 %s\t実の生 (ii) の合計 %s\t%s\t%s\t%s\t割り当ての不一致 %s\n' "$mu" "$fa" "$fb" "$fl" "$ok" "$ng" "$ii" "$fp" "$pk" "$g3" "$asg" >> $T
done
cat $T
