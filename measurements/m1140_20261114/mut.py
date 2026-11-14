import subprocess, sys, os
T = 'D:/PoB_work/c111sm_work/mut'
OUT = 'D:/PoB_work/c111sm_work/mut_result.txt'


def rd(p): return open(os.path.join(T, p), 'rb').read().decode('utf-8')
def wr(p, s): open(os.path.join(T, p), 'wb').write(s.encode('utf-8'))


W = 'PriceOfBread.SimCore/World.cs'
IT = 'PriceOfBread.SimCore/Instruments/InstrumentsTransport.cs'
MK = 'PriceOfBread.SimCore/Market.cs'


def sub(p, a, b):
    s = rd(p)
    assert s.count(a) == 1, (p, a, s.count(a))
    wr(p, s.replace(a, b))


M = {
    # 項2：Seize を無条件にする（接収の速さ 0 でも走る）
    'M-seize': (W, 'bool seize = Coef.NationalizationSeizureRate > 0;', 'bool seize = true;', 'StateFactoryTests'),
    # 項6 G4：受け手の側を読まない
    'M-g4read': (IT, 'if (!double.IsNaN(a.MoveBefore))\n            {\n                a.G4Moves++;', 'if (false)\n            {\n                a.G4Moves++;', 'TransportInstrumentTests'),
    'M-nonser': ('PriceOfBread.SimCore/Model.cs', '    internal bool FlowA' + 'sleep;\n    internal double ClearFailLp;\n    internal Region[]? ClearPartners;', '    [NonSerialized] internal bool FlowA' + 'sleep;\n    [NonSerialized] internal double ClearFailLp;\n    [NonSerialized] internal Region[]? ClearPartners;', 'SaveRoundTripTests'),
    # 項6 G4：ビット比較を消す
    'M-g4bits': (IT, 'if (BitConverter.DoubleToInt64Bits(r.RailTrack) != BitConverter.DoubleToInt64Bits(a.MoveBefore)) a.G4Viol++;', '', 'TransportInstrumentTests'),
}
which = sys.argv[1:] or list(M)
for k in which:
    p, a, b, flt = M[k]
    orig = rd(p)
    crlf = '\r\n' in orig
    if crlf: a = a.replace('\n', '\r\n')
    sub(p, a, b)
    r = subprocess.run(['dotnet', 'test', 'PriceOfBread.Tests/PriceOfBread.Tests.csproj', '-c', 'Release', '--artifacts-path', 'D:/PoB_work/c111sm_work/mutart', '--disable-build-servers', '--filter', 'FullyQualifiedName~' + flt], cwd=T, capture_output=True, text=True, encoding='utf-8', errors='replace')
    lines = [l for l in r.stdout.splitlines() if ('成功!' in l or '失敗!' in l or 'error' in l.lower())]
    red = [l.strip()[:120] for l in r.stdout.splitlines() if l.strip().startswith('失敗 ')]
    open(OUT, 'a', encoding='utf-8').write(f'{k} rc={r.returncode} ' + ' | '.join(lines[:3]) + ' RED:' + ';'.join(red[:6]) + chr(10))
    wr(p, orig)
