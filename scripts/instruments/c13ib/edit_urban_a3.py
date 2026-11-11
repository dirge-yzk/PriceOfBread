# (812) c13ib I5 の A3：UrbanCapOff の焼き込み（既定 1 の経路だけ残す）と、旗 0 の枝だけが読んでいた物の削除。
import sys
sys.path.insert(0, __file__.rsplit('/', 1)[0] if '/' in __file__ else '.')
from edithelp import Editor

E = Editor(sys.argv[1])
S = 'PriceOfBread.SimCore/'
T = 'PriceOfBread.Tests/'

# ---- SimCore ---------------------------------------------------------------------------------
# 係数 4 本 → 墓標 1 行
E.remove_line(S + 'Coefficients.cs', '    public double UrbanCapFollowSpeed = 0.02;')
E.remove_line(S + 'Coefficients.cs', '    public double UrbanPerInd = 0.08;')
E.replace_line(S + 'Coefficients.cs', '    public double BaseUrbanCap = 0.20;',
               '    // 【墓標2026-11-03 (812)・旗の焼き込み】都市化の天井 BaseUrbanCap（0.20）・UrbanPerInd（0.08）・UrbanCapFollowSpeed（0.02）と旗 UrbanCapOff（(687) 線 urb2 が 2026-10-30 に 1 で点火）＝都市化の天井は置かない（設計思想⑧）。'
               '止めるのは所得差（目標の都市の割合）・飢えの線 FamineMigrationFillGate・農奴の門 SerfMobilityGate。天井の式 PopDecision.UrbanShareCap と平滑化の状態 Region.UrbanIndSmoothed も削除した（旧の枝は旗 0 の経路だけが読んでいた）。')
E.remove_line(S + 'Coefficients.cs', '    public double UrbanCapOff = 1;')
E.edit(S + 'Coefficients.cs', '（UrbanCapOff は (687) で既定 1 に点火）', '（UrbanCapOff は (687) で既定 1 に点火し (812) で焼き込んだ）')
# 状態
E.remove_line(S + 'Model.cs', '    public double UrbanIndSmoothed = -1.0;')
# 天井の式
E.remove_range(S + 'PopDecision.cs', 'private const double MaxUrbanShareCap', '=> Math.Clamp(c.BaseUrbanCap + indForCap * c.UrbanPerInd, 0, MaxUrbanShareCap);')
E.remove_range(S + 'PopDecision.cs', '// 都市化の上限（§2.4）：工業化度が都市化の天井を決める', 'if (targetUrban > maxUrbanShare) targetUrban = maxUrbanShare;',
               replacement='            // 都市化の天井は置かない（(812) 旗 UrbanCapOff の焼き込み・設計思想⑧）。止めるのは所得差（目標の割合）・飢えの線・農奴の門。')
# 閉じ括弧の行（ブロックの終わり）を 1 行消す：直前の置き換えの行の次の行が 12 空白の閉じ括弧
lines, nl = E._lines(S + 'PopDecision.cs')
i = next(k for k, l in enumerate(lines) if '都市化の天井は置かない（(812)'.encode('utf-8') in l)
assert lines[i + 1].rstrip(b'\r') == b'            }', lines[i + 1]
del lines[i + 1]
E._write(S + 'PopDecision.cs', nl.join(lines))
print('ok PopDecision close brace')
# 職の移しの残枠
E.edit(S + 'Meritocracy.cs',
       '    /// 同じ天井式（BaseUrbanCap＋工業化×UrbanPerInd・平滑状態 UrbanIndSmoothed は読み取りのみ・0.85は既存ハードストップの再利用）。\n'
       '    /// CapitalEnabled=false（天井の無い世界）とペア不成立は無制限。</summary>\n',
       '    /// 都市化の天井は無い（(812) 旗 UrbanCapOff の焼き込み）＝無制限。組の人口が 0 の州だけ 0。</summary>\n')
