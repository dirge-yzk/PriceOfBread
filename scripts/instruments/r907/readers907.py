# (907) c78lg 読むだけ：名ごとの読み手の数を木 <commit> の全域（SimCore・App・Runner・Tests・XAML・scripts・docs 以外）で数える。
#  注の中だけに出る行（名が // の後にしか無い行）と、宣言の行（Coefficients.cs の public の行・メソッドの定義の行）は除く。
#  出力：名 \t コードの読み手の数 \t 試験の読み手の数 \t 計器の数 \t scripts の数 \t 先頭の座標（コード）\t 先頭の座標（試験）
# 使い方: python -X utf8 readers907.py <repo> <commit> <名> [<名> ...]
import sys, subprocess, re
repo, commit = sys.argv[1:3]
names = sys.argv[3:]
PATHS = ['PriceOfBread.SimCore', 'PriceOfBread.App', 'PriceOfBread.Runner', 'PriceOfBread.Tests', 'scripts']
for nm in names:
    r = subprocess.run(['git', '-C', repo, '-c', 'core.quotepath=false', 'grep', '-n', '-w', nm, commit, '--'] + PATHS, capture_output=True).stdout.decode('utf-8', 'replace')
    code, test, ins, scr = [], [], [], []
    for l in r.split('\n'):
        if not l: continue
        p = l.split(':', 3)
        if len(p) < 4: continue
        path, ln, t = p[1].lstrip('"'), p[2], p[3]
        k = t.find('//')
        head = t if k < 0 else t[:k]
        if not re.search(r'(?<![A-Za-z0-9_])' + re.escape(nm) + r'(?![A-Za-z0-9_])', head):
            continue   # 注の中だけ
        if path.endswith('Coefficients.cs') and re.search(r'public[^=]*\b' + re.escape(nm) + r'\b\s*(=|\{)', head):
            continue   # 宣言
        if re.search(r'(private|public|internal|static)[^=;(]*\b' + re.escape(nm) + r'\s*\(', head) and not head.rstrip().endswith(';'):
            continue   # メソッドの定義
        if re.search(r'(private|public|internal)[^=;(]*\b' + re.escape(nm) + r'\s*(=>|=[^=]|;|\{)', head) and 'new(' not in head.split(nm)[0]:
            continue   # 欄・プロパティの宣言
        f = path.split('/')[-1]
        loc = f'{f}:{ln}'
        if path.startswith('scripts'): scr.append(loc)
        elif path.startswith('PriceOfBread.Tests'): test.append(loc)
        elif f.startswith('Instruments'): ins.append(loc)
        else: code.append(loc)
    print('\t'.join([nm, str(len(code)), str(len(test)), str(len(ins)), str(len(scr)), '・'.join(code[:8]), '・'.join(test[:4])]))
