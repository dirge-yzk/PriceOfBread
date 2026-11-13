cd /d/PoB_work/wt_c106cf; W=/d/PoB_work/c106cf_work; S=PriceOfBread.SimCore
B="dotnet build PriceOfBread.SimCore -c Release --nologo -p:Deterministic=true -p:DebugType=none -p:DebugSymbols=false -p:ContinuousIntegrationBuild=true -p:IncludeSourceRevisionInInformationalVersion=false --artifacts-path D:/PoB_work/c106cf_ilart"
run() { # $1=name
  rm -rf /d/PoB_work/c106cf_ilart; $B > $W/mut_$1.log 2>&1; echo "build rc=$?" >> $W/mut.result
  D=$(find /d/PoB_work/c106cf_ilart -name PriceOfBread.SimCore.dll | head -1)
  if [ -n "$D" ] && cmp -s $W/base.dll $D; then echo "$1 IL=same" >> $W/mut.result; else echo "$1 IL=DIFF" >> $W/mut.result; fi
  python -X utf8 -I scripts/instruments/c106cf/check_bodies.py D:/PoB_work/wt_c106cf >> $W/mut.result 2>&1; echo "$1 gate1 rc=$?" >> $W/mut.result
}
: > $W/mut.result
# M1：p02 と p03 の中身の名前を入れ替える（ファイル名の順が逆になる）
git mv $S/Coefficients.p02_borrow_polity_epidemic.cs $S/Coefficients.tmp.cs; git mv $S/Coefficients.p03_debt_money.cs $S/Coefficients.p02_debt_money.cs; git mv $S/Coefficients.tmp.cs $S/Coefficients.p03_borrow_polity_epidemic.cs
run M1
git reset -q --hard HEAD; git clean -fdq $S
# M2：p05 から 1 行を落とす（最初の public double の行）
python -X utf8 -I - <<'P'
p='PriceOfBread.SimCore/Coefficients.p05_port_insurance.cs'
l=open(p,'rb').read().split(b'\n')
i=[k for k,x in enumerate(l) if x.startswith(b'    public double ')][0]
del l[i]; open(p,'wb').write(b'\n'.join(l))
P
run M2
git reset -q --hard HEAD; git clean -fdq $S
git status --short >> $W/mut.result
echo "rc=0 mut" > $W/mut.mark
