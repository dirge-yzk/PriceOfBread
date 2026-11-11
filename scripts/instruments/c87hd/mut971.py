# (971) c87hd 突然変異の単体（使い捨ての木で・事前登録 measurements/m971_20261108_prereg_hd.txt [5]）
#   python -X utf8 -I mut971.py <使い捨ての木> <出力フォルダ> <artifacts> [名,名,…]
#   どの突然変異も旗 BillGateReadsHeirDebt と EmptyPopDebtToHeirs の既定を 1 にした木（点火の後の形）の上で掛ける。
#   各突然変異の後に BilateralMarket.cs・EmptyPopEstate.cs・Coefficients.cs を元のバイトへ戻す。腕の門（W1・W2・底の比）は紙の予言のまま（腕は回さない）。
import sys, os, subprocess
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
BM = os.path.join(tree, 'PriceOfBread.SimCore', 'BilateralMarket.cs')
EP = os.path.join(tree, 'PriceOfBread.SimCore', 'EmptyPopEstate.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
bm0 = open(BM, 'rb').read(); ep0 = open(EP, 'rb').read(); co0 = open(CO, 'rb').read()
SUM = 'double heirDebt = 0; if (c.BillGateReadsHeirDebt > 0) foreach (var p3 in r2.Pops) if (p3.Size > 1e-12) heirDebt += p3.Debt.Value;'
GATE = 'bill[t2] = !(merchPay.Debt.Value > 0) && !(heirDebt > 1e-9);'
SETTLE = 'if (p2.Debt.Value > 0) { double back = Math.Min('
MUT = {
    'OK': [],                                                                                                  # 突然変異なし（点火の形そのもの＝全て緑が前提）
    'M1': [(BM, GATE, 'bill[t2] = !(merchPay.Debt.Value > 0);')],                                              # 相続人の債務を読まない（＝B1 そのまま）
    'M2': [(BM, 'foreach (var p2 in r2.Pops)\n', 'double pre2 = 0; foreach (var p0 in r2.Pops) if (p0.Size > 1e-12) pre2 += p0.Debt.Value; foreach (var p2 in r2.Pops)\n'),
           (BM, SUM, 'double heirDebt = 0; if (c.BillGateReadsHeirDebt > 0) heirDebt = pre2;')],               # ① の前に読む（順の誤り）
    'M3': [(BM, SUM, 'double heirDebt = 0; if (c.BillGateReadsHeirDebt > 0) foreach (var rr3 in w2.Regions) foreach (var p3 in rr3.Pops) if (p3.Size > 1e-12) heirDebt += p3.Debt.Value;')],   # 国の全ての POP を読む（州の誤り）
    'M4': [(BM, GATE, 'bill[t2] = !(heirDebt > 1e-9);')],                                                      # 商人の債務を読まない
    'M5': [(EP, '(heirs[i].Size / live)', '(heirs[i].Size / live * 0 + 1.0 / heirs.Count)')],                  # DebtToHeirs を人数の比で配らない（頭割り）
    'M6': [(BM, GATE, 'bill[t2] = !(merchPay.Debt.Value > 0) && !(heirDebt > 0);')],                           # 閾値 0（塵で止まる）
    'M7': [(BM, SUM, 'double heirDebt = 0; if (c.BillGateReadsHeirDebt > 0) foreach (var p3 in r2.Pops) if (p3.Size > 1e-12) heirDebt = Math.Max(heirDebt, p3.Debt.Value);')],   # 和でなく 1 人の最大
    'M8': [(BM, SUM, 'double heirDebt = 0; if (c.BillGateReadsHeirDebt > 0) foreach (var p3 in r2.Pops) heirDebt += p3.Debt.Value;')],   # 人の居ない POP も読む
}
FLT = ('FullyQualifiedName~BillGateHeirDebtTests|FullyQualifiedName~EmptyPopDebtToHeirsTests|FullyQualifiedName~TradeClearingTests|FullyQualifiedName~BillBlockedColumnsTests'
       '|FullyQualifiedName~HeirDebtAfterSettleColumnsTests|FullyQualifiedName~HeirDebtSettledMovedTests|FullyQualifiedName~ImportPayerLiveTests|FullyQualifiedName~TradeClearingBoardTests')
names = sys.argv[4].split(',') if len(sys.argv) > 4 else list(MUT)
try:
    for name in names:
        files = {BM: bm0, EP: ep0}
        for p, a, c in MUT[name]:
            a, c = a.encode(), c.encode()
            assert files[p].count(a) == 1, (name, a[:60], files[p].count(a)); files[p] = files[p].replace(a, c)
        cb = co0.replace(b'public double BillGateReadsHeirDebt = 0.0;', b'public double BillGateReadsHeirDebt = 1.0;').replace(b'public double EmptyPopDebtToHeirs = 0.0;', b'public double EmptyPopDebtToHeirs = 1.0;')
        assert cb.count(b'BillGateReadsHeirDebt = 1.0;') == 1 and cb.count(b'EmptyPopDebtToHeirs = 1.0;') == 1
        for p, b in files.items(): open(p, 'wb').write(b)
        open(CO, 'wb').write(cb)
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--filter', FLT], capture_output=True)
        txt = r.stdout.decode('utf-8', 'replace') + r.stderr.decode('utf-8', 'replace')
        keep = [l for l in txt.splitlines() if ('失敗 ' in l or '[FAIL]' in l or '合格' in l or ' error ' in l.lower() or '成功' in l or 'Passed!' in l or 'Failed!' in l or '合計' in l)]
        with open(os.path.join(out, f'mut_{name}.txt'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(f'# {name} rc={r.returncode}\n' + '\n'.join(keep) + '\n')
        print(name, 'rc', r.returncode, '赤' if r.returncode else '緑', flush=True)
finally:
    open(BM, 'wb').write(bm0); open(EP, 'wb').write(ep0); open(CO, 'wb').write(co0)
