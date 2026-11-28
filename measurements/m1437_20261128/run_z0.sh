#!/usr/bin/env bash
# (1437) 旗 RailProcureNational=0 の計器つき 10 種（main 04a162e34・表を絞る）
export POB_WORK=D:/PoB_work/r1437_work
export POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ"
export POB_INSTRUMENTS_TABLES="trrail_s,sellr_s,trroute_s,trnation_s,region_s,railspend_s,stproc_s,tradeskip_s,port_s"
cd C:/Users/dirge/Documents/PriceOfBread
INS=1 bash scripts/instruments/batch.sh run r1437z0 10
