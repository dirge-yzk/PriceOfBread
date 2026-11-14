# (1133) 問い3・4：外国の取り分の読み手（関税・不平等条約の旗・鉄道の運賃の代理）と持ち主の取り分の読み手（税・徴兵・StateReach の代理）を
# 国×四半期で組み直し、腕どうし（旗 1 の腕 B と旗 0 の基準・ヌル腕）と、同じ盤面での〈植民地の港を本国の港に入れない〉直し（新÷旧）を並べる。
# 使い方: python -X utf8 -I q3_readers.py <出力の場所> <ラベル>=<ins の場所> ...
# 式（50e0b5c93 のソースから・計器の列で組み直す）：
#   ForeignPortShare(w)＝Σ_港 pop×min(1,ΣForeignShares)÷Σ_港 pop（Politics.cs:1066）→ 関税の倍率 1−それ（InternationalMarket.cs:107）
#   TreatyPortInForce＝港のどれかで ΣForeignShares ≧ 0.5×ForeignRuleFromTreaty＝0.25（Politics.cs:1079）
#   ΣForeignShares の代わりに rulepower_s の nowRule2Share（Rule[2]）を使う＝旗 1 では寄せの直後に Rule[2]＝Σx（RulePowerApply）・旗 0 では Rule[2] は Σx へ遅れて寄る（近似・旗 0 の値は rule_s の portForeignShare で確かめる）
#   直し H2：H に加えて港の外国の取り分がその港の持ち主の取り分 Rule[0] を超える時だけ（AI の案・確証なし）／P：本国の港の人口加重の取り分 ≧0.25（AI の案・確証なし）
#   直し H：上の 2 つの和と判定から tier＝Colonial の州を外す（持ち主の本国の港だけ）
#   鉄道の運賃の代理：全ての州の pop×min(1,F) の平均（railRecv の州の列は縮めた生に無い＝代理）
#   持ち主の取り分の代理：pop×Rule[0] の平均（StateReach は ×AdminFill・税と徴兵の RuleDelivery は Rule0×(d+(1−d)AdminFill)+Rule1×d＝AdminFill の列が無い）・首都（国の最初の行）の Rule[0]
import sys, os, json, statistics as st
from collections import defaultdict
outd = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
os.makedirs(outd, exist_ok=True)
ROOT = 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore'
wd = json.load(open(ROOT + '/world.json', encoding='utf-8'))
port = {}
for n in wd['nations']:
    for p in n['provinces']: port.setdefault(p['city'], bool(p.get('coastal')))
for x in json.load(open(ROOT + '/landmass.json', encoding='utf-8')): port.setdefault(x['name'], bool(x.get('coastal')))
LINE = 0.25
def fnum(s):
    try: return float(s)
    except ValueError: return float('nan')
