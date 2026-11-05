#!/usr/bin/env bash
# (867) c72gt 受け渡しの自己試験 g5：G5b／G5p の門の台本を D:/PoB_work/c72gt_work/gates/ に写して sha256 を書き、固定の盤面（合成）の突然変異の表・旗 0 の盤面（実の keep_852/base_ins を腕に見立てる）・
#   G5p の外す窓の数え直し・G4m の閾値の一致・G1a〜G4m の式の diff・G2b の突然変異（mut_po867.py の M8）を回し、最後に読み直し (a)（keep_859_rej/c70ssB_10）をする。
#   呼び方（担当）：bash scripts/instruments/tools/bg.sh D:/PoB_work/c72gt_work/g5_ready.mark bash scripts/instruments/c72gt/selftest867_g5.sh
#   出力＝標準出力（bg.sh が .log に書く）。先頭の行が VERDICT g5 pass|fail <理由>・末尾に sha256。fail のとき c72ss は腕を回さない。
#   読み直し (a) の通りは〈近似〉（定義が r864 の報告＝腕を見た後に由来）。通った場合も採否の決め手は c72ss の種 11〜20。
export PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
R=$(cd "$SD/../../.." && pwd)
G=${C72GT_GATES:-D:/PoB_work/c72gt_work/gates}
M=${C72GT_OUT:-$R/measurements/m867_20261106}
RAW=${C72GT_RAW:-D:/PoB_work/keep_852}
ARM=${C72GT_ARM:-D:/PoB_work/keep_859_rej/c70ssB_10/ins}
T=${C72GT_TMP:-D:/PoB_work/c72gt_work/selftest_g5}
SEEDS=${C72GT_SEEDS:-10}
mkdir -p "$G" "$M" "$T"
FILES="gates_ss867.py gates_ss867_thresholds.tsv mut_ss867.py gates_po867.py gates_po867_thresholds.tsv mut_po867.py"
for f in $FILES; do cp "$SD/$f" "$G/$f" || exit 2; done
( cd "$G" && sha256sum $FILES > SHA256.txt ) || exit 2
NOGO=""
nogo() { NOGO="$NOGO
$1"; }
PY="python -X utf8 -I"
BODY="$T/body.txt"; : > "$BODY"
say() { echo "$@" >> "$BODY"; }
# 1 固定の盤面（合成）の突然変異の表（ss と po の G2b の M8）
$PY "$G/mut_ss867.py" "$T/mutss" "$M/mut_ss867.txt" "$G/gates_ss867_thresholds.tsv" > "$T/mut_ss.out" 2>&1 || nogo "[ss] 固定の盤面の突然変異の表が予言どおりでない（$M/mut_ss867.txt）"
$PY "$G/mut_po867.py" "$T/mutpo" "$M/mut_po867.txt" 120 > "$T/mut_po.out" 2>&1 || nogo "[po] 固定の盤面（M8 を含む）の突然変異の表が予言どおりでない（$M/mut_po867.txt）"
# 2 旗 0 の盤面（実の生・基線を腕に見立てる）：G5b は通り、G5p は紙の予言どおり 1786-99 で落ちる
$PY "$G/gates_ss867.py" $RAW/base_ins $RAW/base_ins $RAW/n1 $RAW/n2 "$M/flag0_ss867.txt" $SEEDS 1 "$G/gates_ss867_thresholds.tsv" > "$T/flag0_ss.out" 2>&1 || nogo "[ss] 旗 0 の盤面の実行が失敗"
J=$(grep '^門の判定' "$M/flag0_ss867.txt")
case "$J" in *"G5b=通"*) ;; *) nogo "[ss] 旗 0 の盤面で G5b が通らない（壊さない門が旗 0 で赤）：$J";; esac
case "$J" in *"G5p=落"*) ;; *) nogo "[ss] 旗 0 の盤面で G5p が落ちない（働いた側の門が旗 0 でも緑＝区別力が無い）：$J";; esac
grep -q '窓 1786-99：.*→ 落ちる' "$M/flag0_ss867.txt" || nogo "[ss] 旗 0 の盤面で 1786-99 の窓が落ちない（紙の予言の外れ）"
for g in G1a G1b G2 G3a G4; do case "$J" in *"$g=落"*) ;; *) nogo "[ss] 旗 0 の盤面で $g が落ちない（働いた門が旗 0 でも緑）：$J";; esac; done
# 3 G5p の外す窓の数え直し（旗 0 の生だけ）：閾値の表の G5p_exclude_* と一致
$PY "$G/gates_ss867.py" --make-g5-thresholds $RAW/base_ins $RAW/n1 $RAW/n2 "$T/thr_recount.tsv" $SEEDS > "$T/thr.out" 2>&1 || nogo "[ss] 閾値の数え直しの実行が失敗"
for w in 1783-85 1786-99 1800-49 1850-99 1900-50; do
  a=$(grep "^G5p_exclude_$w	" "$G/gates_ss867_thresholds.tsv" | cut -f2); b=$(grep "^G5p_exclude_$w	" "$T/thr_recount.tsv" | cut -f2)
  [ -n "$a" ] && [ "$a" = "$b" ] || nogo "[ss] 閾値の表の G5p_exclude_$w ($a) と数え直し ($b) が違う"
