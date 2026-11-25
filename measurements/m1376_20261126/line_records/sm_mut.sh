#!/usr/bin/env bash
cd D:/PoB_work/c145sm_mut || exit 1
ART=D:/PoB_work/c145sm_mutart
F="FullyQualifiedName~DerivedValueRatchetTests|FullyQualifiedName~ValueCountRatchetTests"
run() { dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART --filter "$F" 2>&1 | grep -E "^\s*失敗 |書き手が|入力が|出力が|が [0-9]+ か所|合計|Expected|Actual|error" | cut -c1-230 | head -12; }
echo "== M1 bare writer in World.cs"
python - <<'PY'
p='PriceOfBread.SimCore/World.cs'
s=open(p,'rb').read().decode('utf-8')
k='        // 金の代価＝投入額そのもの。〔履歴 World.cs#90〕'
assert k in s
s=s.replace(k,'        Legitimacy = System.Math.Max(0, Legitimacy - 0.0);\n'+k,1)
open(p,'wb').write(s.encode('utf-8'))
PY
run; git checkout -q -- PriceOfBread.SimCore/World.cs
echo "== M2 add one input to Legitimacy writer"
python - <<'PY'
p='PriceOfBread.SimCore/World.cs'
s=open(p,'rb').read().decode('utf-8')
k='Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect);'
assert k in s
s=s.replace(k,'Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect + margin * 0);',1)
open(p,'wb').write(s.encode('utf-8'))
PY
run; git checkout -q -- PriceOfBread.SimCore/World.cs
echo "== M2b add input via local variable text (fingerprint)"
python - <<'PY'
p='PriceOfBread.SimCore/World.cs'
s=open(p,'rb').read().decode('utf-8')
k='Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect);'
s=s.replace(k,'Legitimacy = System.Math.Max(0, Legitimacy - (-net) * effect * 1.0);',1)
open(p,'wb').write(s.encode('utf-8'))
PY
run; git checkout -q -- PriceOfBread.SimCore/World.cs
echo "== M3 .Value in Instruments"
f=$(ls PriceOfBread.SimCore/Instruments/*.cs | head -1)
python - "$f" <<'PY'
import sys
p=sys.argv[1]
s=open(p,'rb').read().decode('utf-8')
i=s.index('namespace')
j=s.index('\n',i)
s=s[:j+1]+'// m3\n'+s[j+1:]
# append a class with a .Value
s=s+'\ninternal static class M3Probe { internal static double F(PriceOfBread.SimCore.RatePerMonth x) => x.Value; }\n'
open(p,'wb').write(s.encode('utf-8'))
PY
grep -n "struct MoneyPerMonth\|struct .*PerMonth" PriceOfBread.SimCore/Units.cs | head -2
run; git checkout -q -- PriceOfBread.SimCore
echo done
