# (936) c82hd 突然変異の単体（使い捨ての木で・事前登録 measurements/m936_20261107_prereg_hd.txt [5]）
#   python -X utf8 -I mut936.py <使い捨ての木> <出力フォルダ> <artifacts> [名,名,…]
#   どの突然変異も旗 EmptyPopDebtToHeirs の既定を 1 にした木（点火の後の形）の上で掛ける（ConservationTests の与信の番人が B1 の経路を通るように）。
#   各突然変異の後に EmptyPopEstate.cs と Coefficients.cs を元のバイトへ戻す。成績表の列（W1・W2・U1・U2）は紙の予言のまま（腕は回さない）。
import sys, os, subprocess
tree, out, art = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
EP = os.path.join(tree, 'PriceOfBread.SimCore', 'EmptyPopEstate.cs')
CO = os.path.join(tree, 'PriceOfBread.SimCore', 'Coefficients.cs')
ep0 = open(EP, 'rb').read(); co0 = open(CO, 'rb').read()
CALL = 'if (debtors is not null) DebtToHeirs(heirs, debtors, live);'
FOR = 'for (int i = 0; i < heirs.Count - 1; i++)'
GIVE = 'heirs[i].Debt += x;'
LAST = 'heirs[^1].Debt += d.Debt; d.Debt = default;'
COLLECT = 'sum += s; } if (w.Coef.EmptyPopDebtToHeirs > 0 && p.Debt.Value > 0) (debtors ??= new List<Pop>()).Add(p);'
MUT = {
    'OK': [],                                                                             # 突然変異なし（点火の形そのもの＝全て緑が前提）
    'M0': [(CALL, '')],                                                                   # 何もしない
    'M1': [(FOR, 'for (int i = 0; i < heirs.Count; i++)'), (LAST, '')],                   # 末尾にも比の額＝送り手に残りが残る
    'M2': [(GIVE, ''), (LAST, 'd.Debt = default;')],                                     # 送り手を 0 にするが相続人へ渡さない
    'M3': [(GIVE, 'heirs[i].Debt += x + x;'), (LAST, 'heirs[^1].Debt += d.Debt + d.Debt; d.Debt = default;')],   # 2 倍渡す
    'M4': [(COLLECT, 'sum += s; if (w.Coef.EmptyPopDebtToHeirs > 0 && p.Debt.Value > 0) (debtors ??= new List<Pop>()).Add(p); }')],   # 貯蓄 ＞0 の送り手だけ
    'M5': [(CALL, '{ double hs5 = 0, dd5 = 0; foreach (var x5 in heirs) hs5 += Math.Max(0, x5.Savings.Value); if (debtors is not null) { foreach (var x5 in debtors) dd5 += x5.Debt.Value; if (hs5 >= dd5) DebtToHeirs(heirs, debtors, live); } }')],   # 相続人が返せる時だけ
    'M6': [('var d0 = d.Debt;', 'var d0 = d.Debt; double sv6 = 0; foreach (var q6 in heirs) sv6 += Math.Max(0, q6.Savings.Value);'), ('(heirs[i].Size / live)', '(Math.Max(0, heirs[i].Savings.Value) / sv6)')],   # 重みを貯蓄で
}
FLT = 'FullyQualifiedName~EmptyPopDebtToHeirsTests|FullyQualifiedName~ConservationTests.家計の与信は国ごとにPOPの債務の和に等しい'
names = sys.argv[4].split(',') if len(sys.argv) > 4 else list(MUT)
try:
    for name in names:
        b = ep0
        for a, c in MUT[name]:
            a, c = a.encode(), c.encode()
            assert b.count(a) == 1, (name, a[:50], b.count(a)); b = b.replace(a, c)
        cb = co0.replace(b'public double EmptyPopDebtToHeirs = 0.0;', b'public double EmptyPopDebtToHeirs = 1.0;')
        assert cb != co0
        open(EP, 'wb').write(b); open(CO, 'wb').write(cb)
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '--filter', FLT], capture_output=True)
        txt = r.stdout.decode('utf-8', 'replace') + r.stderr.decode('utf-8', 'replace')
        keep = [l for l in txt.splitlines() if ('失敗 ' in l or '[FAIL]' in l or '合格' in l or ' error ' in l.lower() or '成功' in l or 'Passed!' in l or 'Failed!' in l)]
        with open(os.path.join(out, f'mut_{name}.txt'), 'w', encoding='utf-8', newline='\n') as f:
            f.write(f'# (936) 突然変異 {name}（rc={r.returncode}）\n' + '\n'.join(keep) + '\n')
        print(name, 'rc', r.returncode, flush=True)
finally:
    open(EP, 'wb').write(ep0); open(CO, 'wb').write(co0)
