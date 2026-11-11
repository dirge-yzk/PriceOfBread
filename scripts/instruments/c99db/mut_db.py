# (1048) c99db 突然変異（㉚）：使い捨ての木で 1 本ずつ変異を入れ、器と機構の試験を回して赤の試験の名を残す（腕は回さない）。
#   python scripts/instruments/c99db/mut_db.py <コミット> <出力ファイル>
#   木＝D:/PoB_work/c99db_work/mut/src（git archive）・ビルド＝--artifacts-path D:/PoB_work/c99db_work/mut/art。
import subprocess, os, sys, re, shutil
H, OUT = sys.argv[1], sys.argv[2]
W = 'D:/PoB_work/c99db_work/mut'; S = W + '/src'; ART = W + '/art'
REPO = 'C:/Users/dirge/Documents/PriceOfBread'
DOTNET = 'C:/Program Files/dotnet/dotnet.exe'
FILTER = 'FullyQualifiedName~Lordstat|FullyQualifiedName~LordStakeD1bD2|FullyQualifiedName~Rs2LordOpposition|FullyQualifiedName~Rs3LordReverseGain'
PP = 'PriceOfBread.SimCore/PolicyPreferences.cs'
M = [
    ('D2-M1 ρ＝1', PP, 'RentToIncomeOf(w.LastRuleCut, income[0]) * IntermediaryKeepShare(w)', 'RentToIncomeOf(w.LastRuleCut, income[0]) * 1.0'),
    ('D2-M2 max でなく軸ごと（官僚制度は身分の項だけ）', PP, 'double now = Privileges.IntermediaryDegreeFor(b, lr);\n        double other = a == PolicyAxis.Bureaucracy ? Privileges.IntermediaryDegreeFor(BureaucracyStep(b, !increasing), lr)',
     'double now = a == PolicyAxis.Bureaucracy ? Privileges.EstateDegreeFor(b) : Privileges.IntermediaryDegreeFor(b, lr);\n        double other = a == PolicyAxis.Bureaucracy ? Privileges.EstateDegreeFor(BureaucracyStep(b, !increasing))'),
    ('D2-M3 底を外す（官僚制度）', PP, 'return -StatusScale * (0.5 + IntermediaryStake(w, a, e, increasing));', 'return -StatusScale * (IntermediaryStake(w, a, e, increasing));'),
    ('D2-M4 旗を読まない', PP, 'w.Coef.LordIntermediaryStake > 0) return', 'w.Coef.LordIntermediaryStake > 9) return'),
    ('D2-M5 土地改革に足し忘れ', PP, 'LordStake(w, a, e, increasing) + IntermediaryStake(w, a, e, increasing));', 'LordStake(w, a, e, increasing));'),
    ('D2-M6 取り分を地代の側にも足す', PP, 'double lordRentToIncome = RentToIncomeOf(rent[0], income[0]);', 'double lordRentToIncome = RentToIncomeOf(rent[0] + w.LastRuleCut, income[0]);'),
    ('D2-M7 分母を Rent0', PP, 'RentToIncomeOf(w.LastRuleCut, income[0])', 'RentToIncomeOf(w.LastRuleCut, rent[0])'),
    ('D2-M8 減らす向きの得を落とす', PP, '        var b = w.Bureaucracy; var lr = w.LandReform;\n        double now = Privileges.IntermediaryDegreeFor', '        if (!increasing) return 0.0;\n        var b = w.Bureaucracy; var lr = w.LandReform;\n        double now = Privileges.IntermediaryDegreeFor'),
    ('D2-M9 DirectionAware に官僚制度を足し忘れ', PP, '(a == PolicyAxis.Bureaucracy && w.Coef.LordIntermediaryStake > 0) || ', ''),
    ('D1-Ma 比でなく生の月額', PP, 'double clergyToIncome0 = RentToIncomeOf(clergyInc0, income[0]);', 'double clergyToIncome0 = clergyInc0;'),
    ('D1-Mb 段を 1 つずらす', PP, 'other = Privileges.TitheDegreeFor(ReligionStep(s, !increasing), c);', 'other = Privileges.TitheDegreeFor(ReligionStep(ReligionStep(s, !increasing), !increasing), c);'),
    ('D1-Mc 旗を読まない', PP, 'w.Coef.LordReligionStake > 0) return', 'w.Coef.LordReligionStake > 9) return'),
    ('D1-M2 分母を Rent0', PP, 'RentToIncomeOf(clergyInc0, income[0])', 'RentToIncomeOf(clergyInc0, rent[0])'),
    ('D1-M6b 減らす向きも損で読む', PP, 'other = Privileges.TitheDegreeFor(ReligionStep(s, !increasing), c);', 'other = Privileges.TitheDegreeFor(ReligionStep(s, false), c);'),
    ('D1-M6a DirectionAware に国教を足し忘れ', PP, ' || (a == PolicyAxis.StateReligion && w.Coef.LordReligionStake > 0)', ''),
    ("M8' 窓の写しの足し忘れ（Copy）", PP, 'Tl = Tl, ClergyInc0 = ClergyInc0,', 'Tl = Tl,'),
]
os.makedirs(W, exist_ok=True)
if os.path.isdir(S): shutil.rmtree(S)
os.makedirs(S)
subprocess.run(f'cd "{REPO}" && git archive {H} | tar -x -C "{S}"', shell=True, check=True)
def run(cmd, log):
    with open(log, 'wb') as f: return subprocess.run(cmd, cwd=S, stdout=f, stderr=subprocess.STDOUT).returncode
def failed(log):
    t = open(log, 'rb').read().decode('utf-8', 'replace')
    names = sorted(set(re.findall(r'^\s*(?:失敗|Failed)\s+(PriceOfBread\.Tests\.\S+)', t, re.M)))
    tot = re.findall(r'(?:合計|Total tests)[^\n]*', t)
    return names, (tot[-1] if tot else '合計の行なし')
out = open(OUT, 'w', encoding='utf-8', newline='\n')
out.write(f'(1048) c99db 突然変異の出力（木 {H}・試験の束 {FILTER}）\n')
orig = {}
for name, path, old, new in [('元の形', PP, None, None)] + M:
    p = os.path.join(S, path)
    if path not in orig: orig[path] = open(p, 'rb').read()
    src = orig[path].decode('utf-8')
    if old is not None:
        if src.count(old) < 1: out.write(f'## {name}\n  置き換えの元が無い（変異を入れられない）\n'); out.flush(); continue
        src = src.replace(old, new)
    open(p, 'wb').write(src.encode('utf-8'))
    rb = run([DOTNET, 'build', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', ART, '--disable-build-servers'], W + '/build.log')
    if rb != 0:
        out.write(f'## {name}\n  ビルド失敗\n'); out.flush(); open(p, 'wb').write(orig[path]); continue
    run([DOTNET, 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--no-build', '--artifacts-path', ART, '--filter', FILTER], W + '/test.log')
    names, tot = failed(W + '/test.log')
    out.write(f'## {name}\n  {tot}\n  赤 {len(names)}: ' + (', '.join(n.split('.')[-2] + '.' + n.split('.')[-1] for n in names) if names else '（無し）') + '\n'); out.flush()
    open(p, 'wb').write(orig[path])
out.close()
print('MUTDONE')
