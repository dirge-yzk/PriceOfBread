#!/usr/bin/env python
# (854) c70id：変異の使い捨ての写しに 1 本ずつ変異を当てる／元へ戻す。バイトで読み書きする（CR と BOM を保つ）。台本の型は scripts/instruments/c69id/mut846.py と同じ。
#   python mut854.py apply <ID> <写しの根>                    変異を当てる（当てる前に old が 1 回だけ現れることを確かめる）
#   python mut854.py restore <ID> <写しの根> <元の木の根>      その変異が触るファイルを元の木（コミット済み）の中身へ戻す
#   python mut854.py files <ID>      触るファイルを出す
#   python mut854.py filter <ID>     回す試験のフィルタを出す
#   python mut854.py check <元の木の根>   全ての変異の old が元の木に 1 回ずつ現れるかを数える
# 変異の ID と見込み（赤か緑か）は事前登録 measurements/m854_20261105_prereg_id.txt [3] のとおり。
import sys

ML = 'PriceOfBread.SimCore/MarketFlow.cs'
SIM = 'PriceOfBread.SimCore/Simulation.cs'
EPE = 'PriceOfBread.SimCore/EmptyPopEstate.cs'
DIS = 'PriceOfBread.SimCore/Discovery.cs'
SET = 'PriceOfBread.Tests/StateEduDemandTests.cs'

LC = 'FullyQualifiedName~LaborClosedTests'
SC = 'FullyQualifiedName~ServiceClosedTests'
MM = 'FullyQualifiedName~MintAtMarketTests'
FC = 'FullyQualifiedName~FullChainTests'
EP = 'FullyQualifiedName~EmptyPopEstateTests'
IB = 'FullyQualifiedName~IdeaBoardLoadTests'
ST = 'FullyQualifiedName~StateEduDemandTests'

F1_OLD = 'bool free1 = laborClosed && run == 1 && want < wrp && !(mwFloor > 0 && p1 < mwFloor);'
F1_NEW = 'bool free1 = laborClosed && run == 1 && !(mwFloor > 0 && p1 < mwFloor);'
G_PE = '            if (!(pe < wrp)) return false;                          // 最後の日の want が参照より下（参照をまたがない）\n'
G_RW2 = '        if (!(rw2 < wrp)) return false;                             // 参照より下（以後は単調に下がる）\n'
G_FLOOR_OLD = 'if (!(p > Eps) || (mwFloor > 0 && !(p >= mwFloor))) return false;'
G_FLOOR_NEW = 'if (!(p > Eps)) return false;'

S1 = '        if (!(u0 * sg >= 0)) return false;                                                        // ①参照をまたがない（値段と平行移動が同じ側）\n'
SXH = '        double c = sg * lnc / L, t0 = sg * u0 / L, t = t0, xk = 0, b = 0, root = 0, x = 0;\n        bool conv = false;\n        for (int it = 0; it < 40; it++)\n        {\n            x = t + c;\n'
SX = SXH + '            if (!(x > 0)) return false;\n'
SXN = SXH
S2 = '        if (!conv || !(t > 0)) return false;                                                      // ②収束・同じ側\n'
S3 = '        if (!(System.Math.Abs(e0) * L < PinnedClosedMaxDev)) return false;                       // ③不動点から遠い\n'
S3T = '        x = t + c; xk = System.Math.Pow(x, k); b = 1 + xk; root = System.Math.Pow(b, 1.0 / k);\n        double rho = 1.0 / (root * b);\n'
S4 = '        if (!(System.Math.Abs(a * e0) < maxCurv)) return false;                     // ④2次の式の外（縮みが 1 に近い＝不動点の近くでも曲がりが大きい）\n'

EOLD = '        if (!(w.Coef.EmptyPopEstate > 0)) return;\n'

