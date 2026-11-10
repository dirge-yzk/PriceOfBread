# (1019) c95pc 突然変異の台本：使い捨ての木（D:/PoB_work/c95pc_work/mut）で 1 本ずつ変異を入れ、該当の試験を回し、出力の全文を 1 本のファイルへ書く。
# 使い方：python mutate.py <木> <出力> [変異名...]（変異名なし＝全部。M0＝無変異の対照）。バイトで読み書きし、変異ごとに元へ戻す。
import subprocess, sys

TREE = sys.argv[1]
OUT = sys.argv[2]
ONLY = set(sys.argv[3:])
BS = chr(92)
RO = 'PriceOfBread.SimCore/InstrumentsRecipeout.cs'
PC = 'PriceOfBread.SimCore/InstrumentsProdcap.cs'
FPC = 'FullyQualifiedName~ProdcapInstrumentTests'
FRO = 'FullyQualifiedName~RecipeoutInstrumentTests'

MUTS = [
    ('M0 無変異の対照', None, None, None, FPC + '|' + FRO),
    ('Mu5 級を Productivity から決める', RO,
     '        // 自己費用（参照価格の単位→州の名目）。鉱物の RGO は品位の枝つき。',
     '        if (cls == "N" || cls == "S") cls = endow < 0.7 ? "N" : "S";\n        // 自己費用（参照価格の単位→州の名目）。鉱物の RGO は品位の枝つき。', FPC),
    ('Mu-Output Output を Capacity にする', PC,
     'FR(p.Capacity), FR(p.Output)', 'FR(p.Capacity), FR(p.Capacity)', FPC),
    ('Mu-年 年を 1 つずらす', PC,
     'I(yearLabel), I(q), r.Name', 'I(yearLabel + 1), I(q), r.Name', FPC),
    ('Mu-Kind siteFound を Sites の Kind で決める', RO,
     'if (p.SiteFound) a.NSiteFound++;',
     'if (p.SiteRow >= 0 && Catalog.Sites[p.SiteRow].Kind == Catalog.SiteKind.Deposit) a.NSiteFound++;', FRO),
    ('Mu-class 級の写しを自前で書き N の規則を落とす', PC,
     'x.Class, FR(p.Productivity)', '(x.Class == "N" ? "S" : x.Class), FR(p.Productivity)', FPC),
]

with open(OUT, 'wb') as f:
    for name, path, old, new, flt in MUTS:
        if ONLY and name.split()[0] not in ONLY:
            continue
        orig = None
        if path:
            fp = TREE + '/' + path
            orig = open(fp, 'rb').read()
            s = orig.decode('utf-8')
            assert s.count(old) == 1, name
            open(fp, 'wb').write(s.replace(old, new).encode('utf-8'))
        try:
            r = subprocess.run(['dotnet', 'test', TREE + '/PriceOfBread.Tests', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c95pc_mutart', '--filter', flt],
                               capture_output=True)
            txt = r.stdout.decode('utf-8', 'replace')
        finally:
            if orig is not None:
                open(TREE + '/' + path, 'wb').write(orig)
        f.write(('==== ' + name + ' rc=' + str(r.returncode) + '\n').encode('utf-8'))
        f.write(txt.encode('utf-8'))
        f.write(b'\n')
        f.flush()
