#!/usr/bin/env bash
# (923) c80lr 突然変異 M1〜M4（使い捨ての木 MUT_TREE＝枝の頭の detached worktree に d590fc9f の world.json を当てた物・コミットしない）。
#   bash scripts/instruments/tools/bg.sh D:/PoB_work/c80lr_work/mut.mark bash scripts/instruments/c80lr/mut923.sh <MUT_TREE> <出力の作業フォルダ>
#   変異ごとに置き換え → 2 クラス（StateFactoryTests・CornintInstrumentTests）だけ回す → git checkout で戻す。置き換えは python のバイトの置き換え（1 箇所ちょうど・無ければ止める）。
T="${1:?木}"; O="${2:?作業}"; mkdir -p "$O"
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
F='FullyQualifiedName~PriceOfBread.Tests.StateFactoryTests|FullyQualifiedName~PriceOfBread.Tests.CornintInstrumentTests'
rep() { python - "$T/$1" "$2" "$3" <<'PY' || exit 7
import sys
p,a,b=sys.argv[1],sys.argv[2].encode(),sys.argv[3].encode()
d=open(p,'rb').read()
if d.count(a)!=1: print('ANCHOR', d.count(a), p); sys.exit(1)
open(p,'wb').write(d.replace(a,b))
PY
}
run() { echo "=== $1"; SUITE_W="$O/$1" MUT_FILTER="$F" bash "$T/scripts/instruments/c80lr/suite923.sh"; ( cd "$T" && git checkout HEAD -- PriceOfBread.Tests PriceOfBread.SimCore/EconomyStep.cs PriceOfBread.SimCore/InstrumentsCornint.cs ); }
( cd "$T" && git status --short | head -3 )
# M0 変異なし（同じ木・同じ絞り込みの対照）
run M0
# M1 期待を持ち主だけへ戻す（試験の 2 ファイルを基点 b3a4190f へ）
( cd "$T" && git checkout b3a4190f -- PriceOfBread.Tests/StateFactoryTests.cs PriceOfBread.Tests/CornintInstrumentTests.cs ); run M1
# M2 エンジン AddRentRecipients の枠の枝を消す（地代を持ち主へ全額）
rep PriceOfBread.SimCore/EconomyStep.cs 'if (pr.LandShares is { } ls && !Colonial.AbsenteeRgo(rr, pr))' 'if (pr.LandShares is { } ls && ls.Length < 0 && !Colonial.AbsenteeRgo(rr, pr))'; run M2
# M3 計器の国内の枝を持ち主の 1 行へ
rep PriceOfBread.SimCore/InstrumentsCornint.cs 'if (s.LandShares is { } lso)' 'if (s.LandShares is { } lso && lso.Length < 0)'
rep PriceOfBread.SimCore/InstrumentsCornint.cs 'if (s.LandShares is { } lsp) {' 'if (s.LandShares is { } lsp && lsp.Length < 0) {'
rep PriceOfBread.SimCore/InstrumentsCornint.cs 's.LandShares is null ? rent * v / ownerTot[pi]' 's.LandShares is null || true ? rent * v / ownerTot[pi]'; run M3
# M4 計器の輸出の枝を持ち主の 1 行へ
rep PriceOfBread.SimCore/InstrumentsCornint.cs 'if (s.LandShares is { } lsx)' 'if (s.LandShares is { } lsx && lsx.Length < 0)'; run M4
( cd "$T" && git status --short | head -3 )
echo MUTDONE
