# 門 1：[CallerLineNumber] を外した同じ変換のあとの SimCore.dll がバイトで一致するか。
# 使い方: python -I gate_il.py <IL の木（wt_c107hb_il）> <後の SimCore のフォルダ> [--skip-strip-after] [--before-cache <dll>]
# 前は 1cd66aae5 の木（IL の木を git checkout で戻す）。前後とも同じパスで建てる（CallerFilePath が絶対パスを持つため）。
import sys
import os
import re
import shutil
import subprocess

BASE = '1cd66aae5'
PAT = re.compile(rb'\[\s*(?:System\.Runtime\.CompilerServices\.)?CallerLineNumber(?:Attribute)?\s*\]\s*')


def run(cmd, cwd):
    r = subprocess.run(cmd, cwd=cwd, capture_output=True)
    if r.returncode != 0:
        sys.stdout.write((r.stdout + r.stderr).decode('utf-8', 'replace')[-3000:])
        raise SystemExit('失敗: ' + ' '.join(cmd))
    return r.stdout


def strip_attr(simcore):
    n = 0
    for dp, dn, fn in os.walk(simcore):
        if os.sep + 'obj' in dp or os.sep + 'bin' in dp:
            continue
        for f in fn:
            if f.endswith('.cs'):
                p = os.path.join(dp, f)
                b = open(p, 'rb').read()
                nb, k = PAT.subn(b'', b)
                if k:
                    open(p, 'wb').write(nb)
                    n += k
    return n


def build(il, out):
    art = 'D:/PoB_work/c107hb_art_il'
    run(['dotnet', 'build', 'PriceOfBread.SimCore/PriceOfBread.SimCore.csproj', '-c', 'Release', '-nologo', '-v', 'q',
         '-p:DebugType=none', '-p:DebugSymbols=false', '-p:Deterministic=true', '--artifacts-path', art], il)
    # 実体の dll（bin 側）を取る。obj の ref／refint は参照用で本体が無い（初版はこれを拾って 718336 バイトで一致と誤った）
    dll = None
    for dp, dn, fn in os.walk(art):
        d = dp.replace('\\', '/') + '/'
        if 'PriceOfBread.SimCore.dll' in fn and '/bin/' in d and '/ref' not in d:
            dll = os.path.join(dp, 'PriceOfBread.SimCore.dll')
    assert dll and os.path.getsize(dll) > 2000000, dll
    shutil.copyfile(dll, out)


def main():
    il, after = sys.argv[1], sys.argv[2]
    skip_after = '--skip-strip-after' in sys.argv
    work = 'D:/PoB_work/c107hb_work'
    before_dll = work + '/il_before.dll'
    sc = os.path.join(il, 'PriceOfBread.SimCore')
    if not os.path.exists(before_dll):
        run(['git', 'checkout', '-q', '--detach', BASE], il)
        run(['git', 'checkout', '-q', '--', '.'], il)
        print('前：属性を外した数', strip_attr(sc))
        build(il, before_dll)
        run(['git', 'checkout', '-q', '--', '.'], il)
    # 後：IL の木の .cs を後の木の .cs で上書きする
    run(['git', 'checkout', '-q', '--detach', BASE], il)
    run(['git', 'checkout', '-q', '--', '.'], il)
    for dp, dn, fn in os.walk(after):
        if os.sep + 'obj' in dp or os.sep + 'bin' in dp:
            continue
        for f in fn:
            if f.endswith('.cs'):
                rel = os.path.relpath(os.path.join(dp, f), after)
                shutil.copyfile(os.path.join(dp, f), os.path.join(sc, rel))
    k = 0 if skip_after else strip_attr(sc)
    print('後：属性を外した数', k)
    after_dll = work + '/il_after.dll'
    build(il, after_dll)
    a = open(before_dll, 'rb').read()
    b = open(after_dll, 'rb').read()
    print('前', len(a), '後', len(b), 'バイト一致' if a == b else '不一致')
    run(['git', 'checkout', '-q', '--', '.'], il)
    sys.exit(0 if a == b else 1)


main()
