# (1272) al で比が 0.4 未満・基準で 0.4 以上の州×種（植民地格）の窓の中の 階級ごとの食べ物の充足と人数（2 年ごと）・飢えの死の式の見積もり
# 引数: al の cls 基準の cls al の dist 基準の dist
import sys,csv,collections
FL,EX,KX=0.8,0.9,4.0
da={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[3],encoding='utf-8'),delimiter='\t')}
db={(r['seed'],r['region']):r for r in csv.DictReader(open(sys.argv[4],encoding='utf-8'),delimiter='\t')}
EXC={'イカルイト','チャーチル','ナイン','ヌーク','バロー','フォートシンプソン','フォートユーコン'}
sel={k:(int(v['peakYear']),int(v['minYear'])) for k,v in da.items() if v['tierLast']=='Colonial' and k[1] not in EXC and float(v['ratio'])<0.4 and k in db and float(db[k]['ratio'])>=0.4}
def F(x):
    try: return float(x)
    except: return float('nan')
for arm,p in (('al',sys.argv[1]),('base',sys.argv[2])):
    t=collections.defaultdict(float)
    for r in csv.DictReader(open(p,encoding='utf-8'),delimiter='\t'):
        k=(r['seed'],r['region'])
        if k not in sel or not (sel[k][0]<=int(r['year'])<=sel[k][1]): continue
        pop,fw,pe=F(r['popK']),F(r['fwK']),F(r['peK'])
        t['pop']+=pop; t['fw']+=fw; t['pe']+=pe
        for g,n,ff in (('fw',fw,F(r['ffFw'])),('pe',pe,F(r['ffPe']))):
            if ff==ff and n>0:
                t[g+'ffw']+=n*ff; t[g+'n']+=n
                t[g+'fam']+=EX*((FL-ff)/FL)**KX*n if ff<FL else 0
                t[g+'below']+=n if ff<FL else 0
    print(arm,f"対 {len(sel)}・窓の州×2年の人口の和 {t['pop']:.0f}｜工場労働者の割合 {t['fw']/t['pop']:.3f}・農民 {t['pe']/t['pop']:.3f}｜充足 工 {t['fwffw']/t['fwn']:.3f}・農 {t['peffw']/t['pen']:.3f}｜充足 0.8 未満の人の割合 工 {t['fwbelow']/t['fwn']:.3f}・農 {t['pebelow']/t['pen']:.3f}｜飢えの死の式の見積もり（年・千人あたり）工 {1000*t['fwfam']/t['fwn']:.2f}・農 {1000*t['pefam']/t['pen']:.2f}")
