"""(490) pop_s の読み手の前提の番人：層の番号を 8 層（10 歳刻み）のものと読む読み手を、11 層の生に当てたとき黙って誤読しない。

(478) で年齢の層の既定が作者の 11 層（0／1〜5／6〜10／11〜15／16〜20／21〜30／31〜40／41〜50／51〜60／61〜70／71 以上）になり、pop_s の層の列（旧の deathsBand{b}K・mu{b}PerYear）は
名前を変えずに意味が変わった（8 層の mu0＝0〜9 歳・11 層の mu0＝0 歳）。(490) で列の名前を層の下端の年齢にした（deathsFrom{下端}K・muFrom{下端}PerYear）。
8 層の読み手は下端 0・10・…・70 の名前を引く＝11 層の生には muFrom10PerYear が無い＝探しても無くて止まるが、muFrom0PerYear（0 歳）だけは両方にあるので、
読み手の先頭でこの関数を呼び 11 層の生なら止める。使い方＝読み手の先頭で `require_bands8_argv()`（引数に pop_s*.tsv のあるフォルダ〔または その下の ins〕があれば見出しを見る）。
"""
import glob
import os
import sys


def _header_of(path):
    with open(path, encoding='utf-8') as fh:
        return fh.readline().rstrip('\n').split('\t')


def require_bands8_header(h, who=''):
    """見出し（列名の並び）が 8 層（下端 0・10・…・70）なら何もしない。それ以外は SystemExit。"""
    cols = set(h)
    want = {f'muFrom{10 * b}PerYear' for b in range(8)} | {f'deathsFrom{10 * b}K' for b in range(8)}
    if not want <= cols or 'muFrom71PerYear' in cols or 'muFrom1PerYear' in cols:
        raise SystemExit(f'{who}この読み手は年齢の層が 8 層（10 歳刻み）の pop_s だけを前提にする。見出しが 8 層の列（muFrom0PerYear〜muFrom70PerYear）でない'
                         '（11 層の生か (490) より前の名前の生）。8 層の生は POB_AGE_BANDS=8 で回す。11 層で読むなら読み手を層の下端の年齢で書き直す。')


def require_bands8_argv(argv=None):
    """引数のフォルダ（またはその下の ins）の最初の pop_s*.tsv の見出しを調べる。見つからなければ何もしない。"""
    for a in (sys.argv[1:] if argv is None else argv):
        for d in (a, os.path.join(a, 'ins')):
            if os.path.isdir(d):
                fs = sorted(glob.glob(os.path.join(d, 'pop_s*.tsv')))
                if fs:
                    require_bands8_header(_header_of(fs[0]), who=os.path.basename(fs[0]) + '：')
                    return
