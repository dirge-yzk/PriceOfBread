# (1172) c116t1 の突然変異（使い捨ての木の中だけで使う）：python -X utf8 -I mutate.py <木> <名>
# 計器の突然変異（M7・M8・M11）とエンジンの突然変異（M2・M6・M9・M10）を、旧の文字列がちょうど 1 回のときだけ置き換える（バイトで読み書き）。
import sys, os
root, name = sys.argv[1], sys.argv[2]
INS = 'PriceOfBread.SimCore/Instruments/InstrumentsBuyShadow.cs'
BM = 'PriceOfBread.SimCore/BilateralMarket.cs'
M = {
    # 旗 0 の木（試験の盤面の直しが旗 0 でも緑かを確かめる）
    'F0': [('PriceOfBread.SimCore/Coefficients.p04_trade.cs', 'public double TradeBuyBySupplyCurve = 1.0;', 'public double TradeBuyBySupplyCurve = 0.0;')],
    # 計器：退きを成約ごとに足す（行ごとに 1 回でなく）
    'M7': [(INS, 'if (!double.IsNaN(qEng)) { r.AnyEng = true; if (qEng < r.MinQEng) r.MinQEng = qEng;',
                 'if (!double.IsNaN(qEng)) { r.AnyEng = true; if (r.P - qEng > r.Tol) r.N.RetreatRowSum += r.P - qEng; if (qEng < r.MinQEng) r.MinQEng = qEng;'),
           (INS, 'if (rr > 0) { n.RetreatRowN++; n.RetreatRowSum += rr; }', 'if (rr > 0) { n.RetreatRowN++; }')],
    # 計器：影の天井に Q を使う（P に戻さない）
    'M8': [(INS, 'double ysh = System.Math.Min(r.D * pow2e - r.P - r.I0, r.TS + r.D * System.Math.Max(1.0, rootE) - r.P - r.S0);',
                 'double qs = double.IsNaN(qEng) ? r.P : qEng; double ysh = System.Math.Min(r.D * pow2e - qs - r.I0, r.TS + r.D * System.Math.Max(1.0, rootE) - qs - r.S0);')],
    # 計器：影の上界に access を掛ける（a＝0.5）
    'M11': [(INS, 'double e = qA - System.Math.Max(0.0, ysh - y);', 'double e = qA - 0.5 * System.Math.Max(0.0, ysh - y);')],
    # エンジン：行の門の u_min を lp に（先頭の売り手の着値の下界を見ない）
    'M3': [(BM, 'if (headS < sellers.Count) { double hm = sellers[headS].Price * a1LbMin; if (hm < uMin) uMin = hm; }', '')],
    # エンジン：行の門の述語を逆に＝A-1 の路でも旧の門で飛ばす
    'M4': [(BM, 'rowSkip = !(ub > 0);', 'rowSkip = !(m.TargetStock + m.LastDemand - m.LastProduced - m.Stock > 0);')],
    # エンジン：Q＝P（A-1 の Q を P に）
    'M2': [(BM, 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;', 'a1Q = m.LastProduced;')],
    # エンジン：成約ごとに退きを重ねる（Q_d＝P − Σ(P − P̂(u′)) の近い形＝前の成約の Q から更に P−P̂ を引く）
    'M6': [(BM, 'double a1Q = m.LastProduced, a1Root = 1.0;', 'double a1Q = m.LastProduced, a1Root = 1.0, a1QAcc = double.NaN;'),
           (BM, 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;',
                'double a1Prev = double.IsNaN(a1QAcc) ? m.LastProduced : a1QAcc; a1Q = a1Prev - (m.LastProduced - Math.Min(m.LastProduced, ph)); if (!(a1Q >= 0)) a1Q = 0; a1QAcc = a1Q;')],
    # エンジン：u′ の代わりに unitA
    'M10': [(BM, 'double uP = Math.Min(unitA, lp);', 'double uP = unitA;')],
    # エンジン：初版の Q＝P −(P̂(lp) − P̂(u′))（min を外す）
    'M9': [(BM, 'a1Q = Math.Min(m.LastProduced, ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;',
                'long cntM9 = 0; double phLp = A1PHat(a1Pr, a1St, t * gc + g, lp / exRate[ni], nations[ni].World.Coef, ref cntM9); a1Q = m.LastProduced - (phLp - ph); if (!(a1Q >= 0)) a1Q = m.LastProduced;')],
}
for f, old, new in M[name]:
    p = os.path.join(root, f)
    b = open(p, 'rb').read()
    o, n = old.encode('utf-8'), new.encode('utf-8')
    c = b.count(o)
    if c != 1:
        print(f'NG {name} {f} 旧の回数 {c}'); sys.exit(1)
    open(p, 'wb').write(b.replace(o, n))
print(f'OK {name}')
