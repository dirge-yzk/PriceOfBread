#!/usr/bin/env bash
# (853) c70gt 受け渡しの自己試験：直した門の台本（gates_ss853.py・gates_po853.py）を D:/PoB_work/c70gt_work/gates/ に写して sha256 を書き、
#   固定の盤面（合成）の突然変異の表・旗 0 の盤面（実の keep_852/base_ins を腕に見立てる）・一つ抜きの空振りの率を回す。
#   呼び方（担当）：bash scripts/instruments/tools/bg.sh D:/PoB_work/c70gt_work/gates_ready.mark bash scripts/instruments/c70gt/selftest853.sh
#   出力＝標準出力（bg.sh が .log に書く）。先頭に判定（no-go の門の名と理由）・末尾に sha256。rc≠0 なら no-go あり。
#   写した台本の sha256 は事前登録に引く（挙動の線の担当）。
export PYTHONUTF8=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
R=$(cd "$SD/../../.." && pwd)
G=${C70GT_GATES:-D:/PoB_work/c70gt_work/gates}
M=${C70GT_OUT:-$R/measurements/m853_20261105}
RAW=${C70GT_RAW:-D:/PoB_work/keep_852}
T=${C70GT_TMP:-D:/PoB_work/c70gt_work/selftest}
mkdir -p "$G" "$M" "$T"
FILES="gates_ss853.py gates_po853.py gates_ss853_thresholds.tsv gates_po853_thresholds.tsv mut_ss853.py mut_po853.py"
for f in $FILES; do cp "$SD/$f" "$G/$f" || exit 2; done
( cd "$G" && sha256sum $FILES > SHA256.txt ) || exit 2
NOGO=""
nogo() { NOGO="$NOGO\n$1"; }
PY="python -X utf8 -I"
# 1 固定の盤面（合成）の突然変異の表
$PY "$G/mut_ss853.py" "$T/mutss" "$M/mut_ss853.txt" "$G/gates_ss853_thresholds.tsv" > "$T/mut_ss.out" 2>&1 || nogo "[ss] 固定の盤面の突然変異の表が予言どおりでない（$M/mut_ss853.txt）"
$PY "$G/mut_po853.py" "$T/mutpo" "$M/mut_po853.txt" 120 > "$T/mut_po.out" 2>&1 || nogo "[po] 固定の盤面の突然変異の表が予言どおりでない（$M/mut_po853.txt）"
# 2 旗 0 の盤面（実の生・基線を腕に見立てる）：働いた門が落ちる
$PY "$G/gates_ss853.py" $RAW/base_ins $RAW/base_ins $RAW/n1 $RAW/n2 "$M/flag0_ss853.txt" 2 1 "$G/gates_ss853_thresholds.tsv" > "$T/flag0_ss.out" 2>&1 || nogo "[ss] 旗 0 の盤面の実行が失敗"
J=$(grep '^門の判定' "$M/flag0_ss853.txt")
for g in G1a G1b G2 G3a G4; do case "$J" in *"$g=落"*) ;; *) nogo "[ss] 旗 0 の盤面で $g が落ちない（働いた門が旗 0 でも緑）：$J";; esac; done
$PY "$G/gates_po853.py" $RAW/base_ins $RAW/base_ins $RAW/n1 $RAW/n2 "$M/flag0_po853.txt" 10 1 "$G/gates_po853_thresholds.tsv" > "$T/flag0_po.out" 2>&1 || nogo "[po] 旗 0 の盤面の実行が失敗"
J=$(grep '^門の判定' "$M/flag0_po853.txt")
for g in G0 G1; do case "$J" in *"$g=落"*) ;; *) nogo "[po] 旗 0 の盤面で $g が落ちない：$J";; esac; done
# 閾値の表の W_C と、今 3 本の旗 0 の生から数えた W_C の一致
WC_T=$(grep '^W_C' "$G/gates_po853_thresholds.tsv" | cut -f2)
WC_N=$(grep -o 'W_C [0-9.e+-]*' "$M/flag0_po853.txt" | head -1 | cut -d' ' -f2)
$PY -c "import sys; a=float('$WC_T'); b=float('$WC_N'); sys.exit(0 if abs(a-b) <= 1e-6*max(abs(a),1e-12) else 1)" || nogo "[po] 閾値の表の W_C $WC_T と数え直した W_C $WC_N が違う"
grep -q '^no_go' "$G/gates_po853_thresholds.tsv" && nogo "[po] 窓の長さを選べなかった（G2b は門にしない）"
# 3 一つ抜きの空振りの率（記録）
$PY "$G/gates_ss853.py" --loo $RAW/base_ins $RAW/n1 $RAW/n2 "$M/loo_ss853.txt" 10 > "$T/loo_ss.out" 2>&1 || nogo "[ss] 一つ抜きの実行が失敗"
# 4 sha256 の確かめ（写した物が源と同じ）
for f in $FILES; do cmp -s "$SD/$f" "$G/$f" || nogo "[共通] $f の写しが源と違う"; done
if [ -n "$NOGO" ]; then echo "== 判定：no-go あり（ss と po の門のどれかが使えない）"; printf "$NOGO\n"; else echo "== 判定：ss と po の門は使える（固定の盤面は予言どおり・旗 0 の盤面で働いた門が落ちる・閾値の表は数え直しと一致）"; fi
echo "== 固定の盤面（ss）"; cat "$M/mut_ss853.txt" | head -16
echo "== 固定の盤面（po）"; head -14 "$M/mut_po853.txt"
echo "== 旗 0 の盤面（ss・種 1〜2）"; head -12 "$M/flag0_ss853.txt"
echo "== 旗 0 の盤面（po・種 1〜10）"; cat "$M/flag0_po853.txt"
echo "== 一つ抜きの空振りの率（ss・記録）"; cat "$M/loo_ss853.txt"
echo "== 窓の選び（po）"; cat "$M/chooseL_853.txt"
echo "== sha256"; cat "$G/SHA256.txt"
[ -z "$NOGO" ]
