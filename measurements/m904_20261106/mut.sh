# (904) 突然変異：使い捨ての木 wt_c77sm_mut に 1 本ずつ入れ、対象の試験を回し、git checkout で戻す。結果は mut904.txt
export PATH="/usr/bin:/c/Program Files/Git/usr/bin:/c/Program Files/Git/cmd:/c/Program Files/dotnet:$PATH"
cd D:/PoB_work/wt_c77sm_mut || exit 9
OUT=D:/PoB_work/c77sm_work/mut904.txt; : > "$OUT"
run() { # 名前 ファイル 旧 新 フィルタ
  python -I - "$2" "$3" "$4" <<'PY'
import sys
p,a,n=sys.argv[1:4]
b=open(p,'rb').read().decode('utf8'); assert b.count(a)==1,(p,a,b.count(a)); open(p,'wb').write(b.replace(a,n).encode('utf8'))
PY
  r=$(dotnet test PriceOfBread.Tests -c Release --artifacts-path D:/PoB_work/c77sm_mut_art --filter "$5" 2>&1 | grep -E "成功!|失敗!|error CS" | head -3 | tr '\n' ' ')
  echo "$1	$r" >> "$OUT"
  git checkout -q -- "$2"
}
run "M-a_tierBeforeを移した後の値に" PriceOfBread.SimCore/InstrumentsCede.cs "TierBefore = r.Tier.ToString()" "TierBefore = RegionTier.Integrated.ToString()" "FullyQualifiedName~CedeRuleInstrumentTests"
run "M-g1_capColonialを常に偽に" PriceOfBread.SimCore/InstrumentsLandshare.cs "bool capColonial = capR.Tier == RegionTier.Colonial;" "bool capColonial = false;" "FullyQualifiedName~LandShareTests"
run "M-g2_新しい規則の期待に送金を常に足す" PriceOfBread.SimCore/InstrumentsLandshare.cs "if (!capColonial) expTier[" "if (true) expTier[" "FullyQualifiedName~LandShareTests"
run "M-8_数え方を1行1に戻す" PriceOfBread.Tests/WriterRatchetTests.cs "n += rx.Matches(code).Count;" "n += rx.IsMatch(code) ? 1 : 0;" "FullyQualifiedName~WriterRatchetTests"
echo done >> "$OUT"
