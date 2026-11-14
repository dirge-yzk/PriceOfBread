#!/bin/bash
# 使い捨ての木で突然変異を 1 本ずつ回す（T0b）。出力 measurements/m1131_20261114_mutations.txt へ追記する材料を stdout へ。
set -u
SRC=/d/PoB_work/wt_c110tb
MT=/d/PoB_work/c110tb_work/mt
OUT=/d/PoB_work/c110tb_work/mut_out.txt
: > $OUT
[ -d $MT ] || git -C $SRC worktree add --detach $MT HEAD >/dev/null 2>&1
git -C $MT checkout -q --detach $(git -C $SRC rev-parse HEAD)
F=$MT/PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs
run() {
  name="$1"; py="$2"
  git -C $MT checkout -q -- .
  python -I -c "$py" "$F" || { echo "$name: 変更が当たらない" >> $OUT; return; }
  res=$(cd $MT && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c110tb_mtart --filter "FullyQualifiedName~TradeBilateralInstrumentTests" 2>&1 | grep -E "^\s+失敗 |成功!|失敗!" | cut -c1-160)
  echo "== $name" >> $OUT; echo "$res" >> $OUT
}
run M12_P_hatをエンジンの純関数に "import sys;p=sys.argv[1];b=open(p,'rb').read().decode();a='sum += BsPHat(p, r, c, w, baumol, Discovery.Gate(r, p), p.Output, u);';assert a in b;b=b.replace(a,'sum += EconomyStep.SupplyTargetAt(p, r, c, w, baumol, Discovery.Gate(r, p), out _);');open(p,'wb').write(b.encode())"
run M2_境の等号を落とす "import sys;p=sys.argv[1];b=open(p,'rb').read().decode();a='return (true, ubBb > 0, ubBb <= 0 && ubA1 > 0, ubA1 <= 0, ev, xOld, ubBb, ubA1);';assert a in b;b=b.replace(a,'return (true, ubBb > 0, ubBb < 0 && ubA1 > 0, ubA1 < 0, ev, xOld, ubBb, ubA1);');open(p,'wb').write(b.encode())"
run M4_成約の前のSで読む "import sys;p=sys.argv[1];b=open(p,'rb').read().decode();a='double eAbs = m.Stock - ub,';assert a in b;b=b.replace(a,'double eAbs = m.Stock - qA - ub,');open(p,'wb').write(b.encode())"
run M5_分母をDだけにする "import sys;p=sys.argv[1];b=open(p,'rb').read().decode();a='den = System.Math.Max(m.LastDemand, m.TargetStock);';assert a in b;b=b.replace(a,'den = m.LastDemand;');open(p,'wb').write(b.encode())"
echo done >> $OUT
touch /d/PoB_work/c110tb_work/mut.mark
