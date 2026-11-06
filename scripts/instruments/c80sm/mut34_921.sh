#!/usr/bin/env bash
# (921) M3（旧の形で ExpeditionLift を 0.999 にする）と M4（旧の Discovery.Step＝読みが 4 本）を ForceReadGuardTests（新しい試験・使い捨ての木 mutwt へ写す）で赤にする。出力＝D:/PoB_work/c80sm_work/mutation34b.txt
#   ※初版（既存の試験 DiplomacyPendingTests|MilitaryTests|PlayHarnessTests／DiscoveryScoreTests|DiscoveryTierTests|TechInstitutionTests だけ）は M3・M4 とも緑のままだった（mutation34.txt）＝★(790) で機構の試験 ForceReadGuardTests を足した。
W=D:/PoB_work/c80sm_work/mutwt; O=D:/PoB_work/c80sm_work/mutation34b.txt; : > $O
cd $W || exit 2
cp D:/PoB_work/wt_c80sm/PriceOfBread.Tests/ForceReadGuardTests.cs PriceOfBread.Tests/
run() { dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c80sm_work/mut_art --nologo --filter "$1" 2>&1 | grep -E "^ *(失敗|Failed) |合計:|error CS" | cut -c1-200 | head -8 >> $O; }
python -I -c "
p='PriceOfBread.SimCore/Diplomacy.cs'; s=open(p,'rb').read(); a=b'=> 1.0;   // \xe2\x98\x85(613) \xe6\xae\xb53'
assert s.count(a)==1; open(p,'wb').write(s.replace(a,b'=> 0.999;   // \xe2\x98\x85(613) \xe6\xae\xb53'))"
echo "== M3_旧の木で ExpeditionLift を 0.999 → 新しい試験の 1 本目" >> $O
run "FullyQualifiedName~ForceReadGuardTests"
git checkout -q PriceOfBread.SimCore/Diplomacy.cs
echo "== M4_旧の Discovery.Step（読みが複数）→ 新しい試験（2 本とも：1 本目は旧の木の元の値で緑・2 本目が赤になるはず）" >> $O
run "FullyQualifiedName~ForceReadGuardTests"
