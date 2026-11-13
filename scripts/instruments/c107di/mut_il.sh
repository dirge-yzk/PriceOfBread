#!/usr/bin/env bash
# (1111) IL の門の突然変異（使い捨ての木 D:/PoB_work/wt_c107di_il を後のコミットに合わせてから実行）。
#   M1 Display に空の型を 1 つ足す／M4 SimCore に Display の 1 本（PowerLabel.cs）を写しで残す。どちらも ilcompare が赤になること。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
OUT="${1:-D:/PoB_work/c107di_work/il}"; TREE=D:/PoB_work/wt_c107di_il; ILD=D:/PoB_work/c107di_work/ild_art/bin/IlDump/release/IlDump.dll
MB="$OUT/art_before/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll"
run() {   # $1 名 $2 追加ファイル（絶対パス・消す）
  local tag="$1"; local M="$OUT/mut_$tag"; rm -rf "$M"; mkdir -p "$M"; cp "$OUT/before.txt" "$M/before.txt"
  rm -rf "$M/art"
  for p in PriceOfBread.SimCore PriceOfBread.Display; do
    dotnet build "$TREE/$p/$p.csproj" -c Release -p:Deterministic=true -p:DebugType=none -p:IncludeSourceRevisionInInformationalVersion=false --artifacts-path "$M/art" --disable-build-servers > "$M/build_$p.txt" 2>&1 || echo "build 失敗 $tag $p" >> "$M/note.txt"
  done
  dotnet "$ILD" "$M/after.txt" "$M/art/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll" "$M/art/bin/PriceOfBread.Display/release/PriceOfBread.Display.dll"
  dotnet "$ILD" "$M/after_simcore_only.txt" "$M/art/bin/PriceOfBread.SimCore/release/PriceOfBread.SimCore.dll"
  dotnet "$ILD" "$M/after_display_only.txt" "$M/art/bin/PriceOfBread.Display/release/PriceOfBread.Display.dll"
  python -X utf8 -I "$(dirname "$0")/ilcompare.py" "$M" > "$M/gate.txt" 2>&1; echo "rc=$?" >> "$M/gate.txt"
}
# M1
printf 'namespace PriceOfBread.SimCore.Display;\npublic class MutEmpty { }\n' > "$TREE/PriceOfBread.Display/MutEmpty.cs"
run M1; rm -f "$TREE/PriceOfBread.Display/MutEmpty.cs"
# M4：SimCore にも PowerLabel.cs の写しを置く
cp "$TREE/PriceOfBread.Display/PowerLabel.cs" "$TREE/PriceOfBread.SimCore/PowerLabelCopy.cs"
run M4; rm -f "$TREE/PriceOfBread.SimCore/PowerLabelCopy.cs"
git -C "$TREE" status --short | head -3
