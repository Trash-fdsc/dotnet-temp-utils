#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/playv.sh /usr/local/bin/imgw

# Запускаем программу от имени пользователя 'play' с оставшимися параметрами
sudo -u play /usr/bin/celluloid "$@"

