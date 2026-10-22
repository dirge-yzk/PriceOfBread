# (493) gd：費用中立の換算の計算機。使い方：python neutral.py <ソーダ灰の既定の木の量=2.0> [旧の goods.json]
#   参照価格の Python 版（rp.py＝Catalog.ComputeRefPrices の写し・C# の RefPrices との一致は番人 SulfuricAcidSodaAshTests が測る）で、旧の goods.json から
#   新しいレシピの量（旧の化学薬品の価値 1.16 を保つ換算）と参照価格の動きを出す。量を直すときはここの式を直して再計算する。
import json, sys, copy
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rp as R

# (493) gd：財の表の費用中立の換算。旧の goods.json を読み、新しいレシピを当てて参照価格を比べる。
# 旧の goods.json（8c8f987a）：git show 8c8f987a:goods.json > /d/PoB_work/gd_493/goods_old_8c8f987a.json
G0 = R.load(sys.argv[2] if len(sys.argv) > 2 else 'D:/PoB_work/gd_493/goods_old_8c8f987a.json')
ID = {g['name']: g['id'] for g in G0}
old = R.refprices(G0)

# 新しい財 = ソーダ灰（末尾）。名前 化学薬品 -> 硫酸（番号は旧のまま）
G = copy.deepcopy(G0)
ID['硫酸'] = ID['化学薬品']   # 酸の側が旧の化学薬品の番号を引き継ぐ
G[ID['硫酸']]['name'] = '硫酸'
ID['ソーダ灰'] = len(G0)   # 末尾に足す
G.append(dict(id=ID['ソーダ灰'], name='ソーダ灰', tier='Intermediate', baseNeed=0, elasticity=0.4, productivity=1.8, inputs=[], isColonial=False,
              nonTradeable=False, grainChain=False, nutrition=-1, wearRatePerMonth=0.03, use='Consumed'))

def setin(name, lst):
    G[ID[name]]['inputs'] = [dict(goodId=ID[n], qtyPerOutput=q) for n, q in lst]

# 参照価格の錨
setin('硫酸', [('硫黄', 0.8), ('硝石', 0.1), ('石炭', 0.6)])
ASH_WOOD = float(sys.argv[1]) if len(sys.argv) > 1 else 2.0
setin('ソーダ灰', [('木', ASH_WOOD)])
rp1 = R.refprices(G)
ACID = rp1[ID['硫酸']]; ASH = rp1[ID['ソーダ灰']]
print('硫酸', round(ACID, 4), 'ソーダ灰(草木灰)', round(ASH, 4))
OLD_CHEM = old[ID['化学薬品']]
RGO = old[ID['石炭']]   # 採取の財の参照価格（どれも同じ）
ELEC = old[ID['電力']]; MACH = old[ID['機械']]; FLOUR = old[ID['小麦粉']]; LUMBER = old[ID['製材']]

def r4(x): return round(x, 4)
def ash_of(chem_q): return r4(chem_q * OLD_CHEM / ASH)   # 化学薬品の価値をそのまま灰の量へ換算

out = {}
# 紙：両方
out['紙'] = [('木', 1.404), ('硫酸', 0.05), ('ソーダ灰', r4(0.05 * OLD_CHEM / ASH))]
out['石鹸'] = [('パーム油', 0.5), ('ソーダ灰', ash_of(0.3))]
out['医薬品'] = [('硫酸', 0.25), ('ソーダ灰', r4(0.25 * OLD_CHEM / ASH)), ('果実', 0.3)]
out['キニーネ'] = [('金鶏樹皮', 0.5), ('硫酸', 0.3)]
bone = 0.45
out['肥料'] = [('硫酸', r4((0.514 - bone * RGO) / ACID)), ('畜産', bone)]
out['ダイナマイト'] = [('硫酸', 0.4), ('硝石', 0.1)]
out['化学兵器'] = [('塩', 0.7), ('電力', r4((0.6 * OLD_CHEM - 0.7 * RGO) / ELEC)), ('硝石', 0.2)]
out['冷蔵庫'] = [('機械', 0.430), ('石炭', r4(0.3 * OLD_CHEM / RGO))]
out['ガラス'] = None  # 下で
glass_ash = 0.15
rem = (1.3 * RGO - glass_ash * ASH) / RGO   # 珪砂＋石灰石の質量
out['ガラス'] = [('珪砂', r4(0.7)), ('石灰石', r4(rem - 0.7)), ('ソーダ灰', glass_ash)]
out['火薬'] = [('硝石', 0.75), ('硫黄', 0.1), ('木', 0.15)]
out['加工肉'] = [('肉', 0.6), ('小麦粉', r4(0.3 - 0.05 * RGO / FLOUR)), ('塩', 0.05)]
out['内燃機関'] = [('鋼', 1.0), ('機械', r4(0.537 + 0.2 * RGO / MACH))]
out['セメント'] = [('石灰石', 0.8), ('粘土', 0.2), ('石炭', 0.4)]
for k, v in out.items(): setin(k, v)

