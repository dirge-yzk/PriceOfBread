# (847) c69hl 両側の門の小さな盤面（事前登録 m847 §6・期待は盤面の意図から書く＝台本の出力から写さない）＝mkfix840.py の写しに
#   種 2 つ（MU5）・〈腕＝母集合の 1 本〉（MU4）・母集合が空（州が無い／全て KMIN 未満＝MU6）・ヌル腕 3 組（件数の規則の一つ抜き＝MU7・MU8）を足した。
#   python -X utf8 -I mkfix847.py <出力フォルダ>  ＝<出力>/{arm,base,nA,nB,nC}/region_s{1,2}.tsv
import sys, os
H = ['quarter', 'year', 'nationIndex', 'nation', 'region', 'tier', 'activeFlag', 'populationK', 'ruralPopulationK', 'opportunityIndex', 'holdingFactor', 'wageFactor',
     'landFactor', 'employmentShare', 'pushFactor', 'housingFactor', 'wagePremium', 'arableLand', 'migrationOutKPerQuarter', 'migrationInKPerQuarter',
     'crossBorderOutKPerQuarter', 'crossBorderInKPerQuarter', 'warIntensity', 'foodFillShare']
NQ = 12
def drop(lo): return [100.0] * 4 + [lo] * (NQ - 4)   # 4 四半期の 100 の後に lo へ一度に落ちる＝最大の落ち込みの比は lo/100
FLAT = [100.0] * NQ
LOW = [0.5] * NQ      # ピークが KMIN（1 千人）に届かない＝比は nan（母集合から外れる）
REGS = ['マドリード', 'バレンシア', 'ダブリン', 'ナイン', 'リスボン', 'セビリア', 'ビルバオ', 'コルドバ']
# 盤面（集合 → 種 → 州 → 系列）。州が無い＝その集合の表に行を書かない（ビルバオはヌル腕 3 組に無い）
S1 = {
    'arm':  {'マドリード': drop(5.0), 'バレンシア': drop(5.0), 'ダブリン': drop(5.0), 'ナイン': drop(5.0), 'リスボン': drop(20.0),
             'セビリア': drop(5.0), 'ビルバオ': drop(5.0), 'コルドバ': drop(5.0)},
    'base': {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': drop(20.0), 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': FLAT, 'ビルバオ': FLAT, 'コルドバ': FLAT},
    'nA':   {'マドリード': drop(4.0), 'バレンシア': drop(50.0), 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': drop(50.0), 'コルドバ': LOW},
    'nB':   {'マドリード': drop(90.0), 'バレンシア': drop(60.0), 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': drop(60.0), 'コルドバ': LOW},
    'nC':   {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': FLAT, 'コルドバ': LOW},
}
S2 = {
    'arm':  {r: FLAT for r in REGS},
    'base': {r: FLAT for r in REGS},
    'nA':   {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': drop(3.0), 'コルドバ': LOW},
    'nB':   {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': FLAT, 'コルドバ': LOW},
    'nC':   {'マドリード': FLAT, 'バレンシア': FLAT, 'ダブリン': FLAT, 'ナイン': FLAT, 'リスボン': FLAT, 'セビリア': FLAT, 'コルドバ': LOW},
}
out = sys.argv[1]
for seed, BOARD in ((1, S1), (2, S2)):
    for k, regs in BOARD.items():
        d = os.path.join(out, k); os.makedirs(d, exist_ok=True)
        with open(os.path.join(d, f'region_s{seed}.tsv'), 'wb') as f:
            f.write(('\t'.join(H) + '\n').encode())
            for q in range(NQ):
                for rg, ser in regs.items():
                    tier = 'Colonial' if rg == 'ナイン' else 'Core'
                    v = ser[q]
                    row = [q, 1783 + q // 4, 0, 'テスト国', rg, tier, 1, v, v * 0.5] + [1.0] * 8 + [10.0] + [0.0] * 4 + [0.0, 1.0]
                    f.write(('\t'.join(str(x) for x in row) + '\n').encode())
