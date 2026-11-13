#!/usr/bin/env bash
# (1111) 試験の突然変異（使い捨ての木に写して回す）。出力 <作業>/mut_tests.txt。
#   M2 画面の試験 1 本（FmtTests.cs）をエンジンの側へ戻す → エンジンのビルドが赤（App が無い）
#   M3 GoodIdGuardTests の走査の一覧から PriceOfBread.Display を外す → SourceScanCoverageTests が赤
#   M3b DiscontentReaderTests の走査から Display を外す → DiscontentReaderTests が赤（Display の PopTables.cs の Discontent( が消える）
#   M5 Display に型をまたぐ換算の口（MoneyStock を返し RatePerYear と MoneyPerYear を受ける）を足す → DisplayUnitTypeTests の 1 本目が赤
#   M6 Display に年GDP比を作る口を足す → DisplayUnitTypeTests の 2 本目が赤
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
SRC="${1:?ソース木}"; W="${2:?作業}"; T="$W/mut_tree"; OUT="$W/mut_tests.txt"; : > "$OUT"
rm -rf "$T"; mkdir -p "$T"
tar -cf - -C "$SRC" --exclude=bin --exclude=obj --exclude=.git --exclude=docs --exclude=measurements --exclude=uishot . | tar -xf - -C "$T"
b() { dotnet build "$T/$1/$1.csproj" -c Release --artifacts-path "$W/mut_art" --disable-build-servers 2>&1 | grep -E " error |エラー$|成功" | sort -u | cut -c1-200 | head -3; }
t() { dotnet test "$T/$1/$1.csproj" -c Release --no-build --artifacts-path "$W/mut_art" --filter "$2" 2>&1 | grep -E "成功!|失敗!|エラー" | head -3; }
echo "== M0 元（緑のはず）" >> "$OUT"; b PriceOfBread.Tests >> "$OUT"; t PriceOfBread.Tests "FullyQualifiedName~SourceScanCoverageTests|FullyQualifiedName~DiscontentReaderTests" >> "$OUT"
b PriceOfBread.Tests.Ui >> "$OUT"; t PriceOfBread.Tests.Ui "FullyQualifiedName~DisplayUnitTypeTests" >> "$OUT"
rep() { python - "$1" "$2" "$3" <<'PY'
import sys
p,o,n=sys.argv[1:4]
b=open(p,'rb').read(); assert o.encode() in b,(p,o); open(p,'wb').write(b.replace(o.encode(),n.encode(),1))
PY
}
echo "== M3 GoodIdGuard から Display を外す" >> "$OUT"
rep "$T/PriceOfBread.Tests/GoodIdGuardTests.cs" '"PriceOfBread.SimCore", "PriceOfBread.Display", "PriceOfBread.Runner"' '"PriceOfBread.SimCore", "PriceOfBread.Runner"'
b PriceOfBread.Tests >> "$OUT"; t PriceOfBread.Tests "FullyQualifiedName~SourceScanCoverageTests" >> "$OUT"
cp "$SRC/PriceOfBread.Tests/GoodIdGuardTests.cs" "$T/PriceOfBread.Tests/GoodIdGuardTests.cs"
echo "== M3b DiscontentReader から Display を外す" >> "$OUT"
rep "$T/PriceOfBread.Tests/DiscontentReaderTests.cs" 'new[] { root, System.IO.Path.Combine(GoodIdGuardTests.RepoRoot(), "PriceOfBread.Display") }' 'new[] { root }'
b PriceOfBread.Tests >> "$OUT"; t PriceOfBread.Tests "FullyQualifiedName~DiscontentReaderTests" >> "$OUT"
cp "$SRC/PriceOfBread.Tests/DiscontentReaderTests.cs" "$T/PriceOfBread.Tests/DiscontentReaderTests.cs"
echo "== M5 Display に換算の口" >> "$OUT"
printf 'using PriceOfBread.SimCore;\nnamespace PriceOfBread.SimCore.Display;\npublic static class MutConv { public static MoneyStock X(RatePerYear a, MoneyPerYear b) => default; }\n' > "$T/PriceOfBread.Display/MutConv.cs"
b PriceOfBread.Tests.Ui >> "$OUT"; t PriceOfBread.Tests.Ui "FullyQualifiedName~DisplayUnitTypeTests" >> "$OUT"
rm -f "$T/PriceOfBread.Display/MutConv.cs"
echo "== M6 Display に年GDP比を作る口" >> "$OUT"
printf 'using PriceOfBread.SimCore;\nnamespace PriceOfBread.SimCore.Display;\npublic static class MutGdp { public static RatioToGdpYear Y() => default; }\n' > "$T/PriceOfBread.Display/MutGdp.cs"
b PriceOfBread.Tests.Ui >> "$OUT"; t PriceOfBread.Tests.Ui "FullyQualifiedName~DisplayUnitTypeTests" >> "$OUT"
rm -f "$T/PriceOfBread.Display/MutGdp.cs"
echo "== M2 FmtTests.cs をエンジンの側へ戻す" >> "$OUT"
mv "$T/PriceOfBread.Tests.Ui/FmtTests.cs" "$T/PriceOfBread.Tests/FmtTests.cs"
b PriceOfBread.Tests >> "$OUT"
echo "END" >> "$OUT"
