# (805) c12cm 突然変異：使い捨ての木に 1 つずつ誤りを入れる。使い方: MU=Mu6 python -X utf8 -I mutate.py <木の根>（batch.sh prep の編集の台本として呼ぶ＝木の根だけを渡す。MU は環境で渡す）。
#   バイトで読み書きする（CRLF と BOM を保つ）。
#   試験の突然変異（T1＝住居の門・T2＝工業の飽和・T3＝崩壊の講和の期待の側の列）：
#   Mu1  住居の門の述語の > を >=（都市の割合が閾値と等しい盤面で赤）
#   Mu2  住居の門の述語の向きを <
#   Mu3  PopDecision.MigrationValues だけ旧の字句で >=（1 本化の前の形が片方だけ割れる）
#   Mu4  工業の飽和の関数の + を -
#   Mu5  工業の飽和の関数の / を *
#   Mu7  期待の側が条約港の倍率を掛けない（InstrumentsCpeace.cs）
#   Mu8  エンジンが条約港の倍率を掛けない（Diplomacy.cs）＝MuTp と同じ
#   Mu9  期待の側が r を掛けない
#   Mu10 repFactors に r を書かない
#   腕の突然変異（門 G を落とす・種 1〜2・計器つき・全長）：
#   Mu6  実際の賠償の元が saPay でなく sa のまま（c11cp の台本と同じ）
#   MuCut 会議の削り `repBase -= repCut * repBase;` を消す
#   MuTp = Mu8
#   MuR  rep = repBase（r を掛けない）
import sys, os
root = sys.argv[1]; mu = sys.argv[2] if len(sys.argv) > 2 else os.environ['MU']
S = root + '/PriceOfBread.SimCore/'
def rep(path, old, new):
    b = open(path, 'rb').read()
    crlf = b'\r\n' in b
    s = b.decode('utf-8').replace('\r\n', '\n')
    assert s.count(old) == 1, (mu, old[:70], s.count(old))
    s = s.replace(old, new)
    open(path, 'wb').write((s.replace('\n', '\r\n') if crlf else s).encode('utf-8'))
D = S + 'Diplomacy.cs'; I = S + 'InstrumentsCpeace.cs'; P = S + 'PopDecision.cs'; E = S + 'EconomyStep.cs'
if mu == 'Mu1':
    rep(P, '> coef.HousingUrbanThreshold;   // 住居の門', '>= coef.HousingUrbanThreshold;   // 住居の門')
elif mu == 'Mu2':
    rep(P, '> coef.HousingUrbanThreshold;   // 住居の門', '< coef.HousingUrbanThreshold;   // 住居の門')
elif mu == 'Mu3':
    rep(P, 'if (HousingGateOpen(coef, region.SumUrbanPopulation(), totalPop)) housing =', 'if ((totalPop > Eps ? region.SumUrbanPopulation() / totalPop : 0.0) >= coef.HousingUrbanThreshold) housing =')
elif mu == 'Mu4':
    rep(E, '=> 1.0 + productiveInd / coef.IndProductivityScale;', '=> 1.0 - productiveInd / coef.IndProductivityScale;')
elif mu == 'Mu5':
    rep(E, '=> 1.0 + productiveInd / coef.IndProductivityScale;', '=> 1.0 + productiveInd * coef.IndProductivityScale;')
elif mu == 'Mu7':
    rep(I, '* (1.0 - r.RepCutW) * r.TpMultW * r.PayRW;', '* (1.0 - r.RepCutW) * r.PayRW;')
elif mu in ('Mu8', 'MuTp'):
    rep(D, 'saPay * (farCrush ? c.TreatyPortReparationMult : 1.0)).Value;', 'saPay * 1.0).Value;   /*MUTANT*/')
elif mu == 'Mu9':
    rep(I, '* (1.0 - r.RepCutW) * r.TpMultW * r.PayRW;', '* (1.0 - r.RepCutW) * r.TpMultW;')
elif mu == 'Mu10':
    rep(I, ".Append(':').Append(F(r.PayRW));", ".Append(':').Append(\"0\");")
elif mu == 'Mu6':
    rep(D, "double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * saPay * (farCrush", "double repBase = (c.ReparationGdpFraction.Value * loser.GdpPerMonth * sa * (farCrush   /*MUTANT*/")
elif mu == 'MuCut':
    rep(D, 'repBase -= repCut * repBase;', '/*MUTANT*/')
elif mu == 'MuR':
    rep(D, 'rep = repBase * lw.FiscalFundingRatio.Value;', 'rep = repBase;   /*MUTANT*/')
else:
    raise SystemExit('unknown ' + mu)
print('mutated', mu)
