"""(210) 読み手（読むだけ）＝国の群ごとの 1914 年の導出政体・宗主・exact（腕ごと・種ごと）。
使い方：PYTHONUTF8=1 python an_grp210.py <d210 フォルダ> <種の起点> <種の数> <国,国,...> [ラベル]"""
import sys, os, collections
PN = ['絶対', '専制', '幕府', '立憲', '共和', 'ブル独', 'プロ独', '軍政', '無政府']
d, s0, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]); grp = sys.argv[4].split(','); lab = sys.argv[5] if len(sys.argv) > 5 else ''
tot = collections.Counter(); ex = 0; n = 0; suz = collections.Counter()
for s in range(s0, s0 + ns):
    best = {}
    with open(os.path.join(d, f'd{s}.txt'), encoding='utf-8') as f:
        for line in f:
            if not line.startswith('N|523|'): continue
            t = line.rstrip('\n').split('|')
            if t[3] not in grp: continue
            if t[3] not in best or float(t[16]) > float(best[t[3]][16]): best[t[3]] = t
    row = []
    for nat in grp:
        if nat not in best: row.append(f'{nat}=—'); continue
        t = best[nat]; n += 1; ex += int(t[7]); tot[PN[int(t[5])]] += 1; suz[t[6] or '（独立）'] += 1
        row.append(f'{nat[:3]}={PN[int(t[5])]}{"✓" if t[7]=="1" else "×"}/{t[6] or "独立"}・議{float(t[10]):.2f}・正{float(t[9]):.2f}')
    print(f'seed{s}: ' + '  '.join(row))
print(f'{lab}: exact {ex}/{n}・導出 ' + '・'.join(f'{k} {v}' for k, v in tot.most_common()) + '・宗主 ' + '・'.join(f'{k} {v}' for k, v in suz.most_common(8)))
