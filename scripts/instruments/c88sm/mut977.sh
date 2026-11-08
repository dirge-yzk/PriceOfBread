#!/usr/bin/env bash
# (977) c88sm 突然変異の全部：bash mut977.sh <出力ファイル> （作業ツリー D:/PoB_work/wt_c88sm で回す・各変異のあと git checkout で戻す）
OUT="${1:?出力}"; ART=D:/PoB_work/c88sm_art; cd D:/PoB_work/wt_c88sm || exit 1
: > "$OUT"
run() {  # id filter file
  python -I -X utf8 scripts/instruments/c88sm/mut977.py "$1" || { echo "$1 適用失敗" >> "$OUT"; return; }
  dotnet test PriceOfBread.Tests -c Release --artifacts-path $ART --filter "$2" > "D:/PoB_work/c88sm_work/mut_$1.log" 2>&1
  res=$(grep -E "^(成功|失敗)!" "D:/PoB_work/c88sm_work/mut_$1.log" | tail -1)
  bad=$(grep -E "^\s+失敗 " "D:/PoB_work/c88sm_work/mut_$1.log" | sed 's/\[.*//' | tr '\n' ';' 2>/dev/null)
  echo "$1 [$3] $res $bad" >> "$OUT"
  git checkout -- "$3"
}
run M1 "FullyQualifiedName~MoTrendInitTests" PriceOfBread.SimCore/World.cs
run M2 "FullyQualifiedName~MoTrendInitTests" PriceOfBread.SimCore/Instruments.cs
run M3 "FullyQualifiedName~MoTrendInitTests" PriceOfBread.SimCore/Instruments.cs
run M4 "FullyQualifiedName~BasisxInstrumentTests" PriceOfBread.SimCore/InstrumentsBasisx.cs
run M5 "FullyQualifiedName~BasisxInstrumentTests" PriceOfBread.SimCore/InstrumentsBasisx.cs
run M6 "FullyQualifiedName~BasisxInstrumentTests" PriceOfBread.SimCore/InstrumentsBasisx.cs
run M8 "FullyQualifiedName~SecessionLaborLawInstrumentTests" PriceOfBread.Tests/SecessionLaborLawInstrumentTests.cs
run M9 "FullyQualifiedName~TransportInstrumentTests" PriceOfBread.Tests/TransportInstrumentTests.cs
# M7：ident_check.py の複製で _actual() を空にする
python -I -X utf8 - <<'PY' >> "$OUT" 2>&1
import re,subprocess,sys
src=open('scripts/instruments/ident/ident_check.py','rb').read().decode('utf-8')
a="    a = CEARN.get('actual') or []\n    return '（実際に違った列 ' + ','.join(a) + '）' if a else ''"
assert src.count(a)==1
open('D:/PoB_work/c88sm_work/ident_check_m7.py','wb').write(src.replace(a,"    return ''").encode('utf-8'))
r=subprocess.run([sys.executable,'-I','-X','utf8','scripts/instruments/c86sm/cpop_route_test.py','D:/PoB_work/c88sm_work/cpt7','D:/PoB_work/c88sm_work/ident_check_m7.py'],capture_output=True)
t=r.stdout.decode('utf-8','replace').strip().split('\n')
print('M7 [ident_check の _actual を空に] 終了コード',r.returncode,'|',t[-1][:200])
PY
# G1：注以外（コードの値）を変えると門が落ちる
python -I -X utf8 scripts/instruments/c88sm/mut977.py G1
python -I -X utf8 scripts/instruments/c88sm/gate977.py 5e6adf98 . > D:/PoB_work/c88sm_work/g1.log 2>&1; echo "G1 [gate977 に値の変更] 終了コード $? $(tail -1 D:/PoB_work/c88sm_work/g1.log)" >> "$OUT"
git checkout -- PriceOfBread.SimCore/Coefficients.cs
git status --short >> "$OUT"
echo done >> "$OUT"
