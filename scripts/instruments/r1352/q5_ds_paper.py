# (1352) 紙の予言：ds（行き先の機会×行き先の食の充足）で出る州の差 gMax がどれだけ縮むか（行き先＝ロンドン・マンチェスター・距離の摩擦は省く）
# 送る割合 fraction＝RegionMobility(0.03)×gMax（上限 0.2）。受け入れの上限は省く（上側の見積もり）。
import sys, csv
sys.stdout.reconfigure(encoding='utf-8')
src, arm, A, y0, y1 = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
D = ['ロンドン', 'マンチェスター']
rows = {}
for r in csv.DictReader(open(src, encoding='utf-8'), delimiter='\t'):
    if r['arm'] == arm: rows[(int(r['quarter']), r['region'])] = r
qs = sorted({q for q, reg in rows if reg == A and y0 <= int(rows[(q, reg)]['year']) <= y1})
s0 = s1 = obs = 0.0; n0 = n1 = 0
for q in qs:
    a = rows[(q, A)]; st = float(a['stayApprox']); pop = float(a['populationK'])
    g0 = max(float(rows[(q, d)]['opportunityIndex']) - st - 0.15 for d in D)
    g1 = max(float(rows[(q, d)]['opportunityIndex']) * float(rows[(q, d)]['foodFillShare']) - st - 0.15 for d in D)
    f0 = min(0.2, 0.03 * max(0, g0)) * 3 * pop; f1 = min(0.2, 0.03 * max(0, g1)) * 3 * pop   # 月ごと×3＝四半期（月の中の変化は省く）
    s0 += f0; s1 += f1; obs += float(a['migrationOutKPerQuarter']); n0 += g0 > 0; n1 += g1 > 0
print('arm=%s %s %d-%d 四半期 %d' % (arm, A, y0, y1, len(qs)))
print('観測の流出の和 %.0f 千人・紙の旧の式の和 %.0f・紙の ds の和 %.0f（ds/旧 %.2f）・g>0 の四半期 旧 %d → ds %d' % (obs, s0, s1, s1 / s0 if s0 else float('nan'), n0, n1))
