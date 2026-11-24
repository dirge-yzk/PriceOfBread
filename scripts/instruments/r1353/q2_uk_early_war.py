# 問い2・6：開幕の 60 四半期（1783〜1797）で、英が関わった戦争の数（英が先に宣した main の数）・英の戦争の月の和・厭戦の最大・
#          厭戦が 0.9 を超えた最初の四半期・政体の最初の変化（q<70）を腕ごとに数える（読むだけ・種は basename から）
import sys, os, csv, re, statistics
NAT = 'イギリス'; QMAX = 60
print("腕\t種\t英の戦争の数\t英が宣したmain\t最初の戦争のq\t戦争の月の和\t厭戦の最大\t厭戦>0.9の最初のq\t政体の変化(q<70)")
agg = {}
for spec in sys.argv[1:]:
    name, d = spec.split('=', 1)
    seeds = sorted({int(m.group(1)) for f in os.listdir(d) for m in [re.match(r'wars_s(\d+)\.tsv$', f)] if m})
    for s in seeds:
        with open(os.path.join(d, f"wars_s{s}.tsv"), encoding='utf-8') as fh:
            W = [r for r in csv.DictReader(fh, delimiter='\t') if (r['sideA'] == NAT or r['sideB'] == NAT) and int(r['q']) < QMAX and r['civilWarFlag'] == '0']
        n = len(W); mains = sum(1 for r in W if r['sideA'] == NAT and r['entryCode'] == 'main')
        first = min((int(r['q']) for r in W), default=None); months = sum(int(r['months']) for r in W)
        wemax = 0; we09 = None; pol0 = None; polchg = None
        with open(os.path.join(d, f"scalars_s{s}.tsv"), encoding='utf-8') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                if r['nation'] != NAT: continue
                q = int(r['quarter'])
                if q >= 70: break
                we = float(r['warExhaustion'])
                if q < QMAX: wemax = max(wemax, we)
                if we09 is None and we > 0.9: we09 = q
                if pol0 is None: pol0 = r['polityCode']
                elif polchg is None and r['polityCode'] != pol0: polchg = q
        print(f"{name}\t{s}\t{n}\t{mains}\t{first}\t{months}\t{wemax:.2f}\t{we09}\t{polchg}")
        a = agg.setdefault(name, {'n': [], 'mains': [], 'first': [], 'months': [], 'we09': 0, 'pol': 0})
        a['n'].append(n); a['mains'].append(mains); a['first'].append(first if first is not None else QMAX); a['months'].append(months)
        a['we09'] += we09 is not None and we09 < QMAX; a['pol'] += polchg is not None
print("\n# 腕ごと：戦争の数の中央・main の中央・最初の戦争の q の中央・月の和の中央・厭戦>0.9（q<60）の種・政体の変化（q<70）の種")
for k, a in agg.items():
    print(f"{k}\t{statistics.median(a['n'])}\t{statistics.median(a['mains'])}\t{statistics.median(a['first'])}\t{statistics.median(a['months'])}\t{a['we09']}/{len(a['n'])}\t{a['pol']}/{len(a['n'])}")
