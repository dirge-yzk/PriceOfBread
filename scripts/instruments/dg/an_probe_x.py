# 流れG (320)：discprobe の断面（毎年）から、清と主要国の労働力（動力）の値段の瞬間 x／平滑 s（参照比×物価）と蒸気機関の v÷cost を種の中央で並べる（読むだけ）。
# 使い方：PYTHONUTF8=1 python an_probe_x.py <基線の接頭辞> <腕の接頭辞> [種の数]   例：…/probe_arm0 …/probe_arm1 10
import re, sys, statistics, collections
def parse(path):
    out = {}   # (year, nation) -> (x, s, ratio)
    year = None; nation = None; ratio = None
    for l in open(path, encoding='utf-8'):
        m = re.match(r'### (\d{4})年の断面', l)
        if m: year = int(m.group(1)); continue
        m = re.match(r'- (\S+?)（技術', l)
        if m and year:
            nation = m.group(1)
            r = re.search(r'蒸気機関=[^ ]*?｜[^ ]*?比([\d.eE+-]+)÷([\d.eE+-]+)', l)
            ratio = float(r.group(1)) / float(r.group(2)) if r and float(r.group(2)) > 0 else None
            if ratio is None:
                r = re.search(r'蒸気機関=.*?比([\d.eE+-]+)÷([\d.eE+-]+)', l)
                ratio = float(r.group(1)) / float(r.group(2)) if r and float(r.group(2)) > 0 else None
            continue
        m = re.search(r'労働力 ([\d.-]+|NaN)/([\d.-]+|NaN)', l)
        if m and year and nation:
            try: x = float(m.group(1)); s = float(m.group(2))
            except Exception: x = s = float('nan')
            out[(year, nation)] = (x, s, ratio); nation = None
    return out
def main():
    pa, pb = sys.argv[1], sys.argv[2]; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
    arms = {'基線': [parse(f'{pa}_s{s}.txt') for s in range(1, n + 1)], '腕': [parse(f'{pb}_s{s}.txt') for s in range(1, n + 1)]}
    nations = ['清', 'フランス', 'プロイセン', 'オーストリア', 'ロシア', 'アメリカ', 'スペイン', 'オスマン', 'ネーデルラント', '日本']
    for nm in nations:
        print(f'## {nm}：年 | 労働力の x（瞬間・参照比×物価）基線→腕 | s（平滑）基線→腕 | 蒸気機関の v÷cost 基線→腕（種の中央〔n〕）')
        for yr in (1784, 1785, 1786, 1788, 1790, 1793, 1796, 1800):
            cells = []
            for k in range(3):
                row = []
                for a in ('基線', '腕'):
                    vs = [d[(yr, nm)][k] for d in arms[a] if (yr, nm) in d and d[(yr, nm)][k] is not None and d[(yr, nm)][k] == d[(yr, nm)][k]]
                    row.append(f'{statistics.median(vs):.2f}[{len(vs)}]' if vs else '-')
                cells.append('→'.join(row))
            print(f'  {yr} | ' + ' | '.join(cells))
main()
