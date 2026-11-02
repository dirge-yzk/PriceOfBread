cd /d/PoB_work/c6pr/wt_mut
for m in finite max flag; do
  git checkout -q -- PriceOfBread.SimCore/Simulation.cs
  python /d/PoB_work/c6pr/mut.py $m || { echo "MUTFAIL $m"; continue; }
  echo "=== mutation $m ==="
  dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c6pr/art_mut --filter "FullyQualifiedName~PortByLandTests" 2>&1 | grep -E "失敗|成功|Failed|Passed|error CS|Assert|PortByLandTests\." | head -20
done
git checkout -q -- PriceOfBread.SimCore/Simulation.cs
echo MUTDONE
