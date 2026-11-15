# (1168) c115sb：突然変異の表（事前登録 [3]）。使い捨ての木（git worktree）で 1 本ずつ当て、対象の試験の赤を確かめる。
# 使い方：python mut_sb.py <使い捨ての木> <隔離ビルドの出力> <結果の出力ファイル>
import subprocess, sys, os
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
I = 'PriceOfBread.SimCore/Instruments/InstrumentsMintreg.cs'
S = 'PriceOfBread.SimCore/Simulation.cs'
N = 'PriceOfBread.SimCore/MultiNationSim.cs'
C = 'PriceOfBread.SimCore/Credit.cs'
MR = 'FullyQualifiedName~MintregInstrumentTests'
M = {
 'M1': (I, [('if (m.Stock <= 1e-12) { a.NoStock++; continue; }', 'if (m.Stock <= -1) { a.NoStock++; continue; }')], MR),
 'M2': (I, [('if (m.Price > parity && !atM) { if (gapStart', 'if (m.Price >= 0 && !atM) { if (gapStart')], MR),
 'M3': (I, [('bool atM = m.Price > parity && gapLeft > 1e-9;', 'bool atM = m.Price > parity && gapLeft > -1;')], MR),
 'M4': (I, [('bool atM = m.Price > parity && gapLeft > 1e-9;', 'bool atM = m.Price > parity && gapLeft > 1e-9; if (m.Price > parity) a.AtMarket++;')], MR),
 'M5': (I, [('if (w.WarmupFrozen || !Wants("mintreg_s")) return;', 'if (w.WarmupFrozen) return;')], MR),
 'M6': (S, [('if (shortfall > 1e-9)\n                gapLeft = Math.Max(0, (coverTarget', 'if (true)\n                gapLeft = Math.Abs((coverTarget'), ], 'FullyQualifiedName~MintAtMarketTests'),
 'M7': (N, [('if (ratioOk) silverWorld', 'if (false && ratioOk) silverWorld')], 'FullyQualifiedName~MultiNationTests.銀平価'),
 'M8': (C, [('hoardRaw < 0 ? 0 : hoardRaw', 'hoardRaw')], 'FullyQualifiedName~CreditHoardGuardTests'),
 'M9': (C, [('!double.IsFinite(hoardRaw) ? c.CurrencyHoldingRatio.Value : hoardRaw < 0', 'hoardRaw < 0')], 'FullyQualifiedName~CreditHoardGuardTests'),
}
env = dict(os.environ, TEMP='D:/PoB_work/c115sb_tmp', TMP='D:/PoB_work/c115sb_tmp')
with open(out, 'w', encoding='utf-8') as fo:
    for k, (f, subs, flt) in M.items():
        subprocess.run(['git', 'checkout', '--', '.'], cwd=tree)
        p = os.path.join(tree, f); b = open(p, 'rb').read()
        ok = True
        for a, c in subs:
            a = a.encode(); c = c.encode()
            if b.count(a) != 1: ok = False; break
            b = b.replace(a, c)
        if not ok:
            fo.write(f'{k}\t適用できない（元の文が 1 件でない）\n'); fo.flush(); continue
        open(p, 'wb').write(b)
        r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', art, '--filter', flt], cwd=tree, env=env, capture_output=True, text=True, encoding='utf-8', errors='replace')
        tail = [l for l in r.stdout.splitlines() if '失敗!' in l or '成功!' in l or 'error CS' in l][:2]
        fo.write(f'{k}\t{"赤" if r.returncode != 0 and any("失敗!" in t for t in tail) else ("ビルドの失敗" if r.returncode != 0 else "緑")}\t{flt}\t{" / ".join(tail)}\n'); fo.flush()
        subprocess.run(['git', 'checkout', '--', '.'], cwd=tree)
