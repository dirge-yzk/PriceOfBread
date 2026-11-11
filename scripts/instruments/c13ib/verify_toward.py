# (812) c13ib 機械的な検算：Toward・OverSteps の群のコミットの全ての置き換えが、決まった書き換えの規則に合っているかを
# 旧の行と新の行の対で確かめる（読むだけ）。使い方：python verify_toward.py <リポジトリ> <旧> <新>
# 規則：旧 `X += K * (T - X);` / `X += (T - X) * K;` / `X = <式の中の X + K * (T - X)>` が
#       新 `X = PoliticalQuantities.Toward(X, T, K);` に一致する。外れた対を全て出す。
import re, subprocess, sys

repo, a, b = sys.argv[1:4]
files = subprocess.run(['git', '-C', repo, 'diff', '--name-only', a, b, '--', 'PriceOfBread.SimCore'], capture_output=True).stdout.decode().split()
bad = ok = 0
for f in files:
    d = subprocess.run(['git', '-C', repo, 'diff', '-U0', a, b, '--', f], capture_output=True).stdout.decode('utf-8', 'replace').split('\n')
    olds, news = [], []
    for l in d:
        if l.startswith('---') or l.startswith('+++'):
            continue
        if l.startswith('-'):
            olds.append(l[1:].rstrip('\r'))
        elif l.startswith('+'):
            news.append(l[1:].rstrip('\r'))
    n_to = sum(1 for l in news if 'PoliticalQuantities.Toward(' in l or 'Calendar.OverSteps(' in l)
    if n_to == 0:
        continue
    # 旧と新を行の順に対にする（-U0 の差分は置き換えが 1 行対 1 行）
    pairs = list(zip(olds, news))
    for o, n in pairs:
        if 'Toward(' not in n and 'OverSteps(' not in n:
            continue
        m = re.search(r'PoliticalQuantities\.Toward\(([^,]+), (.+), ([^,]+?)\)(?=[;)\s]|$)', n)
        if m:
            x, t, k = m.group(1), m.group(2), m.group(3)
            # 旧の中に x・k・t（括弧を外した形も）が現れ、`+=` か `x +` の形で使われていること
            t0 = re.escape(t)
            good = (re.search(re.escape(x) + r'\s*\+=\s*' + re.escape(k) + r'\s*\*\s*\(\(?' + t0 + r'\)?\s*-\s*' + re.escape(x) + r'\)', o)
                    or re.search(re.escape(x) + r'\s*\+=\s*\(' + t0 + r'\s*-\s*' + re.escape(x) + r'\)\s*\*\s*' + re.escape(k) + r'\s*;', o)
                    or re.search(re.escape(x) + r'\s*\+\s*' + re.escape(k) + r'\s*\*\s*\(\(?' + t0 + r'\)?\s*-\s*' + re.escape(x) + r'\)', o))
            if good:
                ok += 1
            else:
                bad += 1
                print('外れ', f, '\n  旧', o.strip()[:200], '\n  新', n.strip()[:200])
        elif 'OverSteps(' in n:
            ok += 1 if 'Math.Pow(1.0 - ' in o else 0
print('一致', ok, '外れ', bad)
sys.exit(1 if bad else 0)
