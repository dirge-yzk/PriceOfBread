import csv,collections,sys
K='D:/PoB_work/keep_1044'
arm=sys.argv[1]; seeds=range(11,21) if arm=='a11' else range(1,11); d='base_ins' if arm=='base' else arm
PN={'0':'絶対','1':'専制','2':'幕府','3':'立憲','4':'共和','5':'ブ独','6':'プ独','7':'軍独','8':'無政'}
def rows(p):
    with open(p,encoding='utf-8') as f: return list(csv.DictReader(f,delimiter='\t'))
NS=['イギリス','オーストリア','フランス','ロシア','オスマン','スペイン','プロイセン']
tot=collections.Counter(); back=collections.Counter(); samem=collections.Counter(); pair=collections.defaultdict(collections.Counter); seedsN=collections.defaultdict(set)
allm=0; allsame=0
for s in seeds:
    last={}; mc=collections.Counter()
    im=rows(f'{K}/{d}/imp_s{s}.tsv')
    for r in im: mc[(r['month'],r['loser'])]+=1
    for r in im:
        L=r['loser']; key=L if L in NS else '他'
        tot[key]+=1; seedsN[key].add(s); pair[key][(PN[r['winnerPolityCode']],PN[r['loserPolityBeforeCode']])]+=1
        if L in last and last[L]==r['loserPolityBeforeCode']: back[key]+=1   # 前の矯正で置かれた政体のまま次の矯正を受けた
        if mc[(r['month'],L)]>=2: samem[key]+=1
        last[L]=r['imposedPolityCode']
print(arm,'国\t矯正\t種\t前の矯正の政体のまま再矯正\t同じ月に2回以上の月の件\t(勝者,敗者前)上位3')
for k in NS+['他']:
    print(f'{k}\t{tot[k]}\t{len(seedsN[k])}\t{back[k]}\t{samem[k]}\t{pair[k].most_common(3)}')
