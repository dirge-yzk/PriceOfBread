import csv, sys, collections, statistics as st, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
csv.field_size_limit(1<<30)
def rd(p):
    with open(p, encoding='utf-8') as f: return list(csv.DictReader(f, delimiter='\t'))
def n(x):
    try: return float(x)
    except: return float('nan')
for c in ('C0','C1'):
    L = rd('D:/PoB_work/lw_459/out_%s_1/ins/labor_s1.tsv' % c)
    en = [r for r in L if int(n(r['lawCode']))>0]
    bd = [r for r in en if n(r['enactedBindShare'])>0.5]
    print(c, '施行された国×四半期', len(en), '床が過半の州を縛った行', len(bd), '国', sorted(set(r['nation'] for r in bd))[:12])
    if bd:
        print('  縛った行の plain 失業の中央値 %.3f・値段÷mc の中央値 %.3f（施行行全体 失業 %.3f・値段÷mc %.3f）' % (st.median(n(r['plainUnemploymentShare']) for r in bd), st.median(n(r['plainPriceOverMcIndex']) for r in bd), st.median(n(r['plainUnemploymentShare']) for r in en), st.median(n(r['plainPriceOverMcIndex']) for r in en)))
        # law code mix of bind rows
        print('  縛った行の段', collections.Counter(int(n(r['lawCode'])) for r in bd))
    # 主要国の段の到達
    for nm in ('イギリス','フランス','アメリカ','ロシア','清','日本','プロイセン','オスマン','オーストリア'):
        rows=[r for r in L if r['nation']==nm]
        if not rows: continue
        first={}
        for r in rows:
            k=int(n(r['lawCode']))
            for lv in (1,2,3):
                if k>=lv and lv not in first: first[lv]=int(r['year'])
        print('  ', nm, first)
# 政治の混同：fm_s の pushBudgetFactor を C0 と m457 s1 で比べる
def push(p):
    d=collections.defaultdict(list)
    for r in rd(p):
        d[int(r['year'])//25*25].append(n(r['pushBudgetFactor'])) if 'pushBudgetFactor' in r else None
    return d
import os
for p in ('D:/PoB_work/keep_457/m457_ins/fm_s1.tsv','D:/PoB_work/lw_459/out_C0_1/ins/fm_s1.tsv'):
    h=open(p,encoding='utf-8').readline().strip().split('\t')
    print(p.split('/')[-4], 'pushBudgetFactor' in h)
