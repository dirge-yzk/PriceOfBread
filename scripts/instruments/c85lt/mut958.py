# (958) c85lt：mut937.py の写し（中身は同じ・木は D:/PoB_work/c85lt_work/mut_tree＝隔離ビルドは c85lt_work/mut_art）。元：(937) c82lt 突然変異（事前登録 [5]）：使い捨ての木で 1 つずつ書き換え、LandTaxFixedBasisTests と LandfixInstrumentTests を回して赤の試験を表に書く。
#   python -X utf8 mut937.py <木（器のコミットを git archive で展開したフォルダ）> <出力.txt> [突然変異の名 ...]
#   隔離ビルド＝<木>/../mut_art（逐次で使い回す）。書き換えの前に元のファイルへ戻す（バイトで読み書き）。
import os, subprocess, sys

S = 'PriceOfBread.SimCore/'
MUT = {
    'M1pp': [(S + 'EconomyStep.cs', 'if (world.WarmupFrozen && !Colonial.AbsenteeRgo(region, s))',
              'if (world.WarmupFrozen && s.FrozenLandRentPerMonth is null && !Colonial.AbsenteeRgo(region, s))')],   # 形の変え直しの後：最初の助走の月で止め上書きしない
    'M2': [(S + 'EconomyStep.cs', 's.FrozenLandRentPerMonth = rent > 0 ? new MoneyPerMonth(rent) : null;',
            's.FrozenLandRentPerMonth = rent > 0 ? new MoneyPerMonth(rent * 12.0 / 0.05) : null;')],
    'M3': [(S + 'EconomyStep.cs', 'if (world.WarmupFrozen && !Colonial.AbsenteeRgo(region, s))',
            'if (!Colonial.AbsenteeRgo(region, s))')],
    'M4': [(S + 'EconomyStep.cs', '((ls is null ? f : f * ls[k]) - Math.Max(0, rk))',
            '((ls is null ? f * f / Math.Max(1e-9, rent) : f * f / Math.Max(1e-9, rent) * ls[k]) - Math.Max(0, rk))')],
    'M5': [(S + 'EconomyStep.cs', 'if (fz is { } f) region.Pops[rp].LandTaxBasisAdjPerMonth',
            'if (fz is { } f) region.Pops[s.OwnerPopIndex].LandTaxBasisAdjPerMonth')],
    'M6': [(S + 'EconomyStep.cs', '((ls is null ? f : f * ls[k]) - Math.Max(0, rk))',
            '((ls is null ? f : f * ls[k]) - Math.Max(0, rk) + (rp == s.LaborPopIndex ? region.LastSubsistenceOutput : 0))')],
    'M7p': [(S + 'EconomyStep.cs', 'if (world.WarmupFrozen && !Colonial.AbsenteeRgo(region, s))', 'if (world.WarmupFrozen)')],
    'M8p': [(S + 'Territory.cs', ' pr.FrozenLandRentPerMonth = null; }', ' }')],
    'M9': [(S + 'Fiscal.cs', '            var a = AssessPop(w, c, p, crownShare, scan.ScopeInc is null ? null : scan.ScopeInc[pi], incomeRate, progressiveRate, pollRate, landRate);',
            '            double fb9 = c.LandTaxFixedBasis; c.LandTaxFixedBasis = 0; var a = AssessPop(w, c, p, crownShare, scan.ScopeInc is null ? null : scan.ScopeInc[pi], incomeRate, progressiveRate, pollRate, landRate); c.LandTaxFixedBasis = fb9;')],
    'M10': [(S + 'Fiscal.cs', 'w.LandTax * Math.Max(0, LandTaxBase(c, p).Value)', 'w.LandTax * Math.Max(0, p.LandRentPerMonth.Value)')],
    'M11': [(S + 'EconomyStep.cs', 'if (coef.LandTaxFixedBasis > 0 && world is not null && world.LandTaxBasisFixed && s.IsRgo)', 'if (false)')],
    'M12': [(S + 'PolicyPreferences.cs', 'double lb = w.Coef.LandTaxFixedBasis > 0 ? System.Math.Max(0, Fiscal.LandTaxBase(w.Coef, p).Value) : rn;', 'double lb = rn;')],
    'M14': [(S + 'Fiscal.cs', 'if (landRate > 1e-9 && landBase.Value > 0)', 'if (landRate > 1e-9 && p.LandRentPerMonth.Value > 0)')],
}


def main():
    tree, outp = sys.argv[1], sys.argv[2]
    names = sys.argv[3:] or list(MUT)
    art = os.path.join(os.path.dirname(os.path.abspath(tree)), 'mut_art')
    files = sorted({f for m in MUT.values() for f, _, _ in m})
    orig = {f: open(os.path.join(tree, f), 'rb').read() for f in files}
    lines = ['# (937) mut937：突然変異ごとの赤の試験（LandTaxFixedBasisTests・LandfixInstrumentTests）・木 %s' % tree]
    for name in names:
        for f in files:
            open(os.path.join(tree, f), 'wb').write(orig[f])
        ok = True
        for f, old, new in MUT[name]:
            b = orig[f].decode('utf-8')
            if b.count(old) != 1:
                lines.append('%s 書き換えの錨が %d 本（%s）＝適用できない' % (name, b.count(old), f))
                ok = False
                break
            open(os.path.join(tree, f), 'wb').write(b.replace(old, new).encode('utf-8'))
        if not ok:
            continue
        log = os.path.join(os.path.dirname(os.path.abspath(outp)), 'mut937_%s.log' % name)
        with open(log, 'wb') as lg:
            rb = subprocess.call(['dotnet', 'build', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '-v', 'q'], stdout=lg, stderr=subprocess.STDOUT)
            rt = subprocess.call(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--no-build',
                                  '--filter', 'FullyQualifiedName~LandTaxFixedBasisTests|FullyQualifiedName~LandfixInstrumentTests'], stdout=lg, stderr=subprocess.STDOUT) if rb == 0 else -1
        txt = open(log, 'rb').read().decode('utf-8', 'replace')
        red = sorted({ln.split('.')[-1].split(' ')[0] for ln in txt.splitlines() if ln.strip().startswith('失敗 PriceOfBread.Tests.')})
        summ = [ln.strip() for ln in txt.splitlines() if '合計:' in ln]
        lines.append('%s build=%d test=%d 赤 %d：%s ｜ %s' % (name, rb, rt, len(red), '・'.join(red) if red else '（なし）', summ[-1] if summ else ''))
    for f in files:
        open(os.path.join(tree, f), 'wb').write(orig[f])
    open(outp, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
