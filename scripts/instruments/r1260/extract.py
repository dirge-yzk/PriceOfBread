# (1259) c128dc 設計の材料：発見の時計の紙の影の材料を生から抜く（読むだけ・エンジンの関数を呼ばない）
# 使い方: python -X utf8 -I extract.py <生のフォルダ> <種の始め> <種の終わり> <出力 .json>
# 抜く物（種ごと）：clock_s の W 行・engineer_s の T 行・disc_s の E 行・factory_s の高度労働力の供給の能力と担い手の人数（世界の和は四半期ごと・国ごとは T の四半期だけ）・pop_s の N 行（T の四半期の国の人口と識字）
import sys, json

import os
def rows(p):
    if not os.path.exists(p): return
    with open(p, 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t')
        ix = {c: i for i, c in enumerate(h)}
        for b in f:
            yield ix, b.decode('utf-8').rstrip('\r\n').split('\t')

def num(s):
    try:
        return float(s)
    except ValueError:
        return float('nan')

src, s0, s1, out = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
res = {}
for s in range(s0, s1 + 1):
    W = {}
    for ix, x in rows(f'{src}/clock_s{s}.tsv'):
        if x[ix['kind']] == 'W':
            W[int(x[ix['quarter']])] = num(x[ix['worldTotalPerMonth']])
    T = []
    for ix, x in rows(f'{src}/engineer_s{s}.tsv'):
        if x[ix['kind']] != 'T': continue
        T.append([int(x[ix['quarter']]), x[ix['nation']], x[ix['row']], int(x[ix['decade']]),
                  num(x[ix['engineerPerMonth']]), int(float(x[ix['knownFlag']])), num(x[ix['needTerm']]), num(x[ix['techTerm']])])
    D = []
    for ix, x in rows(f'{src}/disc_s{s}.tsv'):
        if x[ix['kind']] != 'E': continue
        D.append([int(x[ix['quarter']]), int(x[ix['year']]), x[ix['nation']], x[ix['row']],
                  num(x[ix['techTerm']]), num(x[ix['needTerm']]), num(x[ix['transmitTerm']])])
    SUP = {}; SPK = {}; NSUP = {}
    for ix, x in rows(f'{src}/factory_s{s}.tsv'):
        q = int(x[ix['quarter']]); v = num(x[ix['advLaborSupplyCapacityUnitsPerMonth']]); k = num(x[ix['advLaborSupplierPopK']])
        if v == v: SUP[q] = SUP.get(q, 0.0) + v
        if k == k: SPK[q] = SPK.get(q, 0.0) + k
        if q % 8 == 7:
            key = f"{q}|{x[ix['nation']]}"
            o = NSUP.setdefault(key, [0.0, 0.0])
            if v == v: o[0] += v
            if k == k: o[1] += k
    POP = {}
    for ix, x in rows(f'{src}/pop_s{s}.tsv'):
        if x[ix['scope']] != 'N': continue
        q = int(x[ix['quarter']])
        if q % 8 != 7: continue
        POP[f"{q}|{x[ix['nation']]}"] = [num(x[ix['popK']]), num(x[ix['nEduLitShare']])]
    res[s] = {'W': W, 'T': T, 'D': D, 'SUP': SUP, 'SPK': SPK, 'NSUP': NSUP, 'POP': POP}
    print(s, len(W), len(T), len(D), len(SUP), len(NSUP), len(POP), flush=True)
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(res, f, ensure_ascii=False)
