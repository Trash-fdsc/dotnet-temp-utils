#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/playvmpv.sh /usr/local/bin/playvmpv

# Запускаем программу от имени пользователя 'play' с оставшимися параметрами
sudo -u plaympv /usr/bin/mpv "$@"

