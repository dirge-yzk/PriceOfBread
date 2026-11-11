# (905) 合流：2 つの keepsum（tools/keepsum.py の出力）を表の名で突き合わせ、sha256 の一致・違い・片方だけを数える（読むだけ）。
#   python -X utf8 kscmp905.py <keepsum A> <keepsum B> [表の頭,...＝違ってよいと宣言した表]
import sys, collections
def load(p):
    d = {}
    for i, l in enumerate(open(p, encoding='utf-8')):
        c = l.rstrip('\n').split('\t')
        if i == 0 or len(c) < 2: continue
        d[c[0]] = c[1]
    return d
a, b = load(sys.argv[1]), load(sys.argv[2])
decl = [x for x in (sys.argv[3].split(',') if len(sys.argv) > 3 and sys.argv[3] else [])]
def head(n): return n.split('_s')[0] + '_s' if '_s' in n else n
same = [n for n in a if n in b and a[n] == b[n]]
diff = [n for n in a if n in b and a[n] != b[n]]
onlya = [n for n in a if n not in b]; onlyb = [n for n in b if n not in a]
und = [n for n in diff + onlya + onlyb if head(n) not in decl]
print(f'A {len(a)} 本・B {len(b)} 本・一致 {len(same)}・違う {len(diff)}・A だけ {len(onlya)}・B だけ {len(onlyb)}')
print('違う表の頭', dict(collections.Counter(head(n) for n in diff)))
print('A だけの頭', dict(collections.Counter(head(n) for n in onlya)), 'B だけの頭', dict(collections.Counter(head(n) for n in onlyb)))
print(f'宣言 {decl} の外の違い {len(und)}', und[:20])
