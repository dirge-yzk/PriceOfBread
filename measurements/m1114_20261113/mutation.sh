#!/usr/bin/env bash
# 突然変異（使い捨ての木 wt_c107hc_il）。使い方：bash mut.sh
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
T=D:/PoB_work/wt_c107hc_il
W=D:/PoB_work/c107hc_work
G=D:/PoB_work/wt_c107hc/scripts/instruments/c107hc/gates.py
OUT=$W/mut_result.txt
: > $OUT
reset() { git -C $T checkout -q -- . ; }
il() {
  rm -rf $T/PriceOfBread.SimCore/obj $T/PriceOfBread.SimCore/bin
  dotnet build $T/PriceOfBread.SimCore/PriceOfBread.SimCore.csproj -c Release -p:Deterministic=true -p:DebugType=none -p:IncludeSourceRevisionInInformationalVersion=false --disable-build-servers -o $W/il_mut > /dev/null 2>&1
  if cmp -s $W/il_mut/PriceOfBread.SimCore.dll $W/il_before/PriceOfBread.SimCore.dll; then echo "  門3 IL 一致（緑）" >> $OUT; else echo "  門3 IL 不一致（赤）" >> $OUT; fi
}
gates() { python -I $G $T 1cd66aae5 2>&1 | grep '^RED' | cut -c1-120 | sed 's/^/  /' >> $OUT; echo "  （赤の行 $(python -I $G $T 1cd66aae5 2>&1 | grep -c '^RED')）" >> $OUT; }
reset
echo "M1 係数の既定値を 1 つ変える（PriceAdjustSpeed 0.13→0.14）" >> $OUT
python -I -c "
p='$T/PriceOfBread.SimCore/Coefficients.cs'
s=open(p,'rb').read().decode('utf-8')
assert 'PriceAdjustSpeed = 0.13;' in s
open(p,'wb').write(s.replace('PriceAdjustSpeed = 0.13;','PriceAdjustSpeed = 0.14;',1).encode('utf-8'))"
gates; il; reset
echo "M2 履歴から 1 係数の節を落とす（Coefficients.md の PriceElasticity）" >> $OUT
python -I -c "
import re
p='$T/docs/history/Coefficients.cs.md'
s=open(p,'rb').read().decode('utf-8')
a=s.index('## PriceElasticity'); b=s.index('## ',a+3)
open(p,'wb').write((s[:a]+s[b:]).encode('utf-8'))"
gates; reset
echo "M3 部品の中の宣言を 2 つ入れ替える（PriceAmpCenter と PriceAmpEdge の行）" >> $OUT
python -I -c "
p='$T/PriceOfBread.SimCore/Coefficients.cs'
L=open(p,'rb').read().decode('utf-8').split(chr(10))
i=[k for k,l in enumerate(L) if 'PriceAmpCenter =' in l][0]
L[i],L[i+1]=L[i+1],L[i]
open(p,'wb').write(chr(10).join(L).encode('utf-8'))"
gates; il; reset
echo "M4 行末の注の短い版を消す（PriceAmpPower の注を消す）" >> $OUT
python -I -c "
p='$T/PriceOfBread.SimCore/Coefficients.cs'
L=open(p,'rb').read().decode('utf-8').split(chr(10))
i=[k for k,l in enumerate(L) if 'PriceAmpPower =' in l][0]
L[i]=L[i].split('//')[0].rstrip()
open(p,'wb').write(chr(10).join(L).encode('utf-8'))"
gates; reset
echo done >> $OUT
