#!/usr/bin/env bash
# (490) 開幕の焼き込みの焼き直し：隔離ビルドの Runner の bakeopen を回して opening_bake.json の候補を書く。
#   使い方：bash scripts/instruments/hz4_490/bake.sh <出力.json> [種=1] [上限の四半期=200]
#   - 木の SOURCE-HASH は measure.sh の src_manifest と同じ数え方（*.cs *.json *.axaml *.csproj *.slnx・bin/obj/docs/dist/.vs/.git/measurements/uishot を除く）で求め、環境変数 POB_SOURCE_HASH で頭書きへ渡す。
#     ⚠ 木には今の opening_bake.json（埋め込みの表）が入っている＝焼き直しのたびに頭書きの sourceHash は変わる（表の本文が同じかは sourceHash の行と generated の行〔UTC の日付＝日をまたぐと変わる〕を除いて比べる・(491) 反証役 hz4〔v〕低#6）。
#   - 出力の隣に <出力>.series.tsv（試走の四半期ごとの能力の和と日用品の値段の中央値＝収束の判定の入力）。
#   - 焼いたら PriceOfBread.SimCore/opening_bake.json へ置いて再ビルド（旗は倒さない）。読み込みの旗は Coefficients.OpeningBakeEnabled（POB_COEF="OpeningBakeEnabled=1"）。
#   - 環境変数 POB_WORK（作業フォルダ・D: 側）が必須。隔離ビルドは $POB_WORK/bake_build。
set -u
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
export PYTHONUTF8=1
OUT="${1:?出力.json を指定してください}"; SEED="${2:-1}"; CAP="${3:-200}"
W="${POB_WORK:?POB_WORK（作業フォルダ・D: 側）を指定してください}"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="${POB_REPO:-$(cd "$SP/../../.." && pwd)}"
HASH=$( cd "$REPO" && find . \( -name bin -o -name obj -o -name docs -o -name dist -o -name .vs -o -name .git -o -name measurements -o -name uishot \) -prune \
      -o -type f \( -name '*.cs' -o -name '*.json' -o -name '*.axaml' -o -name '*.csproj' -o -name '*.slnx' \) -print | sort | xargs sha256sum | sha256sum | cut -c1-16 )
echo "SOURCE-HASH=$HASH"
mkdir -p "$W"
dotnet build "$REPO/PriceOfBread.Runner/PriceOfBread.Runner.csproj" -c Release --artifacts-path "$W/bake_build" -v q || exit 1
POB_SOURCE_HASH="$HASH" dotnet "$W/bake_build/bin/PriceOfBread.Runner/release/PriceOfBread.Runner.dll" bakeopen "$OUT" "$SEED" "$CAP" "$OUT.series.tsv"
