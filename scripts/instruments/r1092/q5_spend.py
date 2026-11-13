# (1092) 問い 2 の律速の分け：陸の装備の予算（claimArmyPerMonth＝Σ Region.MilitaryBudget・Instruments.cs:355）のうち実際に買えた額。
# 買えた額＝四半期の陸軍の台帳の増分（nation の ledgerArmyCumulative＝StateSpent[Army]＝装備の支払い＋兵の給与・EconomyStep.cs:2536・:2692）−その四半期の給与の予算（claimSoldierPayPerMonth・支払い率 1 の国だけ）。
# 比＝買えた額÷予算。1 に近い＝予算で律速・小さい＝配給か値段の門で律速（予算は使い残し）。月と四半期の対応＝四半期 q＝月 3q〜3q+2（近似）。
# あわせて予算の伸び（四半期 q の予算÷四半期 1 の予算）＝値段の上がりの代理（量の必要は兵の数に比例＝兵の比で割る）。
# 使い方: python -X utf8 -I q5_spend.py <表のフォルダ> > q5_out.txt
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
d = sys.argv[1]; QS = [1, 2, 3, 4, 6, 8, 12, 16, 20]
NAT = ['フランス', 'オーストリア', 'プロイセン', 'ロシア', 'イギリス', 'オスマン', '清', '日本', 'アメリカ', 'スペイン']
R = collections.defaultdict(lambda: collections.defaultdict(list)); G = collections.defaultdict(lambda: collections.defaultdict(list))
for s in range(1, 11):
    led = {}; sol = {}; pr = {}
    with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); q = int(c[0])
            if q > 21: break
            led[(c[ix['nation']], q)] = float(c[ix['ledgerArmyCumulative']]); sol[(c[ix['nation']], q)] = float(c[ix['soldierK']]); pr[(c[ix['nation']], q)] = float(c[ix['payRateMeanShare']])
    army = collections.defaultdict(float); pay = collections.defaultdict(float); nm = collections.Counter()
    with open(os.path.join(d, f'fiscal_s{s}.tsv'), encoding='utf-8') as fi:
        h = fi.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fi:
            c = line.rstrip('\n').split('\t'); m = int(c[0])
            if m > 3 * 21 + 2: break
            k = (c[ix['nation']], m // 3); army[k] += float(c[ix['claimArmyPerMonth']]); pay[k] += float(c[ix['claimSoldierPayPerMonth']]); nm[k] += 1
    for n in NAT:
        for q in QS:
            k = (n, q)
            if nm[k] != 3 or (n, q - 1) not in led or k not in led or not (pr.get(k, 0) >= 0.999): continue
            bought = abs(led[k] - led[(n, q - 1)]) - pay[k]
            if army[k] > 1e-9: R[n][q].append(bought / army[k])
            b1 = (n, 1)
            if nm[b1] == 3 and army[b1] > 1e-9 and sol.get(b1, 0) > 0: G[n][q].append((army[k] / sol[k]) / (army[b1] / sol[b1]))
print('買えた額÷陸の装備の予算（10 種の中央・支払い率 1 の四半期だけ）・四半期 ' + ' '.join(map(str, QS)))
for n in NAT: print(n + '\t' + '\t'.join(f'{st.median(R[n][q]):.2f}' if R[n][q] else '-' for q in QS))
print('兵 1 千あたりの予算の伸び（四半期 1＝1）')
for n in NAT: print(n + '\t' + '\t'.join(f'{st.median(G[n][q]):.2f}' if G[n][q] else '-' for q in QS))