done
# 4 G4m の閾値が 860 と同じ・G1a〜G4m の式が 860 と同じ（コメントと版の字を除く行の diff）
a=$(grep '^G4m_limit' "$R/scripts/instruments/c71gt/gates_ss860_thresholds.tsv" | cut -f2); b=$(grep '^G4m_limit' "$G/gates_ss867_thresholds.tsv" | cut -f2)
[ "$a" = "$b" ] || nogo "[ss] G4m の閾値が 860 と違う ($a / $b)"
FIL() { grep -v '^ *#' "$1" | grep 'G1a\|G1b\|G2\|G3a\|G4\|g1a\|g1b' | sed 's/(86[07])/(N)/g'; }
diff <(FIL "$R/scripts/instruments/c71gt/gates_ss860.py") <(FIL "$G/gates_ss867.py") > "$T/gdiff.txt" 2>&1 || nogo "[ss] G1a〜G4m の式の行が 860 と違う：$(head -3 "$T/gdiff.txt")"
cmp -s "$R/scripts/instruments/c71gt/gates_po860.py" "$G/gates_po867.py" || nogo "[po] gates_po867.py が gates_po860.py のバイト一致の写しでない"
# 5 G5p の閾値を旧の w に戻した突然変異（旗 0 の盤面の窓ごとの判定は 2 の出力の〈記録：旧の w の中央〉の列）
say "== G5p の閾値を旧の w へ戻した突然変異（旗 0 の盤面・窓ごとの判定の違い）"; grep '窓 17\|窓 18\|窓 19' "$M/flag0_ss867.txt" >> "$BODY"
# 6 G4m の分母の薄まり（旗 1 の生・読み直しの前・閾値の合わせには使わない）と、読み直し (a)
$PY "$G/gates_ss867.py" --basis-share "$ARM" $SEEDS > "$M/basis_share_867.txt" 2>&1 || nogo "[ss] G4m の分母の薄まりの記録が失敗"
$PY "$G/gates_ss867.py" "$ARM" $RAW/base_ins $RAW/n1 $RAW/n2 "$M/reread_a_c70ssB_867.txt" $SEEDS 1 "$G/gates_ss867_thresholds.tsv" > "$T/reread.out" 2>&1 || nogo "[ss] 読み直し (a) の実行が失敗"
JA=$(grep '^門の判定' "$M/reread_a_c70ssB_867.txt")
REREAD=pass
case "$JA" in "門の判定：全て通る"*) ;; *) REREAD=fail;; esac
# 7 まとめ
if [ -n "$NOGO" ]; then
  echo "VERDICT g5 fail 道具の確かめが通らない（c72ss は腕を回さない）：$(printf "$NOGO" | head -3 | tr '\n' ' ')"
elif [ "$REREAD" = fail ]; then
  echo "VERDICT g5 fail 読み直し (a)（c70ssB_10・近似）で門が落ちた：$JA（c70ss は 3 回の規則の最後で閉じる）"
else
  echo "VERDICT g5 pass 道具は使える・読み直し (a)（c70ssB_10・近似）で全ての門が通る：$JA"
fi
if [ -n "$NOGO" ]; then echo "== no-go"; printf "$NOGO\n"; fi
echo "== 名乗り：G5p の区別力（旗 0 の盤面で落ちる窓）は紙の予言どおり 1786-99 の 1 窓に集中する。1783-85 と 1800-49 は閾値の 3/4 以上に寄る（㉗）。G5b が拾うのは σ の外の経路だけ。一つ抜きは窓を外す力がほぼ無い。読み直し (a) の定義は腕を見た後の報告に由来＝通っても〈近似〉。"
echo "== 旗 0 の盤面（ss・種 $SEEDS）"; cat "$M/flag0_ss867.txt"
echo "== 読み直し (a) c70ssB_10（近似）"; cat "$M/reread_a_c70ssB_867.txt"
echo "== G4m の分母の薄まり（記録）"; cat "$M/basis_share_867.txt"
echo "== 固定の盤面（ss）"; head -16 "$M/mut_ss867.txt"
echo "== 固定の盤面（po・M8 は G2b だけが赤）"; head -14 "$M/mut_po867.txt"
echo "== G5p の外す窓"; grep '^G5p_exclude' "$G/gates_ss867_thresholds.tsv" | cut -c1-120
cat "$BODY"
echo "== sha256"; cat "$G/SHA256.txt"
[ -z "$NOGO" ]
