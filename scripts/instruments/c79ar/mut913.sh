#!/usr/bin/env bash
# (913) mutation test (throwaway copy D:/PoB_work/c79ar_work/mut, not committed): flip Instruments.On to true in the middle of ArmyShape.Compute, three forms.
#   V_old = old form (each of the 3 sites reads On itself) -> expect NullReferenceException (red)
#   V_new = current form (local ins read once)             -> expect green
#   V_m2  = local kept, only the ArmyNeed site back to old -> expect red
export PATH="/c/Program Files/dotnet:$PATH"
M=D:/PoB_work/c79ar_work/mut
F=$M/PriceOfBread.SimCore/ArmyShape.cs
run() { dotnet test $M/PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c79ar_work/mut_art --filter "FullyQualifiedName~MutProbe_OnFlipsMidCompute" 2>&1 | grep -E "Exception|成功!|失敗!|Passed!|Failed!|error " | head -4 | cut -c1-220; }
cp $F $M/ArmyShape.new
PYTHONUTF8=1 python -I - <<'PY'
M='D:/PoB_work/c79ar_work/mut/'
new=open(M+'ArmyShape.new','rb').read()
flip=b'        Instruments.On = true;   // mutation: flag flips mid-compute\n'
a=b'        if (ins) Instruments.ArmyNeed('
assert new.count(a)==1
open(M+'v_new.cs','wb').write(new.replace(a,flip+a))
b=b'        if (Instruments.On) Instruments.ArmyNeed('
open(M+'v_m2.cs','wb').write(new.replace(a,flip+b))
old=new.replace(b'ins ? new double',b'Instruments.On ? new double').replace(a,flip+b).replace(b'        bool ins = Instruments.On;',b'        // (old)')
open(M+'v_old.cs','wb').write(old)
PY
for v in old m2 new; do echo "== V_$v"; cp $M/v_$v.cs $F; run; done
cp $M/ArmyShape.new $F
