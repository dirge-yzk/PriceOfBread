p='PriceOfBread.SimCore/Instruments.cs'
b=open(p,'rb').read().decode('utf-8')
def rep(old,new,cnt=1):
    global b
    assert b.count(old)==cnt,(old,b.count(old))
    b=b.replace(old,new)
rep("        public int QUnpaidObs, QUnpaidMonths, QMegaPass, QMegaOpen;\n",
    "        public int QUnpaidObs, QUnpaidMonths, QMegaPass, QMegaOpen;\n"
    "        // ---- (315) 線O′：借入の門の第2条件〔利払い÷月の名目GDP が MaxInterestShareOfGdp の下〕の周り（読むだけ）。第2条件の限界が 0 の腕では GDP を読まない（GdpGateShare＝NaN・GdpOnlyNow＝0）\n"
    "        public double GdpGateShare = double.NaN;   // PreBorrow で門が読んだ 利払い÷月の名目GDP（第2条件が有効なときだけ）\n"
    "        public int GdpOnlyNow = -1;                // 今月、借入の門を開けたのが第2条件だけか（-1＝判定に来なかった 0＝いいえ 1＝はい＝門が開いている・かつ 利払い÷歳入は限界に届いている）\n"
    "        public int QGdpOnlyMonths, QGdpOnlyBorrowMonths;   // 今の四半期の月数：第2条件だけで門が開いた月・うち実際に借りた月\n")
rep("        st.LimitNow = st.Over067Now = -1; st.GateInt = st.GateRev = double.NaN; st.WarNow = false;   // (281)\n",
    "        st.LimitNow = st.Over067Now = -1; st.GateInt = st.GateRev = double.NaN; st.WarNow = false;   // (281)\n"
    "        st.GdpGateShare = double.NaN; st.GdpOnlyNow = -1;   // (315)\n")
rep("        st.Over067Now = lr > 0 && li > SuspensionInterestShare * lr ? 1 : 0;\n    }\n",
    "        st.Over067Now = lr > 0 && li > SuspensionInterestShare * lr ? 1 : 0;\n"
    "        // (315) 第2条件（利払い÷月の名目GDP）：限界が 0 の腕では GDP を読まない。第2条件だけで門が開いた＝門が開いている ∧ 第1条件〔利払い÷歳入〕は限界に届いている（否定形＝NaN は第1条件が偽の側）\n"
    "        if (w.Coef.MaxInterestShareOfGdp.Value > 0)\n"
    "        {\n"
    "            double gm = w.NominalGdpPerMonth.Value;\n"
    "            st.GdpGateShare = gm > 0 ? li / gm : double.NaN;\n"
    "            st.GdpOnlyNow = st.GateOpenNow == 1 && !(w.LastInterestPerMonth < w.Coef.MaxInterestShareOfRevenue * w.LastRevenuePerMonth) ? 1 : 0;\n"
    "        }\n"
    "        else st.GdpOnlyNow = 0;\n"
    "    }\n")
rep("        if (st.GateOpenNow == 0 && st.WarNow) st.QWarGateClosedMonths++;\n",
    "        if (st.GateOpenNow == 0 && st.WarNow) st.QWarGateClosedMonths++;\n"
    "        if (st.GdpOnlyNow == 1) { st.QGdpOnlyMonths++; if (borrowed > 0) st.QGdpOnlyBorrowMonths++; }   // (315)\n")
rep("        st.QUnpaidObs = st.QUnpaidMonths = st.QMegaPass = st.QMegaOpen = 0; st.QUnpaidSum = st.QUnpaidMax = st.QDevalueShareSum = st.QDevalueShareMax = 0;\n",
    "        st.QUnpaidObs = st.QUnpaidMonths = st.QMegaPass = st.QMegaOpen = 0; st.QUnpaidSum = st.QUnpaidMax = st.QDevalueShareSum = st.QDevalueShareMax = 0;\n"
    "        st.QGdpOnlyMonths = st.QGdpOnlyBorrowMonths = 0;   // (315) 助走の分は数えない\n")
i=b.index("    static List<(string Name, string Value)> Extras289(WState st)")
j=b.rfind("    /// <summary>★(289) 線D",0,i)
assert j>0
extras=r'''    /// <summary>★(315) 線O′ 国×四半期の行の末尾：借入の門を第2条件〔利払い÷月の名目GDP〕だけで開けた月数と、うち実際に借りた月数（限界 <see cref="Coefficients.MaxInterestShareOfGdp"/> が 0 の腕では 0）。</summary>
    static List<(string Name, string Value)> Extras315(WState st)
    {
        string I(int v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return [("gateOpenedByGdpOnlyMonthsCount", I(st.QGdpOnlyMonths)), ("gateOpenedByGdpOnlyBorrowedMonthsCount", I(st.QGdpOnlyBorrowMonths))];
    }

'''
b=b[:j]+extras+b[j:]
rep("        var ext289 = Extras289(st);   // (289) 線D：国の所得の合計・国庫が実際に払った建設の額\n        foreach (var ex in ext289) cols.Add(ex.Value);\n",
    "        var ext289 = Extras289(st);   // (289) 線D：国の所得の合計・国庫が実際に払った建設の額\n        foreach (var ex in ext289) cols.Add(ex.Value);\n"
    "        var ext315 = Extras315(st);   // (315) 線O′：借入の門の第2条件\n        foreach (var ex in ext315) cols.Add(ex.Value);\n")
rep(r"""                + string.Join('\t', ext289.Select(e => e.Name)));
""",
    r"""                + string.Join('\t', ext289.Select(e => e.Name)) + "\t"
                + string.Join('\t', ext315.Select(e => e.Name)));
""")
rep("        C(\"unpaidBeforeDevaluationShare\", unpaid); C(\"devaluedToClaimShare\", double.IsNaN(unpaid) ? double.NaN : (st.Devalued > 0 ? st.Devalued : 0) / st.ClaimsTotal);\n",
    "        C(\"unpaidBeforeDevaluationShare\", unpaid); C(\"devaluedToClaimShare\", double.IsNaN(unpaid) ? double.NaN : (st.Devalued > 0 ? st.Devalued : 0) / st.ClaimsTotal);\n"
    "        C(\"interestToGdpShareAtGate\", st.GdpGateShare); C(\"gateOpenedByGdpOnlyFlag\", st.GdpOnlyNow < 0 ? double.NaN : st.GdpOnlyNow);   // (315) 門の第2条件が読んだ 利払い÷月の名目GDP（第2条件が有効な腕だけ）・第2条件だけで開いたか\n")
open(p,'wb').write(b.encode('utf-8'))
print('ok')
