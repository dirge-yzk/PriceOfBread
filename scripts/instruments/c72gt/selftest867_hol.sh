#!/usr/bin/env bash
# (867) c72gt 受け渡しの自己試験 hol：持ち主が分かれた州の振り分け（holsplit867.py）を D:/PoB_work/c72gt_work/gates/ に写して sha256 を書き、固定の盤面（合成）の突然変異の表・
#   実のヌル腕（keep_852 の M′・N1・N2）の an_h2 -v4 の数え直し・較正の記録・読み直し (c)（keep_866_rej/c71poB_10・近似）を回す。
#   呼び方（担当）：bash scripts/instruments/tools/bg.sh D:/PoB_work/c72gt_work/hol_ready.mark bash scripts/instruments/c72gt/selftest867_hol.sh
#   出力＝標準出力（bg.sh が .log に書く）。先頭の行が VERDICT hol pass|fail <理由>・末尾に sha256。fail のとき c72po は腕を回さない。
#   実のヌル腕には 1/10 が 0 件なので、ヌルを腕に見立てた試験は空振りしか言えない（区別力の証拠にならない）。区別力は合成の盤面の表で見る。
#   名乗る限界：ヌルどうしで持ち主が割れる州では、線の効きと雑音を分けられない（L2 は〈ヌルの幅の内〉の意味）。
export PYTHONUTF8=1 PYTHONDONTWRITEBYTECODE=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
R=$(cd "$SD/../../.." && pwd)
G=${C72GT_GATES:-D:/PoB_work/c72gt_work/gates}
M=${C72GT_OUT:-$R/measurements/m867_20261106}
RAW=${C72GT_RAW:-D:/PoB_work/keep_852}
ARM=${C72GT_HARM:-D:/PoB_work/keep_866_rej/c71poB_10/ins}
T=${C72GT_HTMP:-D:/PoB_work/c72gt_work/selftest_hol}
mkdir -p "$G" "$M" "$T"
FILES="holsplit867.py mut_hol867.py"
for f in $FILES; do cp "$SD/$f" "$G/$f" || exit 2; done
( cd "$G" && sha256sum $FILES > SHA256_hol.txt ) || exit 2
NOGO=""
nogo() { NOGO="$NOGO
$1"; }
PY="python -X utf8 -I"
# 1 固定の盤面（合成）
$PY "$G/mut_hol867.py" "$T/mut" "$M/mut_hol867.txt" > "$T/mut.out" 2>&1 || nogo "[hol] 固定の盤面の表が予言どおりでない（$M/mut_hol867.txt）"
# 2 an_h2 -v4 の数え直し（ヌル 3 本と腕）
for n in "M:base_ins" "N1:n1" "N2:n2"; do
  k=${n%%:*}; d=${n##*:}
  python -X utf8 "$R/scripts/instruments/hol/an_h2.py" $RAW/$d -v4 > "$T/an_$k.txt" 2>&1 || nogo "[hol] an_h2 が失敗（$k）"
done
python -X utf8 "$R/scripts/instruments/hol/an_h2.py" "$ARM" -v4 > "$T/an_arm.txt" 2>&1 || nogo "[hol] an_h2 が失敗（腕）"
# 3 実のヌル腕（N1 を腕に見立てる）：1/10 が 0 件＝空振りしか言えない
$PY "$G/holsplit867.py" "$T/an_N1.txt" $RAW/n1 "$M/hol_null_N1_867.txt" --null "$T/an_M.txt" $RAW/base_ins --null "$T/an_N1.txt" $RAW/n1 --null "$T/an_N2.txt" $RAW/n2 > "$T/null.out" 2>&1 || true
NNULL=$(grep -c '^L[0-3]' "$M/hol_null_N1_867.txt" 2>/dev/null)
# 4 較正の記録
$PY "$G/holsplit867.py" --calib "$T/an_M.txt" $RAW/base_ins "$T/an_N1.txt" $RAW/n1 "$T/an_N2.txt" $RAW/n2 "$M/hol_calib_867.txt" 10 > "$T/calib.out" 2>&1 || nogo "[hol] 較正の記録が失敗"
# 5 読み直し (c)：c71poB_10（近似）・holclass839 の数を並べる
$PY "$G/holsplit867.py" "$T/an_arm.txt" "$ARM" "$M/hol_reread_c_c71poB_867.txt" --null "$T/an_M.txt" $RAW/base_ins --null "$T/an_N1.txt" $RAW/n1 --null "$T/an_N2.txt" $RAW/n2 > "$T/reread.out" 2>&1 || nogo "[hol] 読み直し (c) の実行が失敗"
$PY "$R/scripts/instruments/m839/holclass839.py" "$T/an_arm.txt" "$T/an_M.txt" "$T/an_N1.txt" "$T/an_N2.txt" > "$M/hol_holclass839_c71poB_867.txt" 2>&1
RV=$(grep '^VERDICT hol' "$M/hol_reread_c_c71poB_867.txt")
if [ -n "$NOGO" ]; then
  echo "VERDICT hol fail 道具の確かめが通らない（c72po は腕を回さない）：$(printf "$NOGO" | head -3 | tr '\n' ' ')"
else
  case "$RV" in
    "VERDICT hol pass"*) echo "VERDICT hol pass 道具は使える・読み直し (c)（c71poB_10・近似）：${RV#VERDICT hol pass }＝弱い pass：$(sed -n 1p "$M/hol_calib_867.txt")／$(sed -n 2p "$M/hol_calib_867.txt")（割れる州は広く、ヌルの 1/4 事象に濃縮が無ければ L2 の免除は広い＝採否の決め手は c72po の種 11〜20 の止める線）";;
    *) echo "VERDICT hol fail 読み直し (c)（c71poB_10・近似）で L3 が残る：${RV#VERDICT hol fail }（c71po は 3 回の規則の最後で閉じる）";;
  esac
fi
if [ -n "$NOGO" ]; then echo "== no-go"; printf "$NOGO\n"; fi
echo "== 名乗り：実のヌル腕を腕に見立てた試験は 1/10 が 0 件（今回 ${NNULL:-0} 行）＝空振りしか言えない。区別力は合成の盤面の表（peak／late／nocmp）で見る。L2 はヌルの幅の内の意味で、線の寄与が 0 とは言えない。読み直し (c) の規則は旗 0 とヌル腕の生だけから導いた（腕の行は事前登録のコミットの後に初めて開いた）。"
echo "== 読み直し (c) c71poB_10（近似）"; cat "$M/hol_reread_c_c71poB_867.txt"
echo "== holclass839 の数"; cat "$M/hol_holclass839_c71poB_867.txt"
echo "== 固定の盤面（合成）"; cat "$M/mut_hol867.txt"
echo "== 較正の記録"; cat "$M/hol_calib_867.txt"
echo "== 実のヌル腕（N1 を腕に見立てた）"; cat "$M/hol_null_N1_867.txt"
echo "== sha256"; cat "$G/SHA256_hol.txt"
[ -z "$NOGO" ]
