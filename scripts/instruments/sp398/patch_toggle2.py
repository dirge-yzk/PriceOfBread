# (1316) 注（空振り）：この突然変異の台本は (1309) の S1（走り書きの欄を返り値へ）より前の木の行を書き換える＝今の木では空振り（置き換える行が無く assert で落ちる）。使わない。
import sys
root = sys.argv[1] + '/PriceOfBread.SimCore/'
def rw(name, pairs):
    p = root + name; t = open(p, 'rb').read().decode('utf-8')
    for old, new in pairs:
        assert t.count(old) == 1, (name, old[:70], t.count(old)); t = t.replace(old, new)
    open(p, 'wb').write(t.encode('utf-8'))
# old-emulation: hoist (compute wedge/pow for needless goods too) and spoil (12-sum)
rw('EconomyStep.cs', [
 ("            if (!needless)\n            {\n                // 買い手価格の楔5項", "            if (!needless || PT.FlipR)\n            {\n                // 買い手価格の楔5項"),
 ("                spm.Stock *= 1.0 - spoil;\n", "                if (PT.FlipR) { double spSum = 0; for (int si = 0; si < 12; si++) spSum += spm.SpoilLog[si]; PT.Sink += spSum; }\n                spm.Stock *= 1.0 - spoil;\n"),
])
# sellers-loop prefetch of Market objects (experiment) when FlipB
rw('BilateralMarket.cs', [
 ("            sellers.Clear();\n            for (int t = 0; t < tc; t++)\n            {\n                if (!tr[t].linked) continue;\n                var m = mktOf[t];\n",
  "            sellers.Clear();\n            for (int t = 0; t < tc; t++)\n            {\n                if (PT.FlipB && t + 8 < tc) PrefetchObj(mktOf[t + 8]);\n                if (!tr[t].linked) continue;\n                var m = mktOf[t];\n"),
 ("    private bool LastPairWasLand;", "    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]\n    private static unsafe void PrefetchObj(Market? m)\n    {\n        if (m is not null && System.Runtime.Intrinsics.X86.Sse.IsSupported) System.Runtime.Intrinsics.X86.Sse.Prefetch0(System.Runtime.CompilerServices.Unsafe.AsPointer(ref m.Stock));\n    }\n\n    private bool LastPairWasLand;"),
])

rw('BilateralMarket.cs', [
 ("            double sum = 0; int cnt = 0;\n            for (int t = 0; t < tc; t++)\n            {\n                if (!tr[t].linked) continue;\n                var mk0 = tr[t].r.Markets[g];\n",
  "            PT.Seg(1, PT.FlipB ? 72 : 70);\n            double sum = 0; int cnt = 0;\n            for (int t = 0; t < tc; t++)\n            {\n                if (PT.FlipB && t + 6 < tc && tr[t + 6].linked) PrefetchObj(tr[t + 6].r.Markets[g]);\n                if (!tr[t].linked) continue;\n                var mk0 = tr[t].r.Markets[g];\n"),
 ("            if (cnt == 0) continue;\n            double avg = sum / cnt;\n", "            PT.Seg(1, PT.FlipB ? 73 : 71);\n            if (cnt == 0) continue;\n            double avg = sum / cnt;\n"),
])

# csproj unsafe
p = root + 'PriceOfBread.SimCore.csproj'; t = open(p,'rb').read().decode('utf-8'); nl = '\r\n' if '\r\n' in t else '\n'
t = t.replace('<Nullable>enable</Nullable>'+nl, '<Nullable>enable</Nullable>'+nl+'    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>'+nl); open(p,'wb').write(t.encode('utf-8'))
