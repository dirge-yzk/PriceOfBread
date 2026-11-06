# 読むだけ：docs/ゲーム概要と実装機能.md の第3部の #### 見出しを抜き出す
import re, sys, json
src = sys.argv[1]; out = sys.argv[2]
lines = open(src, 'rb').read().decode('utf-8-sig').split('\n')
feats = []; cur = None; sec = None
for i, l in enumerate(lines, 1):
    m = re.match(r'^## (3-\d+)\.', l)
    if m: sec = m.group(1)
    if l.startswith('#### '):
        if cur: feats.append(cur)
        cur = dict(line=i, sec=sec, title=l[5:].strip(), body=[])
    elif cur is not None:
        if l.startswith('## ') or l.startswith('### '):
            feats.append(cur); cur = None
        else:
            cur['body'].append(l)
if cur: feats.append(cur)
for f in feats:
    b = '\n'.join(f['body'])
    f['files'] = sorted(set(re.findall(r'([A-Za-z][A-Za-z0-9_]*\.(?:cs|json|py|sh|axaml))', b)))
    f['vars'] = sorted(set(re.findall(r'\(([A-Z][A-Za-z0-9_]+)\)', b)))
    f['sym'] = f['title'][0]
    f['struck'] = '~~' in f['title'][:4]
    del f['body']
json.dump(feats, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(feats), sum(1 for f in feats if f['vars']), sum(1 for f in feats if f['files']))
