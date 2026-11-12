# (1083) 問い 3：計器の中の i の写し 2 本の差。rr.I（InstrumentsRulePower.cs:214〜216＝τ を 1 と置いた試し＝F_I÷τ）と interShare（:343＝F_I÷(F_I＋F_R)）。
# rr.I は持ち主の重み OwnerWeight＝Rule[0]＋Rule[1]×I×κ（:217）を通して ρ の乗り継ぎの重みと O の配りの b に入る。
# 式の写し（名乗る）：rr.I＝(F_I÷τ)÷(F_I÷τ＋F_R)（:215 の frProbe は Forces の F_R と同じ式）。使い方: python -X utf8 -I q3_iprobe.py <表のフォルダ> > q3_out.txt
import sys, os, collections, statistics as st
import sys as _s; _s.stdout.reconfigure(newline='\n')  # 出力は LF
d = sys.argv[1]; QS = {0, 100, 300, 600}
res = collections.defaultdict(list)
for s in range(1, 11):
    fi = open(os.path.join(d, f'rulepower_s{s}.tsv'), encoding='utf-8'); h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for line in fi:
        c = line.rstrip('\n').split('\t'); q = int(c[0])
        if q not in QS:
            if q > max(QS): break
            continue
        try:
            FI, FR, tau, k, r1 = float(c[ix['fInterIndex']]), float(c[ix['fResistIndex']]), float(c[ix['tauIndex']]), float(c[ix['kappaIndex']]), float(c[ix['nowRule1Share']])
        except ValueError: continue
        if not (tau > 0): continue
        a = FI / tau; iP = a / (a + FR) if a + FR > 0 else 1.0; iS = FI / (FI + FR) if FI + FR > 0 else 1.0
        res[q].append((abs(iP - iS), abs(r1 * k * (iP - iS)), tau))
print('四半期\t行\t|ΔI| 中央\t|ΔI| 9 割\t|ΔI| 最大\t|ΔOwnerWeight| 9 割\t最大\tτ の中央\tτ の 1 割〜9 割')
for q in sorted(res):
    v = res[q]; a = sorted(x[0] for x in v); b = sorted(x[1] for x in v); t = sorted(x[2] for x in v)
    p = lambda z, r: z[min(len(z) - 1, int(r * len(z)))]
    print(f'{q}\t{len(v)}\t{st.median(a):.3g}\t{p(a, .9):.3g}\t{a[-1]:.3g}\t{p(b, .9):.3g}\t{b[-1]:.3g}\t{st.median(t):.3g}\t{p(t, .1):.3g}〜{p(t, .9):.3g}')
