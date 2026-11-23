#!/usr/bin/env bash
# (1071) 突然変異の台本（使い捨て：木を書き換えて試験を回し git checkout で戻す）。出力は measurements/m1071_20261112_mut/。
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/dotnet:$PATH" PYTHONUTF8=1
R=D:/PoB_work/wt_c102rp; OUT=$R/measurements/m1071_20261112_mut; ART=D:/PoB_work/c102rp_art
cd $R
run() { # 名前 フィルタ
  dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path $ART --filter "$2" > $OUT/$1.txt 2>&1
  echo "$1 rc=$?" >> $OUT/summary.txt
  git checkout -- PriceOfBread.SimCore
}
: > $OUT/summary.txt
python - <<'P'
p='D:/PoB_work/wt_c102rp/PriceOfBread.SimCore/BilateralMarket.cs'
b=open(p,'rb').read().decode()
a="        double viaSea = double.PositiveInfinity;\n        if (tr[a].seaOk && tr[b].seaOk)"
assert a in b
b=b.replace(a,"        if (a > b) (a, b) = (b, a);   // M1\n"+a,1)
open(p,'wb').write(b.encode())
P
run M1_対称化 "FullyQualifiedName~RulePowerTests.P1"
python - <<'P'
p='D:/PoB_work/wt_c102rp/PriceOfBread.SimCore/InstrumentsRulePower.cs'
b=open(p,'rb').read().decode()
a="if (!On || !firstClearingOfQuarter) return;"
assert a in b
open(p,'wb').write(b.replace(a,"if (!firstClearingOfQuarter) return;   // M3",1).encode())
P
run M3_Onでなくても計算 "FullyQualifiedName~RulePowerTests.P3"
python - <<'P'
p='D:/PoB_work/wt_c102rp/PriceOfBread.SimCore/InstrumentsRulePower.cs'
b=open(p,'rb').read().decode()
a="        bool wantP = Wants(\"rulepower_s\"), wantW = Wants(\"rulepeace_s\");\n        if (!(wantP || wantW) ||"
assert a in b
b=b.replace(a,"        bool wantP = true, wantW = true;   // M4：Wants を通さない\n        if (!(wantP || wantW) ||",1)
b=b.replace("using (var wr0 = Open(\"rulepower\", seed)) wr0.WriteLine(RulePowerHeaderText);","System.IO.Directory.CreateDirectory(Dir); System.IO.File.WriteAllText(System.IO.Path.Combine(Dir, $\"rulepower_s{seed}.tsv\"), RulePowerHeaderText + \"\n\");",1)
open(p,'wb').write(b.encode())
p2='D:/PoB_work/wt_c102rp/PriceOfBread.SimCore/BilateralMarket.cs'
P
run M4_Openを通さず直に "FullyQualifiedName~InstrumentsTablesSelectTests"
# (1316) 注（空振り）：下の M2（:40〜:48）は (1309) の S1（走り書きの欄 LastPairWasLand を返り値へ）より前の木を書き換える台本＝今の木では置き換える行が無く assert で落ちる＝赤にならない（反証役 v の指摘）。使わない。
python - <<'P'
p='D:/PoB_work/wt_c102rp/PriceOfBread.SimCore/BilateralMarket.cs'
b=open(p,'rb').read().decode()
a="        land = viaLand < viaSea;   // ★純テレメトリ"
assert a in b
b=b.replace(a,"        LastPairWasLand = viaLand < viaSea; land = viaLand < viaSea;   // M2",1)
open(p,'wb').write(b.encode())
P
run M2_口の中でLastPairWasLandを書く "FullyQualifiedName~RulePowerTests.P1"
echo END >> $OUT/summary.txt
