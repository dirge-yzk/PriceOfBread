# M1（文字列の中の // の後ろを削る）の確かめ。組 B の実コードには // を含む文字列が 0 件のため、固定の見本で確かめる。
# 使い方: python -I m1_fixture.py <作業フォルダ>   → <作業>/m1/{before,good,naive}/F.cs を作る
import sys
import os
import re
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from strip import strip_source

FIX = '''class F {
    // 行ぜんたいの注
    string a = "http://x//y";   // 後ろの注
    string b = @"c//d
 e//f";
    string c = $"{a}//e{(a.Length > 1 ? "p//q" : "r")}:{a.Length:D2}";
    char d = '/'; char e = '"'; /* 塊 */ int g = 1 /* 途中 */ + 2;
    string h = "esc\\"//z";
    /// <summary>docs</summary>
    void M() { }
}
'''
w = sys.argv[1]
for k in ('before', 'good', 'naive'):
    os.makedirs(os.path.join(w, 'm1', k), exist_ok=True)
good, _ = strip_source(FIX)
naive = re.sub(r'(?m)[ \t]*//[^\r\n]*', '', FIX)
for k, t in (('before', FIX), ('good', good), ('naive', naive)):
    open(os.path.join(w, 'm1', k, 'F.cs'), 'wb').write(t.encode('utf-8'))
open(os.path.join(w, 'm1', 'files.txt'), 'w').write('F.cs\n')
print(good)
