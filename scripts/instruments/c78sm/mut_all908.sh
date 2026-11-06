#!/usr/bin/env bash
# (908) 突然変異の一式（木の中を 1 か所ずつ壊して戻す＝実行中はその木にコミットしない）。出力＝D:/PoB_work/c78sm_work/mutation.txt
T=D:/PoB_work/wt_c78sm; O=D:/PoB_work/c78sm_work
PY="python -I $T/scripts/instruments/c78sm/mutate908.py $T"
L=PriceOfBread.SimCore/InstrumentsLawgate2.cs; C=PriceOfBread.SimCore/InstrumentsCrown.cs
$PY $O/mut_lg.txt "FullyQualifiedName~Lawgate2InstrumentTests" \
 "M1_理由を全て1@@$L@@int reason = code == 5 ? Lawgate2ReasonIrreversible(axis) : Lawgate2ReasonPrereq(w, axis, ci + 1, ci);@@int reason = 1;" \
 "M2_緊縮の上限を外す@@$L@@if (target > (int)System.Math.Floor(room) && target > cur) return 8;@@if (false) return 8;" \
 "M3_符号5を控えない@@$L@@if (code is not (5 or 6)) { pend.Rows.Remove(idx); return; }@@if (code is not 6) { pend.Rows.Remove(idx); return; }" \
 "M4_呼ばない@@PriceOfBread.SimCore/Instruments.cs@@        Lawgate2Note(w, code, ci, ti);@@        " \
 "M5_緊縮は官僚が資格任用のときだけ（順序の取り違え）@@$L@@if (a == PolicyAxis.SocialSecurity)@@if (a == PolicyAxis.SocialSecurity && meritOk)"
$PY $O/mut_cr.txt "FullyQualifiedName~CrownPcInstrumentTests" \
 "M7a_②の下限を外す@@$C@@r.T2Zero = raw < 0;\n                pc = System.Math.Max(0, raw);@@r.T2Zero = raw < 0;\n                pc = raw;" \
 "M7a2_②'の下限を外す@@$C@@r.T2pZero = raw < 0;\n                pc = System.Math.Max(0, raw);@@r.T2pZero = raw < 0;\n                pc = raw;" \
 "M7b_②の正統性の因子を外す@@$C@@(x.CrownS - x.Rep) * x.Legitimacy;@@(x.CrownS - x.Rep);" \
 "M7c_軍の力の(1-PC)を外す@@$C@@c.ArmyPoliticalWeight * x.SoldierShare * (1.0 - RuleClamp01(pc))@@c.ArmyPoliticalWeight * x.SoldierShare"
$PY $O/mut_ls.txt "FullyQualifiedName~LandShareTests" \
 "LsMax_最大値を取らない@@PriceOfBread.SimCore/InstrumentsLandshare.cs@@static void LsMax(ref double max, double v) { if (double.IsNaN(max)) return; if (double.IsNaN(v) || v > max) max = v; }@@static void LsMax(ref double max, double v) { }"
cat $O/mut_lg.txt $O/mut_cr.txt $O/mut_ls.txt > $O/mutation.txt
