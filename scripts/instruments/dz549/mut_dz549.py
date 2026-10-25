# (549) 線 dz2：番人 `DiscoveryRootOwnCapacityTests` の突然変異（読むだけ・木は写しの上で壊す＝作業の木は触らない）。
# 使い方：python -X utf8 mut_dz549.py <作業の木> <写し先（空でよい）> <artifacts-path> <出力ログ>
#   写し先に作業の木を写し、変異を 1 つずつ当ててテストをビルドし、`DiscoveryRootOwnCapacityTests` を回して赤になることを確かめる（赤＝殺した）。
#   基準（変異なし）が緑であることを最初に確かめる。生き残った変異は出力に SURVIVED と書く。
import os, shutil, subprocess, sys, re
src, dst, art, logp = sys.argv[1:5]
def run(cmd):
    return subprocess.run(cmd, capture_output=True, text=True, encoding='utf-8', errors='replace')
def rd(p): return open(p, 'rb').read().decode('utf-8')
def wr(p, s): open(p, 'wb').write(s.encode('utf-8'))
if os.path.exists(dst): shutil.rmtree(dst)
ig = shutil.ignore_patterns('bin', 'obj', '.git', 'docs', 'measurements', 'uishot', '.vs')
shutil.copytree(src, dst, ignore=ig)
disc = os.path.join(dst, 'PriceOfBread.SimCore', 'Discovery.cs')
coef = os.path.join(dst, 'PriceOfBread.SimCore', 'Coefficients.cs')
orig_d, orig_c = rd(disc), rd(coef)
TECH = "tech *= OwnSaturation(aAll, c);"
GATE = "if (poolOn && c.DiscoveryRootOwnCapacity > 0 && !(d.Decade > Catalog.DiscoveryBaseDecade))"
muts = [
 ("M1 旗を読まない", disc, lambda s: s.replace("if (poolOn && c.DiscoveryRootOwnCapacity > 0 &&", "if (poolOn && true &&")),
 ("M2 C を掛けない", disc, lambda s: s.replace(TECH, "tech *= 1.0;")),
 ("M3 A を親の産業の技師に替える", disc, lambda s: s.replace(TECH, "tech *= OwnSaturation(aRow, c);")),
 ("M4 d の門を反転する", disc, lambda s: s.replace("!(d.Decade > Catalog.DiscoveryBaseDecade)) tech *=", "(d.Decade > Catalog.DiscoveryBaseDecade)) tech *=")),
 ("M5 制度と思想の行（特許）にも掛ける", disc, lambda s: s.replace("res[row] = new Terms(TermMax * ratio, Saturate(sNeed, hNeed)", "res[row] = new Terms(TermMax * ratio * (c.DiscoveryRootOwnCapacity > 0 && !(d.Decade > Catalog.DiscoveryBaseDecade) ? OwnSaturation(aAll, c) : 1.0), Saturate(sNeed, hNeed)")),
 ("M6 aAll の和を財の前半だけにする", disc, lambda s: s.replace("c.DiscoveryRootOwnCapacity > 0) foreach (double e in eng) aAll += e;", "c.DiscoveryRootOwnCapacity > 0) for (int gi = 0; gi < eng.Length / 2; gi++) aAll += eng[gi];")),
 ("M7 C の分母から Am を落とす", disc, lambda s: s.replace("double f = a / (a + am);\n        return !(f > 0) ? 0.0 : f > 1.0 ? 1.0 : f;\n    }\n\n    public static bool Known", "double f = a / (a + 0 * am);\n        return !(f > 0) ? 0.0 : f > 1.0 ? 1.0 : f;\n    }\n\n    public static bool Known")),
 ("M8 旗の既定を 0 に戻す", coef, lambda s: s.replace("public double DiscoveryRootOwnCapacity = 1;", "public double DiscoveryRootOwnCapacity = 0;")),
 ("M9 OwnCapacity の d の門を外す", disc, lambda s: s.replace("if (!(d > 0)) return 1.0;\n        return OwnSaturation(a, c);", "return OwnSaturation(a, c);")),
]
tests = os.path.join(dst, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj')
dll = os.path.join(art, 'bin', 'PriceOfBread.Tests', 'release', 'PriceOfBread.Tests.dll')
def build_and_test(label):
    b = run(['dotnet', 'build', tests, '-c', 'Release', '--artifacts-path', art, '-v', 'q'])
    if b.returncode != 0: return 'BUILDFAIL', (b.stdout + b.stderr)[-600:]
    t = run(['dotnet', 'test', dll, '--filter', 'FullyQualifiedName~DiscoveryRootOwnCapacityTests|FullyQualifiedName~DiscoveryClockTests'])
    out = t.stdout + t.stderr
    m = re.search(r'(成功|失敗)!\s+-失敗:\s*(\d+)、合格:\s*(\d+)', out)
    return ('GREEN' if t.returncode == 0 else 'RED'), (m.group(0) if m else out[-300:])
with open(logp, 'w', encoding='utf-8') as lg:
    st, info = build_and_test('base'); lg.write(f'BASE {st} {info}\n'); lg.flush()
    for name, path, f in muts:
        wr(disc, orig_d); wr(coef, orig_c)
        cur = rd(path); new = f(cur)
        if new == cur: lg.write(f'{name}: NOCHANGE（変異が当たらなかった）\n'); lg.flush(); continue
        wr(path, new)
        st, info = build_and_test(name)
        lg.write(f'{name}: {"KILLED" if st == "RED" else ("SURVIVED" if st == "GREEN" else st)} {info}\n'); lg.flush()
    wr(disc, orig_d); wr(coef, orig_c)
    lg.write('MUTDONE\n')
