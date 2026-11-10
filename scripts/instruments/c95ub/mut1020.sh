#!/usr/bin/env bash
# (1020) c95ub 突然変異（事前登録 measurements/m1020_20261110_prereg_ub.txt [6]）：使い捨ての木で 1 つずつ入れ、器の試験と RecipeoutInstrumentTests.T3 を回す。
#   使い方: bash mut1020.sh <器のコミット> <使い捨ての木（D:/ の形）> <出力フォルダ（リポジトリの measurements/m1020_20261110）>
#   出力：<出力>/mut_<名>.txt（試験の出力の全文）・<出力>/mut_summary.txt（突然変異ごとの合格・失敗の行）。
set -u
C=${1:?コミット}; T=${2:?木}; O=${3:?出力}
R="$(cd "$(dirname "$0")/../../.." && pwd)"
[ -d "$T" ] || git -C "$R" worktree add --detach "$T" "$C" > /dev/null 2>&1 || { echo "worktree の作成に失敗"; exit 2; }
git -C "$T" checkout -q --detach "$C"
mkdir -p "$O"; : > "$O/mut_summary.txt"
run() {   # $1＝名・$2…＝python の置き換え（ファイル 古い 新しい の 3 つ組）
  local name=$1; shift
  git -C "$T" checkout -q -- PriceOfBread.SimCore
  if [ $# -gt 0 ]; then python -X utf8 -I - "$T" "$@" <<'PY' || { echo "$name 置き換え失敗" >> "$O/mut_summary.txt"; return; }
import sys
t = sys.argv[1]; a = sys.argv[2:]
for i in range(0, len(a), 3):
    p = t + '/' + a[i]; s = open(p, 'rb').read().decode('utf-8')
    assert s.count(a[i + 1]) >= 1, (a[i], a[i + 1][:60])
    s = s.replace(a[i + 1], a[i + 2], 1); open(p, 'wb').write(s.encode('utf-8'))
PY
  fi
  dotnet test "$T/PriceOfBread.Tests" -c Release --artifacts-path D:/PoB_work/c95ub_work/art_mut --filter "FullyQualifiedName~UbiquitousMineralTests|FullyQualifiedName~RecipeoutInstrumentTests.T3" > "$O/mut_$name.txt" 2>&1
  echo "== $name rc=$? $(grep -E '失敗!|成功!|合格!' "$O/mut_$name.txt" | tail -1)" >> "$O/mut_summary.txt"
  grep -E '^ *失敗 ' "$O/mut_$name.txt" | sed 's/^ *//' >> "$O/mut_summary.txt"
}
S=PriceOfBread.SimCore
run M0none
run M0 $S/Catalog.cs "=> c.UbiquitousMineralExempt > 0 && id is LimestoneId" "=> false && id is LimestoneId"
run M1 $S/Simulation.cs "plat, plon) && !Catalog.IsUbiquitousMineral(pr.GoodId, coef))" "plat, plon) && !(coef.UbiquitousMineralExempt > 0))" \
       $S/Simulation.cs "clat, clon) && !Catalog.IsUbiquitousMineral(pr.GoodId, coef))" "clat, clon) && !(coef.UbiquitousMineralExempt > 0))"
run M2 $S/Simulation.cs "clat, clon) && !Catalog.IsUbiquitousMineral(pr.GoodId, coef))" "clat, clon))"
run M3 $S/Simulation.cs "ApplyEndowment(pr, ScarceEndowment(pr.GoodId, coef, sharedWorld) * coef.ResourceEndowmentScale, coef);" "ApplyEndowment(pr, (Catalog.IsUbiquitousMineral(pr.GoodId, coef) ? 1.0 : ScarceEndowment(pr.GoodId, coef, sharedWorld)) * coef.ResourceEndowmentScale, coef);"
run M4 $S/Catalog.cs "id is LimestoneId or SilicaSandId or ClayId;" "id is LimestoneId;"
run M5 $S/InstrumentsRecipeout.cs "lon) && !Catalog.IsUbiquitousMineral(g, c)) {" "lon)) {"
git -C "$T" checkout -q -- PriceOfBread.SimCore
echo MUTDONE >> "$O/mut_summary.txt"
