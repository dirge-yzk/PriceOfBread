# 読むだけ：系統の持ち主のファイル（拠点のファイル＝8系統以上に出る物を除き、最も多く引く系統へ）と 2026-09-26 以後のコミット数
import json, sys, subprocess, collections
repo, feats_p, head = sys.argv[1:4]
feats = json.load(open(feats_p, encoding='utf-8'))
tracked = subprocess.run(['git','-C',repo,'ls-tree','-r','--name-only',head], capture_output=True, text=True).stdout.split('\n')
byname = collections.defaultdict(list)
for p in tracked:
    if p: byname[p.split('/')[-1]].append(p)
cnt = collections.defaultdict(collections.Counter)
for f in feats:
    for fn in f['files']: cnt[fn][f['system']] += 1
own = collections.defaultdict(list); hubs=[]; gone=[]
for fn, c in cnt.items():
    if len(c) >= 8: hubs.append(fn); continue
    paths = [p for p in byname.get(fn, []) if p.startswith('PriceOfBread.SimCore/') or p.startswith('PriceOfBread.Runner/') or p.endswith('.json')]
    if not paths: gone.append(fn); continue
    top = max(c.values()); winners = sorted(s for s, v in c.items() if v == top)
    for s in winners: own[s].extend(paths)
out = []
for s in sorted(set(f['system'] for f in feats)):
    files = sorted(set(own.get(s, [])))
    if files:
        r = subprocess.run(['git','-C',repo,'log','--since=2026-09-26','--format=%h',head,'--']+files, capture_output=True, text=True).stdout.split()
        n = len(r)
    else: n = 0
    out.append((s, n, files))
for s, n, files in out:
    print(f"{s}\t{n}\t{' '.join(p.split('/')[-1] for p in files)}")
print('hubs', sorted(hubs)); print('gone', sorted(gone))
json.dump({s: files for s, n, files in out}, open(feats_p.replace('feats_sys.json','owners.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
