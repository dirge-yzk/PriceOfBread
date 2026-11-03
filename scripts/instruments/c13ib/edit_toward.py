# (812) c13ib 時間 doc 1-7 の形 A：x += k*(t-x) を PoliticalQuantities.Toward(x, t, k) へ。
# 使い方：python edit_toward.py <根> <群 1|2|3>
#   群 1＝Toward の本体を足す＋EconomyStep・Government・Military・MarketFlow
#   群 2＝Politics・TechEducation(60)・PoliticalQuantities・InstrumentsSecession・MultiNationSim
#   群 3＝World・Credit・Factions・Discovery・Simulation・SelfPlay
# 座標は measurements/m812_20261103_toward_sites.tsv（scan_toward.py の出力）。除外：TechEducation.cs:125（c13ia の tl 段6 が消す）・
# PopDecision.cs:105・InstrumentsUrx.cs:154（A3 で消えた）。代入の形（式の中）は下の MANUAL。
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from edithelp import Editor

root, group = sys.argv[1], int(sys.argv[2])
E = Editor(root)
S = 'PriceOfBread.SimCore/'

GROUPS = {
    1: ['EconomyStep.cs', 'Government.cs', 'Military.cs', 'MarketFlow.cs'],
    2: ['Politics.cs', 'TechEducation.cs', 'PoliticalQuantities.cs', 'InstrumentsSecession.cs', 'MultiNationSim.cs'],
    3: ['World.cs', 'Credit.cs', 'Factions.cs', 'Discovery.cs', 'Simulation.cs', 'SelfPlay.cs'],
}
files = GROUPS[group]

if group == 1:
    E.edit(S + 'PoliticalQuantities.cs',
           '    internal static double Pos(double x) => !(x > 0) ? 0 : x;\n',
           '    internal static double Pos(double x) => !(x > 0) ? 0 : x;\n'
           '    /// <summary>x を目標 <paramref name="target"/> へ速さ <paramref name="k"/> だけ寄せる＝`x + k×(target − x)`（時間 doc §3-2 の形 A・約 40 か所の写しの 1 本化）。'
           '中身は呼び元が書いていた 1 回の引き算と 1 回の掛け算と 1 回の足し算そのもの（`x += k*(t-x)`・`x = x + (t-x)*k` と同じ丸め）。k は呼び元の刻み（月・四半期）の速さ。'
           '既にある似た物は Government.Move（政策の軸だけ・段つき）と Pop.Blend（POP の混ぜ・別物）。</summary>\n'
           '    internal static double Toward(double x, double target, double k) => x + k * (target - x);\n')

SKIP = {('TechEducation.cs', 125), ('PopDecision.cs', 105), ('InstrumentsUrx.cs', 154)}
# TSV の k・t が壊れている行（括弧つきの積）は手で組む
MANUAL_ROWS = {
    ('Simulation.cs', 'World.WorkHoursValue'): ('World.Coef.WomenRightsFollowSpeed * (hoursTarget < World.WorkHoursValue ? enforce : 1.0)', 'hoursTarget'),
    ('Simulation.cs', 'World.ChildLaborLegal'): ('World.Coef.WomenRightsFollowSpeed * (childRegress ? 1.0 : enforce)', 'childTarget'),
}


def strip_outer(t):
    if t.startswith('(') and t.endswith(')'):
        d = 0
        for i, ch in enumerate(t):
            d += (ch == '(') - (ch == ')')
            if d == 0 and i < len(t) - 1:
                return t
        return t[1:-1]
    return t


tsv = open(os.path.join(root, 'measurements/m812_20261103_toward_sites.tsv'), 'rb').read().decode('utf-8').split('\n')
n_done = 0
for row in tsv:
    if not row.strip():
        continue
    f, ln, form, x, k, t, text = row.split('\t')
    if f not in files or (f, int(ln)) in SKIP:
        continue
    if (f, x) in MANUAL_ROWS:
        k, t = MANUAL_ROWS[(f, x)]
        old = f'{x} += {k} * ({t} - {x});'
        if f == 'Simulation.cs' and x == 'World.WorkHoursValue':
            old = f'{x} += {k} * ({t} - {x});'
    elif form == 'A':
        old = f'{x} += {k} * ({t} - {x});'
    else:
        old = f'{x} += ({t} - {x}) * {k};'
    new = f'{x} = PoliticalQuantities.Toward({x}, {strip_outer(t)}, {k});'
    E.edit(S + f, old, new)
    n_done += 1

# 式の中の形（代入の右辺に x + k*(t-x) がある物）
MANUAL = {
    'World.cs': [
        ('GdpTrend + Coef.GdpTrendSmoothing * (g - GdpTrend);', 'PoliticalQuantities.Toward(GdpTrend, g, Coef.GdpTrendSmoothing);'),
        ('GdpPcTrend + Coef.GdpTrendSmoothing * (pc - GdpPcTrend);', 'PoliticalQuantities.Toward(GdpPcTrend, pc, Coef.GdpTrendSmoothing);'),
        ('_moTrend += c.MonetaryTrendSpeed * (ratio - _moTrend);', '_moTrend = PoliticalQuantities.Toward(_moTrend, ratio, c.MonetaryTrendSpeed);'),
        ('PriceLevel += c.MonetaryPriceSensitivity * (cyclical - PriceLevel);', 'PriceLevel = PoliticalQuantities.Toward(PriceLevel, cyclical, c.MonetaryPriceSensitivity);'),
    ],
    'Credit.cs': [
        ('bank.Confidence = PoliticalQuantities.Clamp01(bank.Confidence + spd * (targetConf - bank.Confidence));',
         'bank.Confidence = PoliticalQuantities.Clamp01(PoliticalQuantities.Toward(bank.Confidence, targetConf, spd));'),
    ],
    'Factions.cs': [
        ('st.Cohesion = PoliticalQuantities.Clamp01(st.Cohesion + growth * (1.0 - st.Cohesion));',
         'st.Cohesion = PoliticalQuantities.Clamp01(PoliticalQuantities.Toward(st.Cohesion, 1.0, growth));'),
    ],
    'Discovery.cs': [
        ('s[g] = s[g] >= 0 ? s[g] + a * (x[g] - s[g]) : x[g];', 's[g] = s[g] >= 0 ? PoliticalQuantities.Toward(s[g], x[g], a) : x[g];'),
    ],
}
for f in files:
    for old, new in MANUAL.get(f, []):
        E.edit(S + f, old, new)
        n_done += 1
print('done', n_done)
