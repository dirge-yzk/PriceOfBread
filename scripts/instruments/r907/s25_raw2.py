# (907) 追加の集計：opium の国の行（scope=N）・cess の tier×4000km 超・route ごとの tier
import sys, os, csv, collections
d = sys.argv[1]
def rows(t):
    for s in range(1, 21):
        p = os.path.join(d, f'{t}_s{s}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8', newline='') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                r['_seed'] = s; yield r
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
O = [r for r in rows('opium') if r['scope'] == 'N']
ban = [r for r in O if r['exportBanFlag'] == '1']
print('[opium N] 国×四半期', len(O), '輸出禁止', len(ban), '禁止した国の数（種×国）', len({(r['_seed'], r['nation']) for r in ban}))
print('[opium N] 緊張>0', sum(1 for r in O if fl(r['tensionAddedPerQuarter']) > 0), '所持の法', dict(collections.Counter(r['possessionLawCode'] for r in O)))
print('[opium N] 所持の法 1以上で緊張>0', sum(1 for r in O if r['possessionLawCode'] in ('1', '2') and fl(r['tensionAddedPerQuarter']) > 0), '所持の法 0 で緊張>0', sum(1 for r in O if r['possessionLawCode'] == '0' and fl(r['tensionAddedPerQuarter']) > 0))
print('[opium N] 輸出禁止の国の輸出 exportsPerClearing>0', sum(1 for r in ban if fl(r['exportsPerClearing']) > 0), '禁止でない国で輸出>0', sum(1 for r in O if r['exportBanFlag'] != '1' and fl(r['exportsPerClearing']) > 0))
C = list(rows('cess'))
print('[cess] tier×(gcKm>4000)', dict(collections.Counter((r['tier'], fl(r['gcKm']) > 4000) for r in C)))
print('[cess] route×tier', dict(collections.Counter((r['route'], r['tier']) for r in C)))
print('[cess] canCross=0 の route', dict(collections.Counter((r['route'], r['sameLandmassFlag']) for r in C if r['canCrossFlag'] == '0')))
