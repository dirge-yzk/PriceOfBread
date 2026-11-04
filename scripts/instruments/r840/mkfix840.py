# (840) c68hl 両側の門の小さな盤面（事前登録 m840 §7・期待は盤面の意図から書く＝台本の出力から写さない）
#   python -X utf8 -I mkfix840.py <出力フォルダ>  ＝<出力>/{arm,base,nA,nB}/region_s1.tsv
import sys, os
H = ['quarter', 'year', 'nationIndex', 'nation', 'region', 'tier', 'activeFlag', 'populationK', 'ruralPopulationK', 'opportunityIndex', 'holdingFactor', 'wageFactor',
     'landFactor', 'employmentShare', 'pushFactor', 'housingFactor', 'wagePremium', 'arableLand', 'migrationOutKPerQuarter', 'migrationInKPerQuarter',
     'settlerOutKPerQuarter', 'settlerInKPerQuarter', 'crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter', 'warIntensity', 'foodFillShare']
NQ = 12
def drop(lo): return [100.0] * 4 + [lo] * (NQ - 4)   # 4 四半期の 100 の後に lo へ一度に落ちる＝最大の落ち込みの比は lo/100
FLAT = [100.0] * NQ
# 盤面（州 → 系列）
BOARD = {
    'arm':  {'マドリード': drop(5.0), 'バレンシア': drop(5.0), 'ダブリン': drop(5.0), 'ナイン': drop(5.0), 'リスボン': drop(20.0)},
    'base': {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': drop(20.0), 'ナイン': FLAT, 'リスボン': FLAT},
    'nA':   {'マドリード': drop(4.0), 'バレンシア': drop(50.0), 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT},
    'nB':   {'マドリード': drop(90.0), 'バレンシア': drop(60.0), 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT},
}
out = sys.argv[1]
for k, regs in BOARD.items():
    d = os.path.join(out, k); os.makedirs(d, exist_ok=True)
    with open(os.path.join(d, 'region_s1.tsv'), 'wb') as f:
        f.write(('\t'.join(H) + '\n').encode())
        for q in range(NQ):
            for i, (rg, ser) in enumerate(regs.items()):
                tier = 'Colonial' if rg == 'ナイン' else 'Core'
                v = ser[q]
                row = [q, 1783 + q // 4, 0, 'テスト国', rg, tier, 1, v, v * 0.5] + [1.0] * 8 + [10.0] + [0.0] * 6 + [0.0, 1.0]
                f.write(('\t'.join(str(x) for x in row) + '\n').encode())
