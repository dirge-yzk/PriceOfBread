#!/usr/bin/env bash
# (889) c75cp 試験の突然変異（コミットしない・1 本ずつ当てて CrownPcInstrumentTests を回し、ファイルを git checkout で戻す）。
#   bash mut889.sh <木> <出力フォルダ>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; O=${2:?出力}; SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; R1="$SP/../tools/replace1.py"
CS=PriceOfBread.SimCore/InstrumentsCrown.cs; TS=PriceOfBread.Tests/CrownPcInstrumentTests.cs
mkdir -p "$O"; cd "$T" || exit 9
[ -z "$(git status --porcelain -- "$CS" "$TS")" ] || { echo "未コミットの変更がある"; exit 8; }
one() {   # one <名> <ファイル> <旧> <新>
  git checkout -q -- "$CS" "$TS"
  python -I "$R1" "$2" "$3" "$4" || { echo "$1 置き換え失敗" | tee "$O/mut_$1.txt"; return; }
  { echo "突然変異 $1（木 $(git rev-parse --short HEAD)）"; git diff "$2" | grep '^[-+][^-+]'
    dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c75cp_work/art_mut --disable-build-servers --filter "FullyQualifiedName~CrownPcInstrumentTests" 2>&1 | grep -E "^\s+失敗 |成功!|失敗!|合計|error CS" | head -30; } > "$O/mut_$1.txt" 2>&1
  echo "$1 done"
}
one M1_summon2x "$CS" 'double raw = pc + c.SummonParliamentRate * (x.Austerity - 0.5) * 2.0;' 'double raw = pc + 2 * c.SummonParliamentRate * (x.Austerity - 0.5) * 2.0;'
one M2_noclamp "$CS" 'r.SummonClamp = raw > 1;\n            pc = RuleClamp01(raw);' 'r.SummonClamp = raw > 1;\n            pc = raw;'
one M3_t1gate "$CS" 'if (c.ParliamentPressureRate > 0 && r.T1Support && r.T1Coerce)' 'if (c.ParliamentPressureRate > 0 && r.T1Support)'
one M4_crownguard "$CS" 'if (c.SummonParliamentRate > 0 && r.RbCrown && r.RbCoop && r.RbLeg && r.RbArmy)' 'if (c.SummonParliamentRate > 0 && r.RbCoop && r.RbLeg && r.RbArmy)'
one M5_nowsnap "$CS" 'prev.Coop, prev.Army, row.X.CurPrev);' 'w.CoopParliamentCrown, w.ArmyAllegiance, row.X.CurPrev);'
one M5b_nowpc "$CS" 'CrownApply(row.X, c, prev.Pc, prev.Polity' 'CrownApply(row.X, c, w.ParliamentControl, prev.Polity'
one M6_crosstol "$TS" 'internal const double CrossTol = 1e-12;' 'internal const double CrossTol = 1;'
git checkout -q -- "$CS" "$TS"
echo MUTDONE
