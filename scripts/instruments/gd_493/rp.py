import json, sys
# 参照価格の写し（Catalog.ComputeRefPrices の Python 版・(493) gd の費用中立の換算用）。goods.json の投入から反復緩和で求める。
# 役務の錨：労働力・兵力・農業労働力 0.5／高度労働力 0.8／他の役務 0.6（Catalog.ServiceRefPrice）。工場 = FactoryVa + 0.5*労働の錨 + 技師*0.8 + Σ投入*参照価格。
FACTORY_VA = 0.06; RGO_VA = 0.34; LABOR_PER = 0.5; FARM_LABOR_PER = 0.4

def load(path):
    return json.load(open(path, encoding='utf-8-sig'))

def adv_per_output(name):
    if name == '電力': return 0.15
    if name in ('機械','蒸気機関','電化製品','汽船','軍艦','機関車','送電網','機関銃','装甲艦','戦車','弩級戦艦','内燃機関'): return 0.10
    if name in ('鉄','鋼','銅','鉛','セメント','ガラス','化学薬品','硫酸','ソーダ灰','火薬'): return 0.05
    return 0.02

def service_ref(g):
    if g['name'] in ('労働力','兵力','農業労働力'): return 0.5
    if g['name'] == '高度労働力': return 0.8
    return 0.6

def refprices(G):
    rp = [0.0]*len(G)
    for g in G:
        if g['tier'] == 'Service': rp[g['id']] = service_ref(g)
        elif not g['inputs']: rp[g['id']] = RGO_VA + FARM_LABOR_PER*0.5
        else: rp[g['id']] = FACTORY_VA
    for _ in range(60):
        for g in G:
            if g['tier'] == 'Service' or not g['inputs']: continue
            c = FACTORY_VA + LABOR_PER*0.5 + adv_per_output(g['name'])*0.8
            for i in g['inputs']: c += i['qtyPerOutput']*rp[i['goodId']]
            rp[g['id']] = c
    return rp

if __name__ == '__main__':
    G = load(sys.argv[1])
    rp = refprices(G)
    for g in G: print(g['id'], g['name'], f"{rp[g['id']]:.4f}")
