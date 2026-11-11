# (833) 恒等の門（出力の突き合わせ版）：gdp_jump833.py --rows の出力と gdp_jump797.py の出力を、名前ごとに比べる。
#   使い方: python -I -X utf8 gatef.py <新の出力> <旧の出力>
import ast, re, sys
def parse(p):
    d = {}; cur = None
    for l in open(p, encoding='utf-8'):
        l = l.rstrip('\n')
        if l.startswith('#') or not l.strip(): continue
        m = re.match(r'\s+(\S+)\s+(\(.*\))$', l)
        if m: d[cur]['rows'].setdefault(m.group(1), []).append(ast.literal_eval(m.group(2))[:6]); continue
        cur = l.split('\t')[0]
        d[cur] = dict(n=(int(re.search(r'100 倍以上 (\d+)', l).group(1)), int(re.search(r'1e10 倍以上 (\d+)', l).group(1)), int(re.search(r'非有限 (\d+)', l).group(1))), rows={}, head=l)
    return d
N, O = parse(sys.argv[1]), parse(sys.argv[2]); allok = True
for k in O:
    n, o = N[k], O[k]
    ok_n = n['n'] == o['n']
    f_n, f_o = sorted(n['rows'].get('建国の直後', [])), sorted(o['rows'].get('建国の直後', []))
    t, x, xo = n['rows'].get('分母の痩せ', []), n['rows'].get('それ以外', []), o['rows'].get('それ以外', [])
    ok_f = f_n == f_o; ok_o = sorted(t + x) == sorted(xo); ok = ok_n and ok_f and ok_o; allok &= ok
    print(f'{k}\t{"一致" if ok else "不一致"}\t(合計,1e10,非有限) {n["n"]}{"=" if ok_n else "≠"}{o["n"]}\t建国の直後 {len(f_n)}{"=" if ok_f else "≠"}{len(f_o)}\t痩せ {len(t)}＋それ以外 {len(x)} {"=" if ok_o else "≠"} 旧のそれ以外 {len(xo)}')
print('門:', '緑（全て一致）' if allok else '赤')
