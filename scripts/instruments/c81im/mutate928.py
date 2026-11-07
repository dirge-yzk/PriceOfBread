#!/usr/bin/env python3
"""(928) 突然変異 M1〜M6 を 1 本ずつ当て、ImposeAndCrownSourceTests が赤になるかを記録する（ファイルはバイトで読み書きし、各回の後に元へ戻す）。
使い方：python scripts/instruments/c81im/mutate928.py <木> <出力 txt>  （木は作業ツリーのルート・事前にコミット済みで差分が無いこと）"""
import subprocess, sys, os
tree, out = sys.argv[1], sys.argv[2]
ART = 'D:/PoB_work/c81im_art2'
MUTS = [
 ('M1 ImpAfter の呼び口を外す', 'PriceOfBread.SimCore/Diplomacy.cs',
  b'if (impPre is not null) Instruments.ImpAfter(', b'if (false) Instruments.ImpAfter('),
 ('M2 orderInMonthIndex を常に 1', 'PriceOfBread.SimCore/InstrumentsImpose.cs',
  b'crushing ? "1" : "0", I(n)));', b'crushing ? "1" : "0", I(1)));'),
 ('M3 Transition の口の符号を 0', 'PriceOfBread.SimCore/Politics.cs',
  b'Instruments.CrownSource(w, 1);', b'Instruments.CrownSource(w, 0);'),
 ('M4 SettleRevolt の口を外す', 'PriceOfBread.SimCore/Politics.cs',
  b'if (Instruments.On) Instruments.CrownSource(w, 3); }', b'if (false) Instruments.CrownSource(w, 3); }'),
 ('M5 crownRegisterSourceCode の列を常に 0', 'PriceOfBread.SimCore/InstrumentsCrown.cs',
  b'I(CrownSourceOf(w)));', b'I(0));'),
 ('M6 loserCrownBeforeCode を矯正の後の値で読む', 'PriceOfBread.SimCore/InstrumentsImpose.cs',
  b'I((int)pre.Crown)', b'I((int)dw.Crown)'),
 ('M7 廃位の口を外す', 'PriceOfBread.SimCore/CrownParliament.cs',
  b'Instruments.CrownSourceIfOn(w, 4);', b'/* */'),
 ('M8 開幕の口を外す', 'PriceOfBread.SimCore/Simulation.cs',
  b'if (Instruments.On) Instruments.CrownSource(World, 0);', b'/* */'),
]
res = []
for name, rel, old, new in MUTS:
    p = os.path.join(tree, rel)
    orig = open(p, 'rb').read()
    if orig.count(old) != 1:
        res.append(f'{name}\t適用できず（{orig.count(old)} 箇所）'); continue
    open(p, 'wb').write(orig.replace(old, new))
    try:
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests'), '-c', 'Release', '--artifacts-path', ART,
                            '--filter', 'FullyQualifiedName~ImposeAndCrownSourceTests'], capture_output=True, text=True, encoding='utf8', errors='replace')
        txt = r.stdout + r.stderr
        if 'error CS' in txt: v = 'ビルド失敗'
        elif '失敗!' in txt:
            fails = [l.strip() for l in txt.splitlines() if l.strip().startswith('失敗 ')]
            v = '赤：' + ' / '.join(f[:60] for f in fails)
        elif '成功!' in txt: v = '緑のまま（穴）'
        else: v = '不明'
    finally:
        open(p, 'wb').write(orig)
    res.append(f'{name}\t{rel}\t{v}')
    print(res[-1], flush=True)
open(out, 'wb').write(('\n'.join(res) + '\n').encode('utf8'))
