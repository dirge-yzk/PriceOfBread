# (1151) c113cr 突然変異（事前登録 measurements/m1151_20261115_prereg_cr.txt §3）：写しの木 <木> に 1 本の突然変異を当てる。使い捨ての木だけに使う（本物の worktree に当てない）。
# 使い方: python -X utf8 -I mutate_cr.py <木> <FLAG1|Ma|Mb|Md|Me|Me2|Mf1|Mf2>（FLAG1＝旗の既定を 1 にするだけ・他の突然変異の前に必ず当てる）
import sys, os
tree, m = sys.argv[1], sys.argv[2]
E = 'PriceOfBread.SimCore/RulePowerEngine.cs'
C = 'PriceOfBread.SimCore/Coefficients.p08_audit_soldiers.cs'
MUT = {
    'FLAG1': [(C, 'public double RuleFromPower = 0.0;', 'public double RuleFromPower = 1.0;')],
    'Ma': [(E, 'bool adj = nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _);',
            'bool adj = (nd >= 0 && Diplomacy.CessionAdjacent(nd, winNodes, out _)) || dip.Wars.Exists(wr => (ReferenceEquals(wr.A, n.P) && ReferenceEquals(wr.B, on.P)) || (ReferenceEquals(wr.B, n.P) && ReferenceEquals(wr.A, on.P)));')],
    'Mb': [(E, 'internal static double RulePowerCandY(double y, double s, bool adjacent) => adjacent ? y : y * s;', 'internal static double RulePowerCandY(double y, double s, bool adjacent) => y;')],
    # Md：c109rc の形（先に処理した国の τ だけ・段 ② で置いた値を含む・国ごとに取り直す）へ戻す
    'Md': [(E, 'if (double.IsNaN(worldMed))\n', 'if (true)\n'),
           (E, 'foreach (var m in list) foreach (var rg in m.R) if (!double.IsNaN(rg.RuleTau) && !(m.Unsolved is { } mu && mu.Contains(System.Array.IndexOf(m.R, rg)))) all.Add(rg.RuleTau);',
               'foreach (var m in list) { if (ReferenceEquals(m, n)) break; foreach (var rg in m.R) if (!double.IsNaN(rg.RuleTau)) all.Add(rg.RuleTau); }')],
    'Me': [(E, 'if (double.IsNaN(r.RuleTau)) RulePowerSeedOpening(r, dip);', 'if (false) RulePowerSeedOpening(r, dip);')],
    'Me2': [(E, 'if (double.IsNaN(r.RuleTau)) RulePowerSeedOpening(r, dip);', 'if (true) RulePowerSeedOpening(r, dip);'),
            (E, 'for (int j = 0; j < r.ForeignShares.Length; j++) if (r.ForeignShares[j] > 0) return;', '')],
    'Mf1': [(E, 'double up = capReach[s] * n.Ow[s]; if (up <= rho) continue;', 'double up = capReach[s] * n.Ow[s]; if (up < rho) continue;'),
            (E, 'double up = originUp[i]; if (up <= rho) continue;', 'double up = originUp[i]; if (up < rho) continue;')],
    'Mf2': [(E, 'double up = capReach[s] * n.Ow[s]; if (up <= rho) continue;', 'double up = capReach[s]; if (up <= rho) continue;')],
}
for f, old, new in MUT[m]:
    p = os.path.join(tree, f); b = open(p, 'rb').read().decode('utf-8')
    crlf = '\r\n' in b
    o, n = (old.replace('\n', '\r\n'), new.replace('\n', '\r\n')) if crlf else (old, new)
    if b.count(o) != 1: print(f'MUTFAIL {m} {f} count={b.count(o)}'); sys.exit(1)
    open(p, 'wb').write(b.replace(o, n).encode('utf-8'))
print(f'MUT {m} ok')