E.edit(S + 'Meritocracy.cs',
       '        if (c.UrbanCapOff > 0) return double.PositiveInfinity;   // ★(605) 旗 UrbanCapOff：都市化の天井なし（PopDecision.Run と同じ）\n'
       '        double indForCap = c.UrbanCapFollowSpeed < 1.0 && r.UrbanIndSmoothed >= 0 ? r.UrbanIndSmoothed : r.TotalIndustrialization;\n'
       '        double maxShare = PopDecision.UrbanShareCap(c, indForCap);\n'
       '        return Math.Max(0, maxShare * total - urban.Size);\n',
       '        return double.PositiveInfinity;   // 都市化の天井なし（PopDecision.Run と同じ）\n')
# 計器の写し
E.remove_range(S + 'InstrumentsUrx.cs', 'if (region.CapitalEnabled && !(coef.UrbanCapOff > 0))', 'if (target > cap) { target = cap; capBound = true; }',
               replacement='        // (812) 旗 UrbanCapOff の焼き込み：天井なし＝cap は 1.0・capBound は false のまま（計器の列は残す）')
lines, nl = E._lines(S + 'InstrumentsUrx.cs')
i = next(k for k, l in enumerate(lines) if '(812) 旗 UrbanCapOff の焼き込み：天井なし'.encode('utf-8') in l)
assert lines[i + 1].rstrip(b'\r') == b'        }', lines[i + 1]
del lines[i + 1]
E._write(S + 'InstrumentsUrx.cs', nl.join(lines))
print('ok InstrumentsUrx close brace')

# ---- Tests -----------------------------------------------------------------------------------
# Hz7IdentityTests：天井の式そのものの試験（式ごと消えた）
E.remove_method(T + 'Hz7IdentityTests.cs', '都市化の天井は基礎と工業化度の項を0から085に挟んだ手書きの式とビット単位で同じ')
E.remove_line(T + 'Hz7IdentityTests.cs', '    private static readonly double[] Inds = ')
# MilitaryTests・MobilityTests：効かない天井の目盛りの行
E.remove_line(T + 'MilitaryTests.cs', '            c.BaseUrbanCap = 0.85; c.UrbanPerInd = 0;')
E.remove_line(T + 'MobilityTests.cs', '            sim.World.Coef.BaseUrbanCap = 0.9;   // 天井に余裕のある状態（英は月20')
E.remove_line(T + 'MobilityTests.cs', '            sim.World.Coef.BaseUrbanCap = 0.9;   // 天井に余裕のある状態（天井の封鎖は専用テストが見る）')
E.remove_method(T + 'MobilityTests.cs', '失業プールは都市化天井の残枠を超えて農村から都市へ流さない')
# MigrationPairTests
E.edit(T + 'MigrationPairTests.cs',
       'r.CapitalEnabled = true; c.UrbanCapOff = 0; r.Industrialization = 1.0; r.UrbanIndSmoothed = -1.0;',
       'r.CapitalEnabled = true; r.Industrialization = 1.0;')
E.remove_range(T + 'MigrationPairTests.cs', '// 職の移しの天井：犯罪者（規模 0）が農村に化けると', 'Assert.Equal(Math.Max(0, maxShare * total - urban.Size), Headroom(r, c), 9);')
E.remove_range(T + 'MigrationPairTests.cs', 'private static double Headroom(Region r, Coefficients c)', 'typeof(Meritocracy).GetMethod("UrbanHeadroom"')
# UrxInstrumentTests
E.remove_line(T + 'UrxInstrumentTests.cs', '        double smoothed0 = r.UrbanIndSmoothed;')
E.remove_line(T + 'UrxInstrumentTests.cs', '        Assert.Equal(smoothed0, r.UrbanIndSmoothed);')
E.remove_line(T + 'UrxInstrumentTests.cs', '        Match("天井で切れる"')
E.replace_line(T + 'UrxInstrumentTests.cs', '        Match("天井なし（旗 1）"', '        Match("天井なし", 0.1, 0.5, 0.9, 0.9, capital: true);')
E.remove_range(T + 'UrxInstrumentTests.cs', 'var (r, _, _, c) = Board(0.1, 0.5, 0.9, 0.9, capital: true); c.UrbanCapOff = 0;', 'Assert.True(d.CapBound, "天井で切れた印が立たない");',
               replacement='        var (r, _, _, c) = Board(0.1, 0.5, 0.9, 0.9, capital: true);\n'
                           '        Assert.True(Instruments.UrxPdDrive(c, r, 1.0, out var dOff));\n'
                           '        Assert.False(dOff.CapBound, "天井の印が立った"); Assert.Equal(dOff.TargetPre, dOff.TargetCapped, 12);   // 天井なし＝印が立たず目標は切られない')
