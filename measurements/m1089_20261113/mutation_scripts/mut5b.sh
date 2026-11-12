cd D:/PoB_work/c104fb_work/mut || exit 1
F='FullyQualifiedName~希少鉱物は共有世界の非産地'
echo "=== 変異なし"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "失敗|成功|Assert|error CS" | head -4 | cut -c1-260
python D:/PoB_work/c104fb_work/mut.py M5
echo "=== M5"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "失敗|成功|Assert|error CS" | head -4 | cut -c1-260
git checkout -q -- .
