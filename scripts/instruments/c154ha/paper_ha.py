# (1436) c154ha 紙の台本：今の盤面（基線の生）で、受け入れの上限を〈行き先の人口〉から〈農村の空き〉へ替えたときの一次の見込み。
# 生の region_s（arableLand・ruralPopulationK・populationK・landFactor）と migcap_s（domPlanNewKPerMonth・domCapKPerMonth）を読むだけ。エンジンと係数を読まない。
# 旧の上限 capOld＝r×pop×(F·H·L)^2（migcap の domCap）。新の上限（農村の空きだけ＝職の空きは生に無い＝下限）capNew＝r×room×(F·H)^2＝capOld×room÷(pop×L²)。
# 一次＝計画の量を固定して min(計画, 上限) を足す（帰り道〔計画の変化〕は数えない）。
import sys, collections
d = sys.argv[1].rstrip('/') + '/'
seeds = range(1, 11) if len(sys.argv) < 3 else [int(x) for x in sys.argv[2].split(',')]
W = ('アメリカ', '西部'); WATCH = [('イギリス','ロンドン'),('イギリス','マンチェスター'),('アメリカ','ニューヨーク'),('アメリカ','西部'),('アメリカ','セントポール'),('イギリス','フォートウィリアム')]
tot = collections.defaultdict(lambda: [0.0]*6)   # dec -> plan, oldReal, newReal, rowsPlan, bindOld, bindNew
watch = collections.defaultdict(lambda: [0.0]*3)
zero_room_plan = collections.Counter(); per_seed = {}
for s in seeds:
    reg = {}
    with open(d + f'region_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            try: reg[(a[ix['quarter']], a[ix['nation']], a[ix['region']])] = (float(a[ix['arableLand']]), float(a[ix['ruralPopulationK']]), float(a[ix['populationK']]), float(a[ix['landFactor']]))
            except (ValueError, IndexError): pass
    so = sn = 0.0
    with open(d + f'migcap_s{s}.tsv', encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            a = line.rstrip('\n').split('\t')
            try:
                y = int(a[ix['year']]); plan = float(a[ix['domPlanNewKPerMonth']]); cap = float(a[ix['domCapKPerMonth']])
            except (ValueError, IndexError): continue
            if not (plan > 0): continue
            k = (a[ix['quarter']], a[ix['nation']], a[ix['region']]); g = reg.get(k)
            if g is None: continue
            ar, ru, pop, L = g
            room = ar - ru if ar < 1e8 else 1e8
            room = room if room > 0 else 0.0
            capNew = cap * room / (pop * L * L) if pop > 0 and L > 0 else 0.0
            dec = y // 10 * 10; t = tot[dec]
            o = min(plan, cap); n = min(plan, capNew)
            t[0] += plan; t[1] += o; t[2] += n; t[3] += 1; t[4] += plan > cap; t[5] += plan > capNew
            so += o; sn += n
            if room == 0: zero_room_plan[dec] += plan
            if (a[ix['nation']], a[ix['region']]) in WATCH:
                wv = watch[(a[ix['nation']], a[ix['region']], y // 50 * 50)]; wv[0] += plan; wv[1] += o; wv[2] += n
    per_seed[s] = (so, sn)
print('== 十年ごと（10 種の和・KPerMonth の行の和）: 十年 計画 旧の実 新の実(農村の空きだけ) 新÷旧 計画のある行 旧が律速 新が律速 空き0の計画')
for dec in sorted(tot):
    t = tot[dec]; print(dec, round(t[0]), round(t[1]), round(t[2]), round(t[2] / t[1], 3) if t[1] > 0 else 'na', int(t[3]), int(t[4]), int(t[5]), round(zero_room_plan[dec]))
print('== 種ごと：旧の実の和 新の実の和 新÷旧')
for s, (o, n) in per_seed.items(): print(s, round(o), round(n), round(n / o, 3) if o > 0 else 'na')
print('== 見張りの州（半世紀ごと）: 国 州 半世紀 計画 旧の実 新の実')
for k in sorted(watch): v = watch[k]; print(*k, round(v[0], 1), round(v[1], 1), round(v[2], 1))
