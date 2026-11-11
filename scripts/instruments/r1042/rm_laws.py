# (1042) 法の推移：国教の法・移民の法の国×四半期の分布（B 10 種）と、アメリカ・イギリス・清の最初に段が変わった年
import collections
D = 'D:/PoB_work/keep_1035/base_ins'
dist_s = collections.Counter(); dist_i = collections.Counter(); first = collections.defaultdict(list)
for s in range(1, 11):
    prev = {}
    with open(f'{D}/monlaw_s{s}.tsv', 'rb') as f:
        h = f.readline().decode('utf-8').rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.decode('utf-8').rstrip('\r\n').split('\t'); n = x[ix['nation']]; y = int(x[ix['year']])
            sr = int(x[ix['StateReligion']]); im = int(x[ix['ImmigrationRestriction']])
            dist_s[sr] += 1; dist_i[im] += 1
            if n in ('アメリカ', 'イギリス', '清', 'ロシア', '日本'):
                if n in prev and prev[n] != (sr, im): first[(n, s)].append((y, sr, im))
                if n not in prev: first[(n, s)].append((y, sr, im))
                prev[n] = (sr, im)
T = sum(dist_s.values())
print('国教の法（0 国教・1 内心・2 信仰の自由・3 無神論）国×四半期の割合', {k: round(v / T, 4) for k, v in sorted(dist_s.items())})
print('移民の法（0 開放・1 選別・2 閉鎖）国×四半期の割合', {k: round(v / T, 4) for k, v in sorted(dist_i.items())})
for (n, s), v in sorted(first.items()):
    if s <= 3: print(n, '種', s, '（年・国教・移民）', v[:8])
