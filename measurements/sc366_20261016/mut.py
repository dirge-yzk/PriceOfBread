import subprocess, sys, os, re
WT = 'D:/PoB_work/wt_sc/'
S = WT + 'PriceOfBread.SimCore/'
env = dict(os.environ); env['PATH'] = '/c/Program Files/dotnet:' + env.get('PATH', '')
muts = [
 ('M1 既定を1.0へ', 'Coefficients.cs', 'public double CriminalClassEnabled = 0.0;', 'public double CriminalClassEnabled = 1.0;', 0),
 ('M2 補助関数の門を外す', 'Simulation.cs', 'if (!(coef.CriminalClassEnabled > 0)) return -1;', '', 0),
 ('M3 添字を14に固定', 'Simulation.cs', 'return pops.Count - 1;', 'return 14;', 0),
 ('M4 POP を足さず添字だけ返す', 'Simulation.cs', 'pops.Add(new Pop { ClassId = PopClass.Criminal, ClassName = "犯罪者", IsUrban = false, Size = 0, Satisfaction = 1.0, Literacy = literacy });', '', 0),
 ('M4b 昇格の州で添字を受けない', 'Simulation.cs', 'SoldierPopIndex = 12, CriminalPopIndex = AddCriminalPop(pops, coef, peasant.Literacy),', 'SoldierPopIndex = 12,', 0),
 ('M5 税の除外を外す', 'Fiscal.cs', 'if (p.IsCriminal) continue;', '', 0),
 ('M6 州の間の移民の除外を外す', 'Territory.cs', ' || sp.IsCriminal) continue;', ') continue;', 0),
 ('M7 農村都市の再配分の除外を外す', 'PopDecision.cs', 'if (p.Closed || p.IsCriminal) continue;', 'if (p.Closed) continue;', 0),
 ('M8 識字の除外を外す', 'TechEducation.cs', 'if (p.IsCriminal) continue;', '', 0),
 ('M9 修学の蓄えの除外を外す', 'TechEducation.cs', 'if (!p.IsCriminal) p.EducationEma', 'p.EducationEma', 0),
 ('M10a 転職の行き先（強制の残り）の除外を外す', 'Meritocracy.cs', ' || i == r.CriminalPopIndex) continue;', ') continue;', 0),
 ('M10b 転職の行き先（softmax）の除外を外す', 'Meritocracy.cs', ' || i == r.CriminalPopIndex) continue;', ') continue;', 1),
 ('M10c 転職の行き先（waterfall）の除外を外す', 'Meritocracy.cs', ' || i == r.CriminalPopIndex) continue;', ') continue;', 2),
 ('M11 Count を14へ', 'PopClass.cs', 'public const int Count = 15;', 'public const int Count = 14;', 0),
 ('M12 Criminal の番号を13へ', 'PopClass.cs', 'public const int Criminal = 14;', 'public const int Criminal = 13;', 0),
 ('M13 述語を別の番号へ', 'Model.cs', 'public bool IsCriminal => ClassId == PopClass.Criminal;', 'public bool IsCriminal => false;', 0),
]
only = sys.argv[1:]
res = []
for name, f, old, new, nth in muts:
    if only and not any(name.startswith(o) for o in only): continue
    p = S + f
    orig = open(p, 'rb').read()
    ob = old.encode(); nb = new.encode()
    idx = -1
    for _ in range(nth + 1):
        idx = orig.index(ob, idx + 1)
    mut = orig[:idx] + nb + orig[idx + len(ob):]
    open(p, 'wb').write(mut)
    try:
        r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/sc_366/art', '--filter', 'FullyQualifiedName~CriminalVesselTests|FullyQualifiedName~DormantIdentityTests'], cwd=WT, capture_output=True, text=True, env=env, encoding='utf-8', errors='replace')
        out = r.stdout + r.stderr
        m = re.search(r'(成功|失敗)!.*', out)
        failed = [l.strip() for l in out.splitlines() if l.strip().startswith('失敗 ')]
        err = 'error' in out and 'エラー' in out and '0 エラー' not in out
        res.append((name, m.group(0) if m else out[-300:], failed[:4], err))
    finally:
        open(p, 'wb').write(orig)
    print(res[-1], flush=True)
print('DONE')
