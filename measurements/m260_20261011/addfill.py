import sys, os
root = sys.argv[1]
exec(open('D:/PoB_work/e260/edit1.py', encoding='utf-8').read().split('edit(\'PriceOfBread.SimCore/Instruments.cs\'')[0])
os.chdir(root)
edit('PriceOfBread.SimCore/Instruments.cs', [
("""        double popK = 0, soldierK = 0;
        int core = 0, integrated = 0, colonial = 0;
""", """        double popK = 0, soldierK = 0, supplyFillSum = 0;
        int core = 0, integrated = 0, colonial = 0;
"""),
("""            foreach (var p in r.Pops) { popK += p.Size; if (p.IsSoldier) soldierK += p.Size; }
            if (r.Tier == RegionTier.Core) core++;""", """            foreach (var p in r.Pops) { popK += p.Size; if (p.IsSoldier) soldierK += p.Size; supplyFillSum += p.SupplyFill * p.Size; }
            if (r.Tier == RegionTier.Core) core++;"""),
("""cols.Add(I(st.FacDoneClosedAtStart));
""", """cols.Add(I(st.FacDoneClosedAtStart));
        cols.Add(F(popK > 0 ? supplyFillSum / popK : 0));   // 人口加重の供給の充足（補給崩壊の物差し＝この値が0.3 を割る国×四半期を数える・旧の注入 inj_s3a の fill と同じ式）
"""),
("""\\tstateFacilityCompletedGateClosedAtStartCount");""", """\\tstateFacilityCompletedGateClosedAtStartCount\\tsupplyFillShare");"""),
])
