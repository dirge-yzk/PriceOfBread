# (1269) c130k15 突然変異を木に 1 本入れる（バイトで読み書き・ちょうど 1 回の置き換え）。使い方: python mut_apply.py <木> <μ0..μ3>
import sys
tree, m = sys.argv[1], sys.argv[2]
ENG = 'PriceOfBread.SimCore/Meritocracy.cs'
TST = 'PriceOfBread.Tests/JobSearchLiteracyOffTests.cs'
OLD = 'double eff = c.JobSearchLiteracyOff > 0 ? c.JobSearchRatePerQuarter.Value   //'
MUT = {
    # μ0：旗 0（試験の中の旗の置きを 0 に）
    'm0': (TST, '        c.JobSearchLiteracyOff = flag;\n', '        c.JobSearchLiteracyOff = 0;\n'),
    # μ1：旗 1 の枝に重みを残す
    'm1': (ENG, OLD, 'double eff = c.JobSearchLiteracyOff > 0 ? c.JobSearchRatePerQuarter.Value * (1.0 - c.JobSearchLiteracyWeight * (1.0 - p.Literacy))   //'),
    # μ2：識字の向きを逆
    'm2': (ENG, OLD, 'double eff = c.JobSearchLiteracyOff > 0 ? c.JobSearchRatePerQuarter.Value * (1.0 - c.JobSearchLiteracyWeight * p.Literacy)   //'),
    # μ3：大きさの取り違え（識字に依らないが半分）
    'm3': (ENG, OLD, 'double eff = c.JobSearchLiteracyOff > 0 ? c.JobSearchRatePerQuarter.Value * 0.5   //'),
}
f, o, n = MUT[m]; p = f'{tree}/{f}'
b = open(p, 'rb').read(); ob, nb = o.encode('utf-8'), n.encode('utf-8')
if b.count(ob) != 1: print(f'{m} 置き換えの旧が {b.count(ob)} 回'); sys.exit(1)
open(p, 'wb').write(b.replace(ob, nb)); print(f'{m} 入れた：{p}')
