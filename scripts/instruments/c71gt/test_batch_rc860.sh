#!/usr/bin/env bash
# (860) c71gt：batch.sh の終了コードの試し（事前登録 [1-5]）。偽の measure.sh を置いた作業フォルダで run を回し、終了コードを確かめる。
#   bash test_batch_rc860.sh [batch.sh のパス=この台本の2つ上の batch.sh] [作業フォルダ=D:/PoB_work/c71gt_work/t_batch]
#   本物の measure.sh は呼ばない（偽の measure.sh が成績表を書くだけ）。腕の印は専用の置き場（POB_ARMS_DIR）で他の腕に触れない。
#   突然変異＝旧の batch.sh（git show 62190ef0:scripts/instruments/batch.sh）を同じ場所に置いて回すと〈rc 1＋成績表あり→0〉の行が赤になる。
export PYTHONUTF8=1
SD=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
BATCH=${1:-$SD/../batch.sh}
T=${2:-D:/PoB_work/c71gt_work/t_batch}
BSP=$(cd "$(dirname "$BATCH")" && pwd)
rm -rf "$T"; mkdir -p "$T/arms"
FAIL=0
# 偽の measure.sh：FAKE_MODE で振る舞いを替える
mk() {   # mk <作業フォルダ>
  mkdir -p "$1/src_x"
  cat > "$1/src_x/measure.sh" <<'EOS'
#!/usr/bin/env bash
OUT="$1"
case "${FAKE_MODE:-}" in
  rc1_table)   printf 'x\n\n  判定: ★硬性ゲートに赤（終了コード1＝差し戻しの理由）\n' > "$OUT"; exit 1 ;;
  rc0_table)   printf 'x\n\n  判定: 全て通る\n' > "$OUT"; exit 0 ;;
  rc1_notable) exit 1 ;;
  rc0_truncated) printf 'x\n途中まで\n' > "$OUT"; exit 0 ;;
  rc1_stale)   exit 1 ;;
esac
EOS
}
case_run() {   # case_run <名> <FAKE_MODE> <期待する終了コード> [env...]
  local name=$1 mode=$2 want=$3; shift 3
  local W="$T/w_$name"; mkdir -p "$W"; mk "$W"
  if [ "$mode" = rc1_stale ]; then printf 'old\n  判定: 前の走りの成績表\n' > "$W/b3_x_3.txt"; fi
  env POB_WORK="$W" POB_ARMS_DIR="$T/arms" FAKE_MODE="$mode" "$@" bash "$BATCH" run x 3 > "$W/out.txt" 2>&1
  local rc=$?
  if [ "$rc" = "$want" ]; then echo "○ $name：終了コード $rc（期待 $want）"; else echo "✗ $name：終了コード $rc（期待 $want）"; FAIL=1; fi
}
case_run rc1_table  rc1_table   0
case_run rc0_table  rc0_table   0
case_run rc1_notable rc1_notable 4
case_run rc0_truncated rc0_truncated 4
case_run rc1_stale  rc1_stale   4
case_run armfull    rc1_table   3 POB_MAX_ARMS=0
case_run ins_nonations rc1_table 2 INS=1 POB_INSTRUMENTS_NATIONS=
# measure rc がログに残る
grep -q 'measure rc=1' "$T/w_rc1_table/ms_x_3.log" && echo "○ ms ログに measure rc=1" || { echo "✗ ms ログに measure rc=1 が無い"; FAIL=1; }
grep -q 'BATCHFAIL' "$T/w_rc1_notable/ms_x_3.log" && echo "○ 成績表なしは ms ログに BATCHFAIL" || { echo "✗ BATCHFAIL が無い"; FAIL=1; }
# 成績表の中身（偽の measure.sh が書いた物）を batch.sh が変えない
cmp -s <(printf 'x\n\n  判定: ★硬性ゲートに赤（終了コード1＝差し戻しの理由）\n') "$T/w_rc1_table/b3_x_3.txt" && echo "○ 成績表は変えない" || { echo "✗ 成績表が変わった"; FAIL=1; }
[ "$BSP" != "" ] && [ "$FAIL" = 0 ] && echo "== 判定：終了コードは予言どおり" || echo "== 判定：予言と違う所あり"
exit $FAIL
