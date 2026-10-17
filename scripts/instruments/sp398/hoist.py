p = 'D:/PoB_work/wt_sp/PriceOfBread.SimCore/EconomyStep.cs'
t = open(p, 'rb').read().decode('utf-8')
a = t.index('            // 買い手価格の楔5項＝唯一定義 BuyerWedge')
b = t.index('            // アヘンは中毒度を1人あたり必要量に上乗せ')
c_marker = '            if (!needless) consumedGoods++;'
c = t.index(c_marker)
blk = t[a:b]
rest = t[b:c]
# --- pull out staple / foodNonStaple lines from blk
lines = blk.split('\n')
out_blk = []
staple_line = None; fns_line = None
for ln in lines:
    if ln.startswith('            bool staple = good.Tier == GoodTier.Staple;'):
        staple_line = ln; continue
    if ln.startswith('            bool foodNonStaple = '):
        fns_line = ln; continue
    out_blk.append(ln)
assert staple_line and fns_line
blk2 = '\n'.join(out_blk)
def sub(s, old, new):
    assert s.count(old) == 1, old
    return s.replace(old, new)
blk2 = sub(blk2, 'var (stateMonop, distM, monRent, guildRent, insure) = BuyerWedge(', '(stateMonop, distM, monRent, guildRent, insure) = BuyerWedge(')
blk2 = sub(blk2, 'double buyerPrice = usePrice', 'buyerPrice = usePrice')
blk2 = sub(blk2, '            double priceFactor;\n', '')
# indent block by 4 and wrap
ind = '\n'.join(('    ' + l if l.strip() else l) for l in blk2.rstrip('\n').split('\n'))
guard = ('            // ★必要の無い財は楔・価格弾力の冪を求めない（計算量・恒等）：下で使うのは POP のループ（needless なら回さない）と後段の POP のループ（同）だけ＝needless の財では値が読まれない。\n'
         '            double stateMonop = 0, distM = 0, monRent = 0, guildRent = 0, insure = 0, buyerPrice = 0, priceFactor = 0;\n'
         '            if (!needless)\n            {\n' + ind + '\n            }\n')
# find end of needless definition statement (the line after '&& !(isService...' ending ');')
nl_start = rest.index('            bool needless = ')
nl_end = rest.index(';\n', rest.index('&& !(isService[gi]', nl_start)) + 2
new_rest = rest[:nl_end] + guard + rest[nl_end:]
new_pre = staple_line + '\n' + fns_line + '\n'
t2 = t[:a] + new_pre + new_rest + t[c:]
open(p, 'wb').write(t2.encode('utf-8'))
print('ok')
