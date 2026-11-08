# (972) c87mt 突然変異：使い捨ての木 <木> の World.cs・Instruments.cs をバイトで書き換え、隔離ビルドで MoTrendInitTests を回し、試験ごとの赤・緑を表にする。
#   python -X utf8 scripts/instruments/c87mt/mut972.py <使い捨ての木> <隔離ビルドの頭> <出力フォルダ>
#   旗の値は試験が係数に置く（T1 は 1・T0 は 0・T3 は両方）＝突然変異は旗 0 の木のまま当てる。各突然変異の後に元のバイトへ戻す。
import os, subprocess, sys
tree, art, out = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(out, exist_ok=True)
W = os.path.join(tree, 'PriceOfBread.SimCore', 'World.cs')
I = os.path.join(tree, 'PriceOfBread.SimCore', 'Instruments.cs')
OLD = b'if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) {'
FIELD = b'    private double _moTrend; internal double MoTrendForInstruments => _moTrend;'
INSL = b'            if (early || !(w.MoTrendForInstruments > 1e-9))   // \xe2\x98\x85(972)'
MUT = {
    'M0': [(W, OLD, b'if (_moTrend <= 1e-9) {')],   # 旗を読まない（旧の形）
    'M1': [(W, OLD, b'if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && _mut972Once++ == 0)) {'),
           (W, FIELD, FIELD + b' private int _mut972Once;')],   # 1 回だけ見送り、2 回目は Max(1e-9) の ratio で置く
    'M2': [(W, OLD, b'if ((_moTrend <= 1e-9 || (c.MoTrendInitNeedsGdp > 0 && gdp > 1e-9)) && !(c.MoTrendInitNeedsGdp > 0 && !(gdp > 1e-9))) {')],   # 毎回の上書き
    'M3': [(W, OLD, b'if (_moTrend <= 1e-9 && !(c.MoTrendInitNeedsGdp > 0 && (gdp > 1e-9))) {')],   # 守りの向きを逆
    'M4': [(I, INSL, b'            if (false)   // \xe2\x98\x85(972)')],   # 計器の行を出さない
}
orig = {p: open(p, 'rb').read() for p in (W, I)}
for p, b in orig.items():
    assert b.count(OLD if p == W else INSL) == 1, p
rows = []
def run(name):
    log = os.path.join(out, f'{name}.log')
    with open(log, 'wb') as f:
        r = subprocess.run(['dotnet', 'build', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--artifacts-path', art, '-v', 'q', '--disable-build-servers'], stdout=f, stderr=subprocess.STDOUT)
        if r.returncode != 0: return 'build-fail', {}
        r = subprocess.run(['dotnet', 'test', os.path.join(tree, 'PriceOfBread.Tests', 'PriceOfBread.Tests.csproj'), '-c', 'Release', '--no-build', '--artifacts-path', art,
                            '--filter', 'FullyQualifiedName~MoTrendInitTests', '--logger', 'console;verbosity=normal'], stdout=f, stderr=subprocess.STDOUT)
    res = {}
    for l in open(log, encoding='utf-8', errors='replace'):
        s = l.strip()
        for tag, v in (('成功 ', '緑'), ('失敗 ', '赤'), ('Passed ', '緑'), ('Failed ', '赤')):
            if s.startswith(tag) and 'MoTrendInitTests' in s:
                res[s[len(tag):].split(' [')[0].strip()] = v
    return 'ok', res
for name in ['ORIG'] + list(MUT):
    try:
        for p, a, b in MUT.get(name, []):
            cur = open(p, 'rb').read(); assert cur.count(a) == 1, (name, p); open(p, 'wb').write(cur.replace(a, b))
        st, res = run(name)
    finally:
        for p, b in orig.items(): open(p, 'wb').write(b)
    rows.append((name, st, res))
    print(name, st, len(res), flush=True)
names = sorted({k for _, _, r in rows for k in r})
with open(os.path.join(out, 'mut_table.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('mutation\tbuild\t' + '\t'.join(names) + '\n')
    for name, st, res in rows: f.write(name + '\t' + st + '\t' + '\t'.join(res.get(k, '-') for k in names) + '\n')
print('MUTDONE')
