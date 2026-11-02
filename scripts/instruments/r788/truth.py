# r788：プローブの表 r788_s*.tsv（講和 1 回ごと）から、同じ月・同じ敗者の複数の講和を 連鎖の根（同じ開戦の連鎖）／独立 に分ける。
# 使い方: python -I truth.py <ins フォルダ> [種の起点=1] [種の数=10]
import sys, os, csv, collections
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
UK = 'イギリス'
def F(x):
    try: return float(x)
    except: return float('nan')
tot = collections.Counter(); amt = collections.Counter(); ev = []
uk = collections.Counter(); ukamt = collections.Counter()
dupe = 0
cede_home = collections.Counter()
for s in range(s0, s0 + n):
    p = os.path.join(d, f'r788_s{s}.tsv')
    rows = list(csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'))
    hits = [r for r in rows if r['hit'] == '1']
    seen = set()
    for r in hits:
        if r['warIdx'] in seen: dupe += 1
        seen.add(r['warIdx'])
        sc = F(r['score']); aw = sc > 0
        r['loser'] = r['B'] if aw else r['A']; r['winner'] = r['A'] if aw else r['B']
        r['lcol'] = (r['bCol'] if aw else r['aCol']) == '1'
        pre = 'b' if aw else 'a'
        r['homeLost'] = (int(r[pre + 'Core0']) + int(r[pre + 'Int0'])) - (int(r[pre + 'Core1']) + int(r[pre + 'Int1']))
        r['colLost'] = int(r[pre + 'Col0']) - int(r[pre + 'Col1'])
        r['single'] = 0.2 * F(r['loserGdpPerMonth']) * F(r['loserR'])   # 1 回分（sa＝1・会議の削りなし・条約港なし）の支払い
        r['seed'] = s
    g = collections.defaultdict(list)
    for r in hits: g[(r['month'], r['loser'])].append(r)
    for (m, lo), rs in g.items():
        k = len(rs)
        roots = collections.Counter(r['rootIdx'] for r in rs)
        repsum = sum(F(r['rep']) for r in rs); basesum = sum(F(r['repBase']) for r in rs)
        single = max(r['single'] for r in rs)
        ev.append(dict(seed=s, month=int(m), year=rs[0]['year'], loser=lo, k=k, nroots=len(roots), maxroot=max(roots.values()),
                       col=any(r['lcol'] for r in rs), rep=repsum, base=basesum, single=single,
                       home=sum(r['homeLost'] for r in rs), colc=sum(r['colLost'] for r in rs),
                       winners='/'.join(r['winner'] + ('*' if int(r['depth']) > 0 else '') for r in rs)))
        for r in rs:
            # 区分：単独／同じ月で同じ根（連鎖）／同じ月で別の根（独立）
            if k == 1: c = 'single'
            elif roots[r['rootIdx']] >= 2: c = 'sameRoot'
            else: c = 'otherRoot'
            tot[c] += 1; amt[c] += F(r['rep'])
            if lo == UK: uk[c] += 1; ukamt[c] += F(r['rep'])
            cede_home[c] += r['homeLost']
print(f'# {d} 種 {s0}〜{s0+n-1}・同じ戦争の添字の重複 {dupe}')
T = sum(tot.values()); A = sum(amt.values())
print('区分\t講和\t割合\t賠償の額\t額の割合\t本国格の州の割譲')
for c in ['single', 'sameRoot', 'otherRoot']:
    print(f'{c}\t{tot[c]}\t{tot[c]/T:.3f}\t{amt[c]:.0f}\t{amt[c]/A if A else 0:.3f}\t{cede_home[c]}')
TU = sum(uk.values()); AU = sum(ukamt.values())
print('英\t' + '\t'.join(f'{c}={uk[c]}({ukamt[c]/AU if AU else 0:.2f})' for c in ['single', 'sameRoot', 'otherRoot']))
multi = [e for e in ev if e['k'] >= 2]
print(f'同じ月・同じ敗者の事象：全 {len(ev)}・複数 {len(multi)}・うち敗者が崩壊 {sum(e["col"] for e in multi)}・k の分布 {sorted(collections.Counter(e["k"] for e in multi).items())}')
print(f'複数の事象の根の数の分布 {sorted(collections.Counter(e["nroots"] for e in multi).items())}')
import statistics as st
ratios = [e['rep'] / e['single'] for e in multi if e['single'] > 0]
if ratios: print(f'複数の事象の Σ賠償÷1 回分：中央 {st.median(ratios):.2f}・最大 {max(ratios):.2f}・>1.5 の割合 {sum(x>1.5 for x in ratios)/len(ratios):.2f}')
r1 = [e['rep'] / e['single'] for e in ev if e['k'] == 1 and e['single'] > 0]
if r1: print(f'単独の講和の 賠償÷1 回分：中央 {st.median(r1):.2f}')
print('本国格の割譲：単独の事象の平均 %.2f・複数の事象の平均 %.2f' % (st.mean([e['home'] for e in ev if e['k']==1] or [0]), st.mean([e['home'] for e in multi] or [0])))
out = os.environ.get('R788_EVENTS')
if out:
    with open(out, 'w', encoding='utf-8') as f:
        f.write('seed\tmonth\tyear\tloser\tk\tnroots\tmaxroot\tcollapse\trepSum\trepBaseSum\tsingle\thomeLost\tcolLost\twinners\n')
        for e in ev: f.write('\t'.join(str(e[k]) for k in ['seed','month','year','loser','k','nroots','maxroot','col','rep','base','single','home','colc','winners']) + '\n')
