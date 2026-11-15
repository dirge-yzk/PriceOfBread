#!/bin/bash
# (1144) 使い捨ての木で突然変異を 1 本ずつ回す（T1）。出力 D:/PoB_work/c112t1_work/mut_out.txt（measurements/m1144_20261115/mutations.txt へ写す）。
set -u
export TEMP=D:/PoB_work/c112t1_work/tmp TMP=D:/PoB_work/c112t1_work/tmp
SRC=/d/PoB_work/wt_c112t1
MT=/d/PoB_work/c112t1_work/mt
OUT=/d/PoB_work/c112t1_work/mut_out.txt
: > $OUT
[ -d $MT ] || git -C $SRC worktree add --detach $MT HEAD >/dev/null 2>&1
git -C $MT checkout -q --detach $(git -C $SRC rev-parse HEAD)
B=PriceOfBread.SimCore/BilateralMarket.cs
E=PriceOfBread.SimCore/EconomyStep.cs
I=PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs
run() {
  name="$1"; filt="$2"; shift 2
  git -C $MT checkout -q -- .
  ok=1
  while [ $# -gt 0 ]; do
    python -I $SRC/scripts/instruments/tools/replace1.py "$MT/$1" --file-old "$2" --file-new "$3" >/dev/null || ok=0
    shift 3
  done
  if [ $ok = 0 ]; then echo "== $name: 変更が当たらない" >> $OUT; return; fi
  res=$(cd $MT && dotnet test PriceOfBread.Tests/PriceOfBread.Tests.csproj -c Release --artifacts-path D:/PoB_work/c112t1_mtart --filter "$filt" 2>&1 | grep -E "^\s+失敗 |成功!|失敗!|エラー メッセージ|^\s{3}[^ a]" | grep -v "^\s*at " | cut -c1-220 | head -12)
  echo "== $name" >> $OUT; echo "$res" >> $OUT
}
D=/d/PoB_work/c112t1_work/mtsnip
mkdir -p $D
w() { printf '%s' "$2" > $D/$1; }
F4="FullyQualifiedName~TradeT1Tests.M4"
# M2 Q＝P（案 B-b）：門と決まり直しの両方
w m2a 'double q0 = Math.Min(m.LastProduced, a1Ph0); if (!(q0 >= 0)) q0 = m.LastProduced;'
w m2b 'double q0 = m.LastProduced;'
w m2c 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;'
w m2d 'a1Q = m.LastProduced;'
run M2_Q等しいP "$F4" $B $D/m2a $D/m2b $B $D/m2c $D/m2d
# M3 在庫の項を外す（案 B-a）
w m3a 'xNeed = m.TargetStock + m.LastDemand * a1Root - a1Q - m.Stock;'
w m3b 'xNeed = m.TargetStock + m.LastDemand * a1Root - a1Q;'
run M3_在庫の項を外す "$F4" $B $D/m3a $D/m3b
# M4 全量を最安へ（案 A-2）
w m4a 'xPrice = xPriceOn ? m.LastDemand * powVal - (a1Q + m.LastImport) : 0.0;'
w m4b 'xPrice = xPriceOn ? double.MaxValue : 0.0;'
w m4c 'xNeed = m.TargetStock + m.LastDemand * a1Root - a1Q - m.Stock;'
w m4d 'xNeed = m.TargetStock + m.LastDemand - m.Stock;'
run M4_全量を最安へ "$F4" $B $D/m4a $D/m4b $B $D/m4c $D/m4d
# M5 行の門の上界に lp
w m5a 'if (headS < sellers.Count) { double hm = sellers[headS].Price * a1LbMin; if (hm < uMin) uMin = hm; }'
w m5b '{ }'
run M5_門の上界にlp "$F4" $B $D/m5a $D/m5b
# M9 写しの基数から在庫の調整を外す（在庫の調整を写さない＝清算の側で読み直す形の代わり）
w m9a 'baseOut = unInvented ? -1.0 : s.Capacity * s.LastInputFill * invAdj;'
w m9b 'baseOut = unInvented ? -1.0 : s.Capacity * s.LastInputFill;'
run M9_基数から在庫の調整を外す "FullyQualifiedName~TradeT1Tests.M1" $E $D/m9a $D/m9b
# M10 min を外す初版 Q＝P−(P̂(lp)−P̂(u′))
w m10a 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;'
w m10b 'a1Q = m.LastProduced - (A1PHat(a1Pr, a1St, t * gc + g, lp / exRate[ni], nations[ni].World.Coef, ref nA1) - ph);'
run M10_minを外す "$F4" $B $D/m10a $D/m10b
# M11 u′ の代わりに unitA
w m11a 'double uP = Math.Min(unitA, lp);'
w m11b 'double uP = unitA;'
run M11_unitA "$F4" $B $D/m11a $D/m11b
# M12a エンジンだけ Toward を (1−k)x＋k×目標 へ（計器は独立のまま＝自己検査が赤のはず）
w m12a 'double o = PoliticalQuantities.Toward(o0, target, cn.SupplyAdjustSpeed);'
w m12b 'double o = (1.0 - cn.SupplyAdjustSpeed) * o0 + cn.SupplyAdjustSpeed * target;'
run M12a_エンジンだけ書き換え "$F4" $B $D/m12a $D/m12b
# M12b 計器の P̂ をエンジンの A1PHat に差し替え＋エンジンの書き換え（自己検査が緑のまま＝独立でない形を拾えない）
w m12c '    internal static double T1PHat(Region r, int g, Coefficients c, double pLoc)
    {'
w m12d '    internal static double T1PHat(Region r, int g, Coefficients c, double pLoc)
    {
        { var arr = TbProducers(r, g); long cc = 0; return InternationalMarket.A1PHat(arr, new[] { 0, arr.Length }, 0, pLoc, c, ref cc); }'
run M12b_計器がエンジンを呼ぶ "$F4" $B $D/m12a $D/m12b $I $D/m12c $D/m12d
# MI 写しを清算の後に 0 へ戻さない
w mia 'if (a1On || Instruments.On) A1Invalidate(nations);'
w mib '{ }'
run MI_写しを戻さない "FullyQualifiedName~TradeT1Tests.M3" $B $D/mia $D/mib
echo done >> $OUT
