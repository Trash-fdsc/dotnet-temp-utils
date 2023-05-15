#!/bin/bash

# Запускаем программу для парсинга файлов AIDE
# Применение
# sudo bash /Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/exec-aide.sh

gold=`pwd`

mr=/Arcs/Repos/smalls/dotnet-temp-utils/setBinExecute/build

$mr/setBinExecute $mr/../config.file /A/service/aide/report.log > /tmp/ex-aide.sh
sudo bash /tmp/ex-aide.sh

echo ----------------------------------------------------------------
echo "Всего обновлено разрешений на файлах"
cat "/tmp/ex-aide.sh" | wc -l
echo ----------------------------------------------------------------

cd "$gold"
