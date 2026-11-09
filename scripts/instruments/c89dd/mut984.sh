#!/usr/bin/env bash
# (984) c89dd 突然変異（使い捨ての木）：事前登録 [2] の M1〜M6 を 1 本ずつ当て、TakeDustFloorTests と HeirDebtSettledMovedTests の赤と緑を書く。
#   bash mut984.sh <使い捨ての木（git worktree・D:/ の形）> <隔離ビルドの置き場> <出力ファイル>
#   置き換えは tools/replace1.py（ちょうど 1 回のときだけ）・当てた後は git checkout で戻す。
T=${1:?木}; ART=${2:?art}; OUT=${3:?出力}
R1="python -I $(cd "$(dirname "$0")/../tools" && pwd)/replace1.py"
M=PriceOfBread.SimCore/Model.cs; WD=PriceOfBread.SimCore/World.cs
run() {   # $1＝名・$2＝ファイル・$3＝旧・$4＝新
  ( cd "$T" && $R1 "$2" "$3" "$4" ) > /dev/null || { echo "$1	置き換え失敗" >> "$OUT"; return; }
  ( cd "$T" && dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" -p:UseSharedCompilation=false \
      --filter "FullyQualifiedName~TakeDustFloorTests|FullyQualifiedName~HeirDebtSettledMovedTests" ) > "$OUT.$1.log" 2>&1
  echo "== $1（$2）" >> "$OUT"
  grep -E "^\s*(失敗|Failed) " "$OUT.$1.log" | sed -E 's/^\s+//' | sort >> "$OUT"
  grep -E "合計:|Total tests|成功!|失敗!" "$OUT.$1.log" | tail -1 >> "$OUT"
  ( cd "$T" && git checkout -q -- "$2" )
}
: > "$OUT"
run M1_floor_back "$M" "? 0 : 1e-12" "? 1e-12 : 1e-12"
run M2_floor_1e-15 "$M" "? 0 : 1e-12" "? 1e-15 : 1e-12"
run M3a_flag_unread_old "$M" "coef is not null && coef.TakeFloorPositive > 0 ?" "false ?"
run M3b_flag_unread_new "$M" "coef is not null && coef.TakeFloorPositive > 0 ?" "true ?"
run M4_held_floor_off "$M" " || !(held > 1e-9)) return 0;   // ★(984)" ") return 0;   // ★(984)"
run M5_world_no_coef "$WD" "fromHeld, from.W!.Coef);" "fromHeld, null);"
run M6_nan_guard "$M" "if (!(take > (coef is not null && coef.TakeFloorPositive > 0 ? 0 : 1e-12)) || " "if ((take <= (coef is not null && coef.TakeFloorPositive > 0 ? 0 : 1e-12)) || "
# 元の形（突然変異なし）を最後にもう 1 度：全て緑
( cd "$T" && dotnet test PriceOfBread.Tests -c Release --artifacts-path "$ART" -p:UseSharedCompilation=false \
    --filter "FullyQualifiedName~TakeDustFloorTests|FullyQualifiedName~HeirDebtSettledMovedTests" ) > "$OUT.orig.log" 2>&1
echo "== 元の形" >> "$OUT"; grep -E "合計:|Total tests|成功!|失敗!" "$OUT.orig.log" | tail -1 >> "$OUT"
echo MUTDONE >> "$OUT"
