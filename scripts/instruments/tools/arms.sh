#!/usr/bin/env bash
# (728) 同時に走る腕の数の管理（D: の印のファイル＝全ての worktree と担当で共有）。
#   bash arms.sh acquire <腕の名> [pid]   空きがあれば印を置いて 0・満杯なら置かずに 3 を返す（待たない＝空きを待つ側は waitmark.sh か arms.sh free を Monitor の終了条件に使う）
#   bash arms.sh release <腕の名> [pid]   自分の印を外す
#   bash arms.sh list                     生きている腕の一覧（死んだ pid の印・24 時間より古い印は消す）
#   bash arms.sh free                     空きの数を出す
# 上限＝POB_MAX_ARMS（既定 6・論理コア 16 で 1 腕が平均 2.7 コア）・印の場所＝POB_ARMS_DIR（既定 /d/PoB_work/.arms）。
# 順位は印の名前（置いた時刻の ns 値＋腕の名）の昇順＝同時に置いても先着の MAX 本だけが残る（置いた後に数え直し、はみ出したら自分の印を外す）。
D="${POB_ARMS_DIR:-/d/PoB_work/.arms}"; MAX="${POB_MAX_ARMS:-6}"
mkdir -p "$D" || exit 1
live() {   # 生きている印を昇順に出す（死んだ・古い印は消す）
  local f pid
  for f in "$D"/*.slot; do
    [ -e "$f" ] || continue
    pid=$(sed -n 1p "$f")
    if [ -n "$(find "$f" -mmin +1440 2>/dev/null)" ] || ! kill -0 "$pid" 2>/dev/null; then rm -f "$f"; else echo "$f"; fi
  done | sort
}
case "${1:-}" in
  acquire)
    N=${2:?腕の名}; P=${3:-$PPID}
    ME="$D/$(date +%s%N)_${N}_${P}.slot"
    printf '%s\n%s\n%s\n' "$P" "$N" "$(date '+%F %T')" > "$ME"
    if live | head -n "$MAX" | grep -qxF "$ME"; then echo "ARMS 取得 $N（上限 $MAX）"; exit 0; fi
    rm -f "$ME"; echo "ARMS 満杯（上限 $MAX・POB_MAX_ARMS で変える）：走っている腕"; bash "$0" list | sed 's/^/  /'; exit 3 ;;
  release) rm -f "$D"/*_"${2:?腕の名}"_"${3:-$PPID}".slot ;;
  list) for f in $(live); do echo "$(sed -n 2p "$f")	pid $(sed -n 1p "$f")	$(sed -n 3p "$f")"; done ;;
  free) n=$(live | wc -l); echo $((MAX > n ? MAX - n : 0)) ;;
  *) echo "使い方：arms.sh acquire|release|list|free"; exit 2 ;;
esac
