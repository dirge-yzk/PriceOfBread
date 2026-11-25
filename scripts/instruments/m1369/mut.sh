cd D:/PoB_work/wt_m1369
export AVALONIA_TELEMETRY_OPTOUT=1
run(){ dotnet test PriceOfBread.Tests.Ui -c Release --artifacts-path D:/PoB_work/m1369_work/art --filter "FullyQualifiedName~DisplayCatalogTests|FullyQualifiedName~FmtTests" 2>&1 | grep -E '\[FAIL\]|成功!|失敗!' ; }
python -X utf8 scripts/instruments/tools/replace1.py PriceOfBread.Display/Table.cs '        if (t == typeof(FractionPerQuarter)) return Unit.FractionPerQuarter;  // (1369) ut の期間あたりの割合（四半期）' '' 2>&1 | tail -1
echo "== M1 UnitOf の FractionPerQuarter の行を外す"; run
git checkout -- PriceOfBread.Display/Table.cs
python -X utf8 scripts/instruments/tools/replace1.py PriceOfBread.App/Fmt.cs '            case Unit.FractionPerMonth: return Percent(v, 1) + "/月";' '' 2>&1 | tail -1
echo "== M2 Fmt の FractionPerMonth の書式を外す"; run
git checkout -- PriceOfBread.App/Fmt.cs
echo "== 戻した後"; git status --short | grep -v '^??'; run
