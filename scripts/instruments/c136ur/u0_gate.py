# (1312) c136ur 読むだけ：urxpd_s の U0 の列（(1312) で足した 21 列）から、門〈都市の POP が 0 で、前の月に案の流れ＞Eps と読んだのに、次の月も 0 の州×月〉と記録を数える。
#   使い方: python -X utf8 -I u0_gate.py <ins（D:/…）> <種の起点 1|11> [案 A|A2（門に使う案・既定 A）]
#   門＝Σ emptyUrbanStuck（案 A は emptyUrbanStuckRegionMonths・案 A′ は emptyUrbanStuckA2RegionMonths）＝0（全ての種の和）。
#   記録：都市 0 の州×月（植民地格・本国格）・そのうち g＞0・開幕（四半期 0〜3）と年代の V の重みつきの平均（国ごと）・農民の稼ぎの内訳の比・押し合い（州×四半期）。
import sys, os, csv, collections

COLS = ['emptyUrbanColRegionMonths', 'emptyUrbanHomeRegionMonths', 'emptyUrbanGPosRegionMonths', 'emptyUrbanGPosA2RegionMonths',
        'emptyUrbanStuckRegionMonths', 'emptyUrbanStuckA2RegionMonths', 'mktRegionMonths', 'mktTotal', 'gPosMarketRegionMonths',
        'gPosMarketA2RegionMonths', 'wVuMarket', 'wVrMarket', 'wVrMarketA2', 'farmLaborSalesSum', 'rgoResidualSum', 'farmEarnedSum',
        'laborSalesSum', 'urbanEarnedSum', 'ppRegionQuarters', 'ppOppRegionQuarters', 'ppCancelK']
WIN = [('開幕 q0-3', 0, 3), ('1784-1814', 4, 127), ('1815-1879', 128, 387), ('1880-1913', 388, 523), ('1914-1950', 524, 9999)]

def f(x):
    try:
        return float(x)
    except (TypeError, ValueError):
        return float('nan')

def main():
    d = sys.argv[1]; sb = int(sys.argv[2]); arm = sys.argv[3] if len(sys.argv) > 3 else 'A'
    tot = collections.defaultdict(float); per = []; win = collections.defaultdict(lambda: collections.defaultdict(float))
    seeds = 0; missing = 0
    for s in range(sb, sb + 10):
        p = os.path.join(d, f'urxpd_s{s}.tsv')
        if not os.path.exists(p):
            continue
        seeds += 1
        acc = collections.defaultdict(float)
        with open(p, encoding='utf-8', newline='') as fh:
            rd = csv.DictReader(fh, delimiter='\t')
            if 'emptyUrbanStuckRegionMonths' not in (rd.fieldnames or []):
                missing += 1; continue
            for r in rd:
                q = int(r['quarter'])
                for k in COLS:
                    v = f(r[k]); acc[k] += v; tot[k] += v
                wn = next(n for n, a, b in WIN if a <= q <= b)
                w = win[(r['nation'], wn)]
                for k in ('mktTotal', 'wVuMarket', 'wVrMarket', 'wVrMarketA2', 'mktRegionMonths', 'gPosMarketRegionMonths', 'gPosMarketA2RegionMonths'):
                    w[k] += f(r[k])
        per.append((s, acc))
    print(f'生 {d}・種 {seeds}（U0 の列が無い種 {missing}）')
    if seeds == 0 or missing == seeds:
        print('U0 の列が無い＝門は読めない'); return
    print('種\t都市0 植民地格\t都市0 本国格\tうち g_A>0\tうち g_A2>0\t次の月も0 案A\t次の月も0 案A2\t押し合い 逆符号/州×四半期\t打ち消し 千人')
    for s, a in per:
        print(f"{s}\t{a['emptyUrbanColRegionMonths']:.0f}\t{a['emptyUrbanHomeRegionMonths']:.0f}\t{a['emptyUrbanGPosRegionMonths']:.0f}\t{a['emptyUrbanGPosA2RegionMonths']:.0f}\t"
              f"{a['emptyUrbanStuckRegionMonths']:.0f}\t{a['emptyUrbanStuckA2RegionMonths']:.0f}\t{a['ppOppRegionQuarters']:.0f}/{a['ppRegionQuarters']:.0f}\t{a['ppCancelK']:.1f}")
    def r_(a, b):
        return a / b if b else float('nan')
    print(f"全体：州×月 {tot['mktRegionMonths']:.0f}・g_A>0 の割合 {r_(tot['gPosMarketRegionMonths'], tot['mktRegionMonths']):.3f}・g_A2>0 の割合 {r_(tot['gPosMarketA2RegionMonths'], tot['mktRegionMonths']):.3f}")
    print(f"農民の稼ぎの内訳（全期間の和の比）：農業労働の売上÷稼ぎ {r_(tot['farmLaborSalesSum'], tot['farmEarnedSum']):.3f}・RGO の残余÷稼ぎ {r_(tot['rgoResidualSum'], tot['farmEarnedSum']):.3f}"
          f"・工場労働の売上÷工員の稼ぎ {r_(tot['laborSalesSum'], tot['urbanEarnedSum']):.3f}")
    print('国\t年代\tV_u（A）\tV_r（A）\tV_r（A′）\tg_A>0 の州×月の割合\tg_A2>0 の割合')
    for (n, wn), w in sorted(win.items(), key=lambda kv: (kv[0][0], [x[0] for x in WIN].index(kv[0][1]))):
        t = w['mktTotal']
        print(f"{n}\t{wn}\t{r_(w['wVuMarket'], t):.4f}\t{r_(w['wVrMarket'], t):.4f}\t{r_(w['wVrMarketA2'], t):.4f}\t{r_(w['gPosMarketRegionMonths'], w['mktRegionMonths']):.3f}\t{r_(w['gPosMarketA2RegionMonths'], w['mktRegionMonths']):.3f}")
    k = 'emptyUrbanStuckRegionMonths' if arm == 'A' else 'emptyUrbanStuckA2RegionMonths'
    v = tot[k]
    print(f"門（案 {arm}）：{k} の全ての種の和 {v:.0f}・{'通る' if v == 0 else '落ちる'}（外す条件＝0 件でない）")

if __name__ == '__main__':
    main()
