# 読むだけ：syscount.py の感度の版＝拠点のファイル（8 系統以上に出る物）も、最も多く名指す系統へ入れて 2026-09-26 以後のコミットを数える（同数は全ての系統へ）
# 使い方: python -X utf8 syscount_sens.py <repo> <feats_sys.json> <head>   （syscount_sens.txt はこの台本と同じ式をその場の命令で出した物）
import json, subprocess, collections, sys
repo, featsp, head = sys.argv[1:4]
fe = json.load(open(featsp, encoding='utf-8'))
tracked = subprocess.run(['git', '-C', repo, 'ls-tree', '-r', '--name-only', head], capture_output=True, text=True).stdout.split('\n')
byname = collections.defaultdict(list)
for p in tracked:
    if p: byname[p.split('/')[-1]].append(p)
cnt = collections.defaultdict(collections.Counter)
for f in fe:
    for fn in f['files']: cnt[fn][f['system']] += 1
own = collections.defaultdict(list)
for fn, c in cnt.items():
    paths = [p for p in byname.get(fn, []) if p.startswith('PriceOfBread.SimCore/') or p.startswith('PriceOfBread.Runner/') or p.endswith('.json')]
    if not paths: continue
    top = max(c.values())
    for s, v in c.items():
        if v == top: own[s].extend(paths)
print('感度：拠点のファイルも最も多く引く系統へ入れた（同数は全てへ）')
for s in sorted(set(f['system'] for f in fe)):
    files = sorted(set(own.get(s, [])))
    n = len(subprocess.run(['git', '-C', repo, 'log', '--since=2026-09-26', '--format=%h', head, '--'] + files, capture_output=True, text=True).stdout.split()) if files else 0
    print(s, n, ' '.join(p.split('/')[-1] for p in files))
