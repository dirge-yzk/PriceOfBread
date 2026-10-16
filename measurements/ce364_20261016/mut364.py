import subprocess, shutil, os, sys, re

MUT = 'D:/PoB_work/ce4_364/mut_tree/'
SRC = 'D:/PoB_work/wt_ce4/'
ART = 'D:/PoB_work/ce4_364/art_mut'
OUT = 'D:/PoB_work/ce4_364/mut_out.txt'

mutations = [
    ('M1 BasketFill が常に 1', 'PriceOfBread.SimCore/EconomyStep.cs',
     "        return food < daily ? food : daily;\n", "        return 1.0;\n",
     'FullyQualifiedName~ConsumptionLayersTests', ''),
    ('M2 BasketFill が食料だけ', 'PriceOfBread.SimCore/EconomyStep.cs',
     "        return food < daily ? food : daily;\n", "        return food;\n",
     'FullyQualifiedName~ConsumptionLayersTests', ''),
    ('M3 aX にも割合を掛ける', 'PriceOfBread.SimCore/EconomyStep.cs',
     "double raw = a * x + r * sp * BasketFill(pops[i]), hand", "double raw = (a * x + r * sp) * BasketFill(pops[i]), hand",
     'FullyQualifiedName~ConsumptionLayersTests', ''),
    ('M4 NaN を 1 の側へ（正規化を外す）', 'PriceOfBread.SimCore/EconomyStep.cs',
     "        food = food > 0 ? (food < 1.0 ? food : 1.0) : 0.0;", "        food = food > 0 ? (food < 1.0 ? food : 1.0) : 1.0;",
     'FullyQualifiedName~ConsumptionLayersTests', ''),
    ('M5 影の n が旗 1 でも就学の充足', 'PriceOfBread.SimCore/InstrumentsPop.cs',
     "double nEdu = c.ConsumptionLayersEnabled > 0 ? nLitSchool : nFill;", "double nEdu = nFill;",
     'FullyQualifiedName~PopulationInstrumentTests', ''),
    ('M6 影の n が旗 0 でも識字', 'PriceOfBread.SimCore/InstrumentsPop.cs',
     "double nEdu = c.ConsumptionLayersEnabled > 0 ? nLitSchool : nFill;", "double nEdu = nLitSchool;",
     'FullyQualifiedName~PopulationInstrumentTests', ''),
    ('M7 nEduFillShare が使った n を出す', 'PriceOfBread.SimCore/InstrumentsPop.cs',
     "NF += sz * fillArr[k]; NU += sz * ins[k].NEdu;", "NF += sz * ins[k].NEdu; NU += sz * ins[k].NEdu;",
     'FullyQualifiedName~PopulationInstrumentTests', ''),
    ('M8 識字の重みを学齢でなく規模で', 'PriceOfBread.SimCore/InstrumentsPop.cs',
     "cl.LitSchoolW += sc * EconomyStep.LiteracyShare(p);", "cl.LitSchoolW += s * EconomyStep.LiteracyShare(p);",
     'FullyQualifiedName~PopulationInstrumentTests', ''),
    ('M10 削除した係数を戻す', 'PriceOfBread.SimCore/Coefficients.cs',
     "    // (364) 旧 (273) 開幕の貯蓄の置き直し〔OpeningSavingsRebaseMode・SavingsYearsByClass〕は削除した", "    public int OpeningSavingsRebaseMode = 0;" + chr(10) + "    // (364) 旧 (273) 開幕の貯蓄の置き直し〔OpeningSavingsRebaseMode・SavingsYearsByClass〕は削除した",
     'FullyQualifiedName~DormantIdentityTests', ''),
]

# the last mutation is a no-op placeholder for the ElectionAi two-branch test: replaced below with a real one
def run(filter_, coef):
    env = dict(os.environ)
    env['POB_COEF'] = coef
    p = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', ART, '--filter', filter_, '--logger', 'console;verbosity=normal'],
                       cwd=MUT, capture_output=True, text=True, env=env, encoding='utf-8', errors='replace')
    o = p.stdout + p.stderr
    fail = re.findall(r'^\s+失敗 (\S+)', o, re.M)
    m = re.search(r'テストの合計数: (\d+)', o)
    err = re.findall(r'error CS\d+.*', o)[:2]
    return fail, (m.group(1) if m else '?'), err


if __name__ == '__main__':
    sel = sys.argv[1:]
    with open(OUT, 'ab') as f:
        for name, rel, a, b, flt, coef in mutations:
            if sel and name.split()[0] not in sel:
                continue
            path = MUT + rel
            orig = open(path, 'rb').read().decode('utf-8')
            assert orig.count(a) == 1, (name, orig.count(a))
            open(path, 'wb').write(orig.replace(a, b).encode('utf-8'))
            try:
                fail, total, err = run(flt, coef)
            finally:
                open(path, 'wb').write(orig.encode('utf-8'))
            line = f"{name}: 失敗 {len(fail)} / {total}  {fail[:6]} {err}\n"
            f.write(line.encode('utf-8')); f.flush()
    print('done')
