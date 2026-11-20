# (1253) c127dc 設計の材料：発見の時計の紙の影の材料を生から抜く（読むだけ）
# 使い方: python -X utf8 -I extract.py <生のフォルダ> <出力 .json>
# 抜く物（種ごと）: clock_s の W 行（8 四半期ごとの世界の技師の和）・nation_s の世界人口（同じ四半期）
#   engineer_s の T 行（8 四半期ごと・国×行の A・既知の印・必要性）・disc_s の E 行（発見の事象・技術力・必要性・伝播）
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
    W = {}; WE = []
    for ix, x in rows(f'{src}/clock_s{s}.tsv'):
        if x[ix['kind']] == 'W':
            W[int(x[ix['quarter']])] = num(x[ix['worldTotalPerMonth']])
    P = {}; G = {}
    for ix, x in rows(f'{src}/nation_s{s}.tsv'):
        q = int(x[ix['quarter']])
        P[q] = P.get(q, 0.0) + num(x[ix['totalPopulationK']])
        g = num(x[ix['nominalGdpPerYear']])
        if g == g: G[q] = G.get(q, 0.0) + g
    T = []; EE = []
    for ix, x in rows(f'{src}/engineer_s{s}.tsv'):
        k = x[ix['kind']]
        rec = [int(x[ix['quarter']]), x[ix['nation']], x[ix['row']], int(x[ix['decade']]),
               num(x[ix['engineerPerMonth']]), int(float(x[ix['knownFlag']])), num(x[ix['needTerm']]), num(x[ix['techTerm']])]
        (T if k == 'T' else EE).append(rec)
    D = []
    for ix, x in rows(f'{src}/disc_s{s}.tsv'):
        if x[ix['kind']] != 'E': continue
        D.append([int(x[ix['quarter']]), int(x[ix['year']]), x[ix['nation']], x[ix['row']],
                  num(x[ix['techTerm']]), num(x[ix['needTerm']]), num(x[ix['transmitTerm']])])
    res[s] = {'W': W, 'P': P, 'G': G, 'T': T, 'EE': EE, 'D': D}
    print(s, len(W), len(P), len(T), len(EE), len(D), flush=True)
with open(out, 'w', encoding='utf-8', newline='\n') as f:
    json.dump(res, f, ensure_ascii=False)
