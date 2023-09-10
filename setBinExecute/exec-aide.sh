#!/bin/bash

# Запускаем программу для парсинга файлов AIDE
# Применение
# sudo bash /Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/exec-aide.sh

gold=`pwd`

mr=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute

$mr/build/setBinExecute $mr/config.file.black $mr/config/first.black > /tmp/ex-aide-black.sh
sudo bash /tmp/ex-aide-black.sh

# $mr/build/setBinExecute $mr/config.file > /tmp/ex-aide.sh
$mr/build/setBinExecute $mr/config.file $mr/config/updates.up > /tmp/ex-aide.sh
# $mr/build/setBinExecute $mr/config.file /A/service/aide/report.log > /tmp/ex-aide.sh
sudo bash /tmp/ex-aide.sh


echo ----------------------------------------------------------------
echo "Всего обновлено разрешений на файлах"
cat "/tmp/ex-aide.sh" | wc -l
echo ----------------------------------------------------------------

cd "$gold"
