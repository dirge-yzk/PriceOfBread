# (833) 両側の門：小さな盤面 FIX の行の区分を、事前登録 [5] の (a)〜(e) の期待と比べる（期待は盤面を作った意図から書いた＝台本の出力から写していない）
#   使い方: python -I -X utf8 fixgate.py <gdp_jump833 --rows の出力>...
import ast, re, sys
EXP = {'a安定200倍': 'それ以外', 'b痩せ': '分母の痩せ', 'c建国3': '建国の直後', 'd建国5': 'それ以外',
       'e比9.9': 'それ以外', 'f比10': '分母の痩せ', 'g比0.1': '分母の痩せ', 'h非有限1e10': 'それ以外',
       'i建国4': '建国の直後', 'j痩せ無限': '分母の痩せ', 'k人口非有限': 'それ以外', 'l人口0と0': 'それ以外'}  # (839) 端の 4 系列
for p in sys.argv[1:]:
    got = {}; cur = None; head = ''
    for l in open(p, encoding='utf-8'):
        if l.startswith('FIX\t'): cur = 'FIX'; head = l; continue
        if not l.startswith(' '): cur = None; continue
        m = re.match(r'\s+(\S+)\s+(\(.*\))$', l.rstrip('\n'))
        if cur and m: got[re.match(r"\(\d+, '([^']*)'", m.group(2)).group(1)] = m.group(1)   # (839) inf・nan を含む組でも名を取る（literal_eval をやめた）
    bad = [f'{k}:{EXP[k]}→{got.get(k, "無し")}' for k in EXP if got.get(k) != EXP[k]]
    e_ok = re.search(r'	1e10 倍以上 1	', head) is not None and re.search(r'	非有限 1	', head) is not None   # (839) 完全一致（部分文字列だと 10〜19 件でも緑）
    print(f'{p}\t{"緑" if not bad and e_ok else "赤"}\t{" ".join(bad) if bad else "-"}\t(e) {"=" if e_ok else "≠"}')
