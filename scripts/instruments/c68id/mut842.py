# -*- coding: utf-8 -*-
# (842) c68id 突然変異の台本：使い捨ての写し（既定 D:/PoB_work/c68id_mut）に 1 件ずつ変更を入れ、隔離ビルドして指定の試験を回し、赤か緑かを記録する。
# 使い方：python mut842.py <写しの木> <隔離ビルドの置き場> <出力ファイル> [<印を付ける名前,...>]
# 各変更は（名前・ファイル・旧・新・試験の絞り込み・期待）の 1 組。旧は写しのファイルにちょうど 1 回出る文字列（バイトで読み書き）。終わるたびに元のバイトへ戻す。
import os, subprocess, sys, time
sys.stdout.reconfigure(encoding='utf-8')
TREE, ART, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
ONLY = set(sys.argv[4].split(',')) if len(sys.argv) > 4 and sys.argv[4] else None
DOTNET = r'C:\Program Files\dotnet\dotnet.exe'
CS = 'PriceOfBread.Tests/PriceOfBread.Tests.csproj'
CEARN = 'FullyQualifiedName~CearnInstrumentTests'
M = [
 # 名前, ファイル, 旧, 新, 試験の絞り込み, 期待
 ('M1 WarPushSpeed を 0.11 に', 'PriceOfBread.SimCore/Coefficients.cs',
  'public double WarPushSpeed = 0.10;', 'public double WarPushSpeed = 0.11;', 'FullyQualifiedName~CoefficientRelationTests', '赤（新しい既定の固定の Fact）'),
 ('M2 cgrp_s の nan を数に戻す', 'PriceOfBread.SimCore/InstrumentsCearn.cs',
  'F(started ? g.Start : double.NaN)', 'F(g.Start)', CEARN, '赤（G7）'),
 ('M2b cgrp_s の残差の nan を数に戻す', 'PriceOfBread.SimCore/InstrumentsCearn.cs',
  'cols.Add(F(started ? sv[gi] - g.Start - net[gi] : double.NaN));', 'cols.Add(F(sv[gi] - g.Start - net[gi]));', CEARN, '赤（G7）'),
 ('M3 m̄ から首都を外す', 'PriceOfBread.SimCore/InstrumentsRule.cs',
  'foreach (var r in w.Regions) { var (cm, cs) = RuleCrowdMilitancy(r); crowdMil += cm; crowdSize += cs; }',
  'foreach (var r in w.Regions) { if (ReferenceEquals(r, w.Regions[0])) continue; var (cm, cs) = RuleCrowdMilitancy(r); crowdMil += cm; crowdSize += cs; }', 'FullyQualifiedName~RuleInstrumentTests', '赤（T5）'),
 ('M4 遅れた R に今の R を入れる', 'PriceOfBread.SimCore/InstrumentsRule.cs',
  'rLag = lagSt.Prev;', 'rLag = rNow;', 'FullyQualifiedName~RuleInstrumentTests', '赤（T7）'),
 ('M5 旗の `> 0` を `>= 0` に', 'PriceOfBread.SimCore/InstrumentsCornint.cs',
  'I(intGrain[0] > 0 ? 1 : 0)', 'I(intGrain[0] >= 0 ? 1 : 0)', 'FullyQualifiedName~CornintInstrumentTests', '赤（C6）'),
 ('W1 CpopRows の呼びを外す', 'PriceOfBread.SimCore/InstrumentsCearn.cs',
  '        CpopRows(sk, seed, q, w, nation, r, gStart, net);   // (836) 同じ州の POP の行', '', CEARN, '赤（G5）'),
 ('W2 CgMonthClose の呼びを外す', 'PriceOfBread.SimCore/Instruments.cs',
  '        CgMonthClose(w);   // (829)', '        // CgMonthClose(w);   // (829)', CEARN, '赤（G5）'),
 ('W3 CearnRegister の呼びを外す', 'PriceOfBread.SimCore/Instruments.cs',
  '        CearnRegister(w);   // (822)', '        // CearnRegister(w);   // (822)', CEARN, '赤'),
 ('W4 CearnTable の CgReset を外す', 'PriceOfBread.SimCore/InstrumentsCearn.cs',
  'a.Clear(); a.Start = sn.Savings; a.Started = true; CgReset(a, r);', 'a.Clear(); a.Start = sn.Savings; a.Started = true;', CEARN, '赤（G5 の monthsClosed）'),
 ('W5 CearnTable の CearnOn の門を外す', 'PriceOfBread.SimCore/InstrumentsCearn.cs',
  '        if (!CearnOn) return;   // (829) 環境変数の門（POB_CEARN=0 で cearn_s と cgrp_s を書かない）', '', CEARN, '赤（G6）'),
 ('W6 CearnWarm の呼びを外す', 'PriceOfBread.SimCore/Instruments.cs',
  'if (time == "warm") CearnWarm(sim);', 'if (time == "warm") { }', CEARN, '赤か緑（助走の流れを捨てる）'),
]
res = []
for name, rel, old, new, flt, want in M:
    tag = name.split()[0]
    if ONLY and tag not in ONLY: continue
    p = os.path.join(TREE, rel)
    orig = open(p, 'rb').read()
    txt = orig.decode('utf-8')
    if txt.count(old) != 1:
        res.append((name, '台本の失敗：旧の文字列が %d 回' % txt.count(old), want)); continue
    open(p, 'wb').write(txt.replace(old, new).encode('utf-8'))
    try:
        t0 = time.time()
        b = subprocess.run([DOTNET, 'build', os.path.join(TREE, CS), '-c', 'Release', '--artifacts-path', ART, '--disable-build-servers', '-v', 'q'], capture_output=True, text=True, encoding='utf-8', errors='replace')
        if b.returncode != 0:
            res.append((name, 'ビルドの失敗', want)); continue
        r = subprocess.run([DOTNET, 'test', os.path.join(TREE, CS), '-c', 'Release', '--no-build', '--artifacts-path', ART, '--filter', flt], capture_output=True, text=True, encoding='utf-8', errors='replace')
        out = r.stdout.splitlines()
        summ = [l for l in out if l.startswith('成功!') or l.startswith('失敗!')]
        failed = [l.strip() for l in out if l.strip().startswith('失敗 ')]
        res.append((name, ('赤' if r.returncode != 0 else '緑') + '｜' + (summ[0] if summ else '') + '｜' + ' / '.join(failed[:4]) + '｜%.0f秒' % (time.time() - t0), want))
    finally:
        open(p, 'wb').write(orig)
    with open(OUT, 'w', encoding='utf-8') as f:
        for n, got, w in res: f.write('%s\t%s\t期待：%s\n' % (n, got, w))
with open(OUT, 'w', encoding='utf-8') as f:
    for n, got, w in res: f.write('%s\t%s\t期待：%s\n' % (n, got, w))
print('done', len(res))
