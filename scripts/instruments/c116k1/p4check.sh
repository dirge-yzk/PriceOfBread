#!/usr/bin/env bash
export TEMP=D:/PoB_work/c116k1_work/tmp TMP=D:/PoB_work/c116k1_work/tmp
cd D:/PoB_work/wt_c116k1 || exit 1
A=D:/PoB_work/c116k1_art; T=PriceOfBread.Tests/PriceOfBread.Tests.csproj; O=D:/PoB_work/c116k1_work/p4check.txt
B() { dotnet build $T -c Release --artifacts-path $A -nologo -v q > /dev/null 2>&1 || echo "BUILDFAIL" >> $O; }
R() { dotnet test $T -c Release --artifacts-path $A --no-build --filter "FullyQualifiedName~RulePowerTests" 2>&1 | grep -E "合計|^\s+失敗 " | sed "s/^/$1: /" >> $O; }
echo "# (1173) RulePowerTests の直しの確かめ $(git rev-parse --short HEAD)" > $O
B; R "旗1(既定)"; POB_COEF="SecessionInclusionOnce=0" R "旗0(POB_COEF)"
cp PriceOfBread.Tests/RulePowerTests.cs D:/PoB_work/c116k1_work/RulePowerTests.cs.keep
python -I scripts/instruments/tools/replace1.py PriceOfBread.Tests/RulePowerTests.cs "                if (q != 0) return;" "                return;" > /dev/null
B; R "突然変異(盤面を置かない)・旗1"
cp D:/PoB_work/c116k1_work/RulePowerTests.cs.keep PriceOfBread.Tests/RulePowerTests.cs
B; echo done >> $O
