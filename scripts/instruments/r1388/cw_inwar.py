# (1388) 内戦の始まりが、親の国の国と国の戦争の最中か直後（8 四半期以内）かを数える（wars_s だけ・四半期の粒度 q）。
#   python -X utf8 -I cw_inwar.py <タグ> <生> <種の起点> [<タグ> <生> <種の起点> ...]
import sys, os, csv
a = sys.argv[1:]
for i in range(0, len(a), 3):
    tag, d, sb = a[i], a[i + 1], int(a[i + 2])
    tot = inw = after = 0; per = []
    for s in range(sb, sb + 10):
        W = list(csv.DictReader(open(os.path.join(d, f'wars_s{s}.tsv'), encoding='utf-8'), delimiter='\t'))
        ext = [(w['sideA'], w['sideB'], float(w['q']), float(w['q']) + float(w['months']) / 3) for w in W if w['civilWarFlag'] != '1']
        n = 0
        for w in W:
            if w['civilWarFlag'] != '1': continue
            tot += 1; n += 1
            p = w['sideA']; y = float(w['q'])
            inn = any(p in (A, B) and s0 <= y <= e0 for A, B, s0, e0 in ext)
            aft = (not inn) and any(p in (A, B) and e0 < y <= e0 + 8 for A, B, s0, e0 in ext)
            inw += inn; after += aft
        per.append(n)
    print(f'{tag}\t内戦 {tot}\t親が交戦中に始まる {inw}\t戦後 2 年以内 {after}\tそれ以外 {tot - inw - after}\t種ごと {per}')