# IndustrializationReaderTests
E.remove_line(T + 'IndustrializationReaderTests.cs', '        Assert.Equal(1.0, c.UrbanCapOff);')
E.edit(T + 'IndustrializationReaderTests.cs', 'foreach (var flag in Flags.Where(x => x != "UrbanCapOff"))', 'foreach (var flag in Flags)')
E.edit(T + 'IndustrializationReaderTests.cs', '        "UrbanCapOff", "CapitalHeadroomFromReturn",   // (667)', '        "CapitalHeadroomFromReturn",   // (667)')
E.edit(T + 'IndustrializationReaderTests.cs',
       'public void 都市化の天井_旗1は天井が無く平滑化の状態も書かない_旗0は旧のまま()',
       'public void 都市化の天井は無く職の移しの残枠は無限()')
E.remove_line(T + 'IndustrializationReaderTests.cs', '        r.UrbanIndSmoothed = -1.0;')
E.remove_range(T + 'IndustrializationReaderTests.cs', 'c.UrbanCapOff = 1; r.Industrialization = 0.0; r.OldVintageCapital = 0;', 'Assert.True(r.UrbanIndSmoothed >= 0, "旗 0 で平滑化の状態が書かれない（旧）");',
               replacement='        r.Industrialization = 0.0; r.OldVintageCapital = 0;\n'
                           '        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));\n'
                           '        r.Industrialization = 5.0;\n'
                           '        Assert.True(double.IsPositiveInfinity(Headroom(r, c)));')
# PolicyPoliticsTests
E.edit(T + 'PolicyPoliticsTests.cs', '=> TaxChainCheck(capOff: true, minFixedMil: 0.0);   // 既定（UrbanCapOff=1）の物差しは (687) のまま変えない',
       '=> TaxChainCheck();   // (687) の物差しのまま変えない')
E.remove_range(T + 'PolicyPoliticsTests.cs', '/// <summary>★(697) 線 ins7（恒等・番人の追加だけ）', '=> TaxChainCheck(capOff: false, minFixedMil: 0.08);')
E.edit(T + 'PolicyPoliticsTests.cs', 'private static void TaxChainCheck(bool capOff, double minFixedMil)', 'private static void TaxChainCheck()')
E.remove_line(T + 'PolicyPoliticsTests.cs', '            w.Coef.UrbanCapOff = capOff ? 1 : 0;')
E.remove_line(T + 'PolicyPoliticsTests.cs', '        Assert.True(dMil >= minFixedMil,')
# DormantIdentityTests
E.edit(T + 'DormantIdentityTests.cs', '旗 0 の経路の番人は IndustrializationReaderTests（都市化の天井_旗1…旗0は旧のまま）。', '(812) で旗ごと焼き込んだ（RetiredFields）。')
E.edit(T + 'DormantIdentityTests.cs', '        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",\n',
       '        "NavalProcurementNeedsPort", "CrossBorderMigrationByOpportunity",\n'
       '        "UrbanCapOff", "BaseUrbanCap", "UrbanPerInd", "UrbanCapFollowSpeed", "UrbanIndSmoothed",   // 都市化の天井の旗と目盛り・Region の平滑化の状態（A3）\n')
