#!/usr/bin/env bash
# (860) c71gt 受け渡しの自己試験：直した門の台本（gates_po860.py ほか）を D:/PoB_work/c71gt_work/gates/ に写して sha256 を書き、
#   固定の盤面（合成）の突然変異の表・旗 0 の盤面（実の keep_852/base_ins を腕に見立てる）・旗 1 の実の生（keep_852_rej/c69poB_10）・
#   閾値の数え直し・突然変異（G2b の形の要・ident_check の by・batch.sh の終了コード）・一つ抜きの空振りの率を回す。
#   呼び方（担当）：bash scripts/instruments/tools/bg.sh D:/PoB_work/c71gt_work/gates_ready.mark bash scripts/instruments/c71gt/selftest860.sh
#   出力＝標準出力（bg.sh が .log に書く）。先頭に判定（no-go の門の名と理由）・末尾に sha256 と G2b の形と旗 1 の実の生の結果。rc≠0 なら no-go あり（c71po は腕を回さない）。
export PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
R=$(cd "$SD/../../.." && pwd)
G=${C71GT_GATES:-D:/PoB_work/c71gt_work/gates}
M=${C71GT_OUT:-$R/measurements/m860_20261105}
RAW=${C71GT_RAW:-D:/PoB_work/keep_852}
REJ=${C71GT_REJ:-D:/PoB_work/keep_852_rej/c69poB_10}
F0=${C71GT_F0:-D:/PoB_work/c71gt_work/f0_839}
T=${C71GT_TMP:-D:/PoB_work/c71gt_work/selftest}
PRE=62190ef0f70668d31fa20b3e2e1394dc7289f8c5
mkdir -p "$G" "$M" "$T"
FILES="gates_po860.py gates_po860_thresholds.tsv mut_po860.py gates_ss860.py gates_ss860_thresholds.tsv mut_ss860.py"
for f in $FILES; do cp "$SD/$f" "$G/$f" || exit 2; done
( cd "$G" && sha256sum $FILES > SHA256.txt ) || exit 2
NOGO=""
nogo() { NOGO="$NOGO\n$1"; }
PY="python -X utf8 -I"
# 1 固定の盤面（合成）の突然変異の表
$PY "$G/mut_ss860.py" "$T/mutss" "$M/mut_ss860.txt" "$G/gates_ss860_thresholds.tsv" > "$T/mut_ss.out" 2>&1 || nogo "[ss] 固定の盤面の突然変異の表が予言どおりでない（$M/mut_ss860.txt）"
$PY "$G/mut_po860.py" "$T/mutpo" "$M/mut_po860.txt" 120 > "$T/mut_po.out" 2>&1 || nogo "[po] 固定の盤面の突然変異の表が予言どおりでない（$M/mut_po860.txt）"
# 2 旗 0 の盤面（実の生・基線を腕に見立てる）：働いた門が落ちる
$PY "$G/gates_ss860.py" $RAW/base_ins $RAW/base_ins $RAW/n1 $RAW/n2 "$M/flag0_ss860.txt" 2 1 "$G/gates_ss860_thresholds.tsv" > "$T/flag0_ss.out" 2>&1 || nogo "[ss] 旗 0 の盤面の実行が失敗"
J=$(grep '^門の判定' "$M/flag0_ss860.txt")
for g in G1a G1b G2 G3a G4; do case "$J" in *"$g=落"*) ;; *) nogo "[ss] 旗 0 の盤面で $g が落ちない（働いた門が旗 0 でも緑）：$J";; esac; done
$PY "$G/gates_po860.py" $RAW/base_ins $RAW/base_ins $RAW/n1 $RAW/n2 "$M/flag0_po860.txt" 10 1 "$G/gates_po860_thresholds.tsv" > "$T/flag0_po.out" 2>&1 || nogo "[po] 旗 0 の盤面の実行が失敗"
J=$(grep '^門の判定' "$M/flag0_po860.txt")
for g in G0 G1; do case "$J" in *"$g=落"*) ;; *) nogo "[po] 旗 0 の盤面で $g が落ちない：$J";; esac; done
for g in G2a G2b; do case "$J" in *"$g=通"*) ;; *) nogo "[po] 旗 0 の盤面で $g が通らない（壊さない門が旗 0 で赤）：$J";; esac; done
# 3 旗 1 の実の生（(850) の腕 B・盤面 m839）：正しい実装を落とさない（確かめだけ・閾値の合わせには使わない）
$PY "$G/gates_po860.py" $REJ/ins $F0 $RAW/n1 $RAW/n2 "$M/flag1_po860.txt" 10 1 "$G/gates_po860_thresholds.tsv" $RAW/base_ins > "$T/flag1_po.out" 2>&1 || nogo "[po] 旗 1 の実の生の実行が失敗"
J1=$(grep '^門の判定' "$M/flag1_po860.txt")
case "$J1" in "門の判定：全て通る"*) ;; *) nogo "[po] 旗 1 の実の生で落ちた門あり（正しい実装が通らない＝形が使えない）：$J1";; esac
$PY "$SD/flag1_forms860.py" $REJ/ins $F0 10 > "$M/flag1_forms860.txt" 2>&1
# 4 閾値の数え直し（形の選びを旗 0 の生から再計算して表と一致）
$PY "$G/gates_po860.py" --choose-form $RAW/base_ins $RAW/n1 $RAW/n2 "$T/chooseForm_860.txt" "$T/thr_recount.tsv" 10 > "$T/choose.out" 2>&1 || nogo "[po] 形 c が旗 0 で満たされない（--choose-form の rc≠0＝G2b を門にしない）"
cp "$T/chooseForm_860.txt" "$M/chooseForm_860.txt"
for k in form persist_L G2b_tol n_base W_S; do
  a=$(grep "^$k	" "$G/gates_po860_thresholds.tsv" | cut -f2); b=$(grep "^$k	" "$T/thr_recount.tsv" | cut -f2)
  [ "$a" = "$b" ] || nogo "[po] 閾値の表の $k $a と数え直した $b が違う"
