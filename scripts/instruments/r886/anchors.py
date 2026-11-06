# 読むだけ：文書の係数名・識別子を 11db605f の SimCore で引き、宣言の行（file:line:錨）を取る
import json, sys, subprocess, re, collections
repo, feats_p, head, outp = sys.argv[1:5]
feats = json.load(open(feats_p, encoding='utf-8'))
files = [p for p in subprocess.run(['git','-C',repo,'ls-tree','-r','--name-only',head,'PriceOfBread.SimCore'],capture_output=True,text=True).stdout.split('\n') if p.endswith('.cs')]
src = {}
for p in files:
    b = subprocess.run(['git','-C',repo,'show',f'{head}:{p}'],capture_output=True).stdout.decode('utf-8-sig', errors='replace')
    src[p] = b.split('\n')
decl_cache = {}
def decl(name):
    if name in decl_cache: return decl_cache[name]
    pat = re.compile(r'\b' + re.escape(name) + r'\b')
    best = None
    order = ['PriceOfBread.SimCore/Coefficients.cs'] + [p for p in files if p != 'PriceOfBread.SimCore/Coefficients.cs']
    for p in order:
        for i, l in enumerate(src[p], 1):
            s = l.split('//')[0]
            if not pat.search(s): continue
            # 宣言：型名の後に名前、または名前の後に = か { か ( （メソッド）で、行頭に修飾子
            if re.search(r'\b(public|internal|private|protected|const|static|readonly)\b[^=;(]*\b' + re.escape(name) + r'\b\s*(=|\{|\(|;|=>)', s):
                best = (p, i); break
        if best: break
    decl_cache[name] = best
    return best
rows = []
for f in feats:
    anchors = []; missing = []
    for v in f['vars']:
        d = decl(v)
        if d: anchors.append(f'{d[0].split("/")[-1]}:{d[1]}:{v}')
        else: missing.append(v)
    f['anchors'] = anchors; f['vars_missing'] = missing
json.dump(feats, open(outp, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
n_any = sum(1 for f in feats if f['anchors']); n_miss = sum(len(f['vars_missing']) for f in feats); n_vars = sum(len(f['vars']) for f in feats)
print('features', len(feats), 'with_anchor', n_any, 'vars', n_vars, 'vars_missing', n_miss)
