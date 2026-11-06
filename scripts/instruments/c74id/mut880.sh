#!/usr/bin/env bash
# (880) c74id 試験の突然変異（使い捨ての木・コミットしない）：M3b・MB1・MB2 を 1 本ずつ当てて CearnTradeColumnsTests を回す。M3b は (874) の試験（496d5ec8）でも回す（緑のままの見込み）。
#   bash mut880.sh <使い捨ての木> <出力フォルダ>
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH" AVALONIA_TELEMETRY_OPTOUT=1
T=${1:?木}; O=${2:?出力}; SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; R1="$SP/../tools/replace1.py"
CS=PriceOfBread.SimCore/InstrumentsCearn.cs; TS=PriceOfBread.Tests/CearnTradeColumnsTests.cs
mkdir -p "$O"; cd "$T" || exit 9
one() {   # one <名> <旧> <新> [old-test]
  git checkout -q -- .
  python -I "$R1" "$CS" "$2" "$3" || { echo "$1 置き換え失敗"; return; }
  [ "${4:-}" = old-test ] && git show 496d5ec8:$TS > $TS
  { echo "突然変異 $1（木 $(git rev-parse --short HEAD)・${4:-新しい試験}）"; git diff --stat; git diff "$CS" | grep '^[-+][^-+]'
    dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c74id_work/art_mut --disable-build-servers --filter "FullyQualifiedName~CearnTradeColumnsTests" 2>&1 | grep -E "^\s+失敗 |成功!|失敗!|合計|error CS|エラー メッセージ" -A1 | head -30; } > "$O/mut_$1.txt" 2>&1
  echo "$1 done"
}
one M3b 'else foreach (var p in r.Pops) ob += System.Math.Max(0, p.Savings.Value);' 'else foreach (var p in r.Pops) if (p.Size > 1e-12) ob += System.Math.Max(0, p.Savings.Value);'
one M3b_oldtest 'else foreach (var p in r.Pops) ob += System.Math.Max(0, p.Savings.Value);' 'else foreach (var p in r.Pops) if (p.Size > 1e-12) ob += System.Math.Max(0, p.Savings.Value);' old-test
one MB1 'if (!a.ImpClrDead && exSub > a.ImpExMaxSubLive)' 'if (exSub > a.ImpExMaxSubLive)'
one MB2 'a.ImpClrDead = false;   // (880) 今の清算の印を戻す' ';   // (880) 突然変異 MB2'
git checkout -q -- .
echo MUTDONE
