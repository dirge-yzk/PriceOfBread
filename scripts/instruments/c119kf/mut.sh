# (1192) c119kf 突然変異（使い捨ての木 D:/PoB_work/wt_c119kfx・push しない）。出力は D:/PoB_work/c119kf_work/mut_*.log
cd D:/PoB_work/wt_c119kfx || exit 1
export TEMP=D:/PoB_work/c119kf_work TMP=D:/PoB_work/c119kf_work
W=D:/PoB_work/c119kf_work
run() {  # $1=名 $2=プロジェクト $3=filter
  dotnet test $2 -c Release --artifacts-path D:/PoB_work/c119kfx_art --filter "$3" > $W/mut_$1.log 2>&1
}
edit() {  # $1=file $2=旧 $3=新
  python -I - "$1" "$2" "$3" <<'PY'
import sys
p,o,n=sys.argv[1:4]
b=open(p,'rb').read(); o=o.encode(); assert b.count(o)==1,(p,o)
open(p,'wb').write(b.replace(o,n.encode()))
PY
}
T=PriceOfBread.Tests/PriceOfBread.Tests.csproj; U=PriceOfBread.Tests.Ui/PriceOfBread.Tests.Ui.csproj
S=PriceOfBread.SimCore/Simulation.cs; M=PriceOfBread.SimCore/Instruments/InstrumentsMintcap.cs; F=PriceOfBread.SimCore/MarketFlow.cs
# M1 否定形の門も消す（床は元から無い）
git checkout -q $S; edit $S '        if (!(capacity > 1e-9)) return;' '        ;'
run M1 $T "FullyQualifiedName~CapacityAnchorTests"
# M3 床を戻す
git checkout -q $S; edit $S '        if (!(capacity > 1e-9)) return;' '        capacity = Math.Max(baseline * 0.3, capacity); if (!(capacity > 1e-9)) return;'
run M3 $T "FullyQualifiedName~CapacityAnchorTests"
# M2 床を 0.9 へ（ident で測る・ここでは試験の赤だけ記録）
git checkout -q $S; edit $S '        if (!(capacity > 1e-9)) return;' '        capacity = Math.Max(baseline * 0.9, capacity); if (!(capacity > 1e-9)) return;'
run M2 $T "FullyQualifiedName~CapacityAnchorTests"
git checkout -q $S
# M4 項 3 の読みを LastImport へ
git checkout -q $M; edit $M 'Markets[Catalog.SilverId].PrevImport;' 'Markets[Catalog.SilverId].LastImport;'
run M4 $T "FullyQualifiedName~MintcapInstrumentTests"
git checkout -q $M
# Mab 二重の塞ぎを両方外す
git checkout -q $F; edit $F 'run == 1 && want < wrp && !(mwFloor' 'run == 1 && !(mwFloor'; edit $F '            if (!(pe < wrp)) return false;
' ''
run Mab $U "FullyQualifiedName~LaborMarketFormTests.帯の床の撤去は労働3財"
git checkout -q $F
# 基線（変異なし）
run M0 $T "FullyQualifiedName~CapacityAnchorTests|FullyQualifiedName~MintcapInstrumentTests"
echo done > $W/mut.mark
