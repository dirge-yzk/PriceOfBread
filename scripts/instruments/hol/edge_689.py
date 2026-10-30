"""(689) hol（読むだけ・PYTHONIOENCODING=utf-8 で実行）：縁にある州（ピークの 1/5 未満＝1/10 の線の 2 倍以内）を一覧にする。読むだけ。
python edge.py <ラベル> <計器の出力ディレクトリ> [...]（ラベルとディレクトリの組を並べる）
各州：走るピーク（1 千人以上）に対する最小の比 r＝min(人口÷それまでのピーク)。r<0.2 の州を出す。
列＝種・州・格・ピーク(年・持ち主)→最小(年・持ち主)・比・持ち主の替わり（四半期・前→後）・州から持ち主の首都までの距離（ピークの時→最小の時）・
減りの内訳（ピークから最小まで：州の間の純の出・越境の純の出・入植の純の出・その他＝残り）。
型 A＝ピークから最小までに持ち主が替わり、かつ最小の時の首都までの距離が 3000km 以上（遠い飛び地の持ち主替わり）。"""
import sys, os, json, math, collections
def fl(x):
    try: return float(x)
    except: return float('nan')
PL = {}
# an_h2.py の ★(144) と同じ＝リポジトリの相対を先に読む（作業の場所の絶対パスは片付けで消える＝距離の列が全て nan になる罠）
for cand in [os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..', 'PriceOfBread.SimCore', 'places.json'), 'C:/Users/dirge/Documents/PriceOfBread/PriceOfBread.SimCore/places.json']:
    if os.path.exists(cand):
        for p in json.load(open(cand, encoding='utf-8')): PL[p['name']] = (p['lat'], p['lon'])
        break
if not PL: print('⚠ places.json が見つからない＝距離の列は nan')
def dist(a, b):
    if a not in PL or b not in PL: return float('nan')
    la1, lo1 = map(math.radians, PL[a]); la2, lo2 = map(math.radians, PL[b])
    h = math.sin((la2 - la1) / 2) ** 2 + math.cos(la1) * math.cos(la2) * math.sin((lo2 - lo1) / 2) ** 2
    return 2 * 6371.0 * math.asin(min(1.0, math.sqrt(h)))
EXEMPT = {'ヌーク', 'ナイン', 'イカルイト', 'フォートシンプソン'}
args = sys.argv[1:]
pairs = [(args[i], args[i + 1]) for i in range(0, len(args), 2)]
TOT = collections.Counter()
for label, d in pairs:
    files = sorted([f for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv')], key=lambda f: int(f[8:-4]))
    for gf in files:
        sd = int(gf[8:-4])
        R = collections.defaultdict(list)
        cap = {}
        with open(os.path.join(d, gf), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t'); H = {h: i for i, h in enumerate(head)}
            for line in fh:
                v = line.rstrip('\n').split('\t')
                q = int(v[H['quarter']]); nat = v[H['nation']]; reg = v[H['region']]
                if (q, nat) not in cap: cap[(q, nat)] = reg
                R[reg].append((q, int(v[H['year']]), nat, v[H['tier']], fl(v[H['populationK']]),
                               fl(v[H['migrationOutKPerQuarter']]) - fl(v[H['migrationInKPerQuarter']]),
                               fl(v[H['crossBorderOutKPerQuarter']]) - fl(v[H['crossBorderInKPerQuarter']]),
                               fl(v[H['settlerOutKPerQuarter']]) - fl(v[H['settlerInKPerQuarter']])))
        for reg, rows in R.items():
            pk = 0.0; pki = 0; best = (1.0, None, None)
            for i, r in enumerate(rows):
                if r[4] > pk: pk = r[4]; pki = i
                if pk >= 1.0 and r[4] / pk < best[0]: best = (r[4] / pk, pki, i)
            rr, a, b = best
            if a is None or rr >= 0.2: continue
            p0, p1 = rows[a], rows[b]
            ch = []
            for j in range(a + 1, b + 1):
                if rows[j][2] != rows[j - 1][2]: ch.append('%d %s→%s' % (rows[j][1], rows[j - 1][2], rows[j][2]))
            d0 = dist(reg, cap.get((p0[0], p0[2]), '')); d1 = dist(reg, cap.get((p1[0], p1[2]), ''))
            mig = sum(r[5] for r in rows[a + 1:b + 1]); xb = sum(r[6] for r in rows[a + 1:b + 1]); st = sum(r[7] for r in rows[a + 1:b + 1])
            D = p0[4] - p1[4]
            typA = bool(ch) and d1 >= 3000
            tag = ('A' if typA else '-') + ('・例外' if reg in EXEMPT else '') + ('・1/10' if rr < 0.1 else '')
            TOT[(label, 'A' if typA else '-', reg in EXEMPT, rr < 0.1)] += 1
            print('%s\ts%d\t%s\t%s\t%s\t%.2f(%d・%s)→%.2f(%d・%s)\t比 %.3f\t替わり[%s]\t首都まで %.0f→%.0f km\t減り %.2f＝州の間 %.2f・越境 %.2f・入植 %.2f・他 %.2f'
                  % (label, sd, reg, p1[3], tag, p0[4], p0[1], p0[2], p1[4], p1[1], p1[2], rr, '・'.join(ch), d0, d1, D, mig, xb, st, D - mig - xb - st))
print('## 集計（ラベル・型・例外の名簿・1/10 未満）')
for k, v in sorted(TOT.items()): print('##', k, v)
