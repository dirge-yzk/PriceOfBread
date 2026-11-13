cd D:/PoB_work/c104fb_work/t4 || exit 1
git diff > D:/PoB_work/c104fb_work/newtests.patch
cp PriceOfBread.Tests/EconomyTests.cs D:/PoB_work/c104fb_work/EconomyTests.new.cs
F='FullyQualifiedName~代替レシピの生産者は構築で常に入る|FullyQualifiedName~原油の採掘は機械の投入を常に持つ|FullyQualifiedName~開幕の中核の度合いは州の文化'
run() { dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c104fb_art2 --filter "$F" 2>&1 | grep -E "^ *失敗|成功!|失敗!|error CS" | head -6 | cut -c1-220; }
echo "=== 変異なし"; run
# 変異は mut.py の表を t4 向けに当てる
for M in M1 M2 M3 M4 M7; do
  python - <<PY
import re,subprocess
src=open('D:/PoB_work/c104fb_work/mut.py',encoding='utf8').read().replace("T = 'D:/PoB_work/c104fb_work/mut/'","T = 'D:/PoB_work/c104fb_work/t4/'")
src=src.replace("subprocess.run(['git', '-C', T, 'checkout', '-q', '--', '.'], check=True)","subprocess.run(['git','-C',T,'checkout','-q','--','PriceOfBread.SimCore'],check=True)")
import sys; sys.argv=['x','$M']; exec(src)
PY
  echo "=== $M"; run
done
git checkout -q -- PriceOfBread.SimCore
