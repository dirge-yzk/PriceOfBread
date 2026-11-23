# (1323) c138lp 計器 lawdir_s の読み手（自分で集計する・エンジンの関数を呼ばない）。
#   門 G1（実際の側・⑮）：全ての行が singlePeakedFlag 1・netUpUsed＝netUpNew・netDownUsed＝netDownNew（相対 1e-9＋1e-12）・行 1 以上・表の欠け 0・
#     netDownNew≠−netUpNew の行が 1 以上で緑（旗 0 の生は singlePeakedFlag 0＝赤）。
#   記録：同じ盤面の新÷旧（後退の純支持 netDownNew と netDownOld の差・符号が変わった行＝旧で後退が支持され新で反対）・軸ごとの件数・英 1800 の参政権の行。
#   使い方: python -X utf8 -I lp_gate.py <生のフォルダ> <種の最初> <種の数>
import sys, csv, os, collections
raw, s0, n = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
def fl(x):
    try: return float(x)
    except Exception: return float('nan')
def near(a, b): return abs(a - b) <= 1e-9 * abs(a) + 1e-12
missing = rows = mism = asym = 0; flags = set(); flip = collections.Counter(); axrows = collections.Counter(); uk = []
print('seed\trows\tmismatch\tasymRows\tdownSignFlip')
for s in range(s0, s0 + n):
    p = os.path.join(raw, f'lawdir_s{s}.tsv')
    if not os.path.exists(p): print(f'{s}\t(lawdir_s が無い)'); missing += 1; continue
    c = [0, 0, 0, 0]
    with open(p, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for row in r:
            if len(row) < len(h): continue
            g = lambda k: fl(row[ix[k]])
            c[0] += 1; fg = row[ix['singlePeakedFlag']]; flags.add(fg)
            o = 'New' if fg == '1' else 'Old'
            if not (near(g('netUp' + o + 'Index'), g('netUpUsedIndex')) and near(g('netDown' + o + 'Index'), g('netDownUsedIndex'))): c[1] += 1
            dn, up, dold = g('netDownNewIndex'), g('netUpNewIndex'), g('netDownOldIndex')
            if not near(-up, dn): c[2] += 1
            if dold > 0 and dn <= 0: c[3] += 1; flip[row[ix['axis']]] += 1
            axrows[row[ix['axis']]] += 1
            if row[ix['nation']] == 'イギリス' and row[ix['axis']] == 'Suffrage' and row[ix['year']] in ('1800', '1801') and s == s0: uk.append(row)
    rows += c[0]; mism += c[1]; asym += c[2]
    print(f'{s}\t' + '\t'.join(map(str, c)))
print(f'# 合計 rows={rows} mismatch={mism} asymRows={asym} missing={missing} singlePeakedFlag={sorted(flags)}')
print('# 後退の純支持が旧で正・新で 0 以下の行（軸ごと・上位 15）: ' + ' '.join(f'{a}:{k}' for a, k in flip.most_common(15)))
if uk:
    print('# 英 1800〜1801 の参政権（最初の種）: ' + ' | '.join('\t'.join(x) for x in uk))
print('GATE ' + ('緑' if mism == 0 and missing == 0 and rows > 0 and flags == {'1'} and asym > 0 else '赤') + '（実際＝新の規則の食い違い 0・表の欠け 0・行 1 以上・全ての行が singlePeakedFlag 1・鏡写しでない行が 1 以上）')
