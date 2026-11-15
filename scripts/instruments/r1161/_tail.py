anc = {}
for l in open(ANC, encoding='utf-8'):
    if '\t' not in l: continue
    rid, rest = l.rstrip('\n').split('\t', 1)
    parts = []
    for a in rest.split(' ; '):
        if '=OK' in a: parts.append(a.split('=OK')[0])
        elif '=NG→' in a:
            src, dst = a.split('=NG→'); parts.append(dst if dst != 'None' else src + '（引けない）')
        elif '=錨なし' in a: parts.append(a.split('=錨なし')[0])
    anc[rid] = ';'.join(p.strip() for p in parts)
out = [HEAD]
miss = []
for l in open(ROWS, encoding='utf-8'):
    c = [x.strip() for x in l.rstrip('\n').split('\t', 1)[1].split('|')]
    rid = c[1]
    if rid not in V: miss.append(rid); continue
    t, j, why, rest, act = V[rid]
    a = AOVR.get(rid, anc.get(rid, c[5]))
    why = why + '。' + F if any(k in t for k in ('頻度',)) else why
    out.append('\t'.join([rid, c[2], c[3], c[4], a, c[6], t, COMMIT, j, why, rest, act]))
open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(out) + '\n')
print(f'行 {len(out)-1}・判定なし {miss}')
from collections import Counter
print(' '.join(f'{k}:{v}' for k, v in Counter(V[r][1] for r in V).most_common()))
print(' '.join(f'{k}:{v}' for k, v in Counter(V[r][0] for r in V).most_common()))
