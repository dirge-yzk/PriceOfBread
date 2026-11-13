cd D:/PoB_work/c104fb_work/mut || exit 1
for M in M5 M6; do
  python D:/PoB_work/c104fb_work/mut.py $M || exit 1
  echo "=== $M" 
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "FullyQualifiedName~希少鉱物の産地の賦存" 2>&1 | grep -E "失敗|成功|Assert|error CS" | head -5 | cut -c1-260
done
git checkout -q -- .
