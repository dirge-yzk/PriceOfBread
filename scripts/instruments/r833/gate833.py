# (833) 恒等の門：gdp_jump833.py と gdp_jump797.py を同じ生に当て、合計・1e10・非有限・建国の直後（件数と行）・〈痩せ＋それ以外＝旧のそれ以外〉（行の集合）を比べる。
#   使い方: python -I -X utf8 gate833.py <新しい台本> <旧の台本> <名前>=<ins フォルダ> ...
import ast, re, subprocess, sys
new, old = sys.argv[1], sys.argv[2]
def run(script, arg, extra):
    out = subprocess.run([sys.executable, '-I', '-X', 'utf8', script] + extra + [arg], capture_output=True, text=True, encoding='utf-8').stdout
    lines = [l for l in out.splitlines() if not l.startswith('#')]
    head = lines[0]
    nums = dict(j100=int(re.search(r'100 倍以上 (\d+)', head).group(1)), j1e10=int(re.search(r'1e10 倍以上 (\d+)', head).group(1)), nonf=int(re.search(r'非有限 (\d+)', head).group(1)))
    rows = {}
    for l in lines[1:]:
        m = re.match(r'\s+(\S+)\s+(\(.*\))$', l)
        if m: rows.setdefault(m.group(1), []).append(ast.literal_eval(m.group(2))[:6])
    return head, nums, rows
allok = True
for arg in sys.argv[3:]:
    name = arg.split('=', 1)[0]
    hn, nn, rn = run(new, arg, ['--rows']); ho, no, ro = run(old, arg, [])
    ok_n = nn == no
    ok_f = sorted(rn.get('建国の直後', [])) == sorted(ro.get('建国の直後', []))
    ok_o = sorted(rn.get('分母の痩せ', []) + rn.get('それ以外', [])) == sorted(ro.get('それ以外', []))
    ok = ok_n and ok_f and ok_o; allok &= ok
    print(f'{name}\t{"一致" if ok else "不一致"}\t合計・1e10・非有限 {"=" if ok_n else "≠"} {nn} / {no}\t建国の直後 {"=" if ok_f else "≠"} {len(rn.get("建国の直後", []))}/{len(ro.get("建国の直後", []))}\t痩せ＋それ以外＝旧のそれ以外 {"=" if ok_o else "≠"} {len(rn.get("分母の痩せ", []))}+{len(rn.get("それ以外", []))}/{len(ro.get("それ以外", []))}')
print('門:', '緑（全て一致）' if allok else '赤')
