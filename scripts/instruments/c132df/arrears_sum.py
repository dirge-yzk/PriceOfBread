# (1284) c132df arrears_s の集計（記録）。旗 1 の木では、金本位の月に predNew が真の行＝その月に本位を離れた月（月の表なので 1 四半期の内の往復も見える）。
# 使い方: python -X utf8 -I arrears_sum.py <生（D:/… の ins）> <種0> <種1>
import sys, csv, collections
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
UK = 'イギリス'
tot = collections.Counter(); per_seed_new = []; uk_new = []; nat_new = collections.Counter(); dec_new = collections.Counter()
for s in range(s0, s1 + 1):
    try: f = open(f'{raw}/arrears_s{s}.tsv', encoding='utf-8')
    except FileNotFoundError: per_seed_new.append(0); uk_new.append(0); tot['missing'] += 1; continue
    n_new = 0; n_uk = 0
    for r in csv.DictReader(f, delimiter='\t'):
        tot['rows'] += 1
        onG = r['onGoldBefore'] == '1'; po = r['predOld'] == '1'; pn = r['predNew'] == '1'; ow = r['owes'] == '1'
        if onG and pn: n_new += 1; nat_new[r['nation']] += 1; dec_new[int(r['year']) // 10 * 10] += 1
        if onG and pn and r['nation'] == UK: n_uk += 1
        if onG and po: tot['goldPredOld'] += 1
        if onG and pn and not po: tot['goldNewOnly'] += 1
        if onG and po and not pn: tot['goldOldOnly'] += 1
        if ow and not onG: tot['owesOffGold'] += 1
        if ow and onG: tot['owesOnGold'] += 1
        try:
            if float(r['arrNewMonth']) > 0 and not onG: tot['offGoldAccrueNew'] += 1
        except ValueError: pass
    per_seed_new.append(n_new); uk_new.append(n_uk)
print(f'# (1284) arrears_sum {raw} 種 {s0}〜{s1}')
print(f'行 {tot["rows"]}・表の無い種 {tot["missing"]}')
print(f'金本位の月に新の述語が真（旗 1 ではその月に離れた月）種ごと {per_seed_new}・英 {uk_new}')
print(f'金本位の月：旧の述語が真 {tot["goldPredOld"]}・新だけ {tot["goldNewOnly"]}・旧だけ {tot["goldOldOnly"]}')
print(f'owes の月：本位の外 {tot["owesOffGold"]}・本位の内 {tot["owesOnGold"]}・本位の外で新の月の量が正 {tot["offGoldAccrueNew"]}')
print(f'国ごと（新の述語で離れた月）{dict(nat_new.most_common(12))}')
print(f'年代 {dict(sorted(dec_new.items()))}')
