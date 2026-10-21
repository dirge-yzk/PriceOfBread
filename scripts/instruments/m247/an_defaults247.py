# ★(491) 既定の表の農奴解放を Full へ（(487) で NationConfig の既定を なし→全面 へ反転した＝旧の None のままだと既定と同じ値のキーを外す処理が露・普・波の明示の None を外してしまう・反証役 mg2〔bv〕低#5）
import json,collections
d=json.load(open('D:/PoB_work/wt_def/PriceOfBread.SimCore/world.json',encoding='utf-8-sig'))
ns=d['nations']
D={'merchant':400,'artisan':800,'literacy':0.23,'clergyFrac':0.4,'intelligentsiaFrac':0.35,'peasantSubsistence':0.7,'polity':'Absolutist','education':'Religious','coercion':400,'treasury':200000,'govDebt':0,'pollTaxStage':1,'landTaxStage':1,'bureaucracy':'Spoils','institutionalDecay':0,'nationalConsciousness0':-1,'initialMassMilitancy':0,'initialTariff':0.0,'industrialization':0.0,'initialSuffrage':'None','initialCensorship':'StatePressOnly','initialCornLaws':'None','initialSerfEmancipation':'Full','initialImmigration':'Open','initialConscription':'Selective','initialMonetaryLaw':'Silver','broadMoneyToGdp':0,'initialInterestRate':0.25,'latitude':0,'longitude':0,'farmLordShare':0.5,'culturalGoodId':-1,'knownDiscoveries':[],'initialColonies':[],'rule':None,'nobility':0,'peasant':0,'worker':0,'bourgeois':0,'professional':0}
allk=['merchant','artisan','literacy','clergyFrac','intelligentsiaFrac','peasantSubsistence','polity','education','coercion','treasury','govDebt','pollTaxStage','landTaxStage','bureaucracy','institutionalDecay','nationalConsciousness0','initialMassMilitancy','initialTariff','industrialization','initialSuffrage','initialCensorship','initialCornLaws','initialSerfEmancipation','initialImmigration','initialConscription','initialMonetaryLaw','broadMoneyToGdp','initialInterestRate','latitude','longitude','farmLordShare','culturalGoodId','knownDiscoveries','initialColonies','rule','nobility','peasant','worker','bourgeois','professional']
print('field default present eqDefault modeValue modeCount effective(mode-or-default) keysAfterA keysAfterB')
for k in allk:
    dv=D[k]
    vals=[]
    for n in ns:
        vals.append(n[k] if k in n else dv)
    present=sum(1 for n in ns if k in n)
    eq=sum(1 for n in ns if k in n and n[k]==dv)
    c=collections.Counter(json.dumps(v,ensure_ascii=False) for v in vals)
    mv,mc=c.most_common(1)[0]
    keysB=present-eq   # defaults unchanged
    keysA=len(ns)-mc if mc>=0 else 0
    print(f"{k}\t{dv}\t{present}\t{eq}\t{mv[:30]}\t{mc}\t{keysB}\t{keysA}")
