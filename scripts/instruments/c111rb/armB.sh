export TEMP=D:/PoB_work/c111rb_work/tmp TMP=D:/PoB_work/c111rb_work/tmp POB_WORK=D:/PoB_work/c111rb_work
cd /d/PoB_work/wt_c111rb
bash scripts/instruments/batch.sh prep c111rbB commit:5e432585b plain || exit 1
INS=1 POB_INSTRUMENTS_NATIONS="清,ロシア,オスマン,フランス,イギリス,プロイセン,オーストリア,日本,アメリカ" bash scripts/instruments/batch.sh run c111rbB 10
