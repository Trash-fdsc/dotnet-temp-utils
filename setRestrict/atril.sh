#!/bin/bash
# sudo ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/atril.sh /usr/local/bin/pdf

# Запускаем программу от имени пользователя 'atril' с оставшимися параметрами
sudo -u atril /usr/bin/atril "$@"
