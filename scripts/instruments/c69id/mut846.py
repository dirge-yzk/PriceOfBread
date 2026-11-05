#!/usr/bin/env python
# (846) c69id：変異の使い捨ての写しに 1 本ずつ変異を当てる／元へ戻す。バイトで読み書きする（CR と BOM を保つ）。
#   python mut846.py apply <ID> <写しの根>      変異を当てる（当てる前に old が 1 回だけ現れることを確かめる）
#   python mut846.py restore <ID> <写しの根> <元の木の根>   その変異が触るファイルを元の木（コミット済み）の中身へ戻す
#   python mut846.py files <ID>                  触るファイルを出す
# 変異の ID は事前登録 measurements/m846_20261105_prereg_id.txt [3] のとおり。
import sys

U = lambda s: s.encode('utf-8')

M = {
    'ML1': ('PriceOfBread.SimCore/MarketFlow.cs', [
        ('            P = pe; return true;', '            P = pe * 1.001; return true;'),
        ('        P = p; return true;\n    }\n\n    internal static bool ServiceClosed', '        P = p * 1.001; return true;\n    }\n\n    internal static bool ServiceClosed'),
    ]),
    'ML2': ('PriceOfBread.SimCore/MarketFlow.cs', [
        ('bool laborClosed = h > 1 && bandFloorOff;', 'bool laborClosed = false;'),
    ]),
    'MS1': ('PriceOfBread.SimCore/MarketFlow.cs', [
        ('        P = p;\n        return true;\n    }\n', '        P = p * (1 + 1e-4);\n        return true;\n    }\n'),
    ]),
    'MS2': ('PriceOfBread.SimCore/MarketFlow.cs', [
        ('if (closedWage && ServiceClosed(P0,', 'if (false && closedWage && ServiceClosed(P0,'),
    ]),
    'ME1': ('PriceOfBread.SimCore/EconomyStep.cs', [
        ('if (want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN', 'if (want > boughtQty) m.LastDemand += 2 * (want - boughtQty);   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN'),
    ]),
    'ME2': ('PriceOfBread.SimCore/EconomyStep.cs', [
        ('if (want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN', 'if (false && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形＝NaN'),
    ]),
    'ME3': ('PriceOfBread.SimCore/EconomyStep.cs', [
        ('if (want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形）', 'if (false && want > boughtQty) m.LastDemand += want - boughtQty;   // ★(452) 取れなかった買いたい分も値段が読む需要へ（否定形）'),
    ]),
    'ME4': ('PriceOfBread.SimCore/EconomyStep.cs', [
        ('* (coef.EduReachFromAdmin > 0 ? 1.0 - coef.EduReachFromAdmin * (1.0 - PoliticalQuantities.Clamp01(region.AdminFill)) : 1.0);   // 国庫の買いたい分', '* 1.0;   // 国庫の買いたい分'),
    ]),
    'MR1': ('PriceOfBread.SimCore/InstrumentsRule.cs', [
        ('        lagSt.Prev = rNow;\n        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;\n', '        if (!(picked || yearEnd) || !(w.TotalPopulation > 0)) return;\n        lagSt.Prev = rNow;\n'),
    ]),
    'MR2': ('PriceOfBread.SimCore/InstrumentsRule.cs', [
        ("string p1 = string.Join('\\t', F(rNow), F(mReg), F(mBar), F(orderNew),", "string p1 = string.Join('\\t', F(rNow), F(mBar), F(mReg), F(orderNew),"),
    ]),
    'MR3': ('PriceOfBread.SimCore/InstrumentsRule.cs', [
        ('double orderNew = !(mReg > 0) ? 1.0 : RuleClamp01(rNow * mBar / mReg);', 'double orderNew = !(mReg > 0) ? 1.0 : RuleClamp01(rNow * mReg / mBar);'),
    ]),
    'MF1': ('PriceOfBread.SimCore/World.cs', [
        ('case Outside.Mint: w.CumulativeGoldMinted += d; break;', 'case Outside.Mint: break;'),
    ]),
    'MF2': ('PriceOfBread.SimCore/Simulation.cs', [
        ('if (m.Price > parity && !atMarket) continue;', 'if (!atMarket) continue;'),
    ]),
    'MF3': ('PriceOfBread.SimCore/Simulation.cs', [
        ('bool atMarket = m.Price > parity && gapLeft > 1e-9;', 'bool atMarket = false;'),
    ]),
}

def main():
    cmd, mid = sys.argv[1], sys.argv[2]
    path, pairs = M[mid]
    if cmd == 'files':
        print(path); return
    root = sys.argv[3]
    p = root.rstrip('/\\') + '/' + path
    if cmd == 'apply':
        b = open(p, 'rb').read()
        crlf = b'\r\n' in b
        s = b.decode('utf-8')
        for old, new in pairs:
            if crlf:
                old = old.replace('\n', '\r\n'); new = new.replace('\n', '\r\n')
            n = s.count(old)
            if n != 1:
                print('NOMATCH', mid, repr(old[:60]), n); sys.exit(2)
            s = s.replace(old, new)
        open(p, 'wb').write(s.encode('utf-8'))
        print('APPLIED', mid, path)
    elif cmd == 'restore':
        src = sys.argv[4].rstrip('/\\') + '/' + path
        open(p, 'wb').write(open(src, 'rb').read())
        print('RESTORED', mid, path)

main()
