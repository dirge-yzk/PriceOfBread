namespace PriceOfBread.SimCore;

/// <summary>(1256) 計器のフックを表ごとに閉じる：書く表がどれも選ばれていなければ真偽が偽＝フックの入口で積算ごと飛ぶ。CearnOn と同じ static bool で、SelectTables・Configure・型の初期化で決め直す（呼びごとに HashSet を引かない）。共有する状態を持つ組は 1 つの bool に束ねる（対応表 measurements/m1256_*_hookmap.tsv）。</summary>
internal static class Tg
{
    internal static bool Admin = true;
    internal static bool AiBuild = true;
    internal static bool Anchor = true;
    internal static bool Arms = true;
    internal static bool Basisx = true;
    internal static bool BuyShadow = true;
    internal static bool Cede = true;
    internal static bool Cess = true;
    internal static bool Chem = true;
    internal static bool Civwar = true;
    internal static bool Clock = true;
    internal static bool Cpwd = true;
    internal static bool Dipcool = true;
    internal static bool Discontent = true;
    internal static bool Divhost = true;
    internal static bool Ed = true;
    internal static bool Equipment = true;
    internal static bool Fb = true;
    internal static bool Fd = true;
    internal static bool House = true;
    internal static bool Idn = true;
    internal static bool Ivf = true;
    internal static bool LawGate = true;
    internal static bool Lw = true;
    internal static bool Lwk = true;
    internal static bool Mintcap = true;
    internal static bool Pay = true;
    internal static bool Paysale = true;
    internal static bool Portch = true;
    internal static bool Prdf = true;
    internal static bool Prds = true;
    internal static bool Pxstep = true;
    internal static bool RailSpend = true;
    internal static bool Rk = true;
    internal static bool SatNeeds = true;
    internal static bool Sb = true;
    internal static bool Scalars = true;
    internal static bool Sf = true;
    internal static bool SilverParity = true;
    internal static bool Silverst = true;
    internal static bool SoldierVac = true;
    internal static bool Subst = true;
    internal static bool TreatyPort = true;
    internal static bool U4 = true;
    internal static bool Wp = true;

    static Tg() { Refresh(); }

    /// <summary>選びの今の値（Instruments.Wants）から全ての bool を決め直す。</summary>
    internal static void Refresh()
    {
        Admin = Instruments.Wants("admrule_s");
        AiBuild = Instruments.Wants("aib_s");
        Anchor = Instruments.Wants("anchor_s");
        Arms = Instruments.Wants("armsfill_s");
        Basisx = Instruments.Wants("basisx_s") || Instruments.Wants("cearn_s") || Instruments.Wants("cgrp_s") || Instruments.Wants("corncf_s") || Instruments.Wants("cornint_s") || Instruments.Wants("cpop_s") || Instruments.Wants("landbase_s") || Instruments.Wants("landbasereg_s") || Instruments.Wants("landfix_s") || Instruments.Wants("landsale_s") || Instruments.Wants("landshare_s") || Instruments.Wants("lordstat_s");
        BuyShadow = Instruments.Wants("minecap_s") || Instruments.Wants("nationgood_s") || Instruments.Wants("prodcap_s") || Instruments.Wants("recipeout_s") || Instruments.Wants("s4cost_s") || Instruments.Wants("s7staple_s") || Instruments.Wants("sellr_s") || Instruments.Wants("t1buy_s") || Instruments.Wants("tb_s") || Instruments.Wants("tradeskip_s") || Instruments.Wants("trbin_s") || Instruments.Wants("trnation_s") || Instruments.Wants("trrail_s") || Instruments.Wants("trroute_s") || Instruments.Wants("trstat_s") || Instruments.Wants("tskpair_s") || Instruments.Wants("tskshadow_s");
        Cede = Instruments.Wants("cederule_s") || Instruments.Wants("center_s") || Instruments.Wants("cpeace_s") || Instruments.Wants("cpmed_s") || Instruments.Wants("crownpc_s") || Instruments.Wants("dep_s") || Instruments.Wants("formation2_s") || Instruments.Wants("ideology_s") || Instruments.Wants("imp_s") || Instruments.Wants("monai_s") || Instruments.Wants("mongood_s") || Instruments.Wants("monlaw_s") || Instruments.Wants("parlseat_s") || Instruments.Wants("poppref_s") || Instruments.Wants("rule_s") || Instruments.Wants("tensionwhy_s") || Instruments.Wants("unifwar_s") || Instruments.Wants("warband_s") || Instruments.Wants("warentry_s") || Instruments.Wants("warpair_s");
        Cess = Instruments.Wants("cess_s") || Instruments.Wants("cessp_s");
        Chem = Instruments.Wants("chem_s") || Instruments.Wants("chemgood_s");
        Civwar = Instruments.Wants("cwmult_s");
        Clock = Instruments.Wants("clock_s");
        Cpwd = Instruments.Wants("cpwd_s") || Instruments.Wants("cpwdft_s") || Instruments.Wants("cpworld_s");
        Dipcool = Instruments.Wants("dipcool_s");
        Discontent = Instruments.Wants("dcont_s");
        Divhost = Instruments.Wants("divhost_s");
        Ed = Instruments.Wants("ed_s");
        Equipment = Instruments.Wants("eq_s") || Instruments.Wants("eqr_s");
        Fb = Instruments.Wants("fb_s");
        Fd = Instruments.Wants("fd_s");
        House = Instruments.Wants("house_s");
        Idn = Instruments.Wants("idn_s");
        Ivf = Instruments.Wants("ivf_s");
        LawGate = Instruments.Wants("lawgate_s");
        Lw = Instruments.Wants("labor_s");
        Lwk = Instruments.Wants("age_s") || Instruments.Wants("lwk_s") || Instruments.Wants("pop_s") || Instruments.Wants("rdh_s") || Instruments.Wants("urx_s") || Instruments.Wants("urxpd_s") || Instruments.Wants("urxrt_s");
        Mintcap = Instruments.Wants("mintcap_s") || Instruments.Wants("mintreg_s");
        Pay = Instruments.Wants("pay_s");
        Paysale = Instruments.Wants("paysale2_s");
        Portch = Instruments.Wants("portch_s") || Instruments.Wants("portlm_s");
        Prdf = Instruments.Wants("prdf_s");
        Prds = Instruments.Wants("prds_s");
        Pxstep = Instruments.Wants("csc_s") || Instruments.Wants("cscx_s") || Instruments.Wants("pxstep_s");
        RailSpend = Instruments.Wants("railspend_s");
        Rk = Instruments.Wants("nation_s") || Instruments.Wants("reach_s") || Instruments.Wants("rponset_s") || Instruments.Wants("rulepeace_s") || Instruments.Wants("rulepower_s") || Instruments.Wants("rulepoweralloc_s") || Instruments.Wants("score_s");
        SatNeeds = Instruments.Wants("satneeds_s");
        Sb = Instruments.Wants("sbr_s") || Instruments.Wants("sbrreg_s") || Instruments.Wants("sbu_s");
        Scalars = Instruments.Wants("scalars_s");
        Sf = Instruments.Wants("sf_s");
        SilverParity = Instruments.Wants("silverpar_s");
        Silverst = Instruments.Wants("silverst_s") || Instruments.Wants("silverwt_s");
        SoldierVac = Instruments.Wants("soldiervac_s");
        Subst = Instruments.Wants("subst_s");
        TreatyPort = Instruments.Wants("tport_s");
        U4 = Instruments.Wants("consf_s");
        Wp = Instruments.Wants("wp_s");
    }
}
