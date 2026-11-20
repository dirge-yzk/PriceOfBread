# (1272) 尾の州×種で 食品の加工の 9 財とそれ以外の加工品の産出（sellr_s の 10 年ごとの標本・量）を両腕で足す
# 引数: al の生 基準の生 al の dist 基準の dist 種の始め 種の終わり
import sys,csv,collections
NINE={'小麦粉','パン','肉','加工肉','干物','ビール','蒸留酒','チーズ','チョコレート'}
RAW={'麦','ジャガイモ','トウモロコシ','野菜','果実','畜産','漁獲','塩','農業労働力','労働力','高度労働力','木','綿花','羊毛','砂糖','葉煙草','染料','麻','生糸','毛皮','金','銀','銅鉱','鉄鉱石','石炭','鉛鉱','錫鉱','硫黄','石灰石','珪砂','粘土','原油','硝石','クロム','ゴム','馬','行政','建造物','住居','帆船','教育','医療','家事奉仕','電力'}
da={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[3],encoding='utf-8'),delimiter='\t')}
db={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[4],encoding='utf-8'),delimiter='\t')}
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
sel={k:(int(v['peakYear']),int(v['minYear'])) for k,v in da.items() if v['tierLast']=='Colonial' and k[1] not in EXC and float(v['ratio'])<0.4 and k in db and float(db[k]['ratio'])>=0.4}
for arm,dr in (('al',sys.argv[1]),('base',sys.argv[2])):
    t=collections.Counter(); g9=collections.Counter()
    for s in range(int(sys.argv[5]),int(sys.argv[6])+1):
        w={k[1]:v for k,v in sel.items() if k[0]==str(s)}
        if not w: continue
        with open(f'{dr}/sellr_s{s}.tsv',encoding='utf-8') as f:
            h=f.readline().rstrip('\n').split('\t'); ix={k:i for i,k in enumerate(h)}
            for line in f:
                c=line.rstrip('\n').split('\t'); r=c[ix['region']]
                if r not in w or not (w[r][0]<=int(c[ix['year']])<=w[r][1]): continue
                g=c[ix['good']]; q=float(c[ix['lastProduced']])
                if g in NINE: t['nine']+=q; g9[g]+=q
                elif g not in RAW: t['otherMade']+=q
                t['xNine']+=float(c[ix['prevExportAll']]) if g in NINE else 0
    print(arm,f"対 {len(sel)}｜9 財の産出 {t['nine']:.1f}（輸出 {t['xNine']:.1f}）・他の加工品 {t['otherMade']:.1f}｜9 財の内訳",' '.join(f"{g}:{v:.1f}" for g,v in g9.most_common()))
