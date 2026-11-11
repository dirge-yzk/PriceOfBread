# (910) c78ep 突然変異の単体（使い捨ての木で・事前登録 measurements/m910_20261107_prereg_ep.txt [5]）
#   python -X utf8 mut910.py <使い捨ての木> <出力フォルダ> <artifacts>
#   どの突然変異も旗 EmptyPopDebtFirst の既定を 1 にした木（点火の後の形）の上で掛ける（ConservationTests の与信の番人が B5 の経路を通るように）。
#   M0＝突然変異なし（点火の形そのもの＝全て緑が前提）。各突然変異の後に EmptyPopEstate.cs と Coefficients.cs を元のバイトへ戻す。
import sys, os, subprocess
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
EP = os.path.join(tree, 'PriceOfBread.SimCore', 'EmptyPopEstate.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
ep0 = open(EP, 'rb').read(); co0 = open(CO, 'rb').read()
REPAY = 'if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) { if (Instruments.On) Instruments.EstateRepay(true); Bank.Lend(w, p, -back); if (Instruments.On) Instruments.EstateRepay(false); } } '
TAIL = '            World.Transfer((w, payers), (w, heirs, null), sum, fromWeights: held);\n        }\n'
MUT = {
    'M0': [],
    'E1': [('Bank.Lend(w, p, -back);', '')],                                                    # 返さない
    'E2': [(REPAY, ''), (TAIL, TAIL + '        if (w.Coef.EmptyPopDebtFirst > 0) foreach (var r2 in w.Regions) foreach (var p in r2.Pops) if (!(p.Size > 1e-12) && p.Debt.Value > 0) { double back = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (back > 0) Bank.Lend(w, p, -back); }\n')],   # 相続の後に返す
    'E3': [('if (p.Size > 1e-12) { live += p.Size; continue; }', 'if (p.Size > 1e-12) { live += p.Size; if (p.Debt.Value > 0) { double b3 = Math.Min(p.Debt.Value, Math.Max(0, p.Savings.Value)); if (b3 > 0) Bank.Lend(w, p, -b3); } continue; }')],   # 人の居る POP も返す
    'E4': [('Bank.Lend(w, p, -back);', 'p.Debt -= new MoneyStock(back);')],                    # 与信を動かさず Debt だけ消す
    'E6': [('if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0)', 'if (w.Coef.EmptyPopDebtFirst > 0 && p.Debt.Value > 0 && r.Pops.Exists(x => x.Size > 1e-12))')],   # 受け手の居ない州で返さない
}
FLT = 'FullyQualifiedName~EmptyPopDebtFirstTests|FullyQualifiedName~ConservationTests.家計の与信は国ごとにPOPの債務の和に等しい|FullyQualifiedName~EmptyPopEstateTests'
names = sys.argv[4].split(',') if len(sys.argv) > 4 else list(MUT)
try:
    for name in names:
        b = ep0
        for a, c in MUT[name]:
            a, c = a.encode(), c.encode()
            assert b.count(a) == 1, (name, a[:50], b.count(a)); b = b.replace(a, c)
        cb = co0.replace(b'public double EmptyPopDebtFirst = 0.0;', b'public double EmptyPopDebtFirst = 1.0;')
        assert cb != co0
        open(EP, 'wb').write(b); open(CO, 'wb').write(cb)
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--filter', FLT], capture_output=True)
        txt = r.stdout.decode('utf-8', 'replace') + r.stderr.decode('utf-8', 'replace')
        keep = [l for l in txt.splitlines() if ('失敗 ' in l or '合格' in l or 'error' in l.lower() or '成功' in l)]
        with open(os.path.join(out, f'mut_{name}.txt'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(f'# (910) 突然変異 {name}（rc={r.returncode}）\n' + '\n'.join(keep) + '\n')
        print(name, 'rc', r.returncode, flush=True)
finally:
    open(EP, 'wb').write(ep0); open(CO, 'wb').write(co0)
