#!/usr/bin/env bash
# (245) 史実比較の桁（NG）の読み手 11 本を標準の計器の出力から回す（読むだけ）。注入は要らない。
#   bash ng.sh <計器の出力ディレクトリ> <タグ> <読み手の出力フォルダ> [基準の指定（tab:<表>:<タグ> か rd:<フォルダ>:<タグ>）]
# 計器の出力＝`POB_INSTRUMENTS=<ディレクトリ> bash measure.sh <出力> selfplay-batch 10`（SimCore/Instruments/Instruments.cs・既定は出さない）が書く
#   nation_s*.tsv・stock_s*.tsv・wars_s*.tsv・casualty_s*.tsv（表と列は scripts/instruments/instr.py の先頭）。`POB_INSTRUMENTS_NATIONS` は要らない（月ごとの財政の行を出さない）。
# 基準を渡すと ngset192.py が NG の集合の差（新しい・外れた・前から）を印字する。
export PYTHONUTF8=1
SP="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
I=${1:?計器の出力ディレクトリ}; T=${2:?タグ}; R=${3:?読み手の出力フォルダ}; mkdir -p "$R"
python $SP/m154/an_navy10.py $I > $R/navy_$T.txt 2>&1
python $SP/m160/an_rail10.py $I $T > $R/rail_$T.txt 2>&1
python $SP/gate/an_gate.py $I $T > $R/gate_$T.txt 2>&1
python $SP/m167/an_steam.py $R/gate_$T.txt > $R/steam_$T.txt 2>&1
python $SP/m182/an_fin182.py $I $T > $R/fin_$T.txt 2>&1
python $SP/m182/an_hist182.py $I $T > $R/hist171_$T.txt 2>&1
python $SP/sol/an_hist.py $I $T > $R/hist_$T.txt 2>&1
python $SP/wpn/an_sold.py $I > $R/sold_$T.txt 2>&1
python $SP/m182/an_wardead182.py $I > $R/wardead_$T.txt 2>&1
python $SP/m182/an_mil182.py $T=$I > $R/mil180_$T.txt 2>&1
python $SP/m186/an_trade186.py t $I $T > $R/trade_$T.txt 2>&1
python $SP/m192/an_warlaw192.py $I > $R/warlaw_$T.txt 2>&1
python $SP/m192/an_lawmatch192.py $T=$R/mil180_$T.txt > $R/lawmatch_$T.txt 2>&1   # (248) rd.sh から移した（mil180 と同じフォルダ・タグで読む）
for f in navy rail steam fin hist171 hist sold wardead mil180 trade warlaw lawmatch; do echo "${f}_$T.txt $(wc -l < $R/${f}_$T.txt) $(grep -c -i 'Traceback\|Error' $R/${f}_$T.txt)"; done
if [ -n "${4:-}" ]; then python $SP/m192/ngset192.py "$4" rd:$R:$T; fi
