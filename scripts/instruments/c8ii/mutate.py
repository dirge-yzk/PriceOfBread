#!/usr/bin/env python
"""(781) c8ii 機構のテストの突然変異：コミット済みの木の 1 ファイルを壊し、別の作業フォルダにビルドして InheritIntentTests を回し、赤になるテストを記録する（壊した後は git checkout で戻す）。
使い方：python scripts/instruments/c8ii/mutate.py <出力テキスト>   （長い＝Bash を run_in_background で。1 変異あたりビルド約 1 分＋テスト約 20 秒）"""
import subprocess, sys, os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
ART = 'D:/PoB_work/c8ii/art_mut'
INS = 'PriceOfBread.SimCore/InstrumentsInheritIntent.cs'
# （名前, ファイル, 古い文, 新しい文, 赤になるはずのテスト）
MUTS = [
    ('M1_登録簿を外す（消えた送り手の行を出さない）', INS,
     "            foreach (var w in preg.Touched)\n                if (InheritAccs.TryGetValue(w, out var acc) && acc.Pending.Count > 0) EmitAcc(w, acc);\n",
     "            foreach (var w in preg.Touched) { }\n",
     '再併合で送り手がsimから消えてもEの行が出る'),
    ('M2_事前登録の列を棟数のまま（工事中の+1 を落とす）', INS,
     "int prereg = Math.Min(ev.Old, ev.InProg ? ev.Built + 1 : ev.Built);",
     "int prereg = Math.Min(ev.Old, ev.Built);",
     '事前登録の規則の列は工事中なら棟数足す1_それ以外は棟数'),
    ('M3_窓が閉じた後も並行の着工を数える', INS,
     "            if (!row.InProg || row.Done) continue;\n            if (row.Reg == r)",
     "            if (!row.InProg) continue;\n            if (row.Reg == r)",
     '継いだ工事の窓の間の並行の着工と自国の基準を数える'),
    ('M4_自国の基準を受け手の別の州でなく移った州で数える', INS,
     "            if (row.Reg == r) { if (row.Prod != p) row.Par++; continue; }\n",
     "            if (row.Reg == r) { if (row.Prod != p) { row.Par++; row.Base++; } continue; }\n",
     '継いだ工事の窓の間の並行の着工と自国の基準を数える'),
    ('M5_計器が世界を書く（Before/After の After で棟を動かす）', INS,
     "        foreach (var ev in acc.Pending) if (ev.Reg == r && ev.Used < 0) ev.Used = ev.Prod.StateTarget;",
     "        foreach (var ev in acc.Pending) if (ev.Reg == r && ev.Used < 0) { ev.Used = ev.Prod.StateTarget; ev.Prod.BuiltScale += 1e-9; }",
     '計器は世界を書かない_計器なしと計器つきで世界の量がビット一致'),
]


def run(cmd, **kw):
    return subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True, encoding='utf-8', errors='replace', **kw)


out = open(sys.argv[1], 'w', encoding='utf-8')
def say(s):
    print(s); out.write(s + '\n'); out.flush()

say('# (781) c8ii 機構のテストの突然変異（コミット済みの木 %s）' % run(['git', 'rev-parse', '--short', 'HEAD']).stdout.strip())
for name, f, old, new, expect in MUTS:
    p = os.path.join(ROOT, f)
    src = open(p, 'rb').read().decode('utf-8')
    if src.count(old) != 1:
        say('%s\t変異の当てはめ失敗（古い文が %d 箇所）' % (name, src.count(old))); continue
    open(p, 'wb').write(src.replace(old, new).encode('utf-8'))
    try:
        b = run(['dotnet', 'build', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '--disable-build-servers'])
        if b.returncode != 0:
            say('%s\tビルド失敗' % name + '\n' + b.stdout[-600:]); continue
        t = run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--no-build', '--artifacts-path', ART, '--filter', 'FullyQualifiedName~InheritIntentTests'])
        failed = [ln.strip() for ln in t.stdout.splitlines() if ln.strip().startswith(('失敗', 'Failed')) and 'InheritIntentTests' in ln or ln.strip().startswith(('失敗 PriceOfBread', 'Failed PriceOfBread'))]
        tail = [ln for ln in t.stdout.splitlines() if '合格' in ln or 'Passed' in ln][-1:] or ['(要約なし)']
        hit = any(expect in ln for ln in t.stdout.splitlines() if ln.lstrip().startswith(('失敗', 'Failed')))
        say('%s\t赤になるはずのテスト＝%s\t赤になった＝%s\t%s\t%s' % (name, expect, 'はい' if hit else '【いいえ】', ' / '.join(x.strip() for x in failed)[:300], tail[0].strip()))
    finally:
        run(['git', 'checkout', '--', f])
say('# 終わり（木は git checkout で戻した）：' + run(['git', 'status', '--short']).stdout.strip())
