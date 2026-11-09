# (989) 突然変異：使い捨ての木（wt_c90tr_m）で試験の 1 行を壊し、絞った試験を回して赤か緑かを書く。使い方：python mut989.py <木> <artifacts> <出力>
import subprocess, sys, io
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
P = tree + "/PriceOfBread.Tests/InstrumentsTests.cs"
orig = open(P, 'rb').read().decode('utf-8')
FILT = "FullyQualifiedName~InstrumentsTests.計器を出しても軌道は変わらず|FullyQualifiedName~InstrumentsTests.国債金利の内訳"
ASSERT_B7 = 'Assert.True(borrowNations.Count >= 2,'
def sub(s, a, b):
    assert s.count(a) == 1, (a, s.count(a)); return s.replace(a, b)
def noassert(s):
    i = s.index(ASSERT_B7); j = s.index("\n", i); return s[:i] + "// " + s[i:j] + s[j:]
M = [
 ("M-0 対照（突然変異なし）", lambda s: s),
 ("M-B7a 借入の盤面を置く行を消す（PlaceBorrowBoard を渡さない）", lambda s: s.replace("instr: false, PlaceBorrowBoard)", "instr: false)").replace("instr: true, PlaceBorrowBoard)", "instr: true)")),
 ("M-B7b M-B7a かつ新しい Assert を外す", lambda s: noassert(s.replace("instr: false, PlaceBorrowBoard)", "instr: false)").replace("instr: true, PlaceBorrowBoard)", "instr: true)"))),
 ("M-B7c 国債の外債を足す行を消す（国庫を移すだけ）", lambda s: sub(s, " w.ForeignDebt += extra; w.GovDebt += extra;", "")),
 ("M-B12a nationIndex＜4 を＜1 にする（床を 1 国にする）", lambda s: sub(s, "int.Parse(r[iIdx]) < 4)", "int.Parse(r[iIdx]) < 1)")),
 ("M-B12b nationIndex＜4 の述語を外す（＜100）", lambda s: sub(s, "int.Parse(r[iIdx]) < 4)", "int.Parse(r[iIdx]) < 100)")),
 ("M-B12c Make を 3 国にする", lambda s: sub(s, "NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk, NationConfig.Austria()];", "NationConfig[] cfgs = [NationConfig.France(), NationConfig.Prussia(), uk];")),
 ("M-B12d 財政の行の床を名の集合で絞らず全行で数える（frFirst を fr.Count に戻す）", lambda s: sub(s, "int frFirst = fr.Count(r => ff.Contains(r[Array.IndexOf(fh, \"nation\")]));", "int frFirst = fr.Count;")),
]
with open(out, 'w', encoding='utf-8') as f:
    for name, fn in M:
        try:
            open(P, 'wb').write(fn(orig).encode('utf-8'))
            r = subprocess.run(["dotnet", "test", tree + "/PriceOfBread.Tests", "-c", "Release", "--artifacts-path", art, "--filter", FILT], capture_output=True, text=True, encoding='utf-8', errors='replace')
            lines = [l.strip() for l in r.stdout.splitlines()]
            res = "赤" if r.returncode != 0 else "緑"
            fails = [l for l in lines if l.startswith("失敗 PriceOfBread")]
            msgs = [lines[i + 2] for i, l in enumerate(lines) if l.startswith("失敗 PriceOfBread") and i + 2 < len(lines)]
            f.write(f"{name}\t{res}\t{' / '.join(x.split('.')[-1] for x in fails)}\t{' / '.join(m[:140] for m in msgs)}\n")
        except AssertionError as e:
            f.write(f"{name}\t適用できない\t\t{e}\n")
        f.flush()
    open(P, 'wb').write(orig.encode('utf-8'))
