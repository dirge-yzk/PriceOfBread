# -*- coding: utf-8 -*-
"""(1407) c150rp 突然変異 M1〜M5（㉚）：使い捨ての木で 1 本ずつ当てて RailProcureNationalTests を回し、試験ごとの赤と緑を表にする。

使い方：python mut_rp.py <使い捨ての木> <成果物のフォルダ> <出力の表>
各突然変異は Transport.cs（M4 は MultiNationSim.cs も）をバイトで書き換え、試験の後に git checkout で戻す。
"""
import sys, os, subprocess, re

TREE, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
TP = 'PriceOfBread.SimCore/Transport.cs'
MP = 'PriceOfBread.SimCore/MultiNationSim.cs'

MUTS = {
    'M0': [],   # 突然変異なし（対照）
    'M1': [(TP, 'if (byReg && c.RailProcureNational > 0 && want - buy > 1e-9) buy += ProcureNational',
                'if (byReg && c.RailProcureNational > 1e300 && want - buy > 1e-9) buy += ProcureNational')],
    'M2': [(TP, 'double fr = c.FreightRatePer10Mm * (km * 0.5 * (kA + kB)) / Diplomacy.DistanceNormKm;',
                'double fr = 0.0 * c.FreightRatePer10Mm * (km * 0.5 * (kA + kB)) / Diplomacy.DistanceNormKm;')],
    'M3': [(TP, 'double got = InternationalMarket.Procure(w, src, gi, take, investor);',
                'double got = InternationalMarket.Procure(w, src, gi, take, investor); src.Markets[gi].Stock += got > 1e-9 ? got : 0.0;')],
    'M4': [(TP, 'foreach (var b in w.Regions)\n        {\n            if (ReferenceEquals(b, r) || gi >= b.Markets.Length) continue;',
                'foreach (var b in MutAll ?? w.Regions)\n        {\n            if (ReferenceEquals(b, r) || gi >= b.Markets.Length) continue;'),
           (TP, '    private static (double K, double RailK) LandMul(',
                '    internal static List<Region>? MutAll;\n    private static (double K, double RailK) LandMul('),
           (MP, 'public void StepQuarter() { for (int d = 0; d < Calendar.DaysPerQuarter; d++) StepDay(); }',
                'public void StepQuarter() { var all = new List<Region>(); foreach (var n0 in Nations) all.AddRange(n0.World.Regions); Transport.MutAll = all; for (int d = 0; d < Calendar.DaysPerQuarter; d++) StepDay(); }')],
    'M5': [(TP, 'if (src is null || unit < bestUnit) { src = b;', 'if (src is null) { src = b;')],
}


def apply(edits):
    for p, o, n in edits:
        fp = os.path.join(TREE, p)
        b = open(fp, 'rb').read()
        ob, nb = o.encode('utf-8'), n.encode('utf-8')
        if b.count(ob) != 1:
            raise SystemExit(f'突然変異の当て先が 1 本でない：{p} {o[:60]}（{b.count(ob)}）')
        open(fp, 'wb').write(b.replace(ob, nb))


def restore():
    subprocess.run(['git', '-C', TREE, 'checkout', '--', TP, MP], check=True)


def run(tag):
    trx = os.path.join(ART, f'mut_{tag}.trx').replace('\\', '/')
    log = os.path.join(ART, f'mut_{tag}.log')
    with open(log, 'wb') as lf:
        subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release',
                        '--artifacts-path', os.path.join(ART, 'art'), '--filter', 'FullyQualifiedName~RailProcureNationalTests',
                        '--logger', f'trx;LogFileName={trx}'], stdout=lf, stderr=subprocess.STDOUT)
    res = {}
    if os.path.exists(trx):
        t = open(trx, 'rb').read().decode('utf-8', 'replace')
        for m in re.finditer(r'<UnitTestResult [^>]*testName="([^"]+)"[^>]*outcome="([^"]+)"', t):
            res[m.group(1).split('.')[-1]] = m.group(2)
    else:
        res['(ビルド)'] = 'Failed'
    return res


rows = []
for tag, ed in MUTS.items():
    restore()
    apply(ed)
    try:
        res = run(tag)
    finally:
        restore()
    for k in sorted(res):
        rows.append(f'{tag}\t{k}\t{"赤" if res[k] != "Passed" else "緑"}\t{res[k]}')
    with open(OUT, 'wb') as f:
        f.write(('突然変異\t試験\t色\t結果\n' + '\n'.join(rows) + '\n').encode('utf-8'))
with open(OUT + '.done', 'wb') as f:
    f.write(b'done\n')
