# (1253) 追加の抜き出し（読むだけ）：factory_s の世界の高度労働力の供給の能力（四半期ごとの和）・pop_s の N 行（国の人口と識字）
# 使い方: python -X utf8 -I extract2.py <生のフォルダ> <出力 .json>
import sys, json

def rows(p):
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

src, out = sys.argv[1], sys.argv[2]
res = {}
for s in range(1, 11):
    SUP = {}
    for ix, x in rows(f'{src}/factory_s{s}.tsv'):
        q = int(x[ix['quarter']]); v = num(x[ix['advLaborSupplyCapacityUnitsPerMonth']])
        if v == v: SUP[q] = SUP.get(q, 0.0) + v
    POP = {}
    for ix, x in rows(f'{src}/pop_s{s}.tsv'):
        if x[ix['scope']] != 'N': continue
        q = int(x[ix['quarter']])
        if q % 8 != 7 and q % 8 != 0: continue
        POP[f"{q}|{x[ix['nation']]}"] = [num(x[ix['popK']]), num(x[ix['nEduLitShare']])]
    res[s] = {'SUP': SUP, 'POP': POP}
    print(s, len(SUP), len(POP), flush=True)
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(res, f, ensure_ascii=False)
