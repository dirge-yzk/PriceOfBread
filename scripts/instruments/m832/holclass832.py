# (832) 空洞化 1/10 未満の件ごとの振り分け（読むだけ・★(813) の規則）：腕の an_h2 -v4 の出力の〈1/10〉の行を、基線の同じ種・同じ州が an_h2 -v4 で 1/4 未満（か 1/10 未満）に出ていたかで〈元からの弱さ〉と〈線が原因〉に分ける。
#   python -X utf8 -I holclass826.py <腕の an_h2 -v4 の出力> <基線の an_h2 -v4 の出力> [<基線 2>...]
#   例外の名簿（規則 A2）の行は件数の外＝表に〈例外〉と書いて振り分けない。
import sys, re
pat = re.compile(r'^\s*\[([^\]・]+)・(1/10|1/4)(・例外の名簿)?\]\s+s(\d+)\s+(\S+?)（')
def cases(path):
    out = []
    for line in open(path, encoding='utf-8', errors='replace'):
        m = pat.match(line)
        if m: out.append((m.group(2), m.group(3) is not None, int(m.group(4)), m.group(5), m.group(1), line.strip()[:160]))
    return out
arm = cases(sys.argv[1])
base = set()
for p in sys.argv[2:]:
    for lvl, ex, s, reg, cause, _ in cases(p): base.add((s, reg))
n = 0
for lvl, ex, s, reg, cause, txt in arm:
    if lvl != '1/10': continue
    n += 1
    if ex: k = '例外の名簿（件数の外）'
    elif (s, reg) in base: k = '元からの弱さ（基線の同じ種・同じ州が 1/4 未満）'
    else: k = '線が原因（基線の同じ種・同じ州は 1/4 未満に無い）'
    print(f'{k}\t{txt}')
print(f'1/10 の行 {n}（例外の名簿を含む）')
