# (924) 矯正講和で英へ専制が押し付けられた四半期の、相手の候補（戦争の表で終わりが近い戦争）と相手の政体
# 使い方: python -X utf8 -I q1_imposer.py <out>
import csv, sys, math
csv.field_size_limit(10**8)
EV = [('base_ins', 3, 156), ('base_ins', 7, 226), ('base_ins', 8, 174), ('base_ins', 9, 250), ('n1', 2, 50), ('n1', 5, 147), ('n2', 9, 184), ('a11', 12, 465), ('a11', 19, 170)]
POL = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和', 5: 'ブル独', 6: 'プロ独', 7: '軍政', 8: '無政府'}
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
for sub, s, q in EV:
    root = f'D:/PoB_work/keep_919/{sub}'
    yq = 1783 + q / 4.0
    cands = []
    with open(f'{root}/wars_s{s}.tsv', encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if 'イギリス' not in (r['sideA'], r['sideB']) or r['civilWarFlag'] == '1': continue
            end = int(r['startYear']) + float(r['months']) / 12.0
            if int(r['startYear']) <= yq and abs(end - yq) <= 1.5:
                other = r['sideB'] if r['sideA'] == 'イギリス' else r['sideA']
                cands.append((other, r['startYear'], f'{end:.2f}'))
    pol = {}
    with open(f'{root}/crownpc_s{s}.tsv', encoding='utf-8', newline='') as fh:
        for r in csv.DictReader(fh, delimiter='\t'):
            if r['quarter'] == str(q - 1):
                try: pol[r['nation']] = POL[int(float(r['polityCode']))]
                except Exception: pass
    out.write(f'{sub} s{s} q{q}({yq:.2f}) 英の前の札={pol.get("イギリス")} 候補=' + '・'.join(f'{o}[{pol.get(o, "?")}]({st}〜{e})' for o, st, e in cands) + '\n')
out.close()
