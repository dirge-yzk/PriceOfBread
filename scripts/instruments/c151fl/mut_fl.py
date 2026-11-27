# -*- coding: utf-8 -*-
"""(1416) c151fl 突然変異 μ1〜μ4（㉚）：使い捨ての木で 1 本ずつ当てて RepayFloorOnlyTests と FsxTests を回し、試験ごとの赤と緑を表にする。

使い方：python mut_fl.py <使い捨ての木> <成果物のフォルダ> <出力の表>
各突然変異は Fiscal.cs の返済の 1 行（旗 1 の枝）をバイトで書き換え、試験の後に git checkout で戻す。
"""
import sys, os, subprocess, re

TREE, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
TP = 'PriceOfBread.SimCore/Fiscal.cs'
MP = TP
O = 'Math.Min(w.GovDebt.Value, c.RepayFloorOnly > 0 ? floorRepay : '
MUTS = {
    'M0': [],   # 突然変異なし（対照）
    'μ1': [(TP, O, 'Math.Min(w.GovDebt.Value, c.RepayFloorOnly > 0 ? Math.Min(spare, Math.Max(floorRepay, excess)) : ')],   # 何もしない（旧の式）
    'μ2': [(TP, O, 'Math.Min(w.GovDebt.Value, c.RepayFloorOnly > 0 ? Math.Min(spare, excess) : ')],                          # 余りだけを返す
    'μ3': [(TP, O, 'c.RepayFloorOnly > 0 ? floorRepay : Math.Min(w.GovDebt.Value, ')],                                       # 旗 1 の枝で min(GovDebt) を外す
    'μ4': [(TP, O, 'Math.Min(w.GovDebt.Value, c.RepayFloorOnly > 0 ? spare : ')],                                            # 床を外し余剰まで返す
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
    subprocess.run(['git', '-C', TREE, 'checkout', '--', TP], check=True)


def run(tag):
    trx = os.path.join(ART, f'mut_{tag}.trx').replace('\\', '/')
    log = os.path.join(ART, f'mut_{tag}.log')
    with open(log, 'wb') as lf:
        subprocess.run(['dotnet', 'test', os.path.join(TREE, 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'), '-c', 'Release',
                        '--artifacts-path', os.path.join(ART, 'art'), '--filter', 'FullyQualifiedName~RepayFloorOnlyTests|FullyQualifiedName~FsxTests',
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
