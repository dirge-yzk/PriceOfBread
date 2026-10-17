import sys
p = sys.argv[1] + '/PriceOfBread.SimCore/EconomyStep.cs'
t = open(p, 'rb').read().decode('utf-8')
def rep(old, new):
    global t
    assert t.count(old) == 1, old[:60] + ' x' + str(t.count(old))
    t = t.replace(old, new)
rep('        var goodGate = scratch.GoodGate;\n', '        PT.Seg(0, 40);\n        var goodGate = scratch.GoodGate;\n')
rep('        var produced = scratch.Produced;\n        var inputNeed', '        PT.Seg(0, 41);\n        var produced = scratch.Produced;\n        var inputNeed')
rep('        // 投資家は投資枠（貯蓄の IndInvestmentFrac）を消費予算から控除', '        PT.Seg(0, 42);\n        // 投資家は投資枠（貯蓄の IndInvestmentFrac）を消費予算から控除')
rep('        // 在庫の腐敗・劣化（保管期間に応じた減衰）：穀物', '        PT.Seg(0, 43);\n        // 在庫の腐敗・劣化（保管期間に応じた減衰）：穀物')
rep('        foreach (var m in region.Markets)\n        {\n            m.PrevImport', '        PT.Seg(0, 44);\n        foreach (var m in region.Markets)\n        {\n            m.PrevImport')
rep('        // 植民地素材の輸入（植民地と制海で供給）', '        PT.Seg(0, 45);\n        // 植民地素材の輸入（植民地と制海で供給）')
rep('        // 労働配分の重み＝前期総需要。工業化度の減価償却', '        PT.Seg(0, 46);\n        // 労働配分の重み＝前期総需要。工業化度の減価償却')
rep('        // ★助走中は減価も止める（2026-08-10 追加裁定）', '        PT.Seg(0, 47);\n        // ★助走中は減価も止める（2026-08-10 追加裁定）')
rep('            var good = consumeOrder[coi];\n', '            PT.Seg(0, 50);\n            var good = consumeOrder[coi];\n')
rep('            int pIdx = -1;   // 阿片の困窮ゲート', '            PT.Seg(0, 51);\n            int pIdx = -1;   // 阿片の困窮ゲート')
rep('            // コア需要（消費・工場投入・建設投資・軍需）＝通常の比例配給。', '            PT.Seg(0, 52);\n            // コア需要（消費・工場投入・建設投資・軍需）＝通常の比例配給。')
rep('                else p.UnemploymentEma = new Share(p.UnemploymentEma.Value + coef.JobSearchUsesEma * (p.UnemploymentRate.Value - p.UnemploymentEma.Value));\n            }\n    }\n', '                else p.UnemploymentEma = new Share(p.UnemploymentEma.Value + coef.JobSearchUsesEma * (p.UnemploymentRate.Value - p.UnemploymentEma.Value));\n            }\n        PT.Seg(0, 60);\n    }\n')
open(p, 'wb').write(t.encode('utf-8'))