FOCUS = ['イギリス', 'ロシア', 'フランス', 'プロイセン', 'オーストリア', '清', '日本', 'オスマン', 'アメリカ', 'スペイン', 'オランダ']
res = {}
trig = {}
for lb, ins in arms:
    per = defaultdict(lambda: defaultdict(list))   # nation -> metric -> list over (seed,q)
    tg = defaultdict(lambda: defaultdict(int))     # nation -> (region,tier) -> 旗を立てた国×四半期の数
    nqn = defaultdict(int)
    for seed in range(1, 11):
        p = os.path.join(ins, f'rulepower_s{seed}.tsv')
        if not os.path.exists(p): continue
        with open(p, encoding='utf-8') as f:
            h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            iq, inn, ir, it, ip, i0, i1, i2 = ix['quarter'], ix['nation'], ix['region'], ix['tier'], ix['popK'], ix['nowRule0Share'], ix['nowRule1Share'], ix['nowRule2Share']
            cur = None; acc = None
            def flush():
                if acc is None: return
                n, a = cur[1], acc
                d = per[n]
                d['tariffScale'].append(1 - (a['pn'] / a['pd'] if a['pd'] > 0 else 0))
                d['tariffScaleH'].append(1 - (a['hn'] / a['hd'] if a['hd'] > 0 else 0))
                d['treaty'].append(1 if a['trig'] else 0)
                d['treatyH'].append(1 if a['trigH'] else 0)
                d['treatyH2'].append(1 if a['trigH2'] else 0)
                d['treatyP'].append(1 if (a['hd'] > 0 and a['hn'] / a['hd'] >= LINE) else 0)
                d['railProxy'].append(a['rn'] / a['rd'] if a['rd'] > 0 else 0)
                d['owner0'].append(a['o0'] / a['rd'] if a['rd'] > 0 else float('nan'))
                d['owner01'].append(a['o01'] / a['rd'] if a['rd'] > 0 else float('nan'))
                d['cap0'].append(a['cap0'])
                for k in a['trig']: tg[n][k] += 1
                nqn[n] += 1
            for ln in f:
                c = ln.rstrip('\n').split('\t')
                key = (int(c[iq]), c[inn])
                if key != cur:
                    flush(); cur = key
                    acc = {'pn': 0.0, 'pd': 0.0, 'hn': 0.0, 'hd': 0.0, 'trig': [], 'trigH': False, 'trigH2': False, 'rn': 0.0, 'rd': 0.0, 'o0': 0.0, 'o01': 0.0, 'cap0': fnum(c[i0])}
                pop = fnum(c[ip]); F = fnum(c[i2]); r0 = fnum(c[i0]); r1 = fnum(c[i1])
                if not (pop > 0): continue
                Fc = min(1.0, F) if F > 0 else 0.0
                col = c[it] == 'Colonial'
                acc['rn'] += pop * Fc; acc['rd'] += pop
                if r0 == r0: acc['o0'] += pop * r0
                if r0 == r0 and r1 == r1: acc['o01'] += pop * (r0 + r1)
                if port.get(c[ir], False):
                    acc['pn'] += pop * Fc; acc['pd'] += pop
                    if not col: acc['hn'] += pop * Fc; acc['hd'] += pop
                    if Fc >= LINE:
                        acc['trig'].append((c[ir], c[it]))
                        if not col: acc['trigH'] = True
                        if not col and r0 == r0 and Fc > r0: acc['trigH2'] = True
            flush()
    res[lb] = per; trig[lb] = (tg, nqn)
    print(lb, 'done', flush=True)

def m(xs):
    xs = [x for x in xs if x == x]
    return sum(xs) / len(xs) if xs else float('nan')
MET = ['treaty', 'treatyH', 'treatyH2', 'treatyP', 'tariffScale', 'tariffScaleH', 'railProxy', 'owner0', 'owner01', 'cap0']
with open(os.path.join(outd, 'q3_readers.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\tnationQuarters\t' + '\t'.join(MET) + '\n')
    for lb in res:
        nats = sorted(res[lb], key=lambda n: (n not in FOCUS, FOCUS.index(n) if n in FOCUS else 0, n))
        for n in nats:
            d = res[lb][n]
            f.write(f'{lb}\t{n}\t{len(d["treaty"])}\t' + '\t'.join(f'{m(d[k]):.4g}' for k in MET) + '\n')
        allv = defaultdict(list)
        for n in res[lb]:
            for k in MET: allv[k].extend(res[lb][n][k])
        f.write(f'{lb}\t(全ての国×四半期)\t{len(allv["treaty"])}\t' + '\t'.join(f'{m(allv[k]):.4g}' for k in MET) + '\n')
with open(os.path.join(outd, 'q3_triggers.tsv'), 'w', encoding='utf-8', newline='\n') as f:
    f.write('arm\tnation\tregion\ttier\tquartersTriggering\tnationQuarters\tshare\n')
    for lb in trig:
        tg, nqn = trig[lb]
        for n in FOCUS:
            for (r, t), k in sorted(tg[n].items(), key=lambda x: -x[1])[:8]:
                f.write(f'{lb}\t{n}\t{r}\t{t}\t{k}\t{nqn[n]}\t{k / max(1, nqn[n]):.3f}\n')
print('ok')
