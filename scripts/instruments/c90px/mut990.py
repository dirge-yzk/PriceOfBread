# (990) c90px：mut990.py の写し（中身は同じ・木は D:/PoB_work/c85lt_work/mut_tree＝隔離ビルドは c85lt_work/mut_art）。元：(990) c90px 突然変異（事前登録 [5]）：使い捨ての木で 1 つずつ書き換え、LandTaxFixedBasisTests と LandfixInstrumentTests を回して赤の試験を表に書く。
#   python -X utf8 mut990.py <木（器のコミットを git archive で展開したフォルダ）> <出力.txt> [突然変異の名 ...]
#   隔離ビルド＝<木>/../mut_art（逐次で使い回す）。書き換えの前に元のファイルへ戻す（バイトで読み書き）。
import os, subprocess, sys

S = 'PriceOfBread.SimCore/'
MUT = {
    'M1': [(S + 'EconomyStep.cs', 'new MoneyPerMonth(world!.LandTaxBasisPhi * (', 'new MoneyPerMonth((1.0 - world!.LandTaxBasisPhi) * (')],   # GX2 の符号を反対（差分の係数を 1−φ へ）
    'M2': [(S + 'Government.cs', '        if (w.WarmupFrozen) return;\n        w.LandTaxBasisPhi', '        w.LandTaxBasisPhi')],   # 助走の歯止めを外す
    'M3': [(S + 'World.cs', '        if (Coef.LandTaxPhiMove > 0) LandTaxBasisPhi = src.LandTaxBasisPhi;', '')],   # CopyLawsFrom の行を外す
    'M4': [(S + 'Government.cs', 'PolicyAxis.LandTax, mandate: true)', 'PolicyAxis.LandTax, mandate: false)')],   # mandate＝false
    'M5': [(S + 'World.cs', '    public double LandTaxBasisPhi { get; internal set; }', '    [System.NonSerialized] public double LandTaxBasisPhiNs;\n    public double LandTaxBasisPhi { get => LandTaxBasisPhiNs; internal set => LandTaxBasisPhiNs = value; }')],   # 保存しない（反射は裏の欄を拾えない）
    'M6': [(S + 'World.cs', 'if (Coef.LandTaxPhiMove > 0) LandTaxBasisPhi = src.LandTaxBasisPhi;', 'LandTaxBasisPhi = src.LandTaxBasisPhi;')],   # 旗 0 でも写す
}


def main():
    tree, outp = sys.argv[1], sys.argv[2]
    names = sys.argv[3:] or list(MUT)
    art = os.path.join(os.path.dirname(os.path.abspath(tree)), 'mut_art')
    files = sorted({f for m in MUT.values() for f, _, _ in m})
    orig = {f: open(os.path.join(tree, f), 'rb').read() for f in files}
    lines = ['# (937) mut990：突然変異ごとの赤の試験（LandTaxFixedBasisTests・LandfixInstrumentTests）・木 %s' % tree]
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
        log = os.path.join(os.path.dirname(os.path.abspath(outp)), 'mut990_%s.log' % name)
        with open(log, 'wb') as lg:
            rb = subprocess.call(['dotnet', 'build', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '-v', 'q'], stdout=lg, stderr=subprocess.STDOUT)
            rt = subprocess.call(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--no-build',
                                  '--filter', 'FullyQualifiedName~LandTaxPhiTests'], stdout=lg, stderr=subprocess.STDOUT) if rb == 0 else -1
        txt = open(log, 'rb').read().decode('utf-8', 'replace')
        red = sorted({ln.split('.')[-1].split(' ')[0] for ln in txt.splitlines() if ln.strip().startswith('失敗 PriceOfBread.Tests.')})
        summ = [ln.strip() for ln in txt.splitlines() if '合計:' in ln]
        lines.append('%s build=%d test=%d 赤 %d：%s ｜ %s' % (name, rb, rt, len(red), '・'.join(red) if red else '（なし）', summ[-1] if summ else ''))
    for f in files:
        open(os.path.join(tree, f), 'wb').write(orig[f])
    open(outp, 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
