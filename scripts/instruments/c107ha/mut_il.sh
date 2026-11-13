#!/usr/bin/env bash
# 門 3 の突然変異（M1・M3・M4）。使い方：bash mut_il.sh <コミット> <il1 の出力フォルダ> <出力フォルダ>
# 基準＝<コミット>＋変換＋Zmut.cs（文字列 "a//b"・数 1）。M1＝文字列を台本が削った "a"。M3＝数を 2 に変える。M4＝il1 の前（変換あり）と後（変換なし）の比較。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
C=$1; IL1=$2; OUT=$3; IL=D:/PoB_work/wt_c107ha_il; REPO=D:/PoB_work/wt_c107ha
mkdir -p $OUT
for v in base m1 m3; do
  git -C $IL checkout -q --detach $C; git -C $IL clean -fdxq
  python -X utf8 -I $REPO/scripts/instruments/c107ha/strip_callerline.py $IL
  S='a//b'; N=1
  [ $v = m1 ] && S='a'
  [ $v = m3 ] && N=2
  printf 'namespace PriceOfBread.SimCore { public static class Zmut { public const string S = "%s"; public const int N = %s; } }\n' "$S" "$N" > $IL/PriceOfBread.SimCore/Zmut.cs
  rm -rf $OUT/art_x
  dotnet build $IL/PriceOfBread.SimCore/PriceOfBread.SimCore.csproj -c Release --artifacts-path $OUT/art_x --disable-build-servers -p:Deterministic=true -p:DebugType=none -p:IncludeSourceRevisionInInformationalVersion=false > $OUT/build_$v.txt 2>&1
  f=$(find $OUT/art_x -name PriceOfBread.SimCore.dll -path "*release*" | head -1); cp "$f" $OUT/Z_$v.dll
  git -C $IL checkout -q -- .; rm -f $IL/PriceOfBread.SimCore/Zmut.cs
done
{
echo "M1 文字列の中の // を台本が削る：違うバイトの数 $(cmp -l $OUT/Z_base.dll $OUT/Z_m1.dll | wc -l)（0 なら門 3 が緑のまま）"
echo "M3 注の隣のコードの数を変える：違うバイトの数 $(cmp -l $OUT/Z_base.dll $OUT/Z_m3.dll | wc -l)"
echo "M4 変換を片方の木だけに当てる（前＝変換あり・後＝変換なし）：違うバイトの数 $(cmp -l $IL1/SimCore_bef_tr.dll $IL1/SimCore_aft_raw.dll | wc -l)"
echo "対照 変換ありの前後：違うバイトの数 $(cmp -l $IL1/SimCore_bef_tr.dll $IL1/SimCore_aft_tr.dll | wc -l)"
} > $OUT/summary.txt
echo DONE >> $OUT/summary.txt
