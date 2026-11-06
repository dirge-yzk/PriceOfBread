# 読むだけ：系統ごとの文書の引用ファイルと、2026-09-26 以後のコミット数
import json, sys, subprocess, collections, os
repo, feats_p, sys_p = sys.argv[1:4]
feats = json.load(open(feats_p, encoding='utf-8'))
systems = []
for l in open(sys_p, encoding='utf-8').read().splitlines()[1:]:
    sid, name, rng, sec = l.split('\t')
    rs = [tuple(map(int, r.split('-'))) for r in rng.split(',')]
    systems.append((sid, name, rs))
def sys_of(line):
    for sid, name, rs in systems:
        for a, b in rs:
            if a <= line <= b: return sid
    return None
cnt = collections.defaultdict(collections.Counter); nfeat = collections.Counter(); miss=[]
for f in feats:
    s = sys_of(f['line'])
    if s is None: miss.append(f['line']); continue
    f['system'] = s; nfeat[s] += 1
    for fn in f['files']: cnt[s][fn] += 1
json.dump(feats, open(feats_p.replace('.json','_sys.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print('unassigned', miss)
# どの系統にいくつ出るか
spread = collections.Counter()
for s in cnt:
    for fn in cnt[s]: spread[fn] += 1
print('hub files (>=8 systems):', sorted([(fn, n) for fn, n in spread.items() if n >= 8], key=lambda x: -x[1]))
