#!/usr/bin/env bash
# (1111) IL の門：前（SimCore.dll）と後（SimCore.dll ∪ Display.dll）の IL の写しを比べる。
#   bash ilgate.sh <前のコミット> <後のコミット> [<出力フォルダ>]    同じ絶対パスの木 D:/PoB_work/wt_c107di_il で 2 回建てる（木は git worktree の使い捨て）。
# 出力：before.txt・after.txt・gate.txt（各門の合否）。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
BEFORE="$1"; AFTER="$2"; OUT="${3:-D:/PoB_work/c107di_work/il}"
REPO=C:/Users/dirge/Documents/PriceOfBread; TREE=D:/PoB_work/wt_c107di_il
ILD=D:/PoB_work/c107di_work/ild_art/bin/IlDump/release/IlDump.dll
mkdir -p "$OUT"
[ -d "$TREE" ] || git -C "$REPO" worktree add --detach "$TREE" "$BEFORE" > "$OUT/wt.txt" 2>&1
build() {   # $1 コミット $2 タグ $3.. 建てるプロジェクト
  local c="$1" tag="$2"; shift 2
  git -C "$TREE" checkout -q --detach "$c" || return 1
  git -C "$TREE" clean -fdxq -e nothing 2>/dev/null
  rm -rf "$OUT/art_$tag"
  for p in "$@"; do
    dotnet build "$TREE/$p/$p.csproj" -c Release -p:Deterministic=true -p:DebugType=none -p:IncludeSourceRevisionInInformationalVersion=false --artifacts-path "$OUT/art_$tag" --disable-build-servers > "$OUT/build_${tag}_$p.txt" 2>&1 || { echo "build 失敗 $tag $p"; return 1; }
  done
}
build "$BEFORE" before PriceOfBread.SimCore || exit 1
dotnet "$ILD" "$OUT/before.txt" "$OUT/art_before/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll" || exit 1
build "$AFTER" after PriceOfBread.SimCore PriceOfBread.Display || exit 1
dotnet "$ILD" "$OUT/after.txt" "$OUT/art_after/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll" "$OUT/art_after/bin/PriceOfBread.Display/release/PriceOfBread.Display.dll" || exit 1
dotnet "$ILD" "$OUT/after_simcore_only.txt" "$OUT/art_after/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll" || exit 1
dotnet "$ILD" "$OUT/after_display_only.txt" "$OUT/art_after/bin/PriceOfBread.Display/release/PriceOfBread.Display.dll" || exit 1
python -X utf8 -I "$(dirname "$0")/ilcompare.py" "$OUT" > "$OUT/gate.txt" 2>&1; echo "rc=$?" >> "$OUT/gate.txt"
