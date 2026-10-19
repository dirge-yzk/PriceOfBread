import csv, statistics as st
# 段0：民間の教育の支払い（spend_s・好きな階級の合計）と国家の買い付け（reform_s）の大きさの比。読むだけ。
base = 'D:/PoB_work/keep_451/m451_ins/'
NAT = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本']
# spend_s bucket: 0..8 (Q0-3,4-7,8-39,40-159,160-187,188-227,228-267,268-399,400-)
BK = {7: (268, 399), 8: (400, 667), 6: (228, 267), 5: (188, 227), 4: (160, 187), 3: (40, 159)}
priv = {}  # (nation, bucket) -> list over seeds of sum pay
for s in range(1, 11):
    tot = {}
    with open(base + f'spend_s{s}.tsv', encoding='utf-8') as f:
        for d in csv.DictReader(f, delimiter='\t'):
            if d['kind'] != 'good' or d['slotName'] != '教育' or d['nation'] not in NAT: continue
            k = (d['nation'], int(d['bucket']))
            tot[k] = tot.get(k, 0.0) + float(d['payPerMonth'])
    for k, v in tot.items(): priv.setdefault(k, []).append(v)
buy = {}
for s in range(1, 11):
    acc = {}
    with open(base + f'reform_s{s}.tsv', encoding='utf-8') as f:
        for d in csv.DictReader(f, delimiter='\t'):
            if d['nation'] not in NAT: continue
            q = int(d['quarter'])
            for b, (a, z) in BK.items():
                if a <= q <= z: acc.setdefault((d['nation'], b), []).append(float(d['educationBuyPerMonth']))
    for k, v in acc.items(): buy.setdefault(k, []).append(st.mean(v))
print('nation bucket | private pay/month (sum classes) | state buy/month | state/private')
for n in NAT:
    for b in (3, 5, 7, 8):
        p = priv.get((n, b), []); u = buy.get((n, b), [])
        if not p or not u: continue
        mp, mu = st.median(p), st.median(u)
        print(f'{n:6s} b{b} | {mp:10.1f} | {mu:8.2f} | {mu / mp if mp else float("nan"):.3f}')
