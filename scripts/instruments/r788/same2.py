# r788：プローブの腕の計器の表が基線 keep_784/base_ins と全行バイト一致かを確かめる（r788_s* は除く）
import sys, os, hashlib
a, b = sys.argv[1], sys.argv[2]
fa = set(f for f in os.listdir(a) if f.endswith('.tsv') and not f.startswith('r788_'))
fb = set(f for f in os.listdir(b) if f.endswith('.tsv'))
h = lambda p: hashlib.sha256(open(p, 'rb').read()).hexdigest()
same = [f for f in sorted(fa & fb) if h(os.path.join(a, f)) == h(os.path.join(b, f))]
diff = [f for f in sorted(fa & fb) if f not in same]
print(f'一致 {len(same)}・不一致 {len(diff)} {diff[:20]}・腕だけ {sorted(fa-fb)[:10]}・基線だけ {sorted(fb-fa)[:10]}')
