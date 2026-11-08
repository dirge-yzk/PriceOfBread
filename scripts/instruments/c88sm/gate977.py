# (977) c88sm 門：SimCore の差が注だけ（各ハンクで −の行と＋の行の最初の // より前の部分が空白を除いて同じ）。
#   使い方：python -I -X utf8 gate977.py <基準のコミット> <リポジトリ>   終了コード 0＝OK
import sys, subprocess, re
base, repo = sys.argv[1], sys.argv[2]
out = subprocess.run(['git', '-C', repo, 'diff', '-U0', base, '--', 'PriceOfBread.SimCore'], capture_output=True).stdout.decode('utf-8')
hunks, cur, files = [], None, {}
fn = ''
for ln in out.split('\n'):
    if ln.startswith('+++ '): fn = ln[6:]
    elif ln.startswith('@@'):
        cur = {'f': fn, 'h': ln, 'm': [], 'p': []}; hunks.append(cur)
    elif cur is not None and ln.startswith('-') and not ln.startswith('---'): cur['m'].append(ln[1:])
    elif cur is not None and ln.startswith('+') and not ln.startswith('+++'): cur['p'].append(ln[1:])
def code(s):
    i = s.find('//')
    return re.sub(r'\s+', '', s if i < 0 else s[:i])
bad = 0; nm = npl = 0
for h in hunks:
    cm = [c for c in map(code, h['m'])]; cp = [c for c in map(code, h['p'])]
    nm += len(cm); npl += len(cp)
    if [c for c in cm if c] != [c for c in cp if c]:
        bad += 1; print('✗', h['f'], h['h'])
print(f'ハンク {len(hunks)}・−の行 {nm}・＋の行 {npl}・コードの部分が違うハンク {bad}')
print('GATEOK' if bad == 0 and hunks else 'GATEFAIL')
sys.exit(0 if bad == 0 and hunks else 1)
