# (1272) 植民地格の全ての州で 9 財と他の加工品の産出÷人口（sellr_s の標本の年）を両腕で
# 引数: al の生 基準の生 al の cls 基準の cls 種の始め 種の終わり
import sys,csv,collections
sys.path.insert(0,'')
NINE={'小麦粉','パン','肉','加工肉','干物','ビール','蒸留酒','チーズ','チョコレート'}
RAW={'麦','ジャガイモ','トウモロコシ','野菜','果実','畜産','漁獲','塩','農業労働力','労働力','高度労働力','木','綿花','羊毛','砂糖','葉煙草','染料','麻','生糸','毛皮','金','銀','銅鉱','鉄鉱石','石炭','鉛鉱','錫鉱','硫黄','石灰石','珪砂','粘土','原油','硝石','クロム','ゴム','馬','行政','建造物','住居','帆船','教育','医療','家事奉仕','電力'}
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
for arm,dr,cp in (('al',sys.argv[1],sys.argv[3]),('base',sys.argv[2],sys.argv[4])):
    col={}
    for r in csv.DictReader(open(cp,encoding='utf-8'),delimiter='\t'):
        if r['tier']=='Colonial' and r['region'] not in EXC: col[(r['seed'],int(r['quarter']),r['region'])]=(float(r['popK']),float(r['fwK']))
    t=collections.defaultdict(lambda:[0.0,0.0,0.0,0.0])
    seen=set()
    for s in range(int(sys.argv[5]),int(sys.argv[6])+1):
        with open(f'{dr}/sellr_s{s}.tsv',encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for line in f:
                c=line.rstrip('\n').split('\t'); k=(str(s),int(c[ix['q']]),c[ix['region']])
                if k not in col: continue
                y=int(c[ix['year']]); g=c[ix['good']]; q=float(c[ix['lastProduced']])
                if k not in seen: seen.add(k); t[y][2]+=col[k][0]; t[y][3]+=col[k][1]
                if g in NINE: t[y][0]+=q
                elif g not in RAW: t[y][1]+=q
    print(arm,'植民地格：年 9財÷人口 / 他の加工品÷人口 / 工場労働者の割合｜',' '.join(f"{y}:{v[0]/v[2]:.3f}/{v[1]/v[2]:.3f}/{v[3]/v[2]:.3f}" for y,v in sorted(t.items()) if v[2]>0))