M = {
    # ---- 項目 2：LaborClosed 4 本（新しい物差し＝StepDay 1 歩を h 回の上の変異）
    'NL1': (ML, LC, [(G_PE, '')]),                                                  # 参照をまたぐ向きの判定を外す
    'NL2': (ML, LC, [(G_FLOOR_OLD, G_FLOOR_NEW)]),                                  # 法の床の判定を外す（品余りの枝）
    'NL3a': (ML, LC, [(F1_OLD, F1_NEW)]),                                           # 1 日目が参照より下の判定だけ外す（内側の判定が止める＝緑の見込み）
    'NL3b': (ML, LC, [(G_PE, ''), (G_RW2, '')]),                                    # 内側の参照の判定だけ外す（1 日目の判定が止める＝緑の見込み）
    'NL3c': (ML, LC, [(F1_OLD, F1_NEW), (G_PE, ''), (G_RW2, '')]),                  # 両方外す（参照より上から出発する旬も閉じた式へ入る）
    # ---- 項目 2：ServiceClosed ②
    'NS1': (ML, SC, [(S1, '')]),                                                    # ① 参照をまたがない判定を外す
    'NS2': (ML, SC, [(S1, ''), (SX, SXN), (S2, '')]),                                # ①・x＞0・② を外す（測定済みの注の主張）
    'NS3': (ML, SC, [(S1, ''), (SX, SXN), (S2, ''), (S3 + S3T, S3T)]),                      # ＋③
    'NS4': (ML, SC, [(S1, ''), (SX, SXN), (S2, ''), (S3 + S3T, S3T), (S4, '')]),            # ＋④（判定を全部外す）
    # ---- 項目 3：市場買い（Simulation.MintGold）
    'MM1': (SIM, MM, [('bool atMarket = m.Price > parity && gapLeft > 1e-9;', 'bool atMarket = false;')]),
    'MM2': (SIM, MM, [('double price = atMarket ? m.Price : parity;', 'double price = parity;')]),
    'MM3': (SIM, MM + '|' + FC, [('if (m.Price > parity && !atMarket) continue;', 'if (!atMarket) continue;')]),
    'MM4': (SIM, MM, [('double qty = atMarket ? Math.Min(m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRatePerMonth.Value), gapLeft / m.Price)', 'double qty = atMarket ? m.Stock * PoliticalQuantities.Clamp01(World.Coef.MintFloorBuyRatePerMonth.Value)')]),
    'MM5': (SIM, MM, [(' * PoliticalQuantities.Clamp01(World.Coef.MintReserveSellRatePerMonth.Value);   // 買い戻しの歩調', ';   // 買い戻しの歩調')]),
    'MM6': (SIM, MM, [('if (World.Coef.MintBuysAtMarket > 0 && shortfall > 1e-9)', 'if (shortfall > 1e-9)')]),
    'MM7': (SIM, MM, [('if (atMarket) gapLeft -= value;', '')]),
    'MM8': ('PriceOfBread.SimCore/World.cs', MM, [('case Outside.Mint: w.CumulativeGoldMinted += d; break;', 'case Outside.Mint: break;')]),
    # ---- 項目 4：EmptyPopEstate の旗 0
    'E6a': (EPE, EP, [(EOLD, '')]),
    'E6b': (EPE, EP, [(EOLD, ''), ('            if (payers is null || !(live > 1e-9)) continue;\n', '            if (payers is null || !(live > 1e-9)) continue;\n            if (!(w.Coef.EmptyPopEstate > 0)) continue;\n')]),
    'E6c': (EPE, EP, [(EOLD, '        if (!(w.Coef.EmptyPopEstate > 0)) { w.AiRng?.NextULong(); return; }\n')]),
    'E6d': (EPE, EP, [(EOLD, '        if (!(w.Coef.EmptyPopEstate > 0)) { var tmp = new List<Pop>(); tmp.Add(null!); return; }\n')]),
    # ---- 項目 5：Discovery.IdeaBoardOf の三項式
    'D1': (DIS, IB, [('double sub = p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis;', 'double sub = p.SubsistenceShare;')]),
    'D2': (DIS, IB, [('double sub = p.SubsistenceBasisKind == SubsistenceBasisKind.Productivity ? p.SubsistenceShare : p.SubsistenceBasis;', 'double sub = p.SubsistenceBasis;')]),
    # ---- 項目 1：OneMonth の adminFill（(846) の形へ戻す）
    'OM1': (SET, ST, [('if (adminFill is double af) r.AdminFill = af;', 'r.AdminFill = adminFill ?? 1.0;')]),
}


def main():
    cmd = sys.argv[1]
    if cmd == 'check':
        root = sys.argv[2].rstrip('/\\')
        bad = 0
        for k, (path, flt, pairs) in M.items():
            s = open(root + '/' + path, 'rb').read().decode('utf-8').replace('\r\n', '\n')
            for old, new in pairs:
                n = s.count(old)
                print(k, n)
                if n != 1:
                    bad += 1
        print('BAD', bad)
        return
    mid = sys.argv[2]
    path, flt, pairs = M[mid]
    if cmd == 'files':
        print(path); return
    if cmd == 'filter':
        print(flt); return
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
                print('NOMATCH', mid, repr(old[:70]), n); sys.exit(2)
            s = s.replace(old, new)
        open(p, 'wb').write(s.encode('utf-8'))
        print('APPLIED', mid, path)
    elif cmd == 'restore':
        src = sys.argv[4].rstrip('/\\') + '/' + path
        open(p, 'wb').write(open(src, 'rb').read())
        print('RESTORED', mid, path)


main()