alts = {}
alts['甜菜糖(砂糖)'] = [('野菜', 2.2), ('石灰石', r4(0.15 * OLD_CHEM / RGO))]
alts['獣脂石鹸(石鹸)'] = [('畜産', 1.1), ('ソーダ灰', ash_of(0.3))]
alts['ハーバーボッシュ(肥料)'] = [('硫酸', 0.15), ('電力', 0.25)]
alts['グアノ肥料(肥料)'] = [('グアノ', 0.6), ('硫酸', 0.1)]
alts['合成染料(染料)'] = [('硫酸', 0.35), ('ソーダ灰', r4((0.7 * OLD_CHEM - 0.35 * ACID - 0.2 * RGO) / ASH)), ('石炭', 0.2)]
alts['無煙火薬(火薬)'] = [('硫酸', 0.3), ('硝石', 0.2), ('綿花', 0.05)]
alts['人絹(絹織物)'] = [('木', r4(0.4 * LUMBER / RGO)), ('硫酸', 0.25), ('ソーダ灰', r4(0.25 * OLD_CHEM / ASH))]
alts['合成ゴム(ゴム)'] = [('石炭', 0.3), ('石灰石', 0.3), ('電力', r4((0.5 * OLD_CHEM - 0.3 * RGO) / ELEC))]
alts['魚の缶詰(缶詰)'] = [('漁獲', 0.55), ('鉄', 0.3), ('錫鉱', 0.05), ('塩', 0.2)]
alts['黄鉄鉱法(硫酸)'] = [('銅鉱', 1.4), ('石炭', 0.4)]
alts['ルブラン法(ソーダ灰)'] = [('塩', 0.5), ('硫酸', 0.25), ('石灰石', 0.25), ('石炭', 0.3)]
alts['ソルベー法(ソーダ灰)'] = [('塩', 0.5), ('石灰石', 0.4), ('石炭', 0.25)]
alts['電解(ソーダ灰)'] = [('塩', 0.5), ('電力', 0.3)]

new = R.refprices(G)
print('--- 既定のレシピ（goods.json の行） ---')
for k, v in out.items(): print(k, v)
print('--- 並存のレシピ ---')
OH = 0.06 + 0.25
for k, v in alts.items():
    adv = R.adv_per_output(k.split('(')[1][:-1])
    cost = OH + adv * 0.8 + sum(q * new[ID[n]] for n, q in v)
    print(k, v, '単位費用', round(cost, 4))
print('--- 参照価格の動き（旧→新・動いた財だけ） ---')
mx = 0
for g in G0:
    d = new[g['id']] - old[g['id']]
    mx = max(mx, abs(d))
    if abs(d) > 1e-6: print(g['id'], g['name'], round(old[g['id']], 5), round(new[g['id']], 5), f'{d:+.5f}')
print('最大の動き', mx)
print('旧の化学薬品', round(old[ID['硫酸']], 4), '→ 硫酸', round(new[ID['硫酸']], 4), ' ソーダ灰', round(new[ID['ソーダ灰']], 4))
# 旧 world: 代替の単位費用（旧）
print('旧 黄鉄鉱法 単位費用', round(OH + 0.05*0.8 + 0.6*RGO + 0.8*RGO + 0.4*RGO, 4))
