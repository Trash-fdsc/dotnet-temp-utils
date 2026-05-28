#!/bin/bash
# ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/playvvlc.sh /usr/local/bin/playvvlc

# Запускаем программу от имени пользователя 'play' с оставшимися параметрами
sudo -u playvlc /usr/bin/vlc "$@"