done
grep -q '^no_go' "$G/gates_po860_thresholds.tsv" && nogo "[po] 形を選べなかった（G2b は門にしない）"
# G4m の閾値の数え直し（3 × 割合の最大）
LIM=$(bash "$SD/mask_shares860.sh" 2>&1 | python -X utf8 -I -c "
import sys,re
r=[float(m.group(1)) for m in re.finditer(r'（([0-9.]+)）・外した E≦0', sys.stdin.read())]
print('%.6f' % (3*max(r)) if len(r)==3 else 'ERR')")
LIM_T=$(grep '^G4m_limit' "$G/gates_ss860_thresholds.tsv" | cut -f2)
[ "$LIM" = "$LIM_T" ] || nogo "[ss] G4m の閾値 $LIM_T と数え直した $LIM が違う"
# 5 突然変異（G2b の形の要を外す）：窓の頭の減衰の項を 0 にした写しでは形 c が旗 0 で満たされず選べない
python -X utf8 -I -c "
import sys
t=open(sys.argv[1],encoding='utf-8').read()
a='rows[i - L + 1][\"ruleUngovBeforeShare\"] * (1 - SPEED) ** L'
assert t.count(a)==1
open(sys.argv[2],'w',encoding='utf-8',newline=chr(10)).write(t.replace(a,'0.0'))" "$G/gates_po860.py" "$T/gates_po860_mutant.py" || nogo "[共通] 突然変異の写しが作れない"
$PY "$T/gates_po860_mutant.py" --choose-form $RAW/base_ins $RAW/n1 $RAW/n2 "$T/chooseForm_mutant.txt" "$T/thr_mutant.tsv" 2 > "$T/choose_mut.out" 2>&1
[ $? -ne 0 ] && echo "○ 突然変異（減衰の項を外す）：形 c が旗 0 で満たされず選べない（rc≠0）" > "$T/mut_decay.txt" || { echo "✗ 突然変異（減衰の項を外す）が拾われない" > "$T/mut_decay.txt"; nogo "[po] 減衰の項を外した突然変異が拾われない"; }
cp "$T/mut_decay.txt" "$M/mut_decay860.txt"
# 6 ident_check の by と batch.sh の終了コード（新で緑・旧で赤）
git -C "$R" show $PRE:scripts/instruments/ident/ident_check.py > "$T/ident_check_old.py"
$PY "$SD/test_ident_display860.py" "$R/scripts/instruments/ident/ident_check.py" "$T/t10new" > "$M/t10_new.txt" 2>&1 || nogo "[ident_check] T10 が新で赤"
$PY "$SD/test_ident_display860.py" "$T/ident_check_old.py" "$T/t10old" > "$M/t10_old.txt" 2>&1 && nogo "[ident_check] T10 が旧で赤にならない（試験が拾わない）"
bash "$SD/test_batch_rc860.sh" "$R/scripts/instruments/batch.sh" "$T/tb_new" > "$M/test_batch_rc860_new.txt" 2>&1 || nogo "[batch.sh] 終了コードの試しが新で赤"
mkdir -p "$T/oldsp" && git -C "$R" show $PRE:scripts/instruments/batch.sh > "$T/oldsp/batch.sh" && cp -r "$R/scripts/instruments/tools" "$T/oldsp/tools" && cp -r "$R/scripts/instruments/mon" "$T/oldsp/mon" 2>/dev/null
POB_REPO="$R" bash "$SD/test_batch_rc860.sh" "$T/oldsp/batch.sh" "$T/tb_old" > "$M/test_batch_rc860_old.txt" 2>&1 && nogo "[batch.sh] 終了コードの試しが旧で赤にならない（試験が拾わない）"
# 7 G5 の式が 853 と同じ（コメント以外の G5 を含む行の集合）
diff <(grep -v '^ *#' "$R/scripts/instruments/c70gt/gates_ss853.py" | grep 'G5\|g5') <(grep -v '^ *#' "$SD/gates_ss860.py" | grep 'G5\|g5') > "$T/g5diff.txt" 2>&1 || nogo "[ss] G5 の式が 853 と違う：$(head -3 "$T/g5diff.txt")"
# 8 一つ抜きの空振りの率（記録）
$PY "$G/gates_po860.py" --loo $RAW/base_ins $RAW/n1 $RAW/n2 "$M/loo_po860.txt" 10 "$G/gates_po860_thresholds.tsv" > "$T/loo_po.out" 2>&1 || nogo "[po] 一つ抜きの実行が失敗"
$PY "$G/gates_ss860.py" --loo $RAW/base_ins $RAW/n1 $RAW/n2 "$M/loo_ss860.txt" 10 > "$T/loo_ss.out" 2>&1 || nogo "[ss] 一つ抜きの実行が失敗"
# 9 sha256 の確かめ（写した物が源と同じ）
for f in $FILES; do cmp -s "$SD/$f" "$G/$f" || nogo "[共通] $f の写しが源と違う"; done
if [ -n "$NOGO" ]; then echo "== 判定：no-go あり（G2b か道具の確かめが通らない＝c71po は腕を回さない）"; printf "$NOGO\n"; else echo "== 判定：po の門は使える（G2b は形 c・固定の盤面は予言どおり・旗 0 の盤面で働いた門が落ち壊さない門が通る・旗 1 の実の生で全ての門が通る・閾値の表は数え直しと一致）"; fi
echo "== G2b の形"; sed -n '/選んだ形/p' "$M/chooseForm_860.txt"
echo "== 旗 1 の実の生での結果（po・種 1〜10・閾値の合わせには使っていない）"; cat "$M/flag1_po860.txt"; cat "$M/flag1_forms860.txt"
echo "== 固定の盤面（po）"; head -14 "$M/mut_po860.txt"
echo "== 固定の盤面（ss）"; head -18 "$M/mut_ss860.txt"
echo "== 旗 0 の盤面（po・種 1〜10）"; cat "$M/flag0_po860.txt"
echo "== 旗 0 の盤面（ss・種 1〜2）"; head -12 "$M/flag0_ss860.txt"
echo "== 突然変異（減衰の項を外す）"; cat "$M/mut_decay860.txt"
echo "== T10（新）"; tail -4 "$M/t10_new.txt"; echo "== T10（旧＝赤になるはず）"; tail -4 "$M/t10_old.txt"
echo "== batch.sh の終了コードの試し（新）"; cat "$M/test_batch_rc860_new.txt"; echo "== 同（旧＝赤になるはず）"; cat "$M/test_batch_rc860_old.txt"
echo "== 一つ抜きの空振りの率（po）"; cat "$M/loo_po860.txt"; echo "== 同（ss）"; tail -2 "$M/loo_ss860.txt"
echo "== 形の選び"; cat "$M/chooseForm_860.txt"
echo "== sha256"; cat "$G/SHA256.txt"
[ -z "$NOGO" ]
