#!/usr/bin/env python3
# (780) c8sv 段0 の補い：助走（_calendarFrozen）の注入は計器 silverpar_s が捕捉しない＝助走を含む全ての月で World.cs の silverRel の挟みと Fiscal.cs の b の上端に届いたかを
# 使い捨ての木（batch.sh prep の編集の python）で数える。読むだけ（値を変えない・ファイルへ追記するだけ）。コミットする木には当てない。
# 使い方：POB_WORK=<作業> bash scripts/instruments/batch.sh prep probe dir:<木> plain scripts/instruments/c8sv/probe_warmup_edit.py  →  batch.sh run probe 2
# 出力：D:/PoB_work/c8sv/probe_hits.txt（W＝World の silverRel：値が変わった月と届いた月だけ・B＝Fiscal の b：上端 cap に届いた月）
import sys, os
root = sys.argv[1]
BS = chr(92)
def rw(rel, fn):
    p = os.path.join(root, rel); b = open(p, 'rb').read(); n = fn(b); assert n != b, rel; open(p, 'wb').write(n)
def lg(tag, frozen, hit, val, ws):
    t = BS + 't'; nl = BS + 'n'
    return ('System.IO.File.AppendAllText(@"D:' + BS + 'PoB_work' + BS + 'c8sv' + BS + 'probe_hits.txt", $"' + tag + t + '{Environment.ProcessId}' + t + '{(' + frozen + ' ? 1 : 0)}' + t + hit + t + '{' + val + ':R}' + t + '{' + ws + ':R}' + nl + '");').encode('utf-8')
def world(b):
    a = b'            silverRel = Math.Clamp(silverRel, c.ExchangeRateFloor, c.ExchangeRateCeil);'
    assert b.count(a) == 1
    new = (b'            lock (typeof(World)) { bool hit_ = !(silverRel >= c.ExchangeRateFloor && silverRel <= c.ExchangeRateCeil); if (hit_ || silverRel != PobProbeLast) { PobProbeLast = silverRel; try { '
           + lg('W', 'WarmupFrozen', '{(hit_ ? 1 : 0)}', 'silverRel', 'WorldSilverPrice') + b' } catch { } } }\n' + a)
    b = b.replace(a, new)
    f = b'    public double WorldSilverPrice { get; internal set; }'
    assert b.count(f) == 1
    return b.replace(f, f + b'\n    static double PobProbeLast;')
def fiscal(b):
    a = b'            b = Math.Clamp(b, 1.0, c.GoldDebtBurdenCap);'
    assert b.count(a) == 1
    new = (b'            if (b >= c.GoldDebtBurdenCap || !(b > 0 && b < double.PositiveInfinity)) { try { ' + lg('B', 'w.WarmupFrozen', '1', 'b', 'w.WorldSilverPrice') + b' } catch { } }\n' + a)
    return b.replace(a, new)
rw('PriceOfBread.SimCore/World.cs', world)
rw('PriceOfBread.SimCore/Fiscal.cs', fiscal)
print('probe edit ok')
