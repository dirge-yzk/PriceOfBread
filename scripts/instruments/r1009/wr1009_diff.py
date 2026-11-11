# (1009) c93wn：旧 wr1004_count.txt と新 wr1009_count.txt の主な量を盤面ごとに並べ、事前登録の自己検算（追記 0）を確かめる。読むだけ。
# 使い方：python -X utf8 -I wr1009_diff.py <旧 wr1004_count.txt> <新 wr1009_count.txt> <出力のフォルダ>
import sys, os, re

OLD, NEW, OUT = sys.argv[1], sys.argv[2], sys.argv[3]

def blocks(p):
    s = open(p, 'rb').read().decode('utf-8')
    out = {}
    for part in s.split('#### 盤面 ')[1:]:
        name, body = part.split('\n', 1)
        out[name.strip()] = body
    return out

def grab(body, pat, n=1, cast=float):
    m = re.search(pat, body)
    if not m: return None
    return tuple(cast(m.group(i + 1)) for i in range(n)) if n > 1 else cast(m.group(1))

METRICS = [
    ('全ての行 年あたり', r'全ての行: [^\n]*年あたり ([\d.]+)', 'rate'),
    ('国際 年あたり', r'国際（内戦以外）: [^\n]*年あたり ([\d.]+)', 'rate'),
    ('内戦 年あたり', r'内戦: [^\n]*年あたり ([\d.]+)', 'rate'),
    ('主の開戦 年あたり', r'主の開戦: [^\n]*年あたり ([\d.]+)', 'rate'),
    ('侵略の拒否 年あたり', r'侵略の拒否: [^\n]*年あたり ([\d.]+)', 'rate'),
    ('窓1815-1914 国際', r'窓 1815-1914: 国際 年あたり ([\d.]+)', 'same'),
    ('窓1783-1814 国際', r'窓 1783-1814: 国際 年あたり ([\d.]+)', 'w3175'),
    ('窓1915-末 国際', r'窓 1915-19\d\d: 国際 年あたり ([\d.]+)', 'info'),
    ('窓1915-末 内戦', r'窓 1915-19\d\d: [^\n]*内戦 ([\d.]+)', 'info'),
    ('期間 国際 中央', r'dur_intl: [^\n]*中央 ([\d.]+)', 'same'),
    ('期間 国際 帯を除く 件', r'durnc_intl: 件 (\d+)', 'info'),
    ('期間 国際 全て 件', r'dur_intl: 件 (\d+)', 'same'),
    ('期間 内戦 帯を除く 件', r'durnc_civ: 件 (\d+)', 'info'),
    ('NINE 1815-1914 中央', r'NINE: [^\n]*1815-1914 中央 ([\d.]+)', 'same'),
    ('NINE 1815-1914 中央 参戦除く', r'NINE: [^\n]*1815-1914 中央 [\d.]+（参戦を除く ([\d.]+)', 'same'),
    ('FIVE 1815-1914 中央', r'FIVE: [^\n]*1815-1914 中央 ([\d.]+)', 'same'),
    ('DYN 1815-1914 中央', r'DYN: [^\n]*1815-1914 中央 ([\d.]+)', 'same'),
    ('DYN 全期間 中央', r'DYN: 全期間 全ての行 中央 ([\d.]+)', 'same'),
    ('交戦ペア', r'交戦ペア 平均 ([\d.]+)', 'same'),
    ('再戦 main 開始→開始 中央', r'main 開始→開始: [^\n]*中央 ([\d.]+)', 'same'),
    ('侵略 件/種', r'件/種 平均 ([\d.]+)', 'same'),
    ('標的 Middle', r'標的の[^\n]*格の和: [^\n]*Middle (\d+)', 'same'),
    ('標的 Great', r'標的の[^\n]*格の和: [^\n]*Great (\d+)', 'same'),
    ('侵略者 Great', r'侵略者の[^\n]*?格の和[^:]*: [^\n]*Great (\d+)', 'same'),
]

o, n = blocks(OLD), blocks(NEW)
lines = ['量\t盤面\t旧\t新\t新÷旧\t予言\t判定']
bad = 0
for lab, pat, kind in METRICS:
    for b in ('base_ins', 'n1', 'n2', 'a11'):
        a, c = grab(o.get(b, ''), pat), grab(n.get(b, ''), pat)
        r = (c / a) if (a and c is not None) else float('nan')
        if kind == 'rate': exp = 168 / 167; ok = abs(c - a * exp) <= 0.0011   # 両側の丸め 3 桁
        elif kind == 'w3175': exp = 32 / 31.75; ok = abs(c - a * exp) <= 0.0011
        elif kind == 'same': exp, ok = 1.0, a == c
        else: exp, ok = float('nan'), True
        if not ok: bad += 1
        lines.append(f'{lab}\t{b}\t{a}\t{c}\t{r:.4f}\t{exp:.4f}\t{"一致" if ok else "外れ"}')
lines.append(f'予言の外れ {bad} 件（率は両側の丸め 3 桁のため |新−旧×予言|≦0.0011 で読む・info は予言なし）')
os.makedirs(OUT, exist_ok=True)
open(os.path.join(OUT, 'wr1009_diff.txt'), 'wb').write(('\n'.join(lines) + '\n').encode('utf-8'))
print('wrote', len(lines), 'bad', bad)
