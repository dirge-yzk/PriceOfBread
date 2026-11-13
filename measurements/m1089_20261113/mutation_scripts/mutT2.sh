cd D:/PoB_work/c104fb_work/mut || exit 1
F='FullyQualifiedName~SulfuricAcidSodaAshTests|FullyQualifiedName~SecondRecipeContentsTests|FullyQualifiedName~FuelOperatingCostTests|FullyQualifiedName~FullChainTests|FullyQualifiedName~BreakDetectionTests|FullyQualifiedName~DiscoveryScoreTests|FullyQualifiedName~ColonyPromotionTests|FullyQualifiedName~MintAtMarketTests|FullyQualifiedName~ConservationTests|FullyQualifiedName~RegionLabelInvariantTests|FullyQualifiedName~SmallFixes471Tests|FullyQualifiedName~ModernizeReturnAndScoreTests'
echo "=== 変異なし（クラス単位）"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "^ *失敗|成功!|失敗!|error CS" | head -6 | cut -c1-200
for M in M1 M2 M3 M4 M7 M9; do
  python D:/PoB_work/c104fb_work/mut.py $M
  echo "=== $M"; dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "^ *失敗|成功!|失敗!|error CS" | head -6 | cut -c1-200
done
git checkout -q -- .
