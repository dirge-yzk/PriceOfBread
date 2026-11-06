#!/usr/bin/env bash
# (921) M3（旧の形で ExpeditionLift を 0.999 にする）と M4（旧の Discovery.Step の ro と eo の確保の間で On を真にする）。使い捨ての木 D:/PoB_work/c80sm_work/mutwt（b3a4190f）。出力＝D:/PoB_work/c80sm_work/mutation34.txt
W=D:/PoB_work/c80sm_work/mutwt; O=D:/PoB_work/c80sm_work/mutation34.txt; : > $O
cd $W || exit 2
run() { dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c80sm_work/mut_art --nologo --filter "$1" 2>&1 | grep -E "^ *(失敗|Failed) |合計:|NullReference|error CS" | cut -c1-220 | head -8 >> $O; }
python -I -c "
import sys
p='PriceOfBread.SimCore/Diplomacy.cs'; s=open(p,'rb').read(); a=b'=> 1.0;   // \xe2\x98\x85(613) \xe6\xae\xb53'
assert s.count(a)==1; open(p,'wb').write(s.replace(a,b'=> 0.999;   // \xe2\x98\x85(613) \xe6\xae\xb53'))"
echo "== M3_ExpeditionLift を 0.999（旧の形）: DiplomacyPendingTests|MilitaryTests|PlayHarnessTests" >> $O
run "FullyQualifiedName~DiplomacyPendingTests|FullyQualifiedName~MilitaryTests|FullyQualifiedName~PlayHarnessTests"
git checkout -q PriceOfBread.SimCore/Diplomacy.cs
python -I -c "
p='PriceOfBread.SimCore/Discovery.cs'; s=open(p,'rb').read().decode('utf-8')
a='        var eo = Instruments.On ? new double'
assert s.count(a)==1; open(p,'wb').write(s.replace(a,'        Instruments.On = true;\n'+a).encode('utf-8'))"
echo "== M4_旧の Discovery.Step：ro と eo の間で On を真にする: DiscoveryScoreTests|DiscoveryTierTests|TechInstitutionTests" >> $O
run "FullyQualifiedName~DiscoveryScoreTests|FullyQualifiedName~DiscoveryTierTests|FullyQualifiedName~TechInstitutionTests"
git checkout -q PriceOfBread.SimCore/Discovery.cs
